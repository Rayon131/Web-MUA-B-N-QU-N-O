using DATNN.Models;
using DATNN.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Linq; // Thêm using này
using System.Threading.Tasks; // Thêm using này
using System.Collections.Generic; // Thêm using này
using Microsoft.AspNetCore.Http;
using System;
using AppView.Models.Service.VNPay;
using Newtonsoft.Json;
namespace DATNN.Controllers
{
    [Authorize(Roles = "khachhang")]
    public class GioHangController : Controller
    {
        private readonly MyDbContext _context;
        private readonly IVnPayService _vnPayService;
        private readonly IConfiguration _configuration;
        public GioHangController(MyDbContext context, IVnPayService vnPayService, IConfiguration configuration)
        {
            _context = context;
            _vnPayService = vnPayService;
            _configuration = configuration;
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProceedToCheckout(List<int> selectedItems)
        {
            if (selectedItems == null || !selectedItems.Any())
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất một sản phẩm.";
                return RedirectToAction("Index");
            }
            var itemsToCheck = await _context.ChiTietGioHangs
       .Include(ct => ct.SanPhamChiTiet).ThenInclude(sp => sp.SanPham)
       .Include(ct => ct.SanPhamChiTiet).ThenInclude(sp => sp.MauSac) // Để hiển thị chi tiết tên
       .Include(ct => ct.SanPhamChiTiet).ThenInclude(sp => sp.Size)
       .Where(ct => selectedItems.Contains(ct.MaChiTietGioHang))
       .ToListAsync();

            foreach (var item in itemsToCheck)
            {
                // 1. Kiểm tra nếu sản phẩm bị xóa hoặc ngừng kinh doanh
                if (item.SanPhamChiTiet == null || item.SanPhamChiTiet.SanPham == null || item.SanPhamChiTiet.SanPham.TrangThai != 1)
                {
                    TempData["ErrorMessage"] = $"Sản phẩm trong giỏ hàng không còn tồn tại hoặc đã ngừng kinh doanh.";
                    return RedirectToAction("Index"); // Quay lại trang Giỏ hàng
                }

                // 2. Kiểm tra số lượng tồn kho thực tế so với số lượng khách đặt
                // Ví dụ: Khách đặt 3, Kho còn 0 (hoặc < 3)
                if (item.SoLuong > item.SanPhamChiTiet.SoLuong)
                {
                    string tenSp = item.SanPhamChiTiet.SanPham.TenSanPham;
                    string chiTiet = $"{item.SanPhamChiTiet.MauSac.TenMau}/{item.SanPhamChiTiet.Size.TenSize}";
                    int tonKho = item.SanPhamChiTiet.SoLuong;

                    TempData["ErrorMessage"] = $"Sản phẩm '{tenSp}' ({chiTiet}) hiện không đủ hàng. Tồn kho còn lại: {tonKho}. Vui lòng cập nhật số lượng.";

                    // QUAN TRỌNG: Quay lại trang Index (Giỏ hàng) thay vì Detail hay tiếp tục DatHang
                    return RedirectToAction("Index");
                }
            }
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _context.NguoiDungs.FindAsync(int.Parse(userId));

            // ✅ Lấy danh sách địa chỉ người dùng
            var danhSachDiaChi = await _context.DiaChiNguoiDungs
        .Where(dc => dc.MaNguoiDung.ToString() == userId) // 1. Bỏ điều kiện TrangThai == 1 để lấy tất cả địa chỉ
        .OrderByDescending(dc => dc.TrangThai)           // 2. Sắp xếp để địa chỉ mặc định (TrangThai = 1) lên đầu
        .ToListAsync();

            var diaChiDuocChon = danhSachDiaChi.FirstOrDefault(); // Dòng này giờ sẽ chọn đúng địa chỉ mặc định từ danh sách đầy đủ

            var viewModel = new DatHangViewModel
            {
                SelectedCartItemIds = selectedItems,
                Email = currentUser.Email,
                DanhSachDiaChi = danhSachDiaChi
            };

            if (diaChiDuocChon != null)
            {
                ViewBag.SelectedAddressId = diaChiDuocChon.Id;
                viewModel.HoTenNguoiNhan = diaChiDuocChon.Ten;
                viewModel.SoDienThoaiNguoiNhan = diaChiDuocChon.SoDienThoai;
                viewModel.DiaChiGiaoHang = $"{diaChiDuocChon.ChiTietDiaChi}, {diaChiDuocChon.Phuong}, {diaChiDuocChon.Quan}, {diaChiDuocChon.ThanhPho}";

                var ketQuaShip = await TinhPhiVanChuyenAnToan(diaChiDuocChon.Id);
                viewModel.KhoangCachGiaoHang = ketQuaShip.khoangCach;
                viewModel.PhiVanChuyen = ketQuaShip.phiShip;

            }
            else
            {
                viewModel.HoTenNguoiNhan = currentUser.HoTen;
                viewModel.SoDienThoaiNguoiNhan = currentUser.SoDienThoai;
                viewModel.DiaChiGiaoHang = "";
                viewModel.PhiVanChuyen = 0;
                viewModel.KhoangCachGiaoHang = 0;
            }
          
            var cartViewModel = await BuildCartViewModelAsync(userId);
            if (cartViewModel == null)
            {
                TempData["ErrorMessage"] = "Giỏ hàng của bạn đã hết hạn.";
                return RedirectToAction("Index");
            }

            foreach (var selectedId in selectedItems)
            {
                var itemToPurchase = cartViewModel.Items.FirstOrDefault(i => i.ChiTietGioHang.MaChiTietGioHang == selectedId);
                if (itemToPurchase != null) viewModel.ItemsToPurchase.Add(itemToPurchase);
            }

            viewModel.TongTienHang = viewModel.ItemsToPurchase.Sum(i => i.ChiTietGioHang.SoLuong * i.GiaKhuyenMai);
            if (viewModel.PhiVanChuyen > 0)
            {
                decimal giamGiaShip = await TinhFreeshipTuDongAsync(viewModel.TongTienHang, viewModel.PhiVanChuyen);

                if (giamGiaShip > 0)
                {
                    // Lưu vào ViewBag để hiển thị thông báo ở View (VD: "Được giảm 30k ship")
                    ViewBag.GiamGiaShip = giamGiaShip;

                    // Trừ phí ship hiển thị (Không được < 0)
                    viewModel.PhiVanChuyen -= giamGiaShip;
                    if (viewModel.PhiVanChuyen < 0) viewModel.PhiVanChuyen = 0;
                }
            }
            var appliedVoucherCode = HttpContext.Session.GetString("AppliedVoucherCode");
            if (!string.IsNullOrEmpty(appliedVoucherCode))
            {
                var voucher = await _context.MaGiamGias.FirstOrDefaultAsync(v => v.MaCode == appliedVoucherCode);

                // Kiểm tra lại điều kiện voucher với tổng tiền hàng cuối cùng
                if (voucher != null && viewModel.TongTienHang >= (voucher.DieuKienDonHangToiThieu ?? 0))
                {
                    viewModel.AppliedVoucherCode = appliedVoucherCode;
                    viewModel.TienGiamGia = (voucher.LoaiGiamGia == "PhanTram")
                        ? viewModel.TongTienHang * (voucher.GiaTriGiamGia / 100m)
                        : voucher.GiaTriGiamGia;
                }

                // Xóa session sau khi đã sử dụng để tránh bị "dính" cho các lần sau
                HttpContext.Session.Remove("AppliedVoucherCode");
            }

            var now = DateTime.Now;
            viewModel.AvailableVouchers = await _context.MaGiamGias
                .Where(v => v.TrangThai == 1 &&
                v.LoaiApDung == "DonHang" &&
                            v.NgayBatDau <= now &&
                            v.NgayKetThuc >= now &&
                            (!v.TongLuotSuDungToiDa.HasValue || v.DaSuDung < v.TongLuotSuDungToiDa.Value) &&
                            (v.KenhApDung == "Online" || v.KenhApDung == "TatCa"))
                .ToListAsync();

            viewModel.TongThanhToan = viewModel.TongTienHang - viewModel.TienGiamGia + viewModel.PhiVanChuyen;
            ViewBag.IsBannedCOD = await CheckIsBannedCOD(userId);
            return View("DatHang", viewModel);
        }
        private double TinhKhoangCach(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371;
            double radLat1 = ToRadians(lat1);
            double radLat2 = ToRadians(lat2);
            double deltaLat = ToRadians(lat2 - lat1);
            double deltaLon = ToRadians(lon2 - lon1);

            double a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                       Math.Cos(radLat1) * Math.Cos(radLat2) *
                       Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return Math.Round(R * c, 2);
        }

        private double ToRadians(double angle) => angle * Math.PI / 180;

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BuyNowDirect(int sanPhamChiTietId, int soLuong)
        {
          
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }
            var oldAbandonedItems = await _context.ChiTietGioHangs
           .Where(ct => ct.GioHang.MaNguoiDung.ToString() == userId && ct.TrangThai == 2)
           .ToListAsync();

            if (oldAbandonedItems.Any())
            {
                _context.ChiTietGioHangs.RemoveRange(oldAbandonedItems);
                await _context.SaveChangesAsync();
            }
            var sanPhamChiTiet = await _context.SanPhamChiTiets
                .Include(spct => spct.SanPham)
                .Include(spct => spct.MauSac)
                .Include(spct => spct.Size)
                .FirstOrDefaultAsync(spct => spct.MaSanPhamChiTiet == sanPhamChiTietId);

            if (sanPhamChiTiet == null || sanPhamChiTiet.SoLuong < soLuong)
            {
                TempData["ErrorMessage"] = "Sản phẩm không tồn tại hoặc không đủ số lượng.";
                return RedirectToAction("Detail", "SanPhamChiTiet", new { id = sanPhamChiTiet?.MaSanPham ?? 0 });
            }

            // === BẮT ĐẦU SỬA ĐỔI LOGIC TÍNH KHUYẾN MÃI ===

            var allActivePromotions = await _context.KhuyenMais
     .Where(km => km.TrangThai == 1 &&
                    km.NgayBatDau <= DateTime.Now &&
                    km.NgayKetThuc >= DateTime.Now &&
                    !string.IsNullOrEmpty(km.DanhSachSanPhamApDung) &&
                    (km.KenhApDung == "Online" || km.KenhApDung == "TatCa")) // <-- THÊM DÒNG NÀY
     .ToListAsync();

            // 2. Tìm các khuyến mãi áp dụng cho sản phẩm này (cả theo sản phẩm cha và phiên bản con)
            var promotionsForThisVariant = allActivePromotions
                .Where(km => km.DanhSachSanPhamApDung.Split(',').Any(id => id == $"p-{sanPhamChiTiet.MaSanPham}" || id == $"v-{sanPhamChiTiet.MaSanPhamChiTiet}"))
                .ToList();
            // 1. Lấy cấu hình (Thêm dòng này)
            var promoSetting = await _context.SystemSettings.FindAsync("PromotionRule");
            string promoRule = promoSetting?.SettingValue ?? "BestValue";

            decimal giaCuoiCung = sanPhamChiTiet.GiaBan;

            if (promotionsForThisVariant.Any())
            {
                if (promoRule == "Stackable")
                {
                    // Logic cộng dồn
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
                    // Logic giá tốt nhất (Code cũ của bạn nằm ở đây)
                    giaCuoiCung = promotionsForThisVariant
                       .Select(p => CalculateProductDiscountedPrice(sanPhamChiTiet.GiaBan, p))
                       .Min();
                }
            }

            var currentUser = await _context.NguoiDungs.FindAsync(int.Parse(userId));
          
            // 1. Lấy đầy đủ danh sách địa chỉ và sắp xếp
            var danhSachDiaChi = await _context.DiaChiNguoiDungs
                .Where(dc => dc.MaNguoiDung.ToString() == userId)
                .OrderByDescending(dc => dc.TrangThai)
                .ToListAsync();

            var diaChiDuocChon = danhSachDiaChi.FirstOrDefault();

            var viewModel = new DatHangViewModel
            {
                Email = currentUser.Email,
                DanhSachDiaChi = danhSachDiaChi // Cung cấp danh sách cho ComboBox
            };

            // 2. Bổ sung logic tính phí vận chuyển (sao chép từ ProceedToCheckout)
            if (diaChiDuocChon != null)
            {
                ViewBag.SelectedAddressId = diaChiDuocChon.Id;
                viewModel.HoTenNguoiNhan = diaChiDuocChon.Ten;
                viewModel.SoDienThoaiNguoiNhan = diaChiDuocChon.SoDienThoai;
                viewModel.DiaChiGiaoHang = $"{diaChiDuocChon.ChiTietDiaChi}, {diaChiDuocChon.Phuong}, {diaChiDuocChon.Quan}, {diaChiDuocChon.ThanhPho}";

                var ketQuaShip = await TinhPhiVanChuyenAnToan(diaChiDuocChon.Id);
                viewModel.KhoangCachGiaoHang = ketQuaShip.khoangCach;
                viewModel.PhiVanChuyen = ketQuaShip.phiShip;
                decimal tongTienHangTamTinh = soLuong * giaCuoiCung;

                // Gọi hàm tính mức giảm giá ship
                decimal giamGiaShip = await TinhFreeshipTuDongAsync(tongTienHangTamTinh, viewModel.PhiVanChuyen);

                if (giamGiaShip > 0)
                {
                    ViewBag.GiamGiaShip = giamGiaShip; // Để hiển thị thông báo ra View nếu cần
                    viewModel.PhiVanChuyen -= giamGiaShip; // Trừ tiền ship

                    // Đảm bảo không âm
                    if (viewModel.PhiVanChuyen < 0) viewModel.PhiVanChuyen = 0;
                }
            }
            else // Trường hợp người dùng chưa có địa chỉ nào
            {
                viewModel.HoTenNguoiNhan = currentUser.HoTen;
                viewModel.SoDienThoaiNguoiNhan = currentUser.SoDienThoai;
                viewModel.DiaChiGiaoHang = "";
                viewModel.PhiVanChuyen = 0;
                viewModel.KhoangCachGiaoHang = 0;
            }

            // Phần thêm sản phẩm vào ViewModel (giữ nguyên)
            var fakeCartItem = new ChiTietGioHang
            {
                MaSanPhamChiTiet = sanPhamChiTiet.MaSanPhamChiTiet,
                SoLuong = soLuong,
                SanPhamChiTiet = sanPhamChiTiet
            };
            viewModel.ItemsToPurchase.Add(new ChiTietGioHangViewModel { ChiTietGioHang = fakeCartItem, GiaKhuyenMai = giaCuoiCung });

            // Phần tạo ChiTietGioHang tạm thời (giữ nguyên)
            var gioHang = await _context.GioHangs.FirstOrDefaultAsync(g => g.MaNguoiDung.ToString() == userId && g.TrangThai == 1);
            if (gioHang == null)
            {
                gioHang = new GioHang { MaNguoiDung = int.Parse(userId), TrangThai = 1 };
                _context.GioHangs.Add(gioHang);
                await _context.SaveChangesAsync();
            }
            var tempCartDetail = new ChiTietGioHang
            {
                MaGioHang = gioHang.MaGioHang,
                MaSanPhamChiTiet = sanPhamChiTietId,
                SoLuong = soLuong,
                TrangThai = 2 // Trạng thái "Mua ngay"
            };
            _context.ChiTietGioHangs.Add(tempCartDetail);
            await _context.SaveChangesAsync();

            viewModel.SelectedCartItemIds = new List<int> { tempCartDetail.MaChiTietGioHang };

            decimal subTotal = soLuong * giaCuoiCung;
            viewModel.TongTienHang = subTotal;

            // 3. Lấy danh sách mã giảm giá
            var now = DateTime.Now;
            viewModel.AvailableVouchers = await _context.MaGiamGias
                .Where(v => v.TrangThai == 1 &&
                v.LoaiApDung == "DonHang" &&
                            v.NgayBatDau <= now &&
                            v.NgayKetThuc >= now &&
                            (!v.TongLuotSuDungToiDa.HasValue || v.DaSuDung < v.TongLuotSuDungToiDa.Value) &&
                            (v.KenhApDung == "Online" || v.KenhApDung == "TatCa"))
                .ToListAsync();

            // 4. Cập nhật lại tổng thanh toán cuối cùng (đã có phí ship)
            viewModel.TongThanhToan = viewModel.TongTienHang + viewModel.PhiVanChuyen;
            ViewBag.IsBannedCOD = await CheckIsBannedCOD(userId);
            return View("DatHang", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DatHang(DatHangViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || model.SelectedCartItemIds == null || !model.SelectedCartItemIds.Any())
            {
                TempData["ErrorMessage"] = "Phiên làm việc đã hết hạn. Vui lòng thử lại.";
                return RedirectToAction("Index");
            }

            if (model.PhuongThucThanhToan == "COD" && await CheckIsBannedCOD(userId))
            {
                TempData["ErrorMessage"] = "Tài khoản của bạn bị hạn chế thanh toán COD do lịch sử giao dịch bất thường. Vui lòng sử dụng VNPay.";
                return RedirectToAction("Index");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Lấy items từ DB (bao gồm cả trạng thái 1-Giỏ hàng và 2-Mua ngay)
                var cartItems = await _context.ChiTietGioHangs
                    .Include(c => c.SanPhamChiTiet).ThenInclude(spct => spct.SanPham)
                    .Include(c => c.SanPhamChiTiet).ThenInclude(spct => spct.MauSac)
                    .Include(c => c.SanPhamChiTiet).ThenInclude(spct => spct.Size)
                    .Where(c => model.SelectedCartItemIds.Contains(c.MaChiTietGioHang) && c.GioHang.MaNguoiDung.ToString() == userId)
                    .ToListAsync();

                if (!cartItems.Any()) throw new Exception("Không tìm thấy sản phẩm nào để thanh toán.");

                // 2. Lấy cấu hình khuyến mãi
                var promoSetting = await _context.SystemSettings.FindAsync("PromotionRule");
                string promoRule = promoSetting?.SettingValue ?? "BestValue";

                var activeProductPromotions = await _context.KhuyenMais
                    .Where(km => km.TrangThai == 1 && km.NgayBatDau <= DateTime.Now && km.NgayKetThuc >= DateTime.Now &&
                                 !string.IsNullOrEmpty(km.DanhSachSanPhamApDung) && (km.KenhApDung == "Online" || km.KenhApDung == "TatCa"))
                    .ToListAsync();

                // 3. TÍNH TOÁN GIÁ CHUẨN (STACKABLE / BEST VALUE)
                // Dictionary để lưu giá đã tính cho từng item, dùng để lưu vào DonHangChiTiet
                var finalPrices = new Dictionary<int, decimal>();

                foreach (var item in cartItems)
                {
                    // Check tồn kho
                    if (item.SanPhamChiTiet.SoLuong < item.SoLuong)
                        throw new Exception($"Sản phẩm '{item.SanPhamChiTiet.SanPham.TenSanPham}' không đủ số lượng tồn kho.");

                    decimal giaGoc = item.SanPhamChiTiet.GiaBan;
                    decimal giaCuoiCung = giaGoc;

                    var promotionsForThisVariant = activeProductPromotions
                        .Where(km => km.DanhSachSanPhamApDung.Split(',').Any(id => id == $"p-{item.SanPhamChiTiet.MaSanPham}" || id == $"v-{item.SanPhamChiTiet.MaSanPhamChiTiet}"))
                        .ToList();

                    if (promotionsForThisVariant.Any())
                    {
                        // Gọi hàm tính giá chung (nếu bạn đã tách hàm như tôi gợi ý)
                        // Hoặc viết lại logic if/else Stackable tại đây
                        if (promoRule == "Stackable")
                        {
                            var sortedPromos = promotionsForThisVariant.OrderBy(p => p.LoaiGiamGia).ThenBy(p => p.MaKhuyenMai).ToList();
                            foreach (var promo in sortedPromos)
                            {
                                if (promo.LoaiGiamGia == "PhanTram") giaCuoiCung -= giaCuoiCung * (promo.GiaTriGiamGia / 100);
                                else if (promo.LoaiGiamGia == "SoTien") giaCuoiCung -= promo.GiaTriGiamGia;
                                if (giaCuoiCung < 0) giaCuoiCung = 0;
                            }
                        }
                        else
                        {
                            giaCuoiCung = promotionsForThisVariant.Select(p => CalculateProductDiscountedPrice(giaGoc, p)).Min();
                        }
                    }
                    finalPrices.Add(item.MaChiTietGioHang, giaCuoiCung);
                }

                // 4. Tính tổng tiền hàng
                decimal tongTienHang = cartItems.Sum(i => i.SoLuong * finalPrices[i.MaChiTietGioHang]);
                int? addressId = model.SelectedAddressId;

                // 1. Tính PHÍ SHIP GỐC (Lúc chưa giảm - Ví dụ sẽ ra 50,000đ)
                var ketQuaShip = await TinhPhiVanChuyenAnToan(addressId);
                decimal phiShipGoc = ketQuaShip.phiShip;

                // 2. Tính mức FREESHIP được giảm dựa trên PHÍ GỐC (Ví dụ giảm 10,000đ)
                decimal giamGiaShip = await TinhFreeshipTuDongAsync(tongTienHang, phiShipGoc);

                // 3. Phí ship thực tế lưu vào đơn hàng (50k - 10k = 40k) -> ĐÚNG VỚI GIAO DIỆN
                decimal phiShipThucTe = phiShipGoc - giamGiaShip;
                if (phiShipThucTe < 0) phiShipThucTe = 0;
                // 5. Tính Voucher
                decimal tienGiamGiaVoucher = 0;
                MaGiamGia appliedVoucher = null;
                if (!string.IsNullOrEmpty(model.AppliedVoucherCode))
                {
                    appliedVoucher = await _context.MaGiamGias.FirstOrDefaultAsync(v => v.MaCode == model.AppliedVoucherCode);
                    if (appliedVoucher != null && tongTienHang >= (appliedVoucher.DieuKienDonHangToiThieu ?? 0))
                    {
                        // === KIỂM TRA LẠI LẦN CUỐI XEM CÒN LƯỢT KHÔNG ===
                        if (appliedVoucher.TongLuotSuDungToiDa.HasValue && appliedVoucher.DaSuDung >= appliedVoucher.TongLuotSuDungToiDa.Value)
                        {
                            throw new Exception("Mã giảm giá đã hết lượt sử dụng trong lúc bạn đang thao tác.");
                        }

                        // === THÊM CODE CẬP NHẬT SỐ LƯỢNG ===
                        appliedVoucher.DaSuDung++;
                        _context.Update(appliedVoucher);
                        // ===================================

                        tienGiamGiaVoucher = (appliedVoucher.LoaiGiamGia == "PhanTram")
                            ? tongTienHang * (appliedVoucher.GiaTriGiamGia / 100m)
                            : appliedVoucher.GiaTriGiamGia;
                    }
                }

                // 6. Lưu Đơn Hàng
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
                    MaKhuyenMai = null, // Có thể lưu chuỗi JSON các KM đã dùng nếu muốn
                    SoTienDuocGiam = tienGiamGiaVoucher,
                    TongTien = tongTienHang - tienGiamGiaVoucher + phiShipThucTe,
                    TrangThaiDonHang = 2,
                    PhuongThucThanhToan = model.PhuongThucThanhToan,
                    TrangThaiThanhToan = 1,
                    PhiVanChuyen = phiShipThucTe
                };

                _context.DonHangs.Add(donHang);
                await _context.SaveChangesAsync();

                // 7. Lưu Chi Tiết Đơn Hàng (Sử dụng giá đã tính trong finalPrices)
                foreach (var item in cartItems)
                {
                    var sanPhamChiTiet = item.SanPhamChiTiet;
                    sanPhamChiTiet.SoLuong -= item.SoLuong; // Trừ tồn kho

                    var donHangChiTiet = new DonHangChiTiet
                    {
                        MaDonHang = donHang.MaDonHang,
                        MaSanPhamChiTiet = item.MaSanPhamChiTiet,
                        SoLuong = item.SoLuong,
                        // QUAN TRỌNG: Lấy giá từ Dictionary đã tính toán (đã cộng dồn)
                        DonGia = finalPrices[item.MaChiTietGioHang],

                        TenSanPham_Luu = sanPhamChiTiet.SanPham?.TenSanPham ?? "SP",
                        TenMau_Luu = sanPhamChiTiet.MauSac?.TenMau ?? "N/A",
                        TenSize_Luu = sanPhamChiTiet.Size?.TenSize ?? "N/A",
                        HinhAnh_Luu = sanPhamChiTiet.HinhAnh ?? sanPhamChiTiet.SanPham?.AnhSanPham
                    };
                    _context.DonHangChiTiets.Add(donHangChiTiet);
                }

                // 8. Xóa giỏ hàng
                _context.ChiTietGioHangs.RemoveRange(cartItems);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "Đặt hàng thành công!";
                return RedirectToAction("LichSuDatHang");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToAction("Index");
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        // 1. Đổi thành async Task vì phải gọi Database
        public async Task<IActionResult> TaoThanhToanVnPay(DatHangViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (ModelState.IsValid)
            {
                // === BẮT ĐẦU TÍNH TOÁN LẠI (SECURE CALCULATION) ===

                // 1. Gọi lại hàm BuildCartViewModelAsync. 
                // Hàm này đã chứa logic check "Stackable" vs "BestValue" mà bạn vừa sửa.
                // Nó sẽ trả về giá chuẩn xác nhất từ Database.
                var cartViewModel = await BuildCartViewModelAsync(userId);

                if (cartViewModel == null)
                {
                    TempData["ErrorMessage"] = "Giỏ hàng lỗi.";
                    return RedirectToAction("Index");
                }

                // 2. Lọc ra những sản phẩm khách chọn mua (để tính tổng tiền hàng)
                // Lưu ý: Cần xử lý trường hợp "Mua ngay" (temp items) nếu logic build cart của bạn có support
                var itemsToPurchase = cartViewModel.Items
                    .Where(i => model.SelectedCartItemIds.Contains(i.ChiTietGioHang.MaChiTietGioHang))
                    .ToList();
                if (!itemsToPurchase.Any())
                {
                    // 1. Lấy items từ DB dựa trên ID được chọn (bao gồm cả trạng thái 2)
                    var cartItems = await _context.ChiTietGioHangs
                       .Include(c => c.SanPhamChiTiet).ThenInclude(spct => spct.SanPham)
                       .Include(c => c.SanPhamChiTiet).ThenInclude(spct => spct.MauSac)
                       .Include(c => c.SanPhamChiTiet).ThenInclude(spct => spct.Size)
                       .Where(c => model.SelectedCartItemIds.Contains(c.MaChiTietGioHang) && c.GioHang.MaNguoiDung.ToString() == userId)
                       .ToListAsync();

                    // 2. Lấy cấu hình khuyến mãi & Danh sách khuyến mãi active
                    var promoSetting = await _context.SystemSettings.FindAsync("PromotionRule");
                    string promoRule = promoSetting?.SettingValue ?? "BestValue";

                    var activeProductPromotions = await _context.KhuyenMais
                        .Where(km => km.TrangThai == 1 && km.NgayBatDau <= DateTime.Now && km.NgayKetThuc >= DateTime.Now &&
                                     !string.IsNullOrEmpty(km.DanhSachSanPhamApDung) && (km.KenhApDung == "Online" || km.KenhApDung == "TatCa"))
                        .ToListAsync();

                    // 3. Tính toán giá cho từng item thủ công
                    foreach (var item in cartItems)
                    {
                        decimal giaGoc = item.SanPhamChiTiet.GiaBan;
                        decimal giaCuoiCung = giaGoc;

                        var promotionsForThisVariant = activeProductPromotions
                            .Where(km => km.DanhSachSanPhamApDung.Split(',').Any(id => id == $"p-{item.SanPhamChiTiet.MaSanPham}" || id == $"v-{item.SanPhamChiTiet.MaSanPhamChiTiet}"))
                            .ToList();

                        if (promotionsForThisVariant.Any())
                        {
                            if (promoRule == "Stackable")
                            {
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
                                giaCuoiCung = promotionsForThisVariant
                                    .Select(p => CalculateProductDiscountedPrice(giaGoc, p))
                                    .Min();
                            }
                        }

                        // Tạo ViewModel giả lập để thêm vào danh sách tính tổng
                        itemsToPurchase.Add(new ChiTietGioHangViewModel
                        {
                            ChiTietGioHang = item,
                            GiaKhuyenMai = giaCuoiCung
                        });
                    }
                }

                // 3. Tính Tổng tiền hàng chuẩn (Server side)
                decimal tongTienHangChuan = itemsToPurchase.Sum(i => i.ChiTietGioHang.SoLuong * i.GiaKhuyenMai);

                // 4. Tính lại Voucher (Server side)
                decimal tienGiamGiaVoucher = 0;
                if (!string.IsNullOrEmpty(model.AppliedVoucherCode))
                {
                    var voucher = await _context.MaGiamGias.FirstOrDefaultAsync(v => v.MaCode == model.AppliedVoucherCode);
                    // Check điều kiện voucher
                    if (voucher != null &&
                        voucher.TrangThai == 1 &&
                        tongTienHangChuan >= (voucher.DieuKienDonHangToiThieu ?? 0))
                    {
                        tienGiamGiaVoucher = (voucher.LoaiGiamGia == "PhanTram")
                            ? tongTienHangChuan * (voucher.GiaTriGiamGia / 100m)
                            : voucher.GiaTriGiamGia;
                    }
                }

                // 5. Tính lại phí Ship (Optional - có thể tin tưởng model hoặc tính lại geo distance)
                // Để đơn giản ta dùng lại model.PhiVanChuyen, nhưng tốt nhất là nên tính lại.
                decimal phiVanChuyen = model.PhiVanChuyen;

                // 6. CHỐT SỐ TIỀN CUỐI CÙNG
                var finalTotal = tongTienHangChuan - tienGiamGiaVoucher + phiVanChuyen;

                // Gán ngược lại vào model để lưu session và gửi sang VNPay
                model.TongTienHang = tongTienHangChuan;
                model.TienGiamGia = tienGiamGiaVoucher;
                model.TongThanhToan = finalTotal;

                // === KẾT THÚC TÍNH TOÁN LẠI ===

                // Lưu thông tin đơn hàng tạm thời vào Session
                HttpContext.Session.SetString("PendingOrder", JsonConvert.SerializeObject(model));

                var returnUrl = Url.Action("PaymentCallBack", "Payment", null, Request.Scheme);
                var orderId = $"{DateTime.Now.Ticks}_{Guid.NewGuid().ToString("N").Substring(0, 4)}";

                var paymentUrl = _vnPayService.CreatePaymentUrl(new PaymentInformationModel
                {
                    OrderId = orderId,
                    Amount = (double)model.TongThanhToan, // Số tiền này giờ đã CHÍNH XÁC theo logic cộng dồn
                    Name = model.HoTenNguoiNhan,
                    OrderDescription = $"Thanh toan don hang cho {model.HoTenNguoiNhan}"
                }, HttpContext, returnUrl);

                return Redirect(paymentUrl);
            }

            TempData["ErrorMessage"] = "Thông tin đặt hàng không hợp lệ.";
            return View("DatHang", model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThayDoiDiaChiGiaoHang(int orderId, int addressId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 1. Tìm đơn hàng và đảm bảo nó thuộc về người dùng
            var donHang = await _context.DonHangs
                .FirstOrDefaultAsync(d => d.MaDonHang == orderId && d.MaKhachHang.ToString() == userId);

            if (donHang == null)
            {
                return Forbid(); // Không cho phép sửa đơn hàng của người khác
            }

            // 2. Kiểm tra xem đơn hàng có ở trạng thái được phép thay đổi không
            bool isCodPending = donHang.TrangThaiDonHang == 2 && donHang.PhuongThucThanhToan == "COD";
            bool isVnPayPaidAndPending = donHang.TrangThaiDonHang == 7 && donHang.PhuongThucThanhToan == "VnPay";

            if (!isCodPending && !isVnPayPaidAndPending) // Chỉ cho phép thay đổi ở 2 trạng thái này
            {
                TempData["ErrorMessage"] = "Đơn hàng đã được xử lý, không thể thay đổi địa chỉ.";
                return RedirectToAction("ChiTietDonHang", new { id = orderId });
            }

            // 3. Tìm địa chỉ được chọn
            var selectedAddress = await _context.DiaChiNguoiDungs
                .FirstOrDefaultAsync(a => a.Id == addressId && a.MaNguoiDung.ToString() == userId);

            if (selectedAddress == null)
            {
                TempData["ErrorMessage"] = "Địa chỉ được chọn không hợp lệ.";
                return RedirectToAction("ChiTietDonHang", new { id = orderId });
            }

            // ===================================================================
            // === BẮT ĐẦU LOGIC TÍNH TOÁN LẠI PHÍ VẬN CHUYỂN VÀ TỔNG TIỀN ===
            // ===================================================================

            if (donHang.PhuongThucThanhToan == "COD")
            {
                // Gọi hàm an toàn (trả về 30k nếu lỗi, hoặc phí chuẩn nếu tính được)
                var ketQuaShip = await TinhPhiVanChuyenAnToan(addressId);

                decimal phiVanChuyenMoi = ketQuaShip.phiShip;

                // Tính toán lại tổng tiền cuối cùng của đơn hàng
                decimal phiVanChuyenCu = donHang.PhiVanChuyen ?? 0;
                donHang.TongTien = (donHang.TongTien - phiVanChuyenCu) + phiVanChuyenMoi;
                donHang.PhiVanChuyen = phiVanChuyenMoi;
            }

            // 4. Cập nhật thông tin người nhận và địa chỉ cho cả 2 loại đơn
            donHang.HoTenNguoiNhan = selectedAddress.Ten;
            donHang.SoDienThoai = selectedAddress.SoDienThoai;
            donHang.DiaChi = $"{selectedAddress.ChiTietDiaChi}, {selectedAddress.Phuong}, {selectedAddress.Quan}, {selectedAddress.ThanhPho}";

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã cập nhật địa chỉ giao hàng thành công!";
            return RedirectToAction("ChiTietDonHang", new { id = orderId });
        }
        public async Task<IActionResult> LichSuDatHang()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var danhSachDonHang = await _context.DonHangs
                .Where(d => d.MaKhachHang.ToString() == userId && d.TrangThaiThanhToan == 1)
                // THÊM CÁC DÒNG INCLUDE DƯỚI ĐÂY
                .Include(d => d.DonHangChiTiets)
                    .ThenInclude(ct => ct.SanPhamChiTiet)
                        .ThenInclude(spct => spct.SanPham)
                .OrderByDescending(d => d.ThoiGianTao)
                .ToListAsync();

            return View(danhSachDonHang);
        }

        // ==========================================================
        // === ACTION CHI TIẾT ĐƠN HÀNG (ĐÃ CẬP NHẬT) ===
        // ==========================================================
        public async Task<IActionResult> ChiTietDonHang(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var donHang = await _context.DonHangs
                .Include(d => d.KhachHang)
                    .ThenInclude(kh => kh.DiaChiNguoiDungs)
                .Include(d => d.DonHangChiTiets)
                    .ThenInclude(ct => ct.SanPhamChiTiet)
                        .ThenInclude(spct => spct.SanPham)
                .Include(d => d.DonHangChiTiets)
                    .ThenInclude(ct => ct.SanPhamChiTiet)
                        .ThenInclude(spct => spct.MauSac)
                .Include(d => d.DonHangChiTiets)
                    .ThenInclude(ct => ct.SanPhamChiTiet)
                        .ThenInclude(spct => spct.Size)
                .FirstOrDefaultAsync(d => d.MaDonHang == id && d.MaKhachHang.ToString() == userId);

            if (donHang == null)
            {
                return Forbid();
            }

            // ✅ Lấy chính sách có ID = 3
            var chinhSachs = await _context.ChinhSaches
                .Where(cs => cs.id == 3)
                .ToListAsync();

            // ✅ Gói vào ViewModel
            var vm = new ChiTietDonHangViewModel
            {
                DonHang = donHang,
                ChinhSachs = chinhSachs
            };

            return View(vm);
        }


        // ==========================================================
        // === ACTION MỚI: KHÁCH HÀNG TỰ HỦY ĐƠN ===
        // ==========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Sửa lại signature để nhận thêm `lyDoHuy`
        public async Task<IActionResult> KhachHuyDonHang(int id, string lyDoHuy)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var donHang = await _context.DonHangs
                .Include(d => d.DonHangChiTiets)
                .FirstOrDefaultAsync(d => d.MaDonHang == id && d.MaKhachHang.ToString() == userId);

            if (donHang == null)
            {
                return Forbid();
            }

            if (donHang.TrangThaiDonHang == 1 || donHang.TrangThaiDonHang == 2)
            {
                donHang.TrangThaiDonHang = 6; // 6 là trạng thái Hủy

                // THÊM MỚI: LƯU LÝ DO CỦA KHÁCH HÀNG
                donHang.LyDoHuy = string.IsNullOrWhiteSpace(lyDoHuy)
                    ? "Khách hàng tự hủy không nêu lý do."
                    : lyDoHuy;

                // Cộng lại số lượng sản phẩm vào kho
                foreach (var chiTiet in donHang.DonHangChiTiets)
                {
                    var sanPhamChiTiet = await _context.SanPhamChiTiets.FindAsync(chiTiet.MaSanPhamChiTiet);
                    if (sanPhamChiTiet != null)
                    {
                        sanPhamChiTiet.SoLuong += chiTiet.SoLuong;
                    }
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã hủy đơn hàng thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Đơn hàng đã được xử lý, không thể hủy.";
            }

            return RedirectToAction("ChiTietDonHang", new { id = id });
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Xóa các sản phẩm "Mua ngay" (TrangThai=2) còn sót lại
            var abandonedBuyNowItems = await _context.ChiTietGioHangs
                .Where(ct => ct.GioHang.MaNguoiDung.ToString() == userId && ct.TrangThai == 2)
                .ToListAsync();

            if (abandonedBuyNowItems.Any())
            {
                try
                {
                    _context.ChiTietGioHangs.RemoveRange(abandonedBuyNowItems);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Nếu gặp lỗi này nghĩa là dữ liệu đã bị xóa bởi request khác rồi.
                    // Ta chỉ cần clear ChangeTracker để không ảnh hưởng các lệnh sau.
                    var entry = _context.ChangeTracker.Entries()
                                .Where(e => e.State == EntityState.Deleted);
                    foreach (var e in entry)
                    {
                        e.State = EntityState.Detached;
                    }
                }
            }

            var viewModel = await BuildCartViewModelAsync(userId);
            if (viewModel == null) return View("EmptyCart");

            var now = DateTime.Now;
            ViewBag.AvailableVouchers = await _context.MaGiamGias
                .Where(v => v.TrangThai == 1 &&
                v.LoaiApDung == "DonHang" &&
                                v.NgayBatDau <= now &&
                                v.NgayKetThuc >= now &&
                                (!v.TongLuotSuDungToiDa.HasValue || v.DaSuDung < v.TongLuotSuDungToiDa.Value) &&
                                (v.KenhApDung == "Online" || v.KenhApDung == "TatCa"))
                .ToListAsync();

            return View(viewModel);
        }



        // Thêm hàm helper này vào controller nếu chưa có
        private decimal CalculateProductDiscountedPrice(decimal originalPrice, KhuyenMai promotion)
        {
            if (promotion.LoaiGiamGia == "PhanTram")
            {
                return originalPrice * (1 - (promotion.GiaTriGiamGia / 100));
            }
            if (promotion.LoaiGiamGia == "SoTien")
            {
                var newPrice = originalPrice - promotion.GiaTriGiamGia;
                return newPrice > 0 ? newPrice : 0;
            }
            return originalPrice;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int sanPhamChiTietId, int soLuong)
        {
           

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập." });
            }

            // Lấy giỏ hàng và các chi tiết bên trong để tính toán
            var gioHang = await _context.GioHangs
                .Include(g => g.ChiTietGioHangs)
                .FirstOrDefaultAsync(g => g.MaNguoiDung.ToString() == userId && g.TrangThai == 1);

            if (gioHang == null)
            {
                gioHang = new GioHang
                {
                    MaNguoiDung = int.Parse(userId),
                    TrangThai = 1,
                    // QUAN TRỌNG: Khởi tạo collection để nó không bị null
                    ChiTietGioHangs = new List<ChiTietGioHang>()
                };
                _context.GioHangs.Add(gioHang);
            }

            var chiTietTrongGio = gioHang.ChiTietGioHangs.FirstOrDefault(ct => ct.MaSanPhamChiTiet == sanPhamChiTietId);

          
            var sanPhamChiTiet = await _context.SanPhamChiTiets.FindAsync(sanPhamChiTietId);
            if (sanPhamChiTiet == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại." });
            }

            if (chiTietTrongGio != null)
            {
                if (sanPhamChiTiet.SoLuong < chiTietTrongGio.SoLuong + soLuong)
                {
                    return Json(new { success = false, message = "Số lượng tồn kho không đủ." });
                }
                chiTietTrongGio.SoLuong += soLuong;
            }
            else
            {
                if (sanPhamChiTiet.SoLuong < soLuong)
                {
                    return Json(new { success = false, message = "Số lượng tồn kho không đủ." });
                }
                var chiTietMoi = new ChiTietGioHang
                {
                    MaSanPhamChiTiet = sanPhamChiTietId,
                    SoLuong = soLuong,
                    TrangThai = 1
                };
                gioHang.ChiTietGioHangs.Add(chiTietMoi);
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã cập nhật giỏ hàng thành công!" });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int chiTietGioHangId, int newQuantity)
        {
            

            if (newQuantity <= 0)
            {
                return await RemoveFromCart(chiTietGioHangId);
            }

          

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Json(new { success = false, message = "Vui lòng đăng nhập." });

            var chiTietGioHang = await _context.ChiTietGioHangs
                .Include(ct => ct.GioHang)
                .Include(ct => ct.SanPhamChiTiet)
                .FirstOrDefaultAsync(ct => ct.MaChiTietGioHang == chiTietGioHangId);

            if (chiTietGioHang == null || chiTietGioHang.GioHang.MaNguoiDung.ToString() != userId)
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ hàng." });
            }

            // Kiểm tra tồn kho (giữ nguyên)
            if (chiTietGioHang.SanPhamChiTiet.SoLuong < newQuantity)
            {
                return Json(new { success = false, message = $"Số lượng tồn kho không đủ. Chỉ còn lại {chiTietGioHang.SanPhamChiTiet.SoLuong} sản phẩm." });
            }

            // Cập nhật số lượng
            chiTietGioHang.SoLuong = newQuantity;
            await _context.SaveChangesAsync();

            // Tính toán lại toàn bộ giỏ hàng và trả về thông tin mới (giữ nguyên)
            var updatedCart = await BuildCartViewModelAsync(userId);

            // Tìm lại item cụ thể để tính itemTotal
            var updatedItem = updatedCart.Items.FirstOrDefault(i => i.ChiTietGioHang.MaChiTietGioHang == chiTietGioHangId);
            decimal itemTotal = (updatedItem != null) ? (newQuantity * updatedItem.GiaKhuyenMai) : 0;


            return Json(new
            {
                success = true,
                message = "Đã cập nhật số lượng.",
                itemTotal = itemTotal.ToString("N0"), // Trả về tổng tiền của item đã được cập nhật
                subTotal = updatedCart.TongTienHang.ToString("N0"),
                total = updatedCart.TongTienThanhToan.ToString("N0")
            });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromCart(int chiTietGioHangId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId)) return Json(new { success = false, message = "Vui lòng đăng nhập." });

                var chiTietGioHang = await _context.ChiTietGioHangs
                    .Include(ct => ct.GioHang)
                    .FirstOrDefaultAsync(ct => ct.MaChiTietGioHang == chiTietGioHangId);

                if (chiTietGioHang == null || chiTietGioHang.GioHang.MaNguoiDung.ToString() != userId)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ hàng." });
                }

                _context.ChiTietGioHangs.Remove(chiTietGioHang);
                await _context.SaveChangesAsync();

                // Lấy lại thông tin giỏ hàng đã được cập nhật
                var updatedCart = await BuildCartViewModelAsync(userId);

                return Json(new
                {
                    success = true,
                    message = "Đã xóa sản phẩm khỏi giỏ hàng.",
                    tongTienHang = updatedCart?.TongTienHang.ToString("N0") ?? "0",
                    tongTienThanhToan = updatedCart?.TongTienThanhToan.ToString("N0") ?? "0",
                    soLuongSanPham = updatedCart?.Items.Count ?? 0
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Đã xảy ra lỗi hệ thống." });
            }
        }

        // ==========================================================
        // == PHƯƠNG THỨC PRIVATE TÁI SỬ DỤNG ==
        // ==========================================================

        // Hàm này chịu trách nhiệm lấy giỏ hàng và tính toán tất cả các loại giá
        private async Task<GioHangViewModel> BuildCartViewModelAsync(string userId)
        {
            var gioHang = await _context.GioHangs
                .Include(g => g.ChiTietGioHangs).ThenInclude(ct => ct.SanPhamChiTiet).ThenInclude(spct => spct.SanPham)
                .Include(g => g.ChiTietGioHangs).ThenInclude(ct => ct.SanPhamChiTiet).ThenInclude(spct => spct.MauSac)
                .Include(g => g.ChiTietGioHangs).ThenInclude(ct => ct.SanPhamChiTiet).ThenInclude(spct => spct.Size)
                .FirstOrDefaultAsync(g => g.MaNguoiDung.ToString() == userId && g.TrangThai == 1);

            if (gioHang == null || !gioHang.ChiTietGioHangs.Any()) return null;

            var viewModel = new GioHangViewModel();
            decimal tongTienHangDaGiam = 0;

            var activeProductPromotions = await _context.KhuyenMais
                .Where(km => km.TrangThai == 1 && km.NgayBatDau <= DateTime.Now && km.NgayKetThuc >= DateTime.Now &&
                             !string.IsNullOrEmpty(km.DanhSachSanPhamApDung) && (km.KenhApDung == "Online" || km.KenhApDung == "TatCa"))
                .ToListAsync();

            var promoSetting = await _context.SystemSettings.FindAsync("PromotionRule");
            string promoRule = promoSetting?.SettingValue ?? "BestValue"; // Mặc định là BestValue

            foreach (var chiTiet in gioHang.ChiTietGioHangs)
            {
                decimal giaGoc = chiTiet.SanPhamChiTiet.GiaBan;
                decimal giaCuoiCung = giaGoc;

                var promotionsForThisVariant = activeProductPromotions
                    .Where(km => km.DanhSachSanPhamApDung.Split(',').Any(id => id == $"p-{chiTiet.SanPhamChiTiet.MaSanPham}" || id == $"v-{chiTiet.SanPhamChiTiet.MaSanPhamChiTiet}"))
                    .ToList();

                if (promotionsForThisVariant.Any())
                {
                    // === BẮT ĐẦU LOGIC CHUYỂN ĐỔI ===
                    if (promoRule == "Stackable")
                    {
                        // TRƯỜNG HỢP 1: CỘNG DỒN (LŨY KẾ)
                        // Sắp xếp để trừ % trước, trừ tiền sau (để nhất quán)
                        var sortedPromos = promotionsForThisVariant
                            .OrderBy(p => p.LoaiGiamGia)
                            .ThenBy(p => p.MaKhuyenMai).ToList();

                        foreach (var promo in sortedPromos)
                        {
                            if (promo.LoaiGiamGia == "PhanTram")
                            {
                                giaCuoiCung -= giaCuoiCung * (promo.GiaTriGiamGia / 100);
                            }
                            else if (promo.LoaiGiamGia == "SoTien")
                            {
                                giaCuoiCung -= promo.GiaTriGiamGia;
                            }
                            if (giaCuoiCung < 0) giaCuoiCung = 0;
                        }
                    }
                    else
                    {
                        // TRƯỜNG HỢP 2: GIÁ TỐT NHẤT (Mặc định)
                        // Tính thử tất cả các KM, lấy cái nào cho ra giá thấp nhất
                        giaCuoiCung = promotionsForThisVariant
                            .Select(p => CalculateProductDiscountedPrice(giaGoc, p))
                            .Min();
                    }
                    // === KẾT THÚC LOGIC ===
                }

                viewModel.Items.Add(new ChiTietGioHangViewModel { ChiTietGioHang = chiTiet, GiaKhuyenMai = giaCuoiCung });
                tongTienHangDaGiam += chiTiet.SoLuong * giaCuoiCung;
            }

            viewModel.TongTienHang = tongTienHangDaGiam;
            viewModel.TongTienThanhToan = viewModel.TongTienHang;
            return viewModel;
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyVoucher(string voucherCode, decimal subTotal)
        {
            if (string.IsNullOrWhiteSpace(voucherCode))
                return Json(new { success = false, message = "Vui lòng chọn mã giảm giá." });

            var now = DateTime.Now;
            var voucher = await _context.MaGiamGias.FirstOrDefaultAsync(v => v.MaCode == voucherCode.ToUpper().Trim());

            if (voucher == null) return Json(new { success = false, message = "Mã giảm giá không tồn tại." });
            if (voucher.TrangThai != 1) return Json(new { success = false, message = "Mã giảm giá không hoạt động." });
            if (voucher.NgayBatDau > now || voucher.NgayKetThuc < now) return Json(new { success = false, message = "Mã đã hết hạn hoặc chưa có hiệu lực." });
            if (voucher.TongLuotSuDungToiDa.HasValue && voucher.DaSuDung >= voucher.TongLuotSuDungToiDa.Value) return Json(new { success = false, message = "Mã đã hết lượt sử dụng." });
            if (voucher.KenhApDung != "Online" && voucher.KenhApDung != "TatCa") return Json(new { success = false, message = "Mã không áp dụng cho kênh này." });
            if (subTotal < (voucher.DieuKienDonHangToiThieu ?? 0))
                return Json(new { success = false, message = $"Đơn hàng cần đạt tối thiểu {voucher.DieuKienDonHangToiThieu:N0} VNĐ." });

            decimal discountAmount = (voucher.LoaiGiamGia == "PhanTram") ? subTotal * (voucher.GiaTriGiamGia / 100m) : voucher.GiaTriGiamGia;
            decimal finalTotal = subTotal - discountAmount;

            HttpContext.Session.SetString("AppliedVoucherCode", voucher.MaCode);

            return Json(new { success = true, message = "Áp dụng thành công!", discountAmountFormatted = $"- {discountAmount:N0} VNĐ", finalTotalFormatted = $"{finalTotal:N0} VNĐ" });
        }
        [HttpGet]
        public async Task<IActionResult> LayThongTinDiaChi(int diaChiId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var diaChi = await _context.DiaChiNguoiDungs
                .FirstOrDefaultAsync(dc => dc.Id == diaChiId && dc.MaNguoiDung.ToString() == userId);

            if (diaChi == null) return NotFound(new { message = "Không tìm thấy địa chỉ." });

            // 1. Tính phí ship gốc
            var ketQuaShip = await TinhPhiVanChuyenAnToan(diaChiId);
            decimal phiShipCuoiCung = ketQuaShip.phiShip;
            decimal giamGiaShip = 0;
            decimal tongTienHangDeTinhShip = 0;

            // ============================================================
            // === BƯỚC 1: KIỂM TRA XEM CÓ ĐANG "MUA NGAY" KHÔNG? ===
            // ============================================================

            // Lấy các sản phẩm đang ở trạng thái Mua Ngay (TrangThai == 2)
            var buyNowItems = await _context.ChiTietGioHangs
                .Include(ct => ct.SanPhamChiTiet).ThenInclude(sp => sp.SanPham) // Include để lấy giá
                .Where(ct => ct.GioHang.MaNguoiDung.ToString() == userId && ct.TrangThai == 2)
                .ToListAsync();

            if (buyNowItems.Any())
            {
                // --- NẾU LÀ MUA NGAY: TÍNH TỔNG TIỀN CHO SẢN PHẨM MUA NGAY ---

                // Lấy cấu hình khuyến mãi để tính giá chính xác
                var activeProductPromotions = await _context.KhuyenMais
                    .Where(km => km.TrangThai == 1 && km.NgayBatDau <= DateTime.Now && km.NgayKetThuc >= DateTime.Now &&
                                 !string.IsNullOrEmpty(km.DanhSachSanPhamApDung) && (km.KenhApDung == "Online" || km.KenhApDung == "TatCa"))
                    .ToListAsync();

                var promoSetting = await _context.SystemSettings.FindAsync("PromotionRule");
                string promoRule = promoSetting?.SettingValue ?? "BestValue";

                foreach (var item in buyNowItems)
                {
                    decimal giaGoc = item.SanPhamChiTiet.GiaBan;
                    decimal giaCuoiCung = giaGoc;

                    var promotionsForThisVariant = activeProductPromotions
                        .Where(km => km.DanhSachSanPhamApDung.Split(',').Any(id => id == $"p-{item.SanPhamChiTiet.MaSanPham}" || id == $"v-{item.SanPhamChiTiet.MaSanPhamChiTiet}"))
                        .ToList();

                    if (promotionsForThisVariant.Any())
                    {
                        if (promoRule == "Stackable")
                        {
                            var sortedPromos = promotionsForThisVariant.OrderBy(p => p.LoaiGiamGia).ThenBy(p => p.MaKhuyenMai).ToList();
                            foreach (var promo in sortedPromos)
                            {
                                if (promo.LoaiGiamGia == "PhanTram") giaCuoiCung -= giaCuoiCung * (promo.GiaTriGiamGia / 100);
                                else if (promo.LoaiGiamGia == "SoTien") giaCuoiCung -= promo.GiaTriGiamGia;
                                if (giaCuoiCung < 0) giaCuoiCung = 0;
                            }
                        }
                        else
                        {
                            giaCuoiCung = promotionsForThisVariant.Select(p => CalculateProductDiscountedPrice(giaGoc, p)).Min();
                        }
                    }

                    tongTienHangDeTinhShip += item.SoLuong * giaCuoiCung;
                }
            }
            else
            {
                // --- NẾU KHÔNG CÓ MUA NGAY: LẤY TỪ GIỎ HÀNG THƯỜNG (TrangThai == 1) ---
                var gioHangVM = await BuildCartViewModelAsync(userId);
                if (gioHangVM != null)
                {
                    tongTienHangDeTinhShip = gioHangVM.TongTienHang;
                }
            }

            // ============================================================
            // === BƯỚC 2: TÍNH FREESHIP DỰA TRÊN TỔNG TIỀN VỪA TÌM ĐƯỢC ===
            // ============================================================

            giamGiaShip = await TinhFreeshipTuDongAsync(tongTienHangDeTinhShip, phiShipCuoiCung);

            phiShipCuoiCung -= giamGiaShip;
            if (phiShipCuoiCung < 0) phiShipCuoiCung = 0;

            var result = new
            {
                hoTen = diaChi.Ten,
                soDienThoai = diaChi.SoDienThoai,
                diaChiDayDu = $"{diaChi.ChiTietDiaChi}, {diaChi.Phuong}, {diaChi.Quan}, {diaChi.ThanhPho}",
                khoangCach = ketQuaShip.khoangCach,
                phiVanChuyen = phiShipCuoiCung,
                phiShipGoc = ketQuaShip.phiShip,
                tienGiamShip = giamGiaShip
            };

            return Json(result);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Sửa signature để nhận thêm `lyDoHuy`
        public async Task<IActionResult> YeuCauHuyDonHang(int id, string lyDoHuy)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var donHang = await _context.DonHangs.FirstOrDefaultAsync(d => d.MaDonHang == id && d.MaKhachHang.ToString() == userId);

            if (donHang == null)
            {
                return Forbid();
            }

            bool allowCancelRequest = donHang.PhuongThucThanhToan == "VnPay" && donHang.TrangThaiDonHang == 7;

            if (allowCancelRequest)
            {
                donHang.TrangThaiDonHang = 8; // Chuyển sang trạng thái "Yêu cầu hủy"

                // THÊM MỚI: LƯU LẠI LÝ DO YÊU CẦU HỦY
                donHang.LyDoHuy = string.IsNullOrWhiteSpace(lyDoHuy)
                    ? "Khách hàng yêu cầu hủy không nêu lý do."
                    : lyDoHuy;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã gửi yêu cầu hủy đơn hàng thành công! Quản trị viên sẽ sớm xem xét yêu cầu của bạn.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể yêu cầu hủy cho đơn hàng này ở trạng thái hiện tại.";
            }

            return RedirectToAction("ChiTietDonHang", new { id = id });
        }
      
        [HttpPost]
        public async Task<IActionResult> CheckStockRealTime(List<int> selectedCartItemIds)
        {
            if (selectedCartItemIds == null || !selectedCartItemIds.Any())
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm nào được chọn." });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 1. Lấy dữ liệu mới nhất từ Database (Real-time)
            var itemsToCheck = await _context.ChiTietGioHangs
                .Include(ct => ct.SanPhamChiTiet).ThenInclude(sp => sp.SanPham)
                .Include(ct => ct.SanPhamChiTiet).ThenInclude(sp => sp.MauSac)
                .Include(ct => ct.SanPhamChiTiet).ThenInclude(sp => sp.Size)
                .Where(ct => selectedCartItemIds.Contains(ct.MaChiTietGioHang) && ct.GioHang.MaNguoiDung.ToString() == userId)
                .ToListAsync();

            foreach (var item in itemsToCheck)
            {
                // 2. Kiểm tra nếu sản phẩm bị ngừng kinh doanh hoặc bị xóa
                if (item.SanPhamChiTiet == null || item.SanPhamChiTiet.SanPham.TrangThai != 1)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Sản phẩm '{item.SanPhamChiTiet?.SanPham?.TenSanPham ?? "N/A"}' đã ngừng kinh doanh. Vui lòng xóa khỏi giỏ hàng."
                    });
                }

                // 3. SO SÁNH TỒN KHO THỰC TẾ (Real-time)
                // item.SoLuong: Khách muốn mua
                // item.SanPhamChiTiet.SoLuong: Tồn kho hiện tại trong DB
                if (item.SoLuong > item.SanPhamChiTiet.SoLuong)
                {
                    string tenSp = item.SanPhamChiTiet.SanPham.TenSanPham;
                    string chiTiet = $"{item.SanPhamChiTiet.MauSac.TenMau}/{item.SanPhamChiTiet.Size.TenSize}";
                    int tonKhoHienTai = item.SanPhamChiTiet.SoLuong;

                    // Trả về thông báo lỗi cụ thể
                    return Json(new
                    {
                        success = false,
                        message = $"Rất tiếc, sản phẩm '{tenSp}' ({chiTiet}) vừa hết hàng hoặc không đủ số lượng. Tồn kho hiện tại: {tonKhoHienTai}. Vui lòng cập nhật lại giỏ hàng."
                    });
                }
            }

            // Nếu tất cả đều ổn
            return Json(new { success = true });
        }
        private const decimal PHI_SHIP_MAC_DINH = 30000; // Ví dụ: 30k

        // Hàm tính phí ship an toàn (Thay thế logic cũ)
        private async Task<(decimal phiShip, double khoangCach)> TinhPhiVanChuyenAnToan(int? diaChiId)
        {
            // Mặc định trả về phí lỗi 30k nếu không có ID
            if (diaChiId == null) return (PHI_SHIP_MAC_DINH, 0);

            try
            {
                var diaChi = await _context.DiaChiNguoiDungs.FindAsync(diaChiId);
                var cuaHang = await _context.DiaChiCuaHangs.FirstOrDefaultAsync(ch => ch.Id == 1);
                var cauHinh = await _context.ThongTinGiaoHangs.FirstOrDefaultAsync(x => x.Id == 1);

                // 1. Kiểm tra dữ liệu đầu vào (Nếu thiếu tọa độ => coi như LỖI API)
                if (diaChi == null || cuaHang == null || cauHinh == null ||
                    !diaChi.Latitude.HasValue || !diaChi.Longitude.HasValue ||
                    !cuaHang.Latitude.HasValue || !cuaHang.Longitude.HasValue)
                {
                    return (PHI_SHIP_MAC_DINH, 0); // Trả về 30k
                }

                // 2. Tính toán khoảng cách
                double khoangCach = TinhKhoangCach(
                    cuaHang.Latitude.Value, cuaHang.Longitude.Value,
                    diaChi.Latitude.Value, diaChi.Longitude.Value
                );

                decimal phiShip = 0;

                // 3. Logic tính tiền
                if (khoangCach < cauHinh.BanKinhfree)
                {
                    phiShip = 0;
                }
                else
                {
                    // BỎ CHECK BanKinhGiaoHang ở đây để tránh bị nhảy về 30k khi khách ở xa.
                    // Cứ trên 3km là nhân tiền
                    decimal phiTinhToan = (decimal)(khoangCach * (double)cauHinh.PhiGiaoHang);

                    // Luôn áp dụng mức trần 50.000 VNĐ
                    phiShip = Math.Min(phiTinhToan, 50000);
                }

                return (phiShip, khoangCach);
            }
            catch (Exception)
            {
                // 4. Nếu code crash => Trả về 30k
                return (PHI_SHIP_MAC_DINH, 0);
            }
        }
        private async Task<decimal> TinhFreeshipTuDongAsync(decimal tongTienHang, decimal phiShipHienTai)
        {
            var now = DateTime.Now;

            // 1. Lấy tất cả chương trình Freeship đang hoạt động
            var freeshipPrograms = await _context.MaGiamGias
                .Where(v => v.TrangThai == 1 &&
                            v.LoaiApDung == "FreeShip" && // Chỉ lấy loại Freeship
                            v.NgayBatDau <= now &&
                            v.NgayKetThuc >= now &&
                            // Freeship thường không giới hạn lượt dùng, hoặc check nếu có
                            (!v.TongLuotSuDungToiDa.HasValue || v.DaSuDung < v.TongLuotSuDungToiDa.Value) &&
                            (v.KenhApDung == "Online" || v.KenhApDung == "TatCa"))
                .ToListAsync();

            // 2. Lọc các chương trình thỏa mãn điều kiện đơn hàng tối thiểu
            var eligiblePrograms = freeshipPrograms
                .Where(v => tongTienHang >= (v.DieuKienDonHangToiThieu ?? 0))
                .ToList();

            if (!eligiblePrograms.Any()) return 0;

            // 3. Tìm mức giảm tốt nhất (Lớn nhất)
            decimal maxDiscount = 0;

            foreach (var p in eligiblePrograms)
            {
                decimal discount = 0;

                // Trường hợp 1: Freeship 100% (Phần trăm = 100)
                if (p.LoaiGiamGia == "PhanTram" && p.GiaTriGiamGia == 100)
                {
                    discount = phiShipHienTai;
                }
                // Trường hợp 2: Giảm số tiền cụ thể (SoTien)
                else if (p.LoaiGiamGia == "SoTien")
                {
                    // Giảm tối đa không vượt quá phí ship thực tế
                    discount = Math.Min(phiShipHienTai, p.GiaTriGiamGia);
                }

                if (discount > maxDiscount) maxDiscount = discount;
            }

            return maxDiscount;
        }
        private async Task<bool> CheckIsBannedCOD(string userId)
        {
            var user = await _context.NguoiDungs.FindAsync(int.Parse(userId));
            if (user == null || user.TrangThai != 2) return false;

            if (!string.IsNullOrEmpty(user.ResetToken) && user.ResetToken.Contains("|"))
            {
                var parts = user.ResetToken.Split('|');
                if (DateTime.TryParse(parts[1], out DateTime expiryDate))
                {
                    if (DateTime.Now < expiryDate)
                    {
                        return true; // Vẫn trong thời gian cấm
                    }
                    else
                    {
                        // Đã hết hạn cấm -> Tự động mở lại trạng thái bình thường
                        user.TrangThai = 1;
                        user.ResetToken = null;
                        await _context.SaveChangesAsync();
                        return false;
                    }
                }
            }
            return false;
        }
        [HttpGet]
        public async Task GetUpdateStream()
        {
            Response.ContentType = "text/event-stream";
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("Connection", "keep-alive");

            // Hàm tính toán Checksum (giữ nguyên logic cũ)
            decimal GetSystemChecksum()
            {
                // Sử dụng .AsNoTracking() để không làm nặng bộ nhớ vì chúng ta chỉ cần giá trị tức thời
                var totalGia = _context.SanPhamChiTiets.AsNoTracking().Sum(s => s.GiaBan);
                var totalSoLuong = _context.SanPhamChiTiets.AsNoTracking().Sum(s => s.SoLuong);
                var countKhuyenMai = _context.KhuyenMais.AsNoTracking().Count(k => k.TrangThai == 1);
                var countVoucher = _context.MaGiamGias.AsNoTracking().Count(v => v.TrangThai == 1);

                return totalGia + totalSoLuong + countKhuyenMai + countVoucher;
            }

            decimal lastChecksum = GetSystemChecksum();

            // SỬA TẠI ĐÂY: Sử dụng HttpContext.RequestAborted
            while (!HttpContext.RequestAborted.IsCancellationRequested)
            {
                try
                {
                    // Truyền HttpContext.RequestAborted vào Task.Delay để dừng ngay lập tức nếu khách thoát trang
                    await Task.Delay(3000, HttpContext.RequestAborted);

                    decimal currentChecksum = GetSystemChecksum();

                    if (currentChecksum != lastChecksum)
                    {
                        await Response.WriteAsync("data: reload\n\n");
                        await Response.Body.FlushAsync();
                        break;
                    }

                    await Response.WriteAsync("data: ping\n\n");
                    await Response.Body.FlushAsync();
                }
                catch (TaskCanceledException)
                {
                    // Khách hàng đã đóng trình duyệt hoặc chuyển trang, dừng vòng lặp êm ái
                    break;
                }
                catch (Exception)
                {
                    // Tránh treo server nếu có lỗi DB tạm thời
                    break;
                }
            }
        }
    }
}
