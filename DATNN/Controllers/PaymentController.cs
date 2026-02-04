using AppView.Models.Service.VNPay;
using DATNN.Models;
using DATNN.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Globalization;
using System.Security.Claims;

namespace DATNN.Controllers
{
    public class PaymentController : Controller
    {
        private readonly IVnPayService _vnPayService;
        private readonly MyDbContext _context;

        public PaymentController(IVnPayService vnPayService, MyDbContext context)
        {
            _vnPayService = vnPayService;
            _context = context;
        }

        // ===== ACTION XỬ LÝ CALLBACK TỪ VNPAY CHO KHÁCH HÀNG =====
        public async Task<IActionResult> PaymentCallBack()
        {
            Debug.WriteLine($"===== VNPAY CALLBACK RECEIVED =====");
            Debug.WriteLine($"Raw QueryString: {Request.QueryString.ToString()}");
            Debug.WriteLine($"===================================");
            var response = _vnPayService.PaymentExecute(Request.Query);

            if (response == null || response.VnPayResponseCode != "00")
            {
                TempData["ErrorMessage"] = $"Thanh toán VNPay thất bại. Lỗi: {response?.VnPayResponseCode}";
                return RedirectToAction("Index", "GioHang");
            }

            var orderJson = HttpContext.Session.GetString("PendingOrder");
            if (string.IsNullOrEmpty(orderJson))
            {
                TempData["ErrorMessage"] = "Phiên thanh toán đã hết hạn hoặc không hợp lệ.";
                return RedirectToAction("Index", "GioHang");
            }

            var model = JsonConvert.DeserializeObject<DatHangViewModel>(orderJson);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var cartItems = await _context.ChiTietGioHangs
     .Include(c => c.SanPhamChiTiet).ThenInclude(spct => spct.SanPham)
     // THÊM 2 DÒNG NÀY ĐỂ LẤY MÀU VÀ SIZE
     .Include(c => c.SanPhamChiTiet).ThenInclude(spct => spct.MauSac)
     .Include(c => c.SanPhamChiTiet).ThenInclude(spct => spct.Size)
     // ----------------------------------
     .Where(c => model.SelectedCartItemIds.Contains(c.MaChiTietGioHang) && c.GioHang.MaNguoiDung.ToString() == userId)
     .ToListAsync();

                // ==============================================================================
                // BƯỚC 1: TÍNH TOÁN LẠI GIÁ TRỊ ĐƠN HÀNG MỘT CÁCH AN TOÀN TRÊN SERVER
                // ==============================================================================

                var promoSetting = await _context.SystemSettings.FindAsync("PromotionRule");
                string promoRule = promoSetting?.SettingValue ?? "BestValue";

                // Lấy các khuyến mãi đang hoạt động
                var activeProductPromotions = await _context.KhuyenMais
                    .Where(km => km.TrangThai == 1 && km.NgayBatDau <= DateTime.Now && km.NgayKetThuc >= DateTime.Now &&
                                 !string.IsNullOrEmpty(km.DanhSachSanPhamApDung) && (km.KenhApDung == "Online" || km.KenhApDung == "TatCa"))
                    .ToListAsync();

                decimal tongTienHangThucTe = 0;
                var danhSachChiTietDonHang = new List<DonHangChiTiet>();

                foreach (var item in cartItems)
                {
                    // Kiểm tra lại tồn kho
                    if (item.SanPhamChiTiet.SoLuong < item.SoLuong)
                    {
                        throw new Exception($"Sản phẩm '{item.SanPhamChiTiet.SanPham.TenSanPham}' không đủ số lượng tồn kho.");
                    }

                    decimal giaGoc = item.SanPhamChiTiet.GiaBan;
                    decimal giaCuoiCung = giaGoc;

                    // Tìm và áp dụng khuyến mãi
                    var promotionsForThisVariant = activeProductPromotions
                        .Where(km => km.DanhSachSanPhamApDung.Split(',').Any(id => id == $"p-{item.SanPhamChiTiet.MaSanPham}" || id == $"v-{item.SanPhamChiTiet.MaSanPhamChiTiet}"))
                        .ToList();

                    // === SỬA ĐOẠN NÀY ĐỂ HỖ TRỢ CỘNG DỒN ===
                    if (promotionsForThisVariant.Any())
                    {
                        if (promoRule == "Stackable")
                        {
                            // LOGIC CỘNG DỒN (LŨY KẾ)
                            var sortedPromos = promotionsForThisVariant.OrderBy(p => p.LoaiGiamGia).ThenBy(p => p.MaKhuyenMai).ToList();
                            foreach (var promo in sortedPromos)
                            {
                                if (promo.LoaiGiamGia == "PhanTram")
                                    giaCuoiCung -= giaCuoiCung * (promo.GiaTriGiamGia / 100);
                                else if (promo.LoaiGiamGia == "SoTien")
                                    giaCuoiCung -= promo.GiaTriGiamGia;

                                if (giaCuoiCung < 0) giaCuoiCung = 0;
                            }
                        }
                        else
                        {
                            // LOGIC GIÁ TỐT NHẤT (BEST VALUE - Code cũ của bạn)
                            giaCuoiCung = promotionsForThisVariant
                                .Select(p => p.LoaiGiamGia == "PhanTram"
                                             ? giaGoc * (1 - (p.GiaTriGiamGia / 100))
                                             : (giaGoc - p.GiaTriGiamGia > 0 ? giaGoc - p.GiaTriGiamGia : 0))
                                .Min();
                        }
                    }
                    // ===========================================

                    tongTienHangThucTe += item.SoLuong * giaCuoiCung;

                    danhSachChiTietDonHang.Add(new DonHangChiTiet
                    {
                        MaSanPhamChiTiet = item.MaSanPhamChiTiet,
                        SoLuong = item.SoLuong,
                        DonGia = giaCuoiCung, // Giá này giờ đã đúng logic cộng dồn
                        TenSanPham_Luu = item.SanPhamChiTiet.SanPham?.TenSanPham ?? "Sản phẩm không xác định",
                        TenMau_Luu = item.SanPhamChiTiet.MauSac?.TenMau ?? "N/A",
                        TenSize_Luu = item.SanPhamChiTiet.Size?.TenSize ?? "N/A",
                        HinhAnh_Luu = item.SanPhamChiTiet.HinhAnh ?? item.SanPhamChiTiet.SanPham?.AnhSanPham
                    });
                }


                // Tính toán lại tiền giảm giá từ voucher
                decimal tienGiamGiaVoucher = 0;
                MaGiamGia appliedVoucher = null;
                if (!string.IsNullOrEmpty(model.AppliedVoucherCode))
                {
                    appliedVoucher = await _context.MaGiamGias.FirstOrDefaultAsync(v => v.MaCode == model.AppliedVoucherCode);
                    if (appliedVoucher != null && tongTienHangThucTe >= (appliedVoucher.DieuKienDonHangToiThieu ?? 0))
                    {
                        appliedVoucher.DaSuDung++; // Tăng số lượt sử dụng lên 1
                        _context.Update(appliedVoucher);
                        tienGiamGiaVoucher = (appliedVoucher.LoaiGiamGia == "PhanTram")
                            ? tongTienHangThucTe * (appliedVoucher.GiaTriGiamGia / 100m)
                            : appliedVoucher.GiaTriGiamGia;
                    }
                }

                // Tính toán tổng tiền cuối cùng
                decimal tongThanhToanThucTe = tongTienHangThucTe - tienGiamGiaVoucher + model.PhiVanChuyen;


                // ==============================================================================
                // BƯỚC 2: TẠO ĐƠN HÀNG VỚI CÁC GIÁ TRỊ ĐÃ ĐƯỢC TÍNH TOÁN LẠI
                // ==============================================================================
                var vnpPayDateStr = Request.Query["vnp_PayDate"];

                // 2. Chuyển chuỗi thành kiểu DateTime
                DateTime vnpPayDate = DateTime.Now; // Giá trị mặc định nếu có lỗi
                if (!string.IsNullOrEmpty(vnpPayDateStr))
                {
                    DateTime.TryParseExact(vnpPayDateStr, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out vnpPayDate);
                }
                var vnpTxnRef_FromQuery = Request.Query["vnp_TxnRef"].ToString();
                var donHang = new DonHang
                {
                    MaKhachHang = int.Parse(userId),
                    ThoiGianTao = DateTime.Now,
                    DiaChi = model.DiaChiGiaoHang,
                    SoDienThoai = model.SoDienThoaiNguoiNhan,
                    HoTenNguoiNhan = model.HoTenNguoiNhan,
                    Email = model.Email,
                    GhiChu = model.GhiChu,
                    MaGiamGiaID = appliedVoucher?.MaGiamGiaID,
                    MaKhuyenMai = null,
                    SoTienDuocGiam = tienGiamGiaVoucher,
                    TongTien = tongThanhToanThucTe,
                    PhiVanChuyen = model.PhiVanChuyen,
                    PhuongThucThanhToan = "VnPay",
                    TrangThaiDonHang = 7,
                    TrangThaiThanhToan = 1,
                    VnpTxnRef = vnpTxnRef_FromQuery,
                    VnpTransactionNo = response.TransactionId,
                    VnpPayDate = vnpPayDate // 3. Sử dụng ngày tháng chính xác từ VNPAY
                };
                _context.DonHangs.Add(donHang);
                await _context.SaveChangesAsync(); // Lưu để lấy MaDonHang

                // Gán MaDonHang cho các chi tiết và lưu chúng
                foreach (var chiTiet in danhSachChiTietDonHang)
                {
                    chiTiet.MaDonHang = donHang.MaDonHang;
                }
                _context.DonHangChiTiets.AddRange(danhSachChiTietDonHang);

                // Trừ số lượng tồn kho
                foreach (var item in cartItems)
                {
                    var sanPhamChiTiet = await _context.SanPhamChiTiets.FindAsync(item.MaSanPhamChiTiet);
                    if (sanPhamChiTiet != null)
                    {
                        sanPhamChiTiet.SoLuong -= item.SoLuong;
                    }
                }

                _context.ChiTietGioHangs.RemoveRange(cartItems);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                HttpContext.Session.Remove("PendingOrder");

                TempData["SuccessMessage"] = "Thanh toán và đặt hàng thành công!";
                return RedirectToAction("LichSuDatHang", "GioHang");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi lưu đơn hàng: " + ex.Message;
                return RedirectToAction("Index", "GioHang");
            }
        }
    }
}
