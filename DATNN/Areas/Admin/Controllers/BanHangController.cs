using DATNN.Models;
using DATNN.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Linq; // Đảm bảo bạn đã using
using System.Threading.Tasks; // Đảm bảo bạn đã using
using System.Collections.Generic; // Đảm bảo bạn đã using
using System;
using AppView.Models.Service.VNPay;
namespace DATNN.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin")]
    public class BanHangController : Controller
    {
        private readonly IDbContextFactory<MyDbContext> _contextFactory;
        private readonly IVnPayService _vnPayService;
        // SỬA LẠI CONSTRUCTOR
        public BanHangController(IDbContextFactory<MyDbContext> contextFactory, IVnPayService vnPayService) // <-- THÊM THAM SỐ
        {
            _contextFactory = contextFactory;
            _vnPayService = vnPayService; // <-- THÊM DÒNG NÀY
        }
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> GetInitialData()
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            var now = DateTime.Now;

            try
            {
                var userId = GetCurrentUserId();
                const string settingKey = "PromotionRule";
                var promotionRuleSetting = await _context.SystemSettings.FindAsync(settingKey);
                string currentRule = promotionRuleSetting?.SettingValue ?? "BestValue";
                var activeProductPromotions = await _context.KhuyenMais
                              .Where(km => km.TrangThai == 1 && km.NgayBatDau <= now && km.NgayKetThuc >= now &&
                                           !string.IsNullOrEmpty(km.DanhSachSanPhamApDung) && (km.KenhApDung == "TaiQuay" || km.KenhApDung == "TatCa"))
                              .ToListAsync();

                var productsFromDb = await _context.SanPhams
      .Where(p => p.TrangThai == 1 && p.SanPhamChiTiets.Any(ct => ct.TrangThai == 1 && ct.SoLuong > 0))
      .Select(p => new
      {
          p.MaSanPham,
          p.TenSanPham,
          p.AnhSanPham,
          GiaBan = p.SanPhamChiTiets.Where(ct => ct.TrangThai == 1 && ct.SoLuong > 0).Select(ct => (decimal?)ct.GiaBan).FirstOrDefault() ?? 0,

          // === 1. TÍNH TỔNG SỐ LƯỢNG TỪ DB ===
          TongSoLuong = p.SanPhamChiTiets.Where(ct => ct.TrangThai == 1).Sum(ct => ct.SoLuong),
          // ===================================

          VariantIds = p.SanPhamChiTiets.Select(ct => ct.MaSanPhamChiTiet).ToList()
      }).ToListAsync();

                var products = productsFromDb.Select(p => new ProductPosViewModel
                {
                    ProductId = p.MaSanPham,
                    ProductName = p.TenSanPham,
                    ImageUrl = !string.IsNullOrEmpty(p.AnhSanPham) ? $"/images/{p.AnhSanPham}" : "/img/placeholder.png",
                    Price = p.GiaBan,

                    // === 2. GÁN VÀO VIEW MODEL ===
                    TotalStock = p.TongSoLuong,
                    // ============================

                    HasPromotion = activeProductPromotions.Any(km =>
                    {
                        if (string.IsNullOrEmpty(km.DanhSachSanPhamApDung)) return false;
                        var appliedIds = km.DanhSachSanPhamApDung.Split(',');
                        if (appliedIds.Contains($"p-{p.MaSanPham}")) return true;
                        var variantAppliedIds = appliedIds.Where(id => id.StartsWith("v-")).Select(id => int.Parse(id.Substring(2)));
                        return p.VariantIds.Intersect(variantAppliedIds).Any();
                    })
                }).ToList();

                var customers = await _context.NguoiDungs
                    .Where(u => u.Quyen != null && u.Quyen.MaVaiTro == "KHACHHANG" && u.TrangThai == 1)
                    .Select(u => new { MaKhachHang = u.MaNguoiDung, u.HoTen, u.SoDienThoai })
                    .ToListAsync();
                var pendingOrders = await _context.DonHangs
                                   .Include(d => d.MaGiamGia)
                                   // Thêm điều kiện lọc theo ID người dùng
                                   .Where(d => d.MaNguoiDung == userId &&
                                               d.TrangThaiThanhToan == 2 &&
                                              (d.TrangThaiDonHang == 0 || d.TrangThaiDonHang == 1))
                                   .Select(d => new {
                                       maDonHang = d.MaDonHang,
                                       trangThai = d.TrangThaiDonHang,
                                       maKhachHang = d.MaKhachHang,
                                       phuongThucThanhToan = d.PhuongThucThanhToan,
                                       items = d.DonHangChiTiets.Select(dt => new {
                                           productDetailId = dt.MaSanPhamChiTiet,
                                           // Ưu tiên hiển thị tên lưu cứng
                                           name = $"{dt.TenSanPham_Luu ?? dt.SanPhamChiTiet.SanPham.TenSanPham} " +
            $"({dt.TenMau_Luu ?? dt.SanPhamChiTiet.MauSac.TenMau} - {dt.TenSize_Luu ?? dt.SanPhamChiTiet.Size.TenSize})",
                                           quantity = dt.SoLuong,
                                           unitPrice = dt.DonGia,
                                           stock = dt.SanPhamChiTiet.SoLuong + dt.SoLuong // Cộng lại số đang giữ trong giỏ để hiện đúng tồn kho khả dụng
                                       }).ToList(),
                                       voucher = d.MaGiamGia == null ? null : new
                                       {
                                           id = d.MaGiamGia.MaGiamGiaID,
                                           code = d.MaGiamGia.MaCode,
                                           loaiGiamGia = d.MaGiamGia.LoaiGiamGia,
                                           giaTriGiamGia = d.MaGiamGia.GiaTriGiamGia
                                       }
                                   }).ToListAsync();

                var promotions = new List<object>(); // Trả về danh sách rỗng
                return Json(new { products, customers, promotions, pendingOrders, promotionRule = currentRule });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi nghiêm trọng ở server khi tải dữ liệu ban đầu.", details = ex.ToString() });
            }
        }
        private async Task<KhuyenMai?> GetBestApplicablePromotionForProduct(MyDbContext context, int productId)
        {
            var now = DateTime.Now;
            var productIdStr = productId.ToString();

            return await context.KhuyenMais
       .Where(km => km.TrangThai == 1 &&
                      km.NgayBatDau <= now &&
                      km.NgayKetThuc >= now &&
                      !string.IsNullOrEmpty(km.DanhSachSanPhamApDung) &&
                      ("," + km.DanhSachSanPhamApDung + ",").Contains("," + productIdStr + ","))
       .OrderByDescending(km => km.GiaTriGiamGia)
       .FirstOrDefaultAsync();
        }

        [HttpGet]
        public async Task<IActionResult> GetProductDetails(int productId)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            const string settingKey = "PromotionRule";
            var promotionRuleSetting = await _context.SystemSettings.FindAsync(settingKey);
            string currentRule = promotionRuleSetting?.SettingValue ?? "BestValue";

            var allActivePromotions = await _context.KhuyenMais
                .Where(km => km.TrangThai == 1 &&
                             km.NgayBatDau <= DateTime.Now && km.NgayKetThuc >= DateTime.Now &&
                             !string.IsNullOrEmpty(km.DanhSachSanPhamApDung) &&
                             (km.KenhApDung == "TaiQuay" || km.KenhApDung == "TatCa"))
                .ToListAsync();

            var productDetailsFromDb = await _context.SanPhamChiTiets
                .Include(pd => pd.MauSac)
                .Include(pd => pd.Size)
                .Where(pd => pd.MaSanPham == productId && pd.TrangThai == 1 && pd.SoLuong > 0)
                .ToListAsync();

            // Tính toán giá trong bộ nhớ cho từng phiên bản
            var productDetails = productDetailsFromDb.Select(pd =>
            {
                // ==========================================================
                // == ĐÂY LÀ PHẦN THAY ĐỔI CỐT LÕI ==
                // ==========================================================
                // Với mỗi phiên bản (pd), lọc ra các KM áp dụng cho nó
                var promotionsForThisVariant = allActivePromotions.Where(promo => {
                    var appliedIds = promo.DanhSachSanPhamApDung.Split(',');
                    // Kiểm tra xem KM có áp dụng cho ID phiên bản NÀY (v-{id})
                    // HOẶC cho ID sản phẩm cha của nó (p-{id}) hay không
                    return appliedIds.Contains($"v-{pd.MaSanPhamChiTiet}") || appliedIds.Contains($"p-{pd.MaSanPham}");
                }).ToList();

                // Tính giá cuối cùng cho phiên bản này dựa trên các KM đã lọc
                decimal finalPrice = CalculateFinalItemPrice(pd.GiaBan, promotionsForThisVariant, currentRule);

                return new
                {
                    pd.MaSanPhamChiTiet,
                    TenMau = pd.MauSac.TenMau,
                    MaMau = GetHexCodeForColor(pd.MauSac.TenMau),
                    TenKichCo = pd.Size.TenSize,
                    pd.SoLuong,
                    GiaBan = finalPrice,
                    GiaGoc = pd.GiaBan
                };
            }).ToList();

            var groupedByColor = productDetails.GroupBy(pd => new { pd.TenMau, pd.MaMau }).Select(g => new ColorGroupViewModel { ColorName = g.Key.TenMau, ColorCode = g.Key.MaMau, Sizes = g.Select(s => new SizeViewModel { ProductDetailId = s.MaSanPhamChiTiet, SizeName = s.TenKichCo, Stock = s.SoLuong, Price = s.GiaBan, OriginalPrice = s.GiaGoc }).ToList() }).ToList();

            var productName = await _context.SanPhams
                .Where(p => p.MaSanPham == productId)
                .Select(p => p.TenSanPham)
                .FirstOrDefaultAsync();

            return Json(new ProductDetailPosViewModel { ProductId = productId, ProductName = productName, Colors = groupedByColor });
        }

        private string GetHexCodeForColor(string colorName)
        {
            if (string.IsNullOrEmpty(colorName)) return "#CCCCCC";
            switch (colorName.ToLower().Trim()) { case "đỏ": return "#FF0000"; case "xanh dương": case "xanh": return "#0000FF"; case "xanh lá": return "#008000"; case "vàng": return "#FFFF00"; case "trắng": return "#FFFFFF"; case "đen": return "#000000"; case "xám": return "#808080"; case "hồng": return "#FFC0CB"; default: return "#CCCCCC"; }
        }

        [HttpPost]
        public async Task<IActionResult> CreatePendingOrder()
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            try
            {
                // === THAY ĐỔI CỐT LÕI BẮT ĐẦU TỪ ĐÂY ===
                var userId = GetCurrentUserId();
                const int MaxPendingOrdersPerUser = 5;

                // 1. Đếm số hóa đơn chờ hiện có của người dùng này
                var pendingOrderCount = await _context.DonHangs
                    .CountAsync(d => d.MaNguoiDung == userId &&
                                     d.TrangThaiThanhToan == 2 &&
                                    (d.TrangThaiDonHang == 0 || d.TrangThaiDonHang == 1));

                // 2. Kiểm tra giới hạn
                if (pendingOrderCount >= MaxPendingOrdersPerUser)
                {
                    // Trả về lỗi nếu đã đạt giới hạn, success = false để frontend nhận biết
                    return Ok(new { success = false, message = $"Bạn đã đạt giới hạn tối đa {MaxPendingOrdersPerUser} hóa đơn chờ." });
                }

                // 3. Nếu chưa đạt giới hạn, tạo hóa đơn mới như cũ
                var newOrder = new DonHang
                {
                    MaNguoiDung = userId,
                    ThoiGianTao = DateTime.Now,
                    TrangThaiDonHang = 0, // Chờ
                    TongTien = 0,
                    SoDienThoai = "",
                    Email = "",
                    DiaChi = "",
                    PhuongThucThanhToan = "",
                    TrangThaiThanhToan = 2 // Chờ thanh toán
                };

                _context.DonHangs.Add(newOrder);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, orderId = newOrder.MaDonHang });
                // === KẾT THÚC THAY ĐỔI ===
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return StatusCode(500, new { success = false, message = "Lỗi server: " + (ex.InnerException?.Message ?? ex.Message) });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrder([FromBody] OrderUpdateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ." });
            }

            await using var _context = await _contextFactory.CreateDbContextAsync();
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var existingOrder = await _context.DonHangs.Include(o => o.DonHangChiTiets).FirstOrDefaultAsync(o => o.MaDonHang == model.OrderId);
                if (existingOrder == null) return NotFound(new { success = false, message = "Không tìm thấy đơn hàng." });

                // 1. Hoàn trả số lượng sản phẩm cũ về kho
                foreach (var oldDetail in existingOrder.DonHangChiTiets)
                {
                    var productDetail = await _context.SanPhamChiTiets.FindAsync(oldDetail.MaSanPhamChiTiet);
                    if (productDetail != null) productDetail.SoLuong += oldDetail.SoLuong;
                }
                _context.DonHangChiTiets.RemoveRange(existingOrder.DonHangChiTiets);
                // Lưu ý: Chưa gọi SaveChangesAsync(), sẽ lưu cùng lúc ở cuối

                // 2. Tính toán lại chi tiết đơn hàng và trừ kho
                // 2. Lấy tất cả khuyến mãi đang hoạt động (giống GetProductDetails)
                var now = DateTime.Now;
                const string settingKey = "PromotionRule";
                var promotionRuleSetting = await _context.SystemSettings.FindAsync(settingKey);
                string currentRule = promotionRuleSetting?.SettingValue ?? "BestValue";

                var allActivePromotions = await _context.KhuyenMais
                    .Where(km => km.TrangThai == 1 &&
                                 km.NgayBatDau <= now && km.NgayKetThuc >= now &&
                                 !string.IsNullOrEmpty(km.DanhSachSanPhamApDung) &&
                                 (km.KenhApDung == "TaiQuay" || km.KenhApDung == "TatCa"))
                    .ToListAsync();

                // 3. Tính toán lại chi tiết đơn hàng, trừ kho VÀ TÍNH LẠI GIÁ KHUYẾN MÃI
                decimal subTotal = 0;
                var newOrderDetails = new List<DonHangChiTiet>();

                foreach (var item in model.OrderDetails)
                {
                    // Nạp thông tin sản phẩm cha để kiểm tra khuyến mãi theo sản phẩm
                    var productDetail = await _context.SanPhamChiTiets
                                              .Include(pd => pd.SanPham)
                                               .Include(pd => pd.MauSac) // <--- THÊM DÒNG NÀY
                              .Include(pd => pd.Size)   // <--- THÊM DÒNG NÀY
                                              .FirstOrDefaultAsync(pd => pd.MaSanPhamChiTiet == item.ProductDetailId);

                    if (productDetail == null || productDetail.SoLuong < item.Quantity)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(new { success = false, message = $"Sản phẩm ID {item.ProductDetailId} không đủ số lượng trong kho." });
                    }

                    productDetail.SoLuong -= item.Quantity;

                    // TÍNH TOÁN LẠI GIÁ KHUYẾN MÃI Ở SERVER
                    var promotionsForThisVariant = allActivePromotions.Where(promo => {
                        var appliedIds = promo.DanhSachSanPhamApDung.Split(',');
                        return appliedIds.Contains($"v-{productDetail.MaSanPhamChiTiet}") || appliedIds.Contains($"p-{productDetail.MaSanPham}");
                    }).ToList();

                    decimal finalPrice = CalculateFinalItemPrice(productDetail.GiaBan, promotionsForThisVariant, currentRule);

                    subTotal += item.Quantity * finalPrice; // Dùng giá đã tính lại

                    newOrderDetails.Add(new DonHangChiTiet
                    {
                        MaDonHang = existingOrder.MaDonHang,
                        MaSanPhamChiTiet = item.ProductDetailId,
                        SoLuong = item.Quantity,
                        DonGia = finalPrice,
                        TenSanPham_Luu = productDetail.SanPham?.TenSanPham ?? "Sản phẩm không xác định",
                        TenMau_Luu = productDetail.MauSac?.TenMau ?? "N/A",
                        TenSize_Luu = productDetail.Size?.TenSize ?? "N/A",
                        HinhAnh_Luu = productDetail.HinhAnh ?? productDetail.SanPham?.AnhSanPham
                    });
                }
                // Thêm các chi tiết mới vào context
                _context.DonHangChiTiets.AddRange(newOrderDetails);

                // 3. Xử lý Voucher
                decimal voucherDiscount = 0;
                MaGiamGia appliedVoucher = null;
                if (model.MaGiamGiaId.HasValue)
                {
                    var voucher = await _context.MaGiamGias.FindAsync(model.MaGiamGiaId.Value);
                    if (voucher != null && voucher.TrangThai == 1 && voucher.NgayBatDau <= now && voucher.NgayKetThuc >= now &&
                        (!voucher.TongLuotSuDungToiDa.HasValue || voucher.DaSuDung < voucher.TongLuotSuDungToiDa.Value) &&
                        subTotal >= (voucher.DieuKienDonHangToiThieu ?? 0))
                    {
                        appliedVoucher = voucher;
                        voucherDiscount = voucher.LoaiGiamGia == "PhanTram"
                            ? subTotal * (voucher.GiaTriGiamGia / 100m)
                            : voucher.GiaTriGiamGia;
                    }
                }

                // 4. Cập nhật thông tin đơn hàng
                decimal finalTotal = subTotal - voucherDiscount;
                existingOrder.MaKhachHang = model.CustomerId;
                existingOrder.MaGiamGiaID = appliedVoucher?.MaGiamGiaID;
                existingOrder.PhuongThucThanhToan = model.PaymentMethod;
                existingOrder.TongTien = finalTotal < 0 ? 0 : finalTotal;
                existingOrder.SoTienDuocGiam = voucherDiscount;

                if (model.Status == 2) // Khi nhấn "Thanh toán ngay"
                {
                    // <<< THAY ĐỔI QUAN TRỌNG: Tăng lượt sử dụng voucher tại đây >>>
                    // Logic này sẽ được thực thi cho cả Chuyển khoản và Tiền mặt
                    if (appliedVoucher != null)
                    {
                        appliedVoucher.DaSuDung++;
                    }

                    if (model.PaymentMethod == "Chuyển khoản")
                    {
                        if (existingOrder.TongTien <= 0)
                        {
                            await transaction.RollbackAsync();
                            return BadRequest(new { success = false, message = "Không thể thanh toán online cho đơn hàng có giá trị bằng 0 hoặc âm." });
                        }

                        existingOrder.TrangThaiDonHang = 1; // Chờ thanh toán

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        var paymentModel = new PaymentInformationModel
                        {
                            OrderId = existingOrder.MaDonHang.ToString(),
                            Amount = (double)existingOrder.TongTien,
                            OrderDescription = $"Thanh toan don hang {existingOrder.MaDonHang}",
                            Name = "Khach hang",
                            OrderType = "other"
                        };

                        var returnUrl = Url.Action("PaymentCallbackVnpay", "Payment", new { Area = "Admin" }, Request.Scheme);
                        var paymentUrl = _vnPayService.CreatePaymentUrl(paymentModel, HttpContext, returnUrl);

                        return Ok(new { success = true, redirectTo = paymentUrl });
                    }
                    else // Thanh toán Tiền mặt
                    {
                        existingOrder.TrangThaiDonHang = 2; // Đã thanh toán
                        existingOrder.TienMatDaNhan = model.TienMatDaNhan;
                        // <<< THAY ĐỔI: Dòng tăng lượt sử dụng đã được chuyển lên trên, nên xóa ở đây >>>
                        // if (appliedVoucher != null) { appliedVoucher.DaSuDung++; } // <-- XÓA DÒNG NÀY
                    }
                }
                else // Chỉ lưu đơn hàng (Chờ thanh toán)
                {
                    existingOrder.TrangThaiDonHang = 1;
                }

                // Lưu tất cả thay đổi và commit giao dịch cho các trường hợp còn lại
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // === THAY THẾ TOÀN BỘ CÁC DÒNG `return Ok(...)` CŨ Ở CUỐI BẰNG ĐOẠN NÀY ===
                if (model.Status == 2 && model.PaymentMethod == "Tiền mặt")
                {
                    // Trả về orderId để frontend có thể gọi action in hóa đơn
                    return Ok(new { success = true, message = "Thanh toán thành công!", orderId = existingOrder.MaDonHang });
                }
                else
                {
                    return Ok(new { success = true, message = "Cập nhật đơn hàng thành công!" });
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { success = false, message = "Lỗi server: " + (ex.InnerException?.Message ?? ex.Message) });
            }
        }
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier); if (userIdClaim == null) throw new InvalidOperationException("Không thể xác định người dùng đang đăng nhập."); return int.Parse(userIdClaim.Value);
        }
        [HttpPost]
        public async Task<IActionResult> CancelOrder([FromBody] CancelOrderViewModel model)
        {
            if (model.OrderId <= 0)
            {
                return BadRequest(new { success = false, message = "Mã đơn hàng không hợp lệ." });
            }

            await using var _context = await _contextFactory.CreateDbContextAsync();

            // Nạp đơn hàng cùng với chi tiết để có thông tin hoàn kho
            var existingOrder = await _context.DonHangs
                .Include(o => o.DonHangChiTiets)
                .FirstOrDefaultAsync(o => o.MaDonHang == model.OrderId);

            if (existingOrder == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy đơn hàng để xóa." });
            }

            try
            {
                // 1. Hoàn trả lại số lượng sản phẩm vào kho trước khi xóa đơn
                foreach (var detail in existingOrder.DonHangChiTiets)
                {
                    var productDetail = await _context.SanPhamChiTiets.FindAsync(detail.MaSanPhamChiTiet);
                    if (productDetail != null)
                    {
                        productDetail.SoLuong += detail.SoLuong;
                    }
                }

                // 2. Xóa các chi tiết đơn hàng (EF Core thường tự xóa nếu có Cascade, nhưng làm thủ công cho chắc chắn)
                _context.DonHangChiTiets.RemoveRange(existingOrder.DonHangChiTiets);

                // 3. Xóa luôn đơn hàng khỏi Database
                _context.DonHangs.Remove(existingOrder);

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Đơn hàng đã được hủy và hoàn kho thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi server khi xóa đơn hàng: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetProductDetailByQrCode(int productDetailId)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            var productDetail = await _context.SanPhamChiTiets
                .Include(pd => pd.SanPham)
                .Include(pd => pd.MauSac)
                .Include(pd => pd.Size)
                .FirstOrDefaultAsync(pd => pd.MaSanPhamChiTiet == productDetailId);

            if (productDetail == null || productDetail.TrangThai != 1 || productDetail.SoLuong <= 0)
            {
                return NotFound(new { message = "Sản phẩm không tồn tại, đã ngừng kinh doanh hoặc đã hết hàng." });
            }

            // === BẮT ĐẦU PHẦN LOGIC SỬA ĐỔI ===
            // 1. Lấy quy tắc khuyến mãi (BestValue hoặc Stackable)
            const string settingKey = "PromotionRule";
            var promotionRuleSetting = await _context.SystemSettings.FindAsync(settingKey);
            string currentRule = promotionRuleSetting?.SettingValue ?? "BestValue";

            // 2. Lấy tất cả khuyến mãi đang hoạt động áp dụng tại quầy
            var allActivePromotions = await _context.KhuyenMais
                .Where(km => km.TrangThai == 1 &&
                             km.NgayBatDau <= DateTime.Now && km.NgayKetThuc >= DateTime.Now &&
                             !string.IsNullOrEmpty(km.DanhSachSanPhamApDung) &&
                             (km.KenhApDung == "TaiQuay" || km.KenhApDung == "TatCa"))
                .ToListAsync();

            // 3. Lọc các khuyến mãi áp dụng riêng cho phiên bản này
            var promotionsForThisVariant = allActivePromotions.Where(promo => {
                var appliedIds = promo.DanhSachSanPhamApDung.Split(',');
                // Kiểm tra xem KM có áp dụng cho ID phiên bản NÀY (v-{id})
                // HOẶC cho ID sản phẩm cha của nó (p-{id}) hay không
                return appliedIds.Contains($"v-{productDetail.MaSanPhamChiTiet}") || appliedIds.Contains($"p-{productDetail.MaSanPham}");
            }).ToList();

            // 4. Tính giá cuối cùng bằng hàm helper đã có
            decimal finalPrice = CalculateFinalItemPrice(productDetail.GiaBan, promotionsForThisVariant, currentRule);
            // === KẾT THÚC PHẦN LOGIC SỬA ĐỔI ===

            var result = new
            {
                productDetailId = productDetail.MaSanPhamChiTiet,
                name = $"{productDetail.SanPham.TenSanPham} ({productDetail.MauSac.TenMau} - {productDetail.Size.TenSize})",
                productName = productDetail.SanPham.TenSanPham,
                colorName = productDetail.MauSac.TenMau,
                sizeName = productDetail.Size.TenSize,
                image = productDetail.HinhAnh ?? productDetail.SanPham.AnhSanPham,
                quantity = 1,
                unitPrice = finalPrice, // Sử dụng giá cuối cùng đã được tính toán chính xác
                stock = productDetail.SoLuong // Trả về thêm số lượng tồn kho
            };

            return Ok(result);
        }
        private decimal CalculateFinalItemPrice(decimal basePrice, List<KhuyenMai> promotions, string rule)
        {
            if (!promotions.Any()) return basePrice;

            if (rule == "Stackable")
            {
                decimal currentPrice = basePrice;
                foreach (var promo in promotions)
                {
                    currentPrice = CalculateDiscountedPrice(currentPrice, promo);
                }
                return currentPrice;
            }
            else // BestValue
            {
                return promotions.Select(p => CalculateDiscountedPrice(basePrice, p)).Min();
            }
        }
        private decimal CalculateDiscountedPrice(decimal originalPrice, KhuyenMai promotion)
        {
            if (promotion.LoaiGiamGia == "PhanTram") return originalPrice * (1 - promotion.GiaTriGiamGia / 100m);
            if (promotion.LoaiGiamGia == "SoTien") return Math.Max(0, originalPrice - promotion.GiaTriGiamGia);
            return originalPrice;
        }
        [HttpGet]
        public async Task<IActionResult> GetAvailableVouchers()
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();
            var now = DateTime.Now;

            var vouchers = await _context.MaGiamGias
                .Where(v => v.TrangThai == 1 &&
                v.LoaiApDung == "DonHang" &&
                (v.KenhApDung == "TaiQuay" || v.KenhApDung == "TatCa") &&
                             v.NgayBatDau <= now &&
                             v.NgayKetThuc >= now &&
                             // SỬA: Dùng TongLuotSuDungToiDa từ model MaGiamGia của bạn
                             (!v.TongLuotSuDungToiDa.HasValue || v.DaSuDung < v.TongLuotSuDungToiDa.Value))
                .Select(v => new {
                    Id = v.MaCode,
                    Text = $"{v.MaCode} - {v.TenChuongTrinh}"
                })
                .ToListAsync();

            return Json(vouchers);
        }


        // 2. API XÁC THỰC VOUCHER KHI NHẤN "ÁP DỤNG"
        [HttpPost]
        public async Task<IActionResult> ApplyVoucher([FromBody] ApplyVoucherRequest model)
        {
            if (string.IsNullOrWhiteSpace(model.VoucherCode))
            {
                return BadRequest(new { success = false, message = "Vui lòng nhập mã voucher." });
            }

            await using var _context = await _contextFactory.CreateDbContextAsync();
            var now = DateTime.Now;
            var voucherCodeUpper = model.VoucherCode.ToUpper().Trim();
            var voucher = await _context.MaGiamGias.FirstOrDefaultAsync(v => v.MaCode == voucherCodeUpper);

            // --- Các bước kiểm tra giữ nguyên ---
            if (voucher == null) return Json(new { success = false, message = "Mã giảm giá không tồn tại." });
            if (voucher.KenhApDung != "TaiQuay" && voucher.KenhApDung != "TatCa")
            {
                return Json(new { success = false, message = "Mã này chỉ áp dụng cho kênh bán hàng Online." });
            }
            if (voucher.LoaiApDung != "DonHang")
                return Json(new { success = false, message = "Mã này không phải mã giảm giá đơn hàng (Ví dụ: Đây là mã Freeship)." });
            if (voucher.TrangThai != 1) return Json(new { success = false, message = "Mã giảm giá đã bị vô hiệu hóa." });
            if (voucher.NgayBatDau > now) return Json(new { success = false, message = "Mã giảm giá chưa đến ngày áp dụng." });
            if (voucher.NgayKetThuc < now) return Json(new { success = false, message = "Mã giảm giá đã hết hạn." });
            if (voucher.TongLuotSuDungToiDa.HasValue && voucher.DaSuDung >= voucher.TongLuotSuDungToiDa.Value) return Json(new { success = false, message = "Mã giảm giá đã hết lượt sử dụng." });
            if (model.SubTotal < (voucher.DieuKienDonHangToiThieu ?? 0)) return Json(new { success = false, message = $"Đơn hàng cần đạt tối thiểu {voucher.DieuKienDonHangToiThieu:N0}đ để áp dụng mã này." });

            // --- THAY ĐỔI QUAN TRỌNG: Trả về thông tin gốc của Voucher ---
            return Json(new
            {
                success = true,
                message = $"Áp dụng thành công voucher '{voucher.MaCode}'.",
                voucherId = voucher.MaGiamGiaID,
                loaiGiamGia = voucher.LoaiGiamGia,     // << Trả về Loại giảm giá
                giaTriGiamGia = voucher.GiaTriGiamGia  // << Trả về Giá trị giảm giá
            });
        }
        public async Task<IActionResult> PrintReceipt(int orderId)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            var order = await _context.DonHangs
                .Include(o => o.KhachHang)
                .Include(o => o.MaGiamGia)
                .Include(o => o.DonHangChiTiets)
                    .ThenInclude(dt => dt.SanPhamChiTiet.SanPham)
                .Include(o => o.DonHangChiTiets)
                    .ThenInclude(dt => dt.SanPhamChiTiet.MauSac)
                .Include(o => o.DonHangChiTiets)
                    .ThenInclude(dt => dt.SanPhamChiTiet.Size)
                .FirstOrDefaultAsync(o => o.MaDonHang == orderId);

            if (order == null)
            {
                return NotFound("Không tìm thấy hóa đơn.");
            }

            return View(order);
        }
        public async Task<IActionResult> PrintReceiptonl(int orderId)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync();

            var order = await _context.DonHangs
                .Include(o => o.KhachHang)
                .Include(o => o.MaGiamGia)
                .Include(o => o.DonHangChiTiets)
                    .ThenInclude(dt => dt.SanPhamChiTiet.SanPham)
                .Include(o => o.DonHangChiTiets)
                    .ThenInclude(dt => dt.SanPhamChiTiet.MauSac)
                .Include(o => o.DonHangChiTiets)
                    .ThenInclude(dt => dt.SanPhamChiTiet.Size)
                .FirstOrDefaultAsync(o => o.MaDonHang == orderId);

            if (order == null)
            {
                return NotFound("Không tìm thấy hóa đơn.");
            }

            // 👉 Lấy địa chỉ cửa hàng
            var store = await _context.DiaChiCuaHangs
         .FirstOrDefaultAsync(x => x.Id == 1);
            var stores = await _context.ThongTinCuaHangs
        .FirstOrDefaultAsync(x => x.Id == 1);

            ViewBag.Store = store;
            ViewBag.Stores = stores;

            return View(order);
        }

        // Class nhỏ để nhận dữ liệu từ frontend, không cần thay đổi
        public class ApplyVoucherRequest
        {
            public string VoucherCode { get; set; }
            public decimal SubTotal { get; set; }
        }
        public class CancelOrderViewModel
        {
            public int OrderId { get; set; }
        }
    }

}
