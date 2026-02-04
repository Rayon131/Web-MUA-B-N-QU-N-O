using DATNN;
using DATNN.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DATNN.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin")]
    public class XuatXuController : Controller
    {
        private readonly MyDbContext _context;

        public XuatXuController(MyDbContext context)
        {
            _context = context;
        }

        // GET: Admin/XuatXu
        public async Task<IActionResult> Index(int page = 1)
        {
            int pageSize = 14;

            var totalItems = await _context.XuatXus
                .Where(x => x.TrangThai == 1)
                .CountAsync();

            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var danhSachXuatXu = await _context.XuatXus
                .Where(x => x.TrangThai == 1)
                .OrderBy(x => x.MaXuatXu)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(danhSachXuatXu);
        }
        public async Task<IActionResult> VoHieu(int id)
        {
            var item = await _context.XuatXus.FindAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            item.TrangThai = 0;
            _context.Update(item);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> KhoiPhuc(int id)
        {
            var item = await _context.XuatXus.FindAsync(id);
            if (item == null)
                return NotFound();

            item.TrangThai = 1;
            _context.Update(item);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(VoHieuHoa)); // quay về danh sách bị vô hiệu hóa
        }
        public async Task<IActionResult> VoHieuHoa(int page = 1)
        {
            int pageSize = 14;

            var totalItems = await _context.XuatXus
                .Where(x => x.TrangThai == 0)
                .CountAsync();

            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var danhSachXuatXu = await _context.XuatXus
                .Where(x => x.TrangThai == 0)
                .OrderBy(x => x.MaXuatXu)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            // ✅ Dùng lại View Index
            return View("Index", danhSachXuatXu);
        }


        // GET: Admin/XuatXu/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var xuatXu = await _context.XuatXus
                .FirstOrDefaultAsync(m => m.MaXuatXu == id);
            if (xuatXu == null)
            {
                return NotFound();
            }

            return View(xuatXu);
        }

        // GET: Admin/XuatXu/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/XuatXu/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaXuatXu,TenXuatXu,MoTa,TrangThai")] XuatXu xuatXu)
        {
            // Chuẩn hóa tên xuất xứ
            var normalizedTenXuatXu = xuatXu.TenXuatXu.Trim().ToLower();

            // Kiểm tra trùng tên
            var isDuplicate = await _context.XuatXus
                .AnyAsync(x => x.TenXuatXu.Trim().ToLower() == normalizedTenXuatXu);

            if (isDuplicate)
            {
                ModelState.AddModelError("TenXuatXu", "Tên xuất xứ đã tồn tại.");
                return View(xuatXu);
            }

            // ✅ Mặc định trạng thái = 1
            xuatXu.TrangThai = 1;

            _context.Add(xuatXu);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateQuick(string TenXuatXu, string? MoTa, int TrangThai)
        {
            if (string.IsNullOrWhiteSpace(TenXuatXu))
                return Json(new { success = false, message = "Tên xuất xứ không được để trống" });

            // Kiểm tra trùng tên
            var normalizedTen = TenXuatXu.Trim().ToLower();
            var isDuplicate = await _context.XuatXus.AnyAsync(x => x.TenXuatXu.Trim().ToLower() == normalizedTen);
            if (isDuplicate)
            {
                return Json(new { success = false, message = "Tên xuất xứ đã tồn tại." });
            }

            var xuatXu = new XuatXu
            {
                TenXuatXu = TenXuatXu.Trim(),
                MoTa = MoTa,
                TrangThai = 1
            };

            _context.XuatXus.Add(xuatXu);
            await _context.SaveChangesAsync();

            return Json(new { success = true, id = xuatXu.MaXuatXu, ten = xuatXu.TenXuatXu });
        }
        // GET: Admin/XuatXu/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var xuatXu = await _context.XuatXus.FindAsync(id);
            if (xuatXu == null)
            {
                return NotFound();
            }
            return View(xuatXu);
        }

        // POST: Admin/XuatXu/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaXuatXu,TenXuatXu,MoTa,TrangThai")] XuatXu xuatXu)
        {
            if (id != xuatXu.MaXuatXu)
            {
                return NotFound();
            }

            // Lấy bản ghi cũ từ DB
            var existing = await _context.XuatXus.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            // Chuẩn hóa tên xuất xứ
            var normalizedTenXuatXu = xuatXu.TenXuatXu.Trim().ToLower();

            // Kiểm tra trùng tên với bản ghi khác
            var isDuplicate = await _context.XuatXus
                .AnyAsync(x => x.TenXuatXu.Trim().ToLower() == normalizedTenXuatXu
                               && x.MaXuatXu != xuatXu.MaXuatXu);

            if (isDuplicate)
            {
                ModelState.AddModelError("TenXuatXu", "Tên xuất xứ đã tồn tại.");
                return View(xuatXu);
            }

            try
            {
                // ✅ Cập nhật thủ công để tránh ghi đè dữ liệu ngoài ý muốn
                existing.TenXuatXu = xuatXu.TenXuatXu;
                existing.MoTa = xuatXu.MoTa;
                existing.TrangThai = 1; // hoặc giữ nguyên nếu bạn muốn

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!XuatXuExists(xuatXu.MaXuatXu))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/XuatXu/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var xuatXu = await _context.XuatXus
                .FirstOrDefaultAsync(m => m.MaXuatXu == id);
            if (xuatXu == null)
            {
                return NotFound();
            }

            return View(xuatXu);
        }

        // POST: Admin/XuatXu/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var xuatXu = await _context.XuatXus.FindAsync(id);
            if (xuatXu != null)
            {
                _context.XuatXus.Remove(xuatXu);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool XuatXuExists(int id)
        {
            return _context.XuatXus.Any(e => e.MaXuatXu == id);
        }
    }
}
