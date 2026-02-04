using AppView.Models.Service.VNPay;
using DATNN.Models;
using DATNN.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using System.Diagnostics;
using System.Security.Claims;

namespace DATNN.Areas.Admin.Controllers
{

    [Area("Admin")]
    [Authorize(Roles = "admin")]
    public class DanhSachDonHangController : Controller
    {
        private readonly MyDbContext _context;
        private readonly IVnPayService _vnPayService;
        private readonly IEmailService _emailService;
        public DanhSachDonHangController(MyDbContext context, IVnPayService vnPayService, IEmailService emailService)
        {
            _context = context;
            _vnPayService = vnPayService;
            _emailService = emailService;
        }
        // Action Index không thay đổi
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate, int? loaiDon, int? trangThaiDonHang,int? maDonHang)
        {
            var query = _context.DonHangs
                .Include(dh => dh.NguoiDung)
                .Include(dh => dh.KhachHang)
                .Include(dh => dh.DonHangChiTiets)
                    .ThenInclude(ct => ct.SanPhamChiTiet)
                        .ThenInclude(spct => spct.SanPham)
                .Include(dh => dh.DonHangChiTiets)
                    .ThenInclude(ct => ct.SanPhamChiTiet)
                        .ThenInclude(spct => spct.Size)
                .Include(dh => dh.DonHangChiTiets)
                    .ThenInclude(ct => ct.SanPhamChiTiet)
                        .ThenInclude(spct => spct.MauSac)
                .AsQueryable();

            if (maDonHang.HasValue)
            {
                query = query.Where(dh => dh.MaDonHang == maDonHang.Value);
                ViewBag.MaDonHang = maDonHang;
            }

            // Lọc theo Ngày (Nếu có) - Không dùng else ở đây
            if (startDate.HasValue)
                query = query.Where(dh => dh.ThoiGianTao >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(dh => dh.ThoiGianTao < endDate.Value.AddDays(1));

            // Lọc theo Loại đơn (Nếu có)
            if (loaiDon == 1)
            {
                query = query.Where(dh => dh.TrangThaiThanhToan == 1);
                if (trangThaiDonHang.HasValue)
                    query = query.Where(dh => dh.TrangThaiDonHang == trangThaiDonHang.Value);
            }
            else if (loaiDon == 2)
            {
                query = query.Where(dh => dh.TrangThaiThanhToan == 2);
            }

            var danhSachDonHang = await query.OrderByDescending(dh => dh.ThoiGianTao).ToListAsync();

            // Gán lại ViewBag để giữ trạng thái giao diện
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.LoaiDon = loaiDon;
            ViewBag.TrangThaiDonHang = trangThaiDonHang;

            return View(danhSachDonHang);
        }


        // Action này sẽ kiểm tra loại đơn hàng và trả về View phù hợp
        public async Task<IActionResult> ChiTietDonHang(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Cập nhật câu truy vấn để lấy TẤT CẢ dữ liệu liên quan
            var donHang = await _context.DonHangs
                .Include(dh => dh.NguoiDung)  // Lấy thông tin Nhân viên tạo đơn
                .Include(dh => dh.KhachHang)  // Lấy thông tin Khách hàng
                .Include(dh => dh.DonHangChiTiets) // Lấy danh sách các sản phẩm trong đơn
                    .ThenInclude(ct => ct.SanPhamChiTiet) // Từ chi tiết đơn hàng, lấy thông tin sản phẩm chi tiết
                        .ThenInclude(spct => spct.SanPham) // Từ sản phẩm chi tiết, lấy thông tin sản phẩm (để có tên)
                .Include(dh => dh.DonHangChiTiets)
                    .ThenInclude(ct => ct.SanPhamChiTiet)
                        .ThenInclude(spct => spct.Size) // Lấy thông tin Size
                .Include(dh => dh.DonHangChiTiets)
                    .ThenInclude(ct => ct.SanPhamChiTiet)
                        .ThenInclude(spct => spct.MauSac) // Lấy thông tin Màu sắc
                .FirstOrDefaultAsync(m => m.MaDonHang == id);

            if (donHang == null)
            {
                return NotFound();
            }

            // **LOGIC ĐỊNH TUYẾN VIEW**
            // Sử dụng TrangThaiThanhToan để quyết định (0 = Tại quầy)
            if (donHang.TrangThaiThanhToan == 2)
            {
                return View("ChiTietDonHangTaiQuay", donHang);
            }
            else
            {
                // Mặc định trả về trang online cho các phương thức khác
                return View("ChiTietDonHangAdmin", donHang);
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        private string GetTrangThaiText(int status)
        {
            return status switch
            {
                1 => "Chờ xác nhận",
                2 => "Đã xác nhận",
                3 => "Đang chuẩn bị",
                4 => "Đang giao hàng",
                5 => "Hoàn thành",
                6 => "Đã hủy",
                7 => "Đã thanh toán",
                _ => "Không xác định"
            };
        }

        public async Task<IActionResult> CapNhatTrangThai(int id)
        {
            var donHang = await _context.DonHangs
                .Include(d => d.KhachHang) // để lấy email khách hàng
                .FirstOrDefaultAsync(d => d.MaDonHang == id);

            if (donHang == null)
                return NotFound();

            int currentStatus = donHang.TrangThaiDonHang;

            // ===== Xử lý trạng thái =====
            if (currentStatus == 7)
            {
                donHang.TrangThaiDonHang = 3;
            }
            else if (currentStatus >= 1 && currentStatus < 5)
            {
                donHang.TrangThaiDonHang++;
            }
            else
            {
                TempData["ErrorMessage"] = "Đơn hàng đã ở trạng thái cuối cùng, không thể cập nhật.";
                return RedirectToAction("ChiTietDonHang", new { id = id });
            }

            // Gán nhân viên đang đăng nhập
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != null)
                donHang.MaNguoiDung = int.Parse(userId);
            // Tạo bản ghi mới
            var log = $"{DateTime.Now:dd/MM/yyyy HH:mm} - Trạng thái: {GetTrangThaiText(donHang.TrangThaiDonHang)}";

            // Ghép vào lịch sử cũ
            donHang.LichSuTrangThai = string.IsNullOrEmpty(donHang.LichSuTrangThai)
                ? log
                : donHang.LichSuTrangThai + "\n" + log;

            await _context.SaveChangesAsync();

            // ===== Gửi email nếu đã thanh toán =====
            if (donHang.TrangThaiThanhToan == 1 && !string.IsNullOrEmpty(donHang.KhachHang?.Email))
            {
                string subject = $"Cập nhật trạng thái đơn hàng #{donHang.MaDonHang}";
                string body = $@"
                        <html>
                        <head>
                          <meta charset='UTF-8'>
                          <style>
                            body {{
                              font-family: 'Segoe UI', Arial, sans-serif;
                              background-color: #f2f3f8;
                              margin: 0;
                              padding: 20px;
                              color: #333;
                            }}
                            .email-wrapper {{
                              max-width: 650px;
                              margin: auto;
                              background: #fff;
                              border-radius: 12px;
                              overflow: hidden;
                              box-shadow: 0 4px 20px rgba(0,0,0,0.08);
                            }}
                            .email-header {{
                              background: linear-gradient(135deg, #3498db, #2ecc71);
                              padding: 25px;
                              text-align: center;
                              color: white;
                            }}
                            .email-header h1 {{
                              margin: 0;
                              font-size: 22px;
                              letter-spacing: 1px;
                            }}
                            .email-body {{
                              padding: 30px;
                              line-height: 1.6;
                            }}
                            .email-body p {{
                              font-size: 16px;
                              color: #555;
                              margin-bottom: 15px;
                            }}
                            .email-body .highlight {{
                              color: #2ecc71;
                              font-weight: bold;
                            }}
                            .email-button {{
                              display: inline-block;
                              padding: 12px 24px;
                              background-color: #3498db;
                              color: #fff !important;
                              text-decoration: none;
                              border-radius: 6px;
                              font-weight: 600;
                              margin-top: 15px;
                              transition: background-color 0.3s ease;
                            }}
                            .email-button:hover {{
                              background-color: #2c80b4;
                            }}
                            .divider {{
                              height: 1px;
                              background-color: #eee;
                              margin: 25px 0;
                            }}
                            .email-footer {{
                              text-align: center;
                              font-size: 14px;
                              color: #888;
                              padding: 20px;
                              background-color: #fafafa;
                              border-top: 1px solid #eee;
                            }}
                            .email-footer strong {{
                              color: #555;
                            }}
                          </style>
                        </head>
                        <body>
                          <div class='email-wrapper'>
                            <div class='email-header'>
                              <h1>📦 Cập nhật trạng thái đơn hàng</h1>
                            </div>
                            <div class='email-body'>
                            <p>Xin chào <strong>{donHang.KhachHang?.HoTen ?? "Quý khách"}</strong>,</p>

                              <p>Chúng tôi xin thông báo rằng đơn hàng của bạn đã được cập nhật trạng thái:</p>
                              <p class='highlight'>{GetTrangThaiText(donHang.TrangThaiDonHang)}</p>
                              <p>Bạn có thể xem chi tiết đơn hàng bằng cách nhấn vào nút bên dưới:</p>
                              <a class='email-button' href='https://localhost:7089/GioHang/ChiTietDonHang/{donHang.MaDonHang}' target='_blank'>
                                🔍 Xem chi tiết đơn hàng
                              </a>
                              <div class='divider'></div>
                              <p>Nếu bạn có bất kỳ thắc mắc nào, vui lòng liên hệ với đội ngũ hỗ trợ của chúng tôi.</p>
                            </div>
                            <div class='email-footer'>
                              <p>Trân trọng,</p>
                              <p><strong>Hệ thống Quản lý Đơn hàng</strong></p>
                              <p>© {DateTime.Now.Year} YourCompany. All rights reserved.</p>
                            </div>
                          </div>
                        </body>
                        </html>";


                await _emailService.SendEmailAsync(donHang.KhachHang.Email, subject, body);
            }

            TempData["SuccessMessage"] = "Cập nhật trạng thái đơn hàng thành công!";
            return RedirectToAction("ChiTietDonHang", new { id = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HuyDonHang(int id, string lyDoHuy, bool hoanShip = false, decimal? phiPhatSinh = 0, bool hoanKho=false)
        {
            var donHang = await _context.DonHangs
                .Include(d => d.DonHangChiTiets)
                .Include(d => d.KhachHang)
                .FirstOrDefaultAsync(d => d.MaDonHang == id);

            if (donHang == null) return NotFound();

            if (donHang.TrangThaiDonHang == 5 || donHang.TrangThaiDonHang == 6)
            {
                TempData["ErrorMessage"] = "Đơn hàng không thể hủy.";
                return RedirectToAction("ChiTietDonHang", new { id });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int adminId = userId != null ? int.Parse(userId) : 1;
            donHang.MaNguoiDung = adminId;

            // ====================================================================
            // 1. TÍNH TOÁN SỐ TIỀN HOÀN (LOGIC SỬA ĐỔI)
            // ====================================================================
            decimal soTienHoan = donHang.TongTien; // Mặc định là hoàn tất cả (gồm cả ship)
            decimal soTienGiuLai = 0;

            // Nếu KHÔNG tích hoàn ship (hoanShip = false) VÀ đơn có phí ship
            if (!hoanShip && donHang.PhiVanChuyen.HasValue && donHang.PhiVanChuyen.Value > 0)
            {
                // Tính tiền hoàn = Tổng tiền khách trả - Phí vận chuyển
                soTienHoan = donHang.TongTien - donHang.PhiVanChuyen.Value;

                // [QUAN TRỌNG] Kiểm tra số âm:
                // Trường hợp khách dùng Voucher giảm giá lớn hơn cả tiền hàng, 
                // khiến Tổng tiền thanh toán < Phí ship niêm yết.
                if (soTienHoan < 0)
                {
                    soTienHoan = 0; // Không thể hoàn âm
                    soTienGiuLai = donHang.TongTien; // Giữ lại toàn bộ vì không đủ bù ship
                }
                else
                {
                    soTienGiuLai = donHang.PhiVanChuyen.Value;
                }
            }

            // ====================================================================
            // 2. XỬ LÝ HOÀN TIỀN VNPAY
            // ====================================================================
            if (donHang.PhuongThucThanhToan == "VnPay" && donHang.TrangThaiThanhToan == 1)
            {
                // Làm sạch nội dung (không dấu, ngắn gọn)
                string orderInfoClean = $"Huy don {donHang.MaDonHang}";
                string userClean = "Admin";

                // Xác định loại giao dịch: 
                // 02: Hoàn toàn phần (Full Refund)
                // 03: Hoàn một phần (Partial Refund - Khi giữ lại ship)
                // Lưu ý: So sánh soTienHoan với TongTien gốc
                string transType = (soTienHoan >= donHang.TongTien) ? "02" : "03";

                var refundRequest = new VnPayRefundRequest
                {
                    vnp_Version = "2.1.0",
                    vnp_Command = "refund",
                    vnp_RequestId = Guid.NewGuid().ToString(),
                    vnp_TxnRef = donHang.VnpTxnRef,

                    // [FIX] Dùng Math.Floor để làm tròn an toàn trước khi nhân 100
                    vnp_Amount = (long)(Math.Floor(soTienHoan) * 100),

                    vnp_OrderInfo = orderInfoClean,
                    vnp_TransactionNo = donHang.VnpTransactionNo,

                    // Đảm bảo ngày tháng chính xác từ dữ liệu VNPAY trả về lúc thanh toán
                    vnp_TransactionDate = donHang.VnpPayDate?.ToString("yyyyMMddHHmmss") ?? DateTime.Now.ToString("yyyyMMddHHmmss"),

                    vnp_CreateBy = userClean,
                    vnp_CreateDate = DateTime.Now.ToString("yyyyMMddHHmmss"),

                    // [FIX] Truyền đúng loại giao dịch đã xác định ở trên
                    vnp_TransactionType = transType
                };

                var refundResponse = await _vnPayService.Refund(refundRequest, HttpContext);

                if (refundResponse == null || refundResponse.vnp_ResponseCode != "00")
                {
                    TempData["ErrorMessage"] = $"Lỗi VNPAY: {refundResponse?.vnp_Message} (Code: {refundResponse?.vnp_ResponseCode})";
                    return RedirectToAction("ChiTietDonHang", new { id });
                }

                // Tạo phiếu chi VNPAY lưu lịch sử
                var noiDungPhieuChi = hoanShip
                    ? $"Hoàn 100% VNPAY đơn hủy #{donHang.MaDonHang}"
                    : $"Hoàn VNPAY đơn hủy #{donHang.MaDonHang} (Trừ ship {soTienGiuLai:N0})";

                var phieuChiVnp = new PhieuChi
                {
                    NgayTao = DateTime.Now,
                    SoTien = soTienHoan,
                    LoaiChiPhi = LoaiPhieuChi.LienQuanDonHang,
                    NoiDung = noiDungPhieuChi,
                    MaDonHang = donHang.MaDonHang,
                    MaNguoiDung = adminId,
                    TrangThai = true
                };
                _context.PhieuChis.Add(phieuChiVnp);
            }
            // ====================================================================
            // 3. XỬ LÝ ĐƠN COD (Phí phát sinh - Giữ nguyên)
            // ====================================================================
            else
            {
                if (phiPhatSinh.HasValue && phiPhatSinh.Value > 0)
                {
                    _context.PhieuChis.Add(new PhieuChi
                    {
                        NgayTao = DateTime.Now,
                        SoTien = phiPhatSinh.Value,
                        LoaiChiPhi = LoaiPhieuChi.LienQuanDonHang,
                        NoiDung = $"Phí phát sinh khi hủy đơn COD #{donHang.MaDonHang}",
                        MaDonHang = donHang.MaDonHang,
                        MaNguoiDung = adminId,
                        TrangThai = true
                    });
                }
            }

            // Cập nhật trạng thái
            donHang.TrangThaiDonHang = 6; // Đã Hủy
            donHang.LyDoHuy = lyDoHuy;
            donHang.TienMatDaNhan = soTienGiuLai; // Lưu doanh thu giữ lại (tiền ship không hoàn)
            if (hoanKho)
            {
                foreach (var chiTiet in donHang.DonHangChiTiets)
                {
                    var sp = await _context.SanPhamChiTiets.FindAsync(chiTiet.MaSanPhamChiTiet);
                    if (sp != null) sp.SoLuong += chiTiet.SoLuong;
                }
            }

            await _context.SaveChangesAsync();

            // Gửi mail (Giữ nguyên)
            if (!string.IsNullOrEmpty(donHang.KhachHang?.Email))
            {
                string subject = $"Xác nhận hủy và hoàn tiền đơn hàng #{donHang.MaDonHang}";
                string body = $"<p>Xin chào {donHang.HoTenNguoiNhan},</p>" +
                              $"<p>Đơn hàng #{donHang.MaDonHang} đã hủy thành công.</p>" +
                              $"<p>Số tiền hoàn trả: <strong>{soTienHoan:N0} đ</strong></p>" +
                              $"<p>Lý do: {donHang.LyDoHuy}</p>";
                await _emailService.SendEmailAsync(donHang.KhachHang.Email, subject, body);
            }

            TempData["SuccessMessage"] = $"Đã hủy đơn thành công. Đã hoàn tiền VNPAY: {soTienHoan:N0}đ.";
            return RedirectToAction("ChiTietDonHang", new { id });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DuyetHuyVaHoanTien(int id)
        {
            var donHang = await _context.DonHangs
                .Include(d => d.DonHangChiTiets)
                .Include(d => d.KhachHang)
                .FirstOrDefaultAsync(d => d.MaDonHang == id);

            if (donHang == null || donHang.TrangThaiDonHang != 8)
            {
                TempData["ErrorMessage"] = "Đơn hàng không hợp lệ hoặc không ở trạng thái chờ duyệt hủy.";
                return RedirectToAction("ChiTietDonHang", new { id = id });
            }

            // Lấy ID Admin đang thao tác để lưu vào Phiếu Chi
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int adminId = userId != null ? int.Parse(userId) : 1;
            donHang.MaNguoiDung = adminId;

            // 1. Chuẩn bị request hoàn tiền
            // [LƯU Ý] Làm sạch dữ liệu để tránh lỗi VNPAY
            string noidungSach = $"Duyet huy don {donHang.MaDonHang}";
            string nguoiTaoSach = "Admin";

            var refundRequest = new VnPayRefundRequest
            {
                vnp_Version = "2.1.0",
                vnp_Command = "refund",
                vnp_RequestId = Guid.NewGuid().ToString(),
                vnp_TxnRef = donHang.VnpTxnRef,
                vnp_Amount = (long)(donHang.TongTien * 100), // Hoàn toàn bộ 100%

                vnp_OrderInfo = noidungSach,
                vnp_CreateBy = nguoiTaoSach,

                vnp_TransactionNo = donHang.VnpTransactionNo,
                vnp_TransactionDate = donHang.VnpPayDate?.ToString("yyyyMMddHHmmss") ?? "",
                vnp_CreateDate = DateTime.Now.ToString("yyyyMMddHHmmss"),
                vnp_TransactionType = "02" // 02: Hoàn toàn phần (Vì đơn chưa giao, hoàn hết)
            };

            var refundResponse = await _vnPayService.Refund(refundRequest, HttpContext);

            if (refundResponse != null && refundResponse.vnp_ResponseCode == "00")
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // 2. Cập nhật trạng thái đơn hàng
                    donHang.TrangThaiDonHang = 6; // 6 = Hủy
                    donHang.GhiChu = (donHang.GhiChu ?? "") + $"\n[System] Đã hoàn tiền VNPAY {donHang.TongTien:N0}đ lúc {DateTime.Now}.";

                    var log = $"{DateTime.Now:dd/MM/yyyy HH:mm} - Trạng thái: {GetTrangThaiText(donHang.TrangThaiDonHang)}";
                    donHang.LichSuTrangThai = string.IsNullOrEmpty(donHang.LichSuTrangThai) ? log : donHang.LichSuTrangThai + "\n" + log;

                    // 3. Cộng lại sản phẩm vào kho
                    foreach (var chiTiet in donHang.DonHangChiTiets)
                    {
                        var sanPhamChiTiet = await _context.SanPhamChiTiets.FindAsync(chiTiet.MaSanPhamChiTiet);
                        if (sanPhamChiTiet != null)
                        {
                            sanPhamChiTiet.SoLuong += chiTiet.SoLuong;
                        }
                    }

                    // ====================================================================
                    // 4. [QUAN TRỌNG] TỰ ĐỘNG TẠO PHIẾU CHI
                    // Để cân bằng sổ sách: Doanh thu (đơn VNPAY) - Chi phí (Phiếu này) = 0
                    // ====================================================================
                    var phieuChi = new PhieuChi
                    {
                        NgayTao = DateTime.Now,
                        SoTien = donHang.TongTien,
                        LoaiChiPhi = LoaiPhieuChi.LienQuanDonHang, // Loại 1: Đơn hàng
                        NoiDung = $"Tự động hoàn tiền VNPAY (Admin duyệt hủy đơn #{donHang.MaDonHang})",
                        MaDonHang = donHang.MaDonHang,
                        MaNguoiDung = adminId,
                        TrangThai = true
                    };
                    _context.PhieuChis.Add(phieuChi);
                    // ====================================================================

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // 5. Gửi email cho khách hàng
                    if (!string.IsNullOrEmpty(donHang.KhachHang?.Email))
                    {
                        string subject = $"Xác nhận hủy và hoàn tiền đơn hàng #{donHang.MaDonHang}";
                        string body = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px;'>
                    <h2 style='color: #0d6efd;'>ĐƠN HÀNG ĐÃ ĐƯỢC HỦY</h2>
                    <p>Xin chào <strong>{donHang.HoTenNguoiNhan ?? "Quý khách"}</strong>,</p>
                    <p>Yêu cầu hủy đơn hàng <strong>#{donHang.MaDonHang}</strong> của bạn đã được chấp nhận.</p>
                    <p>Chúng tôi đã thực hiện hoàn tiền <strong>{donHang.TongTien:N0} VND</strong> về ví VNPAY/Tài khoản ngân hàng của bạn.</p>
                    <hr/>
                    <p>Cảm ơn bạn đã quan tâm đến sản phẩm của chúng tôi.</p>
                </div>";

                        await _emailService.SendEmailAsync(donHang.KhachHang.Email, subject, body);
                    }

                    TempData["SuccessMessage"] = $"Đã duyệt hủy và hoàn tiền {donHang.TongTien:N0}đ thành công!";
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Lỗi cập nhật dữ liệu: " + ex.Message;
                }
            }
            else
            {
                TempData["ErrorMessage"] = $"Hoàn tiền VNPAY thất bại. Mã lỗi: {refundResponse?.vnp_ResponseCode}. Lý do: {refundResponse?.vnp_Message}";
            }

            return RedirectToAction("ChiTietDonHang", new { id = id });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TuChoiHuyDonHang(int id, string lyDoTuChoi)
        {
            var donHang = await _context.DonHangs
                .Include(d => d.KhachHang)
                .FirstOrDefaultAsync(d => d.MaDonHang == id);

            if (donHang == null || donHang.TrangThaiDonHang != 8)
            {
                TempData["ErrorMessage"] = "Đơn hàng không hợp lệ hoặc không ở trạng thái chờ duyệt hủy.";
                return RedirectToAction("ChiTietDonHang", new { id = id });
            }

            if (string.IsNullOrWhiteSpace(lyDoTuChoi))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập lý do từ chối hủy đơn.";
                return RedirectToAction("ChiTietDonHang", new { id = id });
            }

            // 1. Cập nhật trạng thái về 7 (Đã thanh toán)
            donHang.TrangThaiDonHang = 7;

            // 2. Ghi đè hoặc thêm vào ghi chú lý do từ chối
            donHang.GhiChu = (donHang.GhiChu ?? "") + $"\n[Admin] Từ chối yêu cầu hủy. Lý do: {lyDoTuChoi}";

            // 3. Cập nhật lịch sử trạng thái
            var log = $"{DateTime.Now:dd/MM/yyyy HH:mm} - Admin từ chối hủy: {lyDoTuChoi}";
            donHang.LichSuTrangThai = string.IsNullOrEmpty(donHang.LichSuTrangThai)
                ? log
                : donHang.LichSuTrangThai + "\n" + log;

            await _context.SaveChangesAsync();

            // 4. (Tùy chọn) Gửi email thông báo cho khách hàng
            if (!string.IsNullOrEmpty(donHang.KhachHang?.Email))
            {
                string subject = $"Yêu cầu hủy đơn hàng #{donHang.MaDonHang} bị từ chối";
                string body = $"<p>Chào {donHang.KhachHang.HoTen},</p>" +
                              $"<p>Yêu cầu hủy đơn hàng #{donHang.MaDonHang} của bạn đã bị từ chối bởi quản trị viên.</p>" +
                              $"<p><strong>Lý do:</strong> {lyDoTuChoi}</p>" +
                              $"<p>Đơn hàng của bạn sẽ tiếp tục được xử lý.</p>";
                await _emailService.SendEmailAsync(donHang.KhachHang.Email, subject, body);
            }

            TempData["SuccessMessage"] = "Đã từ chối yêu cầu hủy đơn hàng thành công.";
            return RedirectToAction("ChiTietDonHang", new { id = id });
        }

    }
}
