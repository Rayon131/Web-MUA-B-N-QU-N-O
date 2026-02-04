using AppView.Models.Service.VNPay;
using DATNN.Models;
using DATNN.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Security.Claims;

namespace DATNN.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin")] // <-- BẢO MẬT: Chỉ Admin mới được truy cập
    public class QuanLyDoiTraController : Controller
    {
        private readonly MyDbContext _context;
        private readonly IVnPayService _vnPayService;
        private readonly IConfiguration _configuration;


        public QuanLyDoiTraController(MyDbContext context, IVnPayService vnPayService, IConfiguration configuration)
        {
            _context = context;
            _vnPayService = vnPayService;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            var yeuCaus = await _context.YeuCauDoiTras
                .Include(yc => yc.NguoiDung)
                .Include(yc => yc.DonHangChiTiet.SanPhamChiTiet.SanPham)
                .OrderByDescending(yc => yc.NgayTao)
                .ToListAsync();
            return View(yeuCaus);
        }

        // ADMIN: Chi tiết một yêu cầu
        public async Task<IActionResult> Details(int id)
        {
            var yeuCau = await _context.YeuCauDoiTras
                .Include(yc => yc.NguoiDung)
               .Include(yc => yc.DonHangChiTiet.DonHang) // Nạp đơn hàng cha
            .ThenInclude(dh => dh.DonHangChiTiets) // Từ đơn hàng cha, nạp TẤT CẢ chi tiết đơn hàng con
                .ThenInclude(dhct => dhct.SanPhamChiTiet) // Từ mỗi chi tiết con, nạp sản phẩm chi tiết
                    .ThenInclude(spct => spct.SanPham) // Từ sản phẩm chi tiết, nạp sản phẩm cha
        .Include(yc => yc.DonHangChiTiet.DonHang)
            .ThenInclude(dh => dh.DonHangChiTiets)
                .ThenInclude(dhct => dhct.SanPhamChiTiet)
                    .ThenInclude(spct => spct.MauSac) // Nạp màu sắc
        .Include(yc => yc.DonHangChiTiet.DonHang)
            .ThenInclude(dh => dh.DonHangChiTiets)
                .ThenInclude(dhct => dhct.SanPhamChiTiet)
                    .ThenInclude(spct => spct.Size) // Nạp size
                                                    // Nạp các thông tin khác của chính yêu cầu này
        .Include(yc => yc.DonHangChiTiet.SanPhamChiTiet.MauSac)
        .Include(yc => yc.DonHangChiTiet.SanPhamChiTiet.Size)
        .FirstOrDefaultAsync(yc => yc.Id == id);

            if (yeuCau == null) return NotFound();

            // Lấy danh sách sản phẩm để admin chọn khi cần đổi hàng
            var allProductDetails = await _context.SanPhamChiTiets
          .Include(s => s.SanPham)
          .Include(s => s.MauSac)
          .Include(s => s.Size)
          .ToListAsync();

            // 2. Dùng LINQ để nhóm các sản phẩm lại theo MaSanPham (ID sản phẩm cha)
            var groupedProducts = allProductDetails
     .GroupBy(spct => spct.MaSanPham)
     .Select(group => new ProductGroupViewModel
     {
         ProductName = group.First().SanPham.TenSanPham,
         Variants = group.Select(variant => new VariantViewModel
         {
             Id = variant.MaSanPhamChiTiet,
             DisplayText = $"Màu: {variant.MauSac.TenMau}, Size: {variant.Size.TenSize} (Tồn: {variant.SoLuong})",

             // === SỬA LẠI Ở ĐÂY ===
             Price = variant.GiaBan, // Lấy giá bán trực tiếp từ SanPhamChiTiet

             FullTextForSearch = $"{group.First().SanPham.TenSanPham} - Màu: {variant.MauSac.TenMau}, Size: {variant.Size.TenSize}"
         }).ToList()
     })
     .OrderBy(g => g.ProductName)
     .ToList();
            ViewBag.BenChiuPhiList = Enum.GetValues(typeof(BenChiuPhi))
               .Cast<BenChiuPhi>()
               .Select(bcp => new SelectListItem
               {
                   Value = ((int)bcp).ToString(),
                   Text = GetEnumDisplayName(bcp)
               }).ToList();
            // 3. Gửi dữ liệu đã được nhóm ra ViewBag
            ViewBag.ProductGroups = groupedProducts;
            var currentStatus = yeuCau.TrangThai;
            var nextAvailableStatuses = new List<TrangThaiYeuCauDoiTra>();

            switch (currentStatus)
            {
                case TrangThaiYeuCauDoiTra.ChoXacNhan:
                    nextAvailableStatuses.Add(TrangThaiYeuCauDoiTra.ChoXacNhan);
                    nextAvailableStatuses.Add(TrangThaiYeuCauDoiTra.DaDuyet);
                    nextAvailableStatuses.Add(TrangThaiYeuCauDoiTra.DaTuChoi); // Từ chối dựa trên chính sách
                    break;

                case TrangThaiYeuCauDoiTra.DaDuyet:
                    nextAvailableStatuses.Add(TrangThaiYeuCauDoiTra.DaDuyet);
                    nextAvailableStatuses.Add(TrangThaiYeuCauDoiTra.DaNhanHang);
                    break;

                case TrangThaiYeuCauDoiTra.DaNhanHang:
                    nextAvailableStatuses.Add(TrangThaiYeuCauDoiTra.DaNhanHang);
                    // Sau khi kiểm tra hàng, Admin có các lựa chọn sau:
                    if (yeuCau.LoaiYeuCau == LoaiYeuCau.DoiHang)
                    {
                        nextAvailableStatuses.Add(TrangThaiYeuCauDoiTra.DangGiaoHangDoi);
                    }
                    else // LoaiYeuCau == TraHang
                    {
                        nextAvailableStatuses.Add(TrangThaiYeuCauDoiTra.HoanThanh);
                    }
                    // THÊM LỰA CHỌN TỪ CHỐI TẠI BƯỚC NÀY
                    nextAvailableStatuses.Add(TrangThaiYeuCauDoiTra.DaTuChoi); // Từ chối dựa trên kiểm tra thực tế
                    break;

                case TrangThaiYeuCauDoiTra.DangGiaoHangDoi:
                    nextAvailableStatuses.Add(TrangThaiYeuCauDoiTra.DangGiaoHangDoi);
                    nextAvailableStatuses.Add(TrangThaiYeuCauDoiTra.HoanThanh);
                    break;

                // Các trạng thái cuối cùng
                case TrangThaiYeuCauDoiTra.HoanThanh:
                case TrangThaiYeuCauDoiTra.DaTuChoi:
                default:
                    nextAvailableStatuses.Add(currentStatus);
                    break;
            }
            bool daHoanShip = false;

            if (yeuCau.LoaiYeuCau == LoaiYeuCau.TraHang &&
                (yeuCau.TrangThai == TrangThaiYeuCauDoiTra.HoanThanh))
            {
                // Tìm phiếu chi liên quan đến yêu cầu này
                var phieuChi = await _context.PhieuChis
                    .FirstOrDefaultAsync(p => p.MaYeuCauDoiTra == id);

                if (phieuChi != null)
                {
                    decimal tienHang = yeuCau.DonHangChiTiet.DonGia * yeuCau.SoLuongYeuCau;

                    // Nếu số tiền đã chi > tiền hàng => Có bao gồm ship
                    if (phieuChi.SoTien > tienHang)
                    {
                        daHoanShip = true;
                    }
                }
            }

            // Truyền biến này sang View
            ViewBag.DaHoanShip = daHoanShip;
            // Chuyển danh sách đã lọc thành SelectListItem (giữ nguyên)
            ViewBag.TrangThaiList = nextAvailableStatuses.Select(s => new SelectListItem
            {
                Value = ((int)s).ToString(),
                Text = GetEnumDisplayName(s)
            }).ToList();
            decimal tienHangHoan = yeuCau.DonHangChiTiet.DonGia * yeuCau.SoLuongYeuCau;
            decimal tienShipHoan = 0;
            decimal tongTienHoanThucTe = 0;

            // Chỉ tính toán khi là Trả hàng
            if (yeuCau.LoaiYeuCau == LoaiYeuCau.TraHang)
            {
                // Nếu đã hoàn thành, lấy số liệu chính xác từ Phiếu Chi
                if (yeuCau.TrangThai == TrangThaiYeuCauDoiTra.HoanThanh)
                {
                    var phieuChi = await _context.PhieuChis
                        .FirstOrDefaultAsync(p => p.MaYeuCauDoiTra == id);

                    if (phieuChi != null)
                    {
                        tongTienHoanThucTe = phieuChi.SoTien;
                        if (phieuChi.SoTien > tienHangHoan)
                        {
                            tienShipHoan = phieuChi.SoTien - tienHangHoan;
                        }
                    }
                    else
                    {
                        // Fallback nếu không tìm thấy phiếu chi (hiếm gặp)
                        tongTienHoanThucTe = tienHangHoan;
                    }
                }
                else
                {
                    // Nếu chưa hoàn thành, hiển thị số dự kiến (chưa cộng ship)
                    tongTienHoanThucTe = tienHangHoan;
                }
            }

            ViewBag.TongTienHoanThucTe = tongTienHoanThucTe;
            ViewBag.TienHangHoan = tienHangHoan;
            ViewBag.TienShipHoan = tienShipHoan;

            return View(yeuCau);
        }
        // Thêm phương thức helper này vào cuối Controller để lấy tên hiển thị của Enum
        private static string GetEnumDisplayName(Enum enumValue)
        {
            return enumValue.GetType()
                            .GetMember(enumValue.ToString())
                            .First()
                            .GetCustomAttribute<DisplayAttribute>()?
                            .GetName() ?? enumValue.ToString();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CapNhatTrangThai(int id, TrangThaiYeuCauDoiTra trangThai, string ghiChuAdmin, int? MaSanPhamChiTietMoi, BenChiuPhi benChiuPhiShip, decimal? chiPhiShip, bool hoanTienShip = false, bool hoanKho = false)
        {
            var yeuCau = await _context.YeuCauDoiTras
               .Include(yc => yc.DonHangChiTiet.SanPhamChiTiet.SanPham)
               .Include(yc => yc.DonHangChiTiet.DonHang)
               .FirstOrDefaultAsync(yc => yc.Id == id);

            if (yeuCau == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy yêu cầu.";
                return RedirectToAction("Index");
            }

            // --- KHAI BÁO ADMIN ID Ở ĐÂY ĐỂ DÙNG CHUNG CHO TOÀN BỘ HÀM ---
            var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "1");

            yeuCau.TrangThai = trangThai;
            yeuCau.GhiChuAdmin = ghiChuAdmin;
            yeuCau.NgayCapNhat = DateTime.Now;

            // 1. XỬ LÝ NẾU LÀ ĐỔI HÀNG (Tính tiền chênh lệch)
            if (yeuCau.LoaiYeuCau == LoaiYeuCau.DoiHang)
            {
                yeuCau.MaSanPhamChiTietMoi = MaSanPhamChiTietMoi;
                yeuCau.BenChiuPhiShip = benChiuPhiShip;
                yeuCau.ChiPhiShip = chiPhiShip ?? 0;

                decimal tienChenhLech = 0;

                if (MaSanPhamChiTietMoi.HasValue)
                {
                    var sanPhamMoi = await _context.SanPhamChiTiets
                                             .Include(spct => spct.SanPham)
                                               .Include(spct => spct.MauSac) // Thêm Include
                                 .Include(spct => spct.Size)   // Thêm Include
                                             .FirstOrDefaultAsync(spct => spct.MaSanPhamChiTiet == MaSanPhamChiTietMoi.Value);

                    if (sanPhamMoi != null)
                    {
                        yeuCau.TenSanPhamMoi_Luu = sanPhamMoi.SanPham.TenSanPham;
                        yeuCau.TenMauMoi_Luu = sanPhamMoi.MauSac.TenMau;
                        yeuCau.TenSizeMoi_Luu = sanPhamMoi.Size.TenSize;
                        yeuCau.HinhAnhMoi_Luu = sanPhamMoi.HinhAnh ?? sanPhamMoi.SanPham.AnhSanPham;
                        yeuCau.GiaSanPhamMoi_Luu = sanPhamMoi.GiaBan;
                        decimal donGiaCu = yeuCau.DonHangChiTiet.DonGia;
                        decimal donGiaMoi = sanPhamMoi.GiaBan;
                        tienChenhLech = (donGiaMoi - donGiaCu) * yeuCau.SoLuongYeuCau;
                    }
                }

                yeuCau.TienChenhLech = tienChenhLech;

                if (benChiuPhiShip == BenChiuPhi.KhachHang)
                {
                    yeuCau.TongTienThanhToan = tienChenhLech + yeuCau.ChiPhiShip;
                }
                else
                {
                    yeuCau.TongTienThanhToan = tienChenhLech;
                }
            }

            // 2. XỬ LÝ KHI CHUYỂN TRẠNG THÁI "ĐANG GIAO HÀNG ĐỔI"
            if (trangThai == TrangThaiYeuCauDoiTra.DangGiaoHangDoi && yeuCau.LoaiYeuCau == LoaiYeuCau.DoiHang)
            {
                if (yeuCau.MaSanPhamChiTietMoi == null)
                {
                    TempData["ErrorMessage"] = "Vui lòng chọn sản phẩm mới trước khi chuyển sang trạng thái giao hàng.";
                    return RedirectToAction("Details", new { id });
                }

                var sanPhamMoi = await _context.SanPhamChiTiets.FindAsync(yeuCau.MaSanPhamChiTietMoi);
                if (sanPhamMoi != null)
                {
                    if (sanPhamMoi.SoLuong < yeuCau.SoLuongYeuCau)
                    {
                        TempData["ErrorMessage"] = "Sản phẩm mới không đủ số lượng tồn kho.";
                        return RedirectToAction("Details", new { id });
                    }
                    sanPhamMoi.SoLuong -= yeuCau.SoLuongYeuCau;
                    TempData["SuccessMessage"] = "Đã cập nhật trạng thái và trừ kho sản phẩm mới.";
                }
            }

            // 3. XỬ LÝ KHI CHUYỂN TRẠNG THÁI "HOÀN THÀNH"
            if (trangThai == TrangThaiYeuCauDoiTra.HoanThanh)
            {
                if (yeuCau.LoaiYeuCau == LoaiYeuCau.TraHang)
                {
                    var donHang = yeuCau.DonHangChiTiet.DonHang;

                    decimal tienHang = yeuCau.DonHangChiTiet.DonGia * yeuCau.SoLuongYeuCau;
                    decimal tienShipHoan = 0;

                    if (hoanTienShip && donHang.PhiVanChuyen.HasValue)
                    {
                        tienShipHoan = donHang.PhiVanChuyen.Value;
                    }

                    decimal tongTienHoan = tienHang + tienShipHoan;

                    // --- [SỬA LỖI QUAN TRỌNG] KIỂM TRA GIỚI HẠN SỐ TIỀN HOÀN ---
                    // Nếu đơn hàng có áp dụng Voucher, tổng tiền hàng + ship có thể lớn hơn số tiền khách thực trả.
                    // Cần đảm bảo không hoàn quá số tiền khách đã thanh toán (donHang.TongTien).

                    // Lưu ý: Nếu đơn hàng có nhiều sản phẩm và đây là lần hoàn trả thứ 2, 
                    // logic này cần phức tạp hơn (trừ đi số tiền đã hoàn trước đó).
                    // Nhưng ở mức cơ bản, không được vượt quá Tổng Tiền Đơn Hàng.
                    if (tongTienHoan > donHang.TongTien)
                    {
                        tongTienHoan = donHang.TongTien;
                        // Tính lại tiền ship hiển thị (nếu cần) để khớp với tổng tiền
                        if (tongTienHoan < tienHang)
                        {
                            // Trường hợp đặc biệt: Voucher giảm sâu hơn cả tiền ship
                            tienShipHoan = 0;
                        }
                        else
                        {
                            tienShipHoan = tongTienHoan - tienHang;
                        }
                    }

                    // A. HOÀN TIỀN TỰ ĐỘNG VNPAY
                    if (donHang.PhuongThucThanhToan == "VnPay" &&
                   yeuCau.HinhThucHoanTien == HinhThucHoanTien.VNPAY &&
                   !string.IsNullOrEmpty(donHang.VnpTxnRef))
                    {
                        if (!donHang.VnpPayDate.HasValue || string.IsNullOrEmpty(donHang.VnpTransactionNo))
                        {
                            TempData["ErrorMessage"] = "Dữ liệu VNPAY gốc thiếu.";
                            return RedirectToAction("Details", new { id });
                        }

                        string noiDungSach = $"Refund YC {yeuCau.Id}"; // Nội dung ngắn gọn, không dấu tiếng Việt càng tốt
                        string nguoiTaoSach = "Admin";

                        var refundRequest = new VnPayRefundRequest
                        {
                            vnp_Version = "2.1.0",
                            vnp_Command = "refund",
                            vnp_RequestId = Guid.NewGuid().ToString(),
                            vnp_TxnRef = donHang.VnpTxnRef,

                            // QUAN TRỌNG: Ép kiểu long cẩn thận và đảm bảo không âm
                            vnp_Amount = (long)(Math.Floor(tongTienHoan) * 100),

                            vnp_OrderInfo = noiDungSach,
                            vnp_CreateBy = nguoiTaoSach,
                            vnp_TransactionNo = donHang.VnpTransactionNo,
                            vnp_TransactionDate = donHang.VnpPayDate.Value.ToString("yyyyMMddHHmmss"),
                            vnp_CreateDate = DateTime.Now.ToString("yyyyMMddHHmmss"),

                            // 02: Hoàn trả toàn phần (nếu tổng tiền hoàn == tổng tiền đơn hàng)
                            // 03: Hoàn trả một phần
                            vnp_TransactionType = (tongTienHoan >= donHang.TongTien) ? "02" : "03"
                        };

                        var refundResponse = await _vnPayService.Refund(refundRequest, HttpContext);

                        if (refundResponse != null && refundResponse.vnp_ResponseCode == "00")
                        {
                            if (hoanKho) // Chỉ cộng khi checkbox được tích
                            {
                                var sanPhamChiTiet = await _context.SanPhamChiTiets.FindAsync(yeuCau.DonHangChiTiet.MaSanPhamChiTiet);
                                if (sanPhamChiTiet != null) sanPhamChiTiet.SoLuong += yeuCau.SoLuongYeuCau;
                            }

                            // Tạo phiếu chi VNPAY
                            _context.PhieuChis.Add(new PhieuChi
                            {
                                NgayTao = DateTime.Now,
                                SoTien = tongTienHoan, // GHI NHẬN TỔNG TIỀN
                                LoaiChiPhi = LoaiPhieuChi.LienQuanDoiTra,
                                NoiDung = $"Tự động hoàn VNPAY YC #{yeuCau.Id} (Hàng: {tienHang:N0} + Ship: {tienShipHoan:N0})",
                                MaYeuCauDoiTra = yeuCau.Id,
                                MaNguoiDung = adminId,
                                TrangThai = true
                            });

                            TempData["SuccessMessage"] = $"Hoàn tất trả hàng. Đã hoàn {tongTienHoan:N0}đ qua VNPAY.";
                        }
                        else
                        {
                            TempData["ErrorMessage"] = $"Lỗi VNPAY: {refundResponse?.vnp_Message}";
                            return RedirectToAction("Details", new { id });
                        }
                    }
                    else
                    {
                        bool daCoPhieuChi = await _context.PhieuChis.AnyAsync(p => p.MaYeuCauDoiTra == id);
                        if (!daCoPhieuChi)
                        {
                            if (hoanKho) // Chỉ cộng khi checkbox được tích
                            {
                                var sanPhamChiTiet = await _context.SanPhamChiTiets.FindAsync(yeuCau.DonHangChiTiet.MaSanPhamChiTiet);
                                if (sanPhamChiTiet != null) sanPhamChiTiet.SoLuong += yeuCau.SoLuongYeuCau;
                            }

                            // Tạo phiếu chi thủ công với TỔNG TIỀN (Đã bao gồm ship nếu tích chọn)
                            _context.PhieuChis.Add(new PhieuChi
                            {
                                NgayTao = DateTime.Now,
                                SoTien = tongTienHoan, // <--- Dùng biến tổng đã tính ở Bước 1
                                LoaiChiPhi = LoaiPhieuChi.LienQuanDoiTra,
                                MaYeuCauDoiTra = yeuCau.Id,
                                MaNguoiDung = adminId,
                                TrangThai = true,
                                // Ghi chú rõ ràng để dễ đối soát
                                NoiDung = (donHang.PhuongThucThanhToan == "COD")
                                    ? $"Hoàn tiền mặt/CK đơn COD YC #{id} (Hàng: {tienHang:N0} + Ship: {tienShipHoan:N0})"
                                    : $"Hoàn tiền qua TK NH khác đơn VNPAY YC #{id} (Hàng: {tienHang:N0} + Ship: {tienShipHoan:N0})"
                            });

                            TempData["SuccessMessage"] = $"Hoàn tất trả hàng. Đã tạo phiếu chi {tongTienHoan:N0}đ (Bao gồm ship: {tienShipHoan:N0}đ).";
                        }
                    }
                }
                else if (yeuCau.LoaiYeuCau == LoaiYeuCau.DoiHang)
                {
                    if (yeuCau.MaSanPhamChiTietMoi == null)
                    {
                        TempData["ErrorMessage"] = "Vui lòng chọn một sản phẩm mới để đổi cho khách trước khi hoàn thành.";
                        return RedirectToAction("Details", new { id });
                    }

                    if (hoanKho) // Chỉ cộng khi checkbox được tích
                    {
                        var sanPhamCu = await _context.SanPhamChiTiets.FindAsync(yeuCau.DonHangChiTiet.MaSanPhamChiTiet);
                        if (sanPhamCu != null)
                        {
                            sanPhamCu.SoLuong += yeuCau.SoLuongYeuCau;
                        }
                    }
                    TempData["SuccessMessage"] = "Yêu cầu đổi hàng đã hoàn thành.";
                }
            }

            // 4. TỰ ĐỘNG TẠO PHIẾU CHI CHO ĐỔI HÀNG (Shop chịu ship HOẶC Hoàn tiền thừa)
            if (trangThai == TrangThaiYeuCauDoiTra.DangGiaoHangDoi || trangThai == TrangThaiYeuCauDoiTra.HoanThanh)
            {
                bool daCoPhieuChi = await _context.PhieuChis.AnyAsync(p => p.MaYeuCauDoiTra == id);

              
                if (yeuCau.BenChiuPhiShip == BenChiuPhi.CuaHang && yeuCau.ChiPhiShip > 0)
                {
                    // Check xem đã có phiếu ship chưa
                    bool daCoPhieuShip = await _context.PhieuChis.AnyAsync(p => p.MaYeuCauDoiTra == id && p.NoiDung.Contains("Phí ship"));
                    if (!daCoPhieuShip)
                    {
                        _context.PhieuChis.Add(new PhieuChi
                        {
                            NgayTao = DateTime.Now,
                            SoTien = yeuCau.ChiPhiShip ?? 0,
                            LoaiChiPhi = LoaiPhieuChi.LienQuanDoiTra,
                            NoiDung = $"Phí ship đổi trả YC #{yeuCau.Id} (Cửa hàng chịu)",
                            MaYeuCauDoiTra = yeuCau.Id,
                            MaNguoiDung = adminId,
                            TrangThai = true
                        });
                    }
                }

                // 2. TẠO PHIẾU CHI HOÀN TIỀN THỪA (Đổi hàng giá rẻ hơn)
                if (yeuCau.LoaiYeuCau == LoaiYeuCau.DoiHang && yeuCau.TienChenhLech.HasValue && yeuCau.TienChenhLech.Value < 0)
                {
                    // Check xem đã có phiếu hoàn chênh lệch chưa
                    bool daCoPhieuLech = await _context.PhieuChis.AnyAsync(p => p.MaYeuCauDoiTra == id && p.NoiDung.Contains("chênh lệch"));
                    if (!daCoPhieuLech)
                    {
                        _context.PhieuChis.Add(new PhieuChi
                        {
                            NgayTao = DateTime.Now,
                            SoTien = Math.Abs(yeuCau.TienChenhLech.Value),
                            LoaiChiPhi = LoaiPhieuChi.LienQuanDoiTra,
                            NoiDung = $"Hoàn tiền chênh lệch đổi hàng YC #{yeuCau.Id}",
                            MaYeuCauDoiTra = yeuCau.Id,
                            MaNguoiDung = adminId,
                            TrangThai = true
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();

            if (!TempData.ContainsKey("SuccessMessage") && !TempData.ContainsKey("ErrorMessage"))
            {
                TempData["SuccessMessage"] = "Cập nhật trạng thái thành công!";
            }

            return RedirectToAction("Details", new { id });
        }
    }
}
