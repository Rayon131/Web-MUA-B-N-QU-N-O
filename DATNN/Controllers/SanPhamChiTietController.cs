using DATNN.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DATNN.Controllers
{
    public class SanPhamChiTietController : Controller
    {
        private readonly MyDbContext _context;

        public SanPhamChiTietController(MyDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Detail(int? id)
        {
            if (id == null)
            {
                return View("NotFound");
            }

            var sanPham = await _context.SanPhams
                .Include(p => p.ChatLieu)
                .Include(p => p.XuatXu)
                .Include(p => p.SanPhamChiTiets).ThenInclude(ct => ct.MauSac)
                .Include(p => p.SanPhamChiTiets).ThenInclude(ct => ct.Size)
                .FirstOrDefaultAsync(p => p.MaSanPham == id);

            if (sanPham == null)
            {
                return View("NotFound");
            }

            const string settingKey = "PromotionRule";
            var promotionRuleSetting = await _context.SystemSettings.FindAsync(settingKey);
            string currentRule = promotionRuleSetting?.SettingValue ?? "BestValue";

            var allValidPromotions = await _context.KhuyenMais
                   .Where(km => km.TrangThai == 1 &&
                                  km.NgayBatDau <= DateTime.Now &&
                                  km.NgayKetThuc >= DateTime.Now &&
                                  !string.IsNullOrEmpty(km.DanhSachSanPhamApDung) &&
                                  (km.KenhApDung == "Online" || km.KenhApDung == "TatCa"))
                   .ToListAsync();

            var chiTietListForJson = new List<object>();

            foreach (var ct in sanPham.SanPhamChiTiets.Where(c => c.TrangThai == 1))
            {
                // ==========================================================
                // == BẮT ĐẦU LOGIC LỌC KHUYẾN MÃI MỚI ==
                // ==========================================================

                // Lọc ra các KM áp dụng cho phiên bản này hoặc cho sản phẩm cha của nó
                var promotionsForThisVariant = allValidPromotions.Where(promo =>
                {
                    var appliedIds = promo.DanhSachSanPhamApDung.Split(',');
                    // Kiểm tra xem KM có áp dụng cho phiên bản con (v-{id}) HOẶC sản phẩm cha (p-{id}) không
                    return appliedIds.Contains($"v-{ct.MaSanPhamChiTiet}") || appliedIds.Contains($"p-{ct.MaSanPham}");
                }).ToList();

                decimal finalPrice = ct.GiaBan;

                // Áp dụng logic dựa trên cài đặt chung (Stackable/BestValue)
                if (currentRule == "Stackable" && promotionsForThisVariant.Any())
                {
                    decimal stackedPrice = ct.GiaBan;
                    foreach (var promo in promotionsForThisVariant)
                    {
                        stackedPrice = CalculateDiscountedPrice(stackedPrice, promo);
                    }
                    finalPrice = stackedPrice;
                }
                else // Mặc định hoặc khi currentRule == "BestValue"
                {
                    if (promotionsForThisVariant.Any())
                    {
                        var bestPromotion = promotionsForThisVariant
                            .OrderBy(km => CalculateDiscountedPrice(ct.GiaBan, km))
                            .FirstOrDefault();

                        if (bestPromotion != null)
                        {
                            finalPrice = CalculateDiscountedPrice(ct.GiaBan, bestPromotion);
                        }
                    }
                }

                chiTietListForJson.Add(new
                {
                    maSanPhamChiTiet = ct.MaSanPhamChiTiet,
                    maMauSac = ct.MauSac.MaMauSac,
                    tenMau = ct.MauSac.TenMau,
                    maSize = ct.Size.MaSize,
                    tenSize = ct.Size.TenSize,
                    giaBan = ct.GiaBan,
                    giaKhuyenMai = finalPrice,
                    hinhAnh = ct.HinhAnh,
                    soLuong = ct.SoLuong
                });
            }

            var sanPhamChiTietsJson = JsonSerializer.Serialize(chiTietListForJson);
            ViewData["SanPhamChiTietsJson"] = sanPhamChiTietsJson;
            return View(sanPham);
        }

        // Hàm tiện ích để tính toán giá sau khi giảm giá
        private decimal CalculateDiscountedPrice(decimal originalPrice, KhuyenMai promotion)
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
    }
}
