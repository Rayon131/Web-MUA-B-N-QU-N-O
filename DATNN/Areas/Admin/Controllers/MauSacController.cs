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
    public class MauSacController : Controller
    {
        private readonly MyDbContext _context;

        public MauSacController(MyDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        public IActionResult GetAllActive()
        {
            var data = _context.MauSacs
                .Where(x => x.TrangThai == 1)
                .Select(x => new {
                    maMauSac = x.MaMauSac,
                    tenMau = x.TenMau
                })
                .ToList();

            return Json(data);
        }
        // GET: Admin/MauSac
        public ActionResult Index(int page = 1)
        {
            int pageSize = 14;

            // ✅ Chỉ đếm màu đang kích hoạt
            var totalItems = _context.MauSacs
                .Where(m => m.TrangThai == 1)
                .Count();

            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            // ✅ Chỉ lấy màu có trạng thái = 1
            var danhSachMau = _context.MauSacs
                .Where(m => m.TrangThai == 1)
                .OrderBy(m => m.MaMauSac)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(danhSachMau);
        }
        public ActionResult VoHieuHoa(int page = 1)
        {
            int pageSize = 14;

            // ✅ Chỉ đếm màu đang bị vô hiệu hóa
            var totalItems = _context.MauSacs
                .Where(m => m.TrangThai == 0)
                .Count();

            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            // ✅ Chỉ lấy màu có trạng thái = 0
            var danhSachMau = _context.MauSacs
                .Where(m => m.TrangThai == 0)
                .OrderBy(m => m.MaMauSac)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            // ✅ Không cần View mới → dùng lại View Index
            return View("Index", danhSachMau);
        }

        public async Task<IActionResult> VoHieu(int id)
        {
            var item = await _context.MauSacs.FindAsync(id);
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
            var item = await _context.MauSacs.FindAsync(id);
            if (item == null)
                return NotFound();

            item.TrangThai = 1;
            _context.Update(item);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(VoHieuHoa)); // quay về danh sách bị vô hiệu hóa
        }
        // GET: Admin/MauSac/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mauSac = await _context.MauSacs
                .FirstOrDefaultAsync(m => m.MaMauSac == id);
            if (mauSac == null)
            {
                return NotFound();
            }

            return View(mauSac);
        }

        // GET: Admin/MauSac/Create
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken] // Chỉ cần 1 attribute là đủ
        public async Task<IActionResult> CreateQuick(string TenMau, string MoTa, int TrangThai)
        {
            if (string.IsNullOrWhiteSpace(TenMau))
                return Json(new { success = false, message = "Tên màu không được để trống." });

            if (string.IsNullOrWhiteSpace(MoTa) ||
                !System.Text.RegularExpressions.Regex.IsMatch(MoTa.Trim(), "^#([0-9a-fA-F]{6})$"))
                return Json(new { success = false, message = "Mã màu không hợp lệ. (VD: #FFFFFF)" });

            // ✅ Kiểm tra trùng tên
            var normalizedTen = TenMau.Trim().ToLower();
            var isDuplicateTen = await _context.MauSacs
                .AnyAsync(m => m.TenMau.Trim().ToLower() == normalizedTen);

            if (isDuplicateTen)
            {
                return Json(new { success = false, message = "Tên màu đã tồn tại." });
            }

            // ✅ Kiểm tra trùng mã màu
            var normalizedMaMau = MoTa.Trim().ToLower();
            var isDuplicateMaMau = await _context.MauSacs
                .AnyAsync(m => m.MoTa.Trim().ToLower() == normalizedMaMau);

            if (isDuplicateMaMau)
            {
                return Json(new { success = false, message = "Mã màu đã tồn tại." });
            }

            // ✅ Tạo mới
            var mau = new MauSac
            {
                TenMau = TenMau.Trim(),
                MoTa = MoTa.Trim(),
                TrangThai = 1
            };

            _context.MauSacs.Add(mau);
            await _context.SaveChangesAsync();

            return Json(new { success = true, id = mau.MaMauSac, ten = mau.TenMau });
        }


        // POST: Admin/MauSac/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaMauSac,TenMau,MoTa,TrangThai")] MauSac mauSac)
        {
            // Chuẩn hóa tên màu
            var normalizedTenMau = mauSac.TenMau.Trim().ToLower();

            // Kiểm tra trùng tên
            var isDuplicate = await _context.MauSacs
                .AnyAsync(m => m.TenMau.Trim().ToLower() == normalizedTenMau);

            if (isDuplicate)
            {
                ModelState.AddModelError("TenMau", "Tên màu đã tồn tại.");
                return View(mauSac);
            }

            // Kiểm tra mã màu hợp lệ (#RRGGBB)
            if (string.IsNullOrWhiteSpace(mauSac.MoTa) ||
                !System.Text.RegularExpressions.Regex.IsMatch(mauSac.MoTa.Trim(), "^#([0-9a-fA-F]{6})$"))
            {
                ModelState.AddModelError("MoTa", "Mã màu không hợp lệ. Vui lòng nhập mã màu dạng #RRGGBB.");
                return View(mauSac);
            }

            // ✅ Kiểm tra trùng mã màu
            var normalizedMaMau = mauSac.MoTa.Trim().ToLower();

            var isDuplicateMaMau = await _context.MauSacs
                .AnyAsync(m => m.MoTa.Trim().ToLower() == normalizedMaMau);

            if (isDuplicateMaMau)
            {
                ModelState.AddModelError("MoTa", "Mã màu đã tồn tại.");
                return View(mauSac);
            }

            // ✅ Mặc định trạng thái = 1
            mauSac.TrangThai = 1;

            _context.Add(mauSac);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        // GET: Admin/MauSac/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mauSac = await _context.MauSacs.FindAsync(id);
            if (mauSac == null)
            {
                return NotFound();
            }
            return View(mauSac);
        }

        // POST: Admin/MauSac/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaMauSac,TenMau,MoTa,TrangThai")] MauSac mauSac)
        {
            if (id != mauSac.MaMauSac)
            {
                return NotFound();
            }

            // Lấy bản ghi cũ từ DB
            var existing = await _context.MauSacs.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            // Chuẩn hóa tên màu để kiểm tra trùng
            var normalizedTenMau = mauSac.TenMau.Trim().ToLower();

            // Kiểm tra trùng tên với bản ghi khác
            var isDuplicate = await _context.MauSacs
                .AnyAsync(m => m.TenMau.Trim().ToLower() == normalizedTenMau
                               && m.MaMauSac != mauSac.MaMauSac);

            if (isDuplicate)
            {
                ModelState.AddModelError("TenMau", "Tên màu đã tồn tại.");
                return View(mauSac);
            }

            // Kiểm tra mã màu hợp lệ (#RRGGBB)
            if (string.IsNullOrWhiteSpace(mauSac.MoTa) ||
                !System.Text.RegularExpressions.Regex.IsMatch(mauSac.MoTa.Trim(), "^#([0-9a-fA-F]{6})$"))
            {
                ModelState.AddModelError("MoTa", "Mã màu không hợp lệ. Vui lòng nhập mã màu dạng #RRGGBB.");
                return View(mauSac);
            }

            // ✅ Kiểm tra trùng mã màu với bản ghi khác
            var normalizedMaMau = mauSac.MoTa.Trim().ToLower();

            var isDuplicateMaMau = await _context.MauSacs
                .AnyAsync(m => m.MoTa.Trim().ToLower() == normalizedMaMau
                               && m.MaMauSac != mauSac.MaMauSac);

            if (isDuplicateMaMau)
            {
                ModelState.AddModelError("MoTa", "Mã màu đã tồn tại.");
                return View(mauSac);
            }

            try
            {
                // ✅ Cập nhật thủ công để tránh ghi đè dữ liệu ngoài ý muốn
                existing.TenMau = mauSac.TenMau;
                existing.MoTa = mauSac.MoTa;
                existing.TrangThai = 1;

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MauSacExists(mauSac.MaMauSac))
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



        // GET: Admin/MauSac/Delete/5


        // POST: Admin/MauSac/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var mauSac = await _context.MauSacs.FindAsync(id);
            if (mauSac != null)
            {
                _context.MauSacs.Remove(mauSac);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MauSacExists(int id)
        {
            return _context.MauSacs.Any(e => e.MaMauSac == id);
        }
    }
}
