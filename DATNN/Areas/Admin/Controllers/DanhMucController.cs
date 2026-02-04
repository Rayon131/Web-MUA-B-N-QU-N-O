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
    public class DanhMucController : Controller
    {
        private readonly MyDbContext _context;

        public DanhMucController(MyDbContext context)
        {
            _context = context;
        }

        // GET: Admin/DanhMuc
        public async Task<IActionResult> Index(int page = 1)
        {
            int pageSize = 14;

            var totalItems = await _context.DanhMucs
                .Where(d => d.TrangThai == 1)
                .CountAsync();

            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var danhSachDanhMuc = await _context.DanhMucs
                .Where(d => d.TrangThai == 1)
                .OrderBy(d => d.MaDanhMuc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(danhSachDanhMuc);
        }
        [HttpGet]
        public async Task<IActionResult> VoHieuHoa(int page = 1)
        {
            int pageSize = 14;

            var totalItems = await _context.DanhMucs
                .Where(d => d.TrangThai == 0)
                .CountAsync();

            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var danhSachDanhMuc = await _context.DanhMucs
                .Where(d => d.TrangThai == 0)
                .OrderBy(d => d.MaDanhMuc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            // ✅ Dùng lại View Index
            return View("Index", danhSachDanhMuc);
        }
     
        public async Task<IActionResult>VoHieu(int id)
        {
            var item = await _context.DanhMucs.FindAsync(id);
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
            var item = await _context.DanhMucs.FindAsync(id);
            if (item == null)
                return NotFound();

            item.TrangThai = 1;
            _context.Update(item);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(VoHieuHoa)); // quay về danh sách bị vô hiệu hóa
        }

        // GET: Admin/DanhMuc/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var danhMuc = await _context.DanhMucs
                .FirstOrDefaultAsync(m => m.MaDanhMuc == id);
            if (danhMuc == null)
            {
                return NotFound();
            }

            return View(danhMuc);
        }

        // GET: Admin/DanhMuc/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/DanhMuc/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaDanhMuc,TenDanhMuc,MoTa,TrangThai")] DanhMuc danhMuc)
        {
            // Chuẩn hóa tên danh mục
            var normalizedTenDanhMuc = danhMuc.TenDanhMuc.Trim().ToLower();

            // Kiểm tra trùng tên
            var isDuplicate = await _context.DanhMucs
                .AnyAsync(dm => dm.TenDanhMuc.Trim().ToLower() == normalizedTenDanhMuc);

            if (isDuplicate)
            {
                ModelState.AddModelError("TenDanhMuc", "Tên danh mục đã tồn tại.");
                return View(danhMuc);
            }

            // ✅ Mặc định trạng thái = 1
            danhMuc.TrangThai = 1;

            _context.Add(danhMuc);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/DanhMuc/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var danhMuc = await _context.DanhMucs.FindAsync(id);
            if (danhMuc == null)
            {
                return NotFound();
            }
            return View(danhMuc);
        }

        // POST: Admin/DanhMuc/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaDanhMuc,TenDanhMuc,MoTa")] DanhMuc danhMuc)
        {
            if (id != danhMuc.MaDanhMuc)
            {
                return NotFound();
            }

            // Lấy bản ghi cũ từ DB
            var existing = await _context.DanhMucs.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            // Chuẩn hóa tên để kiểm tra trùng
            var normalizedTenDanhMuc = danhMuc.TenDanhMuc.Trim().ToLower();

            var isDuplicate = await _context.DanhMucs
                .AnyAsync(dm => dm.TenDanhMuc.Trim().ToLower() == normalizedTenDanhMuc
                                && dm.MaDanhMuc != danhMuc.MaDanhMuc);

            if (isDuplicate)
            {
                ModelState.AddModelError("TenDanhMuc", "Tên danh mục đã tồn tại.");
                return View(danhMuc);
            }

            try
            {
                // Cập nhật thủ công để tránh ghi đè trạng thái
                existing.TenDanhMuc = danhMuc.TenDanhMuc;
                existing.MoTa = danhMuc.MoTa;

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DanhMucExists(danhMuc.MaDanhMuc))
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateQuick(string TenDanhMuc, string MoTa, int TrangThai = 1)
        {
            if (string.IsNullOrWhiteSpace(TenDanhMuc))
            {
                return Json(new { success = false, message = "Tên danh mục không được để trống." });
            }

            // Kiểm tra trùng tên
            var normalizedTen = TenDanhMuc.Trim().ToLower();
            var isDuplicate = await _context.DanhMucs.AnyAsync(dm => dm.TenDanhMuc.Trim().ToLower() == normalizedTen);
            if (isDuplicate)
            {
                return Json(new { success = false, message = "Tên danh mục đã tồn tại." });
            }

            var danhMuc = new DanhMuc
            {
                TenDanhMuc = TenDanhMuc.Trim(),
                MoTa = MoTa,
                TrangThai = 1
            };

            _context.DanhMucs.Add(danhMuc);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                id = danhMuc.MaDanhMuc,
                ten = danhMuc.TenDanhMuc
            });
        }




        // GET: Admin/DanhMuc/Delete/5


        // POST: Admin/DanhMuc/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var danhMuc = await _context.DanhMucs.FindAsync(id);
            if (danhMuc != null)
            {
                _context.DanhMucs.Remove(danhMuc);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DanhMucExists(int id)
        {
            return _context.DanhMucs.Any(e => e.MaDanhMuc == id);
        }
    }
}
