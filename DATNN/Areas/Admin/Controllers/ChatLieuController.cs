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
    public class ChatLieuController : Controller
    {
        private readonly MyDbContext _context;

        public ChatLieuController(MyDbContext context)
        {
            _context = context;
        }

        // GET: Admin/ChatLieu
        public async Task<IActionResult> Index(int page = 1)
        {
            int pageSize = 14;

            var totalItems = await _context.ChatLieus
                .Where(c => c.TrangThai == 1)
                .CountAsync();

            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var danhSachChatLieu = await _context.ChatLieus
                .Where(c => c.TrangThai == 1)
                .OrderBy(c => c.MaChatLieu)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(danhSachChatLieu);
        }
        public async Task<IActionResult> VoHieu(int id)
        {
            var item = await _context.ChatLieus.FindAsync(id);
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
            var item = await _context.ChatLieus.FindAsync(id);
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

            var totalItems = await _context.ChatLieus
                .Where(c => c.TrangThai == 0)
                .CountAsync();

            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var danhSachChatLieu = await _context.ChatLieus
                .Where(c => c.TrangThai == 0)
                .OrderBy(c => c.MaChatLieu)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            // ✅ Dùng lại View Index
            return View("Index", danhSachChatLieu);
        }

        // GET: Admin/ChatLieu/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var chatLieu = await _context.ChatLieus
                .FirstOrDefaultAsync(m => m.MaChatLieu == id);
            if (chatLieu == null)
            {
                return NotFound();
            }

            return View(chatLieu);
        }

        // GET: Admin/ChatLieu/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/ChatLieu/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaChatLieu,TenChatLieu,MoTa,TrangThai")] ChatLieu chatLieu)
        {
            // Chuẩn hóa tên chất liệu
            var normalizedTenChatLieu = chatLieu.TenChatLieu.Trim().ToLower();

            // Kiểm tra trùng tên
            var isDuplicate = await _context.ChatLieus
                .AnyAsync(c => c.TenChatLieu.Trim().ToLower() == normalizedTenChatLieu);

            if (isDuplicate)
            {
                ModelState.AddModelError("TenChatLieu", "Tên chất liệu đã tồn tại.");
                return View(chatLieu);
            }

            // ✅ Mặc định trạng thái = 1
            chatLieu.TrangThai = 1;

            _context.Add(chatLieu);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateQuick(string TenChatLieu, string? MoTa, int TrangThai)
        {
            if (string.IsNullOrWhiteSpace(TenChatLieu))
                return Json(new { success = false, message = "Tên chất liệu không được để trống" });

            // Kiểm tra trùng tên
            var normalizedTen = TenChatLieu.Trim().ToLower();
            var isDuplicate = await _context.ChatLieus.AnyAsync(c => c.TenChatLieu.Trim().ToLower() == normalizedTen);
            if (isDuplicate)
            {
                return Json(new { success = false, message = "Tên chất liệu đã tồn tại." });
            }

            var chatLieu = new ChatLieu
            {
                TenChatLieu = TenChatLieu.Trim(),
                MoTa = MoTa,
                TrangThai = 1
            };

            _context.ChatLieus.Add(chatLieu);
            await _context.SaveChangesAsync();

            return Json(new { success = true, id = chatLieu.MaChatLieu, ten = chatLieu.TenChatLieu });
        }

        // GET: Admin/ChatLieu/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var chatLieu = await _context.ChatLieus.FindAsync(id);
            if (chatLieu == null)
            {
                return NotFound();
            }
            return View(chatLieu);
        }

        // POST: Admin/ChatLieu/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaChatLieu,TenChatLieu,MoTa,TrangThai")] ChatLieu chatLieu)
        {
            if (id != chatLieu.MaChatLieu)
            {
                return NotFound();
            }

            // Lấy bản ghi cũ từ DB
            var existing = await _context.ChatLieus.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            // Chuẩn hóa tên để so sánh
            var normalizedTenChatLieu = chatLieu.TenChatLieu.Trim().ToLower();

            // Kiểm tra tên đã tồn tại ở bản ghi khác
            var isDuplicate = await _context.ChatLieus
                .AnyAsync(c => c.TenChatLieu.Trim().ToLower() == normalizedTenChatLieu
                               && c.MaChatLieu != chatLieu.MaChatLieu);

            if (isDuplicate)
            {
                ModelState.AddModelError("TenChatLieu", "Tên chất liệu đã tồn tại.");
                return View(chatLieu);
            }

            try
            {
                // ✅ Cập nhật thủ công để tránh ghi đè dữ liệu ngoài ý muốn
                existing.TenChatLieu = chatLieu.TenChatLieu;
                existing.MoTa = chatLieu.MoTa;
                existing.TrangThai = 1; // hoặc giữ nguyên nếu bạn muốn

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ChatLieuExists(chatLieu.MaChatLieu))
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


        // GET: Admin/ChatLieu/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var chatLieu = await _context.ChatLieus
                .FirstOrDefaultAsync(m => m.MaChatLieu == id);
            if (chatLieu == null)
            {
                return NotFound();
            }

            return View(chatLieu);
        }

        // POST: Admin/ChatLieu/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var chatLieu = await _context.ChatLieus.FindAsync(id);
            if (chatLieu != null)
            {
                _context.ChatLieus.Remove(chatLieu);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ChatLieuExists(int id)
        {
            return _context.ChatLieus.Any(e => e.MaChatLieu == id);
        }
    }
}
