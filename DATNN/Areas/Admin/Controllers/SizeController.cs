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
    public class SizeController : Controller
    {
        private readonly MyDbContext _context;

        public SizeController(MyDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index(int page = 1)
        {
            int pageSize = 14;

            var totalItems = await _context.Sizes
                .Where(s => s.TrangThai == 1)
                .CountAsync();

            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var danhSachSize = await _context.Sizes
                .Where(s => s.TrangThai == 1)
                .OrderBy(s => s.MaSize)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(danhSachSize);
        }
        public async Task<IActionResult> VoHieu(int id)
        {
            var item = await _context.Sizes.FindAsync(id);
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
            var item = await _context.Sizes.FindAsync(id);
            if (item == null)
                return NotFound();

            item.TrangThai = 1;
            _context.Update(item);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(VoHieuHoa)); // quay về danh sách bị vô hiệu hóa
        }
        // GET: Admin/Size
        public async Task<IActionResult> VoHieuHoa(int page = 1)
        {
            int pageSize = 14;

            var totalItems = await _context.Sizes
                .Where(s => s.TrangThai == 0)
                .CountAsync();

            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var danhSachSize = await _context.Sizes
                .Where(s => s.TrangThai == 0)
                .OrderBy(s => s.MaSize)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            // ✅ Dùng lại View Index, không cần tạo View mới
            return View("Index", danhSachSize);
        }

        [HttpGet]
        public IActionResult GetAllActive()
        {
            var data = _context.Sizes
                .Where(x => x.TrangThai == 1)
                .Select(x => new {
                    maSize = x.MaSize,
                    tenSize = x.TenSize
                })
                .ToList();

            return Json(data);
        }

        // GET: Admin/Size/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var size = await _context.Sizes
                .FirstOrDefaultAsync(m => m.MaSize == id);
            if (size == null)
            {
                return NotFound();
            }

            return View(size);
        }

        // GET: Admin/Size/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Size/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaSize,TenSize,MoTa,TrangThai")] Size size)
        {
            // Chuẩn hóa tên size
            var normalizedTenSize = size.TenSize.Trim().ToLower();

            // Kiểm tra trùng tên
            var isDuplicate = await _context.Sizes
                .AnyAsync(s => s.TenSize.Trim().ToLower() == normalizedTenSize);

            if (isDuplicate)
            {
                ModelState.AddModelError("TenSize", "Tên size đã tồn tại.");
                return View(size);
            }

            // ✅ Mặc định trạng thái = 1
            size.TrangThai = 1;

            _context.Add(size);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateQuick(string TenSize, string? MoTa, int TrangThai)
        {
            if (string.IsNullOrWhiteSpace(TenSize))
                return Json(new { success = false, message = "Tên size không được để trống" });

            // Kiểm tra trùng tên
            var normalizedTen = TenSize.Trim().ToLower();
            var isDuplicate = await _context.Sizes.AnyAsync(s => s.TenSize.Trim().ToLower() == normalizedTen);
            if (isDuplicate)
            {
                return Json(new { success = false, message = "Tên size đã tồn tại." });
            }

            var size = new Size
            {
                TenSize = TenSize.Trim(),
                MoTa = MoTa,
                TrangThai = 1
            };

            _context.Sizes.Add(size);
            await _context.SaveChangesAsync();

            return Json(new { success = true, id = size.MaSize, ten = size.TenSize });
        }

        // GET: Admin/Size/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var size = await _context.Sizes.FindAsync(id);
            if (size == null)
            {
                return NotFound();
            }
            return View(size);
        }

        // POST: Admin/Size/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaSize,TenSize,MoTa,TrangThai")] Size size)
        {
            if (id != size.MaSize)
            {
                return NotFound();
            }

            // Lấy bản ghi cũ từ DB
            var existing = await _context.Sizes.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            // Chuẩn hóa tên size
            var normalizedTenSize = size.TenSize.Trim().ToLower();

            // Kiểm tra trùng tên với bản ghi khác
            var isDuplicate = await _context.Sizes
                .AnyAsync(s => s.TenSize.Trim().ToLower() == normalizedTenSize
                               && s.MaSize != size.MaSize);

            if (isDuplicate)
            {
                ModelState.AddModelError("TenSize", "Tên size đã tồn tại.");
                return View(size);
            }

            try
            {
                // ✅ Cập nhật thủ công để tránh ghi đè dữ liệu ngoài ý muốn
                existing.TenSize = size.TenSize;
                existing.MoTa = size.MoTa;
                existing.TrangThai = 1; // hoặc giữ nguyên nếu bạn muốn

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SizeExists(size.MaSize))
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



        // GET: Admin/Size/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var size = await _context.Sizes
                .FirstOrDefaultAsync(m => m.MaSize == id);
            if (size == null)
            {
                return NotFound();
            }

            return View(size);
        }

        // POST: Admin/Size/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var size = await _context.Sizes.FindAsync(id);
            if (size != null)
            {
                _context.Sizes.Remove(size);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool SizeExists(int id)
        {
            return _context.Sizes.Any(e => e.MaSize == id);
        }
    }
}
