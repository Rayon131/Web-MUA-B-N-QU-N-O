using DATNN.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DATNN.Controllers
{
    public class SanPhamController : Controller
    {
        private readonly MyDbContext _context;
        

        public SanPhamController(MyDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? categoryId, int? minPrice,int? maxPrice, string keyword,int page = 1)
        {
            int pageSize = 9;
            var now = DateTime.Now.Date; // Lấy ngày hiện tại

            var activePromos = await _context.KhuyenMais
        .Where(km => km.TrangThai == 1 && km.NgayBatDau <= now && km.NgayKetThuc >= now)
        .ToListAsync();

            // 2. Lấy Quy tắc khuyến mãi từ SystemSettings (Mặc định là BestValue nếu chưa cài đặt)
            var promoSetting = await _context.SystemSettings.FindAsync("PromotionRule");
            string promoRule = promoSetting?.SettingValue ?? "BestValue"; // "BestValue" hoặc "Stackable"


            // 2. Query sản phẩm (Giữ nguyên logic của bạn)
            var query = _context.SanPhams
                .Include(p => p.DanhMuc)
                .Include(p => p.SanPhamChiTiets)
                .Where(p => p.TrangThai == 1);

            // ✅ Lọc theo danh mục
            if (categoryId.HasValue)
                query = query.Where(p => p.MaDanhMuc == categoryId.Value);

            // ✅ Lọc theo khoảng giá
            if (minPrice.HasValue)
                query = query.Where(p => p.SanPhamChiTiets.FirstOrDefault().GiaBan >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => p.SanPhamChiTiets.FirstOrDefault().GiaBan <= maxPrice.Value);

            // ✅ Lọc theo tên sản phẩm gần đúng
            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(p => p.TenSanPham.Contains(keyword));

            var totalSanPham = await query.CountAsync();
            var sanPhams = await query
                .OrderByDescending(p => p.ThoiGianTao)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            var promoData = new Dictionary<int, (decimal GiaMoi, int PhanTram)>();

            foreach (var sp in sanPhams)
            {
                var giaGoc = sp.SanPhamChiTiets?.FirstOrDefault()?.GiaBan ?? 0;
                if (giaGoc == 0) continue;

                // Lấy tất cả KM áp dụng cho sp này
                var validPromos = activePromos.Where(km =>
                    !string.IsNullOrEmpty(km.DanhSachSanPhamApDung) &&
                    km.DanhSachSanPhamApDung.Split(',').Contains("p-" + sp.MaSanPham))
                    .ToList();

                if (validPromos.Any())
                {
                    decimal giaMoi = giaGoc;

                    if (promoRule == "Stackable")
                    {
                        // Bắt đầu với giá gốc
                        decimal giaHienTai = giaGoc;

                        // Duyệt qua từng khuyến mãi và TRỪ DẦN vào giá hiện tại
                        foreach (var promo in validPromos)
                        {
                            if (promo.LoaiGiamGia == "PhanTram")
                            {
                                // Tính số tiền giảm dựa trên GIÁ HIỆN TẠI (đã bị giảm bởi các KM trước)
                                // Chứ không phải tính trên giá gốc ban đầu
                                decimal soTienGiam = giaHienTai * (promo.GiaTriGiamGia / 100);
                                giaHienTai -= soTienGiam;
                            }
                            else if (promo.LoaiGiamGia == "SoTien")
                            {
                                giaHienTai -= promo.GiaTriGiamGia;
                            }

                            // Đảm bảo không bị âm sau mỗi lần trừ
                            if (giaHienTai < 0) giaHienTai = 0;
                        }

                        // Gán kết quả cuối cùng
                        giaMoi = giaHienTai;
                    }
                    // --- TRƯỜNG HỢP 2: GIÁ TỐT NHẤT (BEST VALUE) ---
                    else
                    {
                        // Logic cũ: Tính từng cái rồi chọn cái rẻ nhất
                        giaMoi = validPromos.Select(promo =>
                        {
                            if (promo.LoaiGiamGia == "PhanTram")
                                return giaGoc * (100 - Math.Min(promo.GiaTriGiamGia, 100)) / 100;
                            else
                                return Math.Max(0, giaGoc - promo.GiaTriGiamGia);
                        }).Min();
                    }

                    // --- XỬ LÝ CHUNG CUỐI CÙNG ---

                    // 1. Đảm bảo giá không âm
                    giaMoi = Math.Max(0, giaMoi);

                    // 2. Tính lại % tổng để hiển thị ra Badge
                    // (Ví dụ: Gốc 100k, Cộng dồn giảm 30k -> Hiển thị -30%)
                    int phanTramHienThi = giaGoc > 0 ? (int)(((giaGoc - giaMoi) / giaGoc) * 100) : 0;

                    // Chỉ thêm vào dict nếu thực sự có giảm giá
                    if (phanTramHienThi > 0 || giaMoi < giaGoc)
                    {
                        promoData[sp.MaSanPham] = (giaMoi, phanTramHienThi);
                    }
                }
            }

            ViewBag.PromoData = promoData;

            var vm = new HomeViewModel
            {
                DanhMucs = await _context.DanhMucs.Select(dm => new DanhMucViewModel { MaDanhMuc = dm.MaDanhMuc, TenDanhMuc = dm.TenDanhMuc, SoLuong = dm.SanPhams.Count() }).ToListAsync(),
                SanPhams = sanPhams,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)totalSanPham / pageSize),
                SelectedCategoryId = categoryId,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                Keyword = keyword
            };

            return View(vm);
        }





    }
}
