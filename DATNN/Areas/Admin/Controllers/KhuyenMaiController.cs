using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DATNN;
using DATNN.Models;
using Microsoft.AspNetCore.Authorization;

namespace DATNN.Areas.Admin.Controllers
{
    [Area("Admin")]
	[Authorize(Roles = "admin")]
	public class KhuyenMaiController : Controller
    {
        private readonly MyDbContext _context;

        public KhuyenMaiController(MyDbContext context)
        {
            _context = context;
        }

        // GET: Admin/KhuyenMai
        public async Task<IActionResult> Index(string statusFilter = "active")
        {
            var query = _context.KhuyenMais.AsQueryable();
            var now = DateTime.Now.Date;

            switch (statusFilter)
            {
                case "upcoming":
                    query = query.Where(km => km.TrangThai == 1 && km.NgayBatDau > now);
                    break;
                case "expired":
                    query = query.Where(km => km.NgayKetThuc < now);
                    break;
                case "inactive":
                    query = query.Where(km => km.TrangThai == 0 && km.NgayKetThuc >= now);
                    break;
                default: // "active"
                    statusFilter = "active";
                    query = query.Where(km => km.TrangThai == 1 && km.NgayBatDau <= now && km.NgayKetThuc >= now);
                    break;
            }

            const string settingKey = "PromotionRule";
            var promotionRuleSetting = await _context.SystemSettings.FindAsync(settingKey);
            ViewBag.CurrentPromotionRule = promotionRuleSetting?.SettingValue ?? "BestValue";

            var khuyenMais = await query.OrderByDescending(km => km.NgayBatDau).ToListAsync();
            ViewBag.CurrentStatusFilter = statusFilter;
            var allAppliedItems = khuyenMais
                          .Where(km => !string.IsNullOrEmpty(km.DanhSachSanPhamApDung))
                          .SelectMany(km => km.DanhSachSanPhamApDung.Split(','))
                          .Where(s => !string.IsNullOrWhiteSpace(s))
                          .Distinct()
                          .ToList();


            // 2. Tách ra 2 loại ID: ID sản phẩm cha và ID phiên bản
            var parentIdsFromPromos = allAppliedItems
                .Where(x => x.StartsWith("p-"))
                .Select(x => int.Parse(x.Substring(2))) // Bỏ "p-" và chuyển thành số
                .ToList();

            var variantIdsFromPromos = allAppliedItems
                .Where(x => x.StartsWith("v-"))
                .Select(x => int.Parse(x.Substring(2))) // Bỏ "v-" và chuyển thành số
                .ToList();

            // 3. Với các ID phiên bản, truy vấn DB để tìm ra ID sản phẩm cha tương ứng
            var parentIdsFromVariants = new List<int>();
            if (variantIdsFromPromos.Any())
            {
                parentIdsFromVariants = await _context.SanPhamChiTiets
                    .Where(ct => variantIdsFromPromos.Contains(ct.MaSanPhamChiTiet))
                    .Select(ct => ct.MaSanPham)
                    .Distinct()
                    .ToListAsync();
            }

            // 4. Gộp 2 danh sách ID sản phẩm cha lại và loại bỏ trùng lặp
            var finalProductIds = parentIdsFromPromos.Union(parentIdsFromVariants).Distinct().ToList();

            // 5. Lấy thông tin (Tên) của các sản phẩm đó từ database
            var sanPhamInfos = new Dictionary<int, string>();
            if (finalProductIds.Any())
            {
                sanPhamInfos = await _context.SanPhams
                    .Where(p => finalProductIds.Contains(p.MaSanPham))
                    .ToDictionaryAsync(p => p.MaSanPham, p => p.TenSanPham);
            }

            ViewBag.SanPhamInfoDict = sanPhamInfos;

            return View(khuyenMais);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var khuyenMai = await _context.KhuyenMais.FindAsync(id);
            if (khuyenMai == null)
                return Json(new { success = false, message = "Không tìm thấy chương trình khuyến mãi." });

            if (khuyenMai.TrangThai == 1) // Nếu đang bật -> Tắt (Vô hiệu hóa)
            {
                khuyenMai.TrangThai = 0;
            }
            else // Nếu đang tắt -> Bật lại
            {
                if (khuyenMai.NgayKetThuc < DateTime.Now.Date)
                {
                    return Json(new { success = false, message = "Không thể kích hoạt lại khuyến mãi đã hết hạn." });
                }
                khuyenMai.TrangThai = 1;
            }

            _context.Update(khuyenMai);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
        // GET: Admin/KhuyenMai/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.ProductTree = await _context.SanPhams
                .Where(p => p.TrangThai == 1)
                .Include(p => p.SanPhamChiTiets).ThenInclude(ct => ct.MauSac)
                .Include(p => p.SanPhamChiTiets).ThenInclude(ct => ct.Size)
                .ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
     [Bind("TenKhuyenMai,KenhApDung,LoaiGiamGia,GiaTriGiamGia,DanhSachSanPhamApDung,GhiChu,NgayBatDau,NgayKetThuc")] KhuyenMai khuyenMai,
     string[] selectedItems)
        {
            // 1. Kiểm tra ngày kết thúc >= ngày bắt đầu (đã có)
            if (khuyenMai.NgayKetThuc.Date < khuyenMai.NgayBatDau.Date)
            {
                ModelState.AddModelError("NgayKetThuc", "Ngày kết thúc không được nhỏ hơn ngày bắt đầu.");
            }

            // 2. Kiểm tra ngày bắt đầu không được là ngày trong quá khứ
            if (khuyenMai.NgayBatDau.Date < DateTime.Now.Date)
            {
                ModelState.AddModelError("NgayBatDau", "Ngày bắt đầu không được là một ngày trong quá khứ.");
            }

            // 3. Kiểm tra phải chọn ít nhất một sản phẩm (đã có)
            if (selectedItems == null || !selectedItems.Any())
            {
                ModelState.AddModelError("DanhSachSanPhamApDung", "Bạn phải chọn ít nhất một sản phẩm hoặc phiên bản để áp dụng.");
            }

            // 4. Kiểm tra logic cho Giá trị giảm giá
            if (khuyenMai.LoaiGiamGia == "PhanTram" && khuyenMai.GiaTriGiamGia > 100)
            {
                ModelState.AddModelError("GiaTriGiamGia", "Giá trị giảm giá theo phần trăm không được lớn hơn 100.");
            }
            else if (khuyenMai.LoaiGiamGia == "SoTien" && khuyenMai.GiaTriGiamGia > 100000)
            {
                ModelState.AddModelError("GiaTriGiamGia", "Giá trị giảm giá không được vượt quá 100,000 VNĐ.");
            }

            // 5. Kiểm tra tên khuyến mãi đã tồn tại hay chưa
            var tenKhuyenMaiDaTonTai = await _context.KhuyenMais.AnyAsync(km => km.TenKhuyenMai == khuyenMai.TenKhuyenMai);
            if (tenKhuyenMaiDaTonTai)
            {
                ModelState.AddModelError("TenKhuyenMai", "Tên khuyến mãi này đã tồn tại. Vui lòng chọn một tên khác.");
            }


            if (ModelState.IsValid)
            {
                khuyenMai.TrangThai = 1;

                if (selectedItems != null && selectedItems.Any())
                {
                    khuyenMai.DanhSachSanPhamApDung = string.Join(",", selectedItems);
                }

                _context.Add(khuyenMai);
                await _context.SaveChangesAsync();
                // Thêm TempData để thông báo thành công
                TempData["SuccessMessage"] = "Tạo mới chương trình khuyến mãi thành công!";
                return RedirectToAction(nameof(Index));
            }

            // Nếu model không hợp lệ, trả lại view với dữ liệu đã nhập
            ViewBag.ProductTree = await _context.SanPhams
                .Where(p => p.TrangThai == 1)
                .Include(p => p.SanPhamChiTiets).ThenInclude(ct => ct.MauSac)
                .Include(p => p.SanPhamChiTiets).ThenInclude(ct => ct.Size)
                .ToListAsync();
            // Giữ lại các mục đã chọn khi validate thất bại
            ViewBag.SelectedIds = selectedItems?.ToList() ?? new List<string>();
            return View(khuyenMai);
        }


        // GET: Admin/KhuyenMai/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var khuyenMai = await _context.KhuyenMais.FindAsync(id);
            if (khuyenMai == null) return NotFound();

            ViewBag.ProductTree = await _context.SanPhams
                .Where(p => p.TrangThai == 1)
                .Include(p => p.SanPhamChiTiets)
                    .ThenInclude(ct => ct.MauSac)
                .Include(p => p.SanPhamChiTiets)
                    .ThenInclude(ct => ct.Size)
                .ToListAsync();

            var selectedIds = new List<string>();
            if (!string.IsNullOrEmpty(khuyenMai.DanhSachSanPhamApDung))
            {
                selectedIds = khuyenMai.DanhSachSanPhamApDung.Split(',').ToList();
            }
            ViewBag.SelectedIds = selectedIds;

            return View(khuyenMai);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("MaKhuyenMai,TenKhuyenMai,KenhApDung,LoaiGiamGia,GiaTriGiamGia,DanhSachSanPhamApDung,GhiChu,NgayBatDau,NgayKetThuc")] KhuyenMai khuyenMai,
            string[] selectedItems)
        {
            if (id != khuyenMai.MaKhuyenMai) return NotFound();

            var originalPromoStatus = await _context.KhuyenMais.AsNoTracking()
                                            .Where(km => km.MaKhuyenMai == id)
                                            .Select(km => km.TrangThai).FirstOrDefaultAsync();

            if (khuyenMai.NgayKetThuc.Date < khuyenMai.NgayBatDau.Date)
            {
                ModelState.AddModelError("NgayKetThuc", "Ngày kết thúc không được nhỏ hơn ngày bắt đầu.");
            }
            if (selectedItems == null || !selectedItems.Any())
            {
                ModelState.AddModelError("DanhSachSanPhamApDung", "Bạn phải chọn ít nhất một sản phẩm để áp dụng.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    khuyenMai.TrangThai = originalPromoStatus;
                    khuyenMai.DanhSachSanPhamApDung = selectedItems != null && selectedItems.Any()
                        ? string.Join(",", selectedItems)
                        : null;
                    _context.Update(khuyenMai);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.KhuyenMais.Any(e => e.MaKhuyenMai == khuyenMai.MaKhuyenMai))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.ProductTree = await _context.SanPhams
                .Where(p => p.TrangThai == 1)
                .Include(p => p.SanPhamChiTiets)
                    .ThenInclude(ct => ct.MauSac)
                .Include(p => p.SanPhamChiTiets)
                    .ThenInclude(ct => ct.Size)
                .ToListAsync();
            var selectedIds = new List<string>();
            if (!string.IsNullOrEmpty(khuyenMai.DanhSachSanPhamApDung))
            {
                selectedIds = khuyenMai.DanhSachSanPhamApDung.Split(',').ToList();
            }
            ViewBag.SelectedIds = selectedIds;

            return View(khuyenMai);
        }

        // GET: Admin/KhuyenMai/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var khuyenMai = await _context.KhuyenMais.FirstOrDefaultAsync(m => m.MaKhuyenMai == id);
            if (khuyenMai == null) return NotFound();
            return View(khuyenMai);
        }

        // POST: Admin/KhuyenMai/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var khuyenMai = await _context.KhuyenMais.FindAsync(id);
            // Thay vì xóa, bạn có thể chỉ đổi trạng thái để lưu lịch sử
            // khuyenMai.TrangThai = 0; // Đã hủy
            // _context.Update(khuyenMai);
            if (khuyenMai != null)
            {
                _context.KhuyenMais.Remove(khuyenMai);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePromotionRule()
        {
            const string settingKey = "PromotionRule";
            var setting = await _context.SystemSettings.FindAsync(settingKey);

            if (setting == null)
            {
                // Nếu chưa có cài đặt, tạo mới và mặc định là "BestValue" (giá trị cao nhất)
                setting = new SystemSetting { SettingKey = settingKey, SettingValue = "Stackable" };
                _context.SystemSettings.Add(setting);
            }
            else
            {
                // Nếu đã có, đảo ngược giá trị
                setting.SettingValue = (setting.SettingValue == "BestValue") ? "Stackable" : "BestValue";
                _context.SystemSettings.Update(setting);
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, newRule = setting.SettingValue });
        }
    }
}
