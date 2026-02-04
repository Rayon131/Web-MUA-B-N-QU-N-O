using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DATNN;
using DATNN.Models;
using System.Security.Claims;
using DATNN.Service;

namespace DATNN.Controllers
{
	public class DiaChiNguoiDungController : Controller
	{
		private readonly MyDbContext _context;
        private readonly GeocodingService _geocodingService;

        public DiaChiNguoiDungController(MyDbContext context, GeocodingService geocodingService)
		{
			_context = context;
            _geocodingService = geocodingService;
        }

        // GET: DiaChiNguoiDung
        public async Task<IActionResult> Index()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Index", "Login");
            }

            int userId = int.Parse(userIdString);

            // Lấy danh sách địa chỉ có trạng thái 0 hoặc 1, sắp xếp trạng thái 1 lên đầu
            var diaChiNguoiDungs = _context.DiaChiNguoiDungs
                .Include(d => d.NguoiDung)
                .Where(d => d.MaNguoiDung == userId && (d.TrangThai == 0 || d.TrangThai == 1))
                .OrderByDescending(d => d.TrangThai); // trạng thái 1 sẽ lên đầu

            return View(await diaChiNguoiDungs.ToListAsync());
        }


        // GET: DiaChiNguoiDung/Details/5
        public async Task<IActionResult> Details(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var diaChiNguoiDung = await _context.DiaChiNguoiDungs
				.Include(d => d.NguoiDung)
				.FirstOrDefaultAsync(m => m.Id == id);
			if (diaChiNguoiDung == null)
			{
				return NotFound();
			}

			return View(diaChiNguoiDung);
		}

        // GET: DiaChiNguoiDung/Create
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Ten,SoDienThoai,ChiTietDiaChi")] DiaChiNguoiDung diaChiNguoiDung)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Index", "Login");
            }

            var userId = int.Parse(userIdString);
            diaChiNguoiDung.MaNguoiDung = userId;

            // Lấy tên địa phương từ hidden input
            diaChiNguoiDung.Phuong = Request.Form["TenPhuong"];
            diaChiNguoiDung.Quan = Request.Form["TenQuan"];
            diaChiNguoiDung.ThanhPho = Request.Form["TenThanhPho"];

            // Làm sạch địa chỉ
            diaChiNguoiDung.ChiTietDiaChi = diaChiNguoiDung.ChiTietDiaChi?.Trim();

            // ========================= SỬA ĐỔI Ở ĐÂY =========================
            // Gọi GeocodingService để lấy tọa độ theo logic mới
            var (lat, lon) = await _geocodingService.LayToaDoTuDiaChiAsync(
                diaChiNguoiDung.ChiTietDiaChi,
                diaChiNguoiDung.Phuong,
                diaChiNguoiDung.Quan,
                diaChiNguoiDung.ThanhPho
            );
            // =================================================================

            diaChiNguoiDung.Latitude = lat;
            diaChiNguoiDung.Longitude = lon;

            // Kiểm tra user đã có địa chỉ chưa
            bool hasAddress = await _context.DiaChiNguoiDungs.AnyAsync(d => d.MaNguoiDung == userId && (d.TrangThai == 0 || d.TrangThai == 1));
            diaChiNguoiDung.TrangThai = hasAddress ? 0 : 1;

            _context.Add(diaChiNguoiDung);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        // GET: DiaChiNguoiDung/Edit/5
        public async Task<IActionResult> Edit(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var diaChiNguoiDung = await _context.DiaChiNguoiDungs.FindAsync(id);
			if (diaChiNguoiDung == null)
			{
				return NotFound();
			}
			ViewData["MaNguoiDung"] = new SelectList(_context.NguoiDungs, "MaNguoiDung", "Email", diaChiNguoiDung.MaNguoiDung);
			return View(diaChiNguoiDung);
		}

        // POST: DiaChiNguoiDung/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Ten,SoDienThoai,ChiTietDiaChi")] DiaChiNguoiDung diaChiNguoiDung)
        {
            if (id != diaChiNguoiDung.Id)
            {
                return NotFound();
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Index", "Login");
            }

            var userId = int.Parse(userIdString);

            // Lấy bản ghi cũ từ DB
            var diaChiCu = await _context.DiaChiNguoiDungs.FirstOrDefaultAsync(d => d.Id == id && d.MaNguoiDung == userId);
            if (diaChiCu == null)
            {
                return NotFound();
            }

            try
            {
                // Cập nhật thông tin mới
                diaChiCu.Ten = diaChiNguoiDung.Ten;
                diaChiCu.SoDienThoai = diaChiNguoiDung.SoDienThoai;
                diaChiCu.ChiTietDiaChi = diaChiNguoiDung.ChiTietDiaChi?.Trim();

                // Lấy tên địa phương từ hidden input
                diaChiCu.Phuong = Request.Form["TenPhuong"];
                diaChiCu.Quan = Request.Form["TenQuan"];
                diaChiCu.ThanhPho = Request.Form["TenThanhPho"];

                // ========================= SỬA ĐỔI Ở ĐÂY =========================
                // Gọi GeocodingService để lấy tọa độ mới theo logic mới
                var (lat, lon) = await _geocodingService.LayToaDoTuDiaChiAsync(
                    diaChiCu.ChiTietDiaChi,
                    diaChiCu.Phuong,
                    diaChiCu.Quan,
                    diaChiCu.ThanhPho
                );
                // =================================================================

                diaChiCu.Latitude = lat;
                diaChiCu.Longitude = lon;

                // Không thay đổi MaNguoiDung và TrangThai
                _context.Update(diaChiCu);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.DiaChiNguoiDungs.Any(e => e.Id == diaChiNguoiDung.Id))
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
        public async Task<IActionResult> SetHidden(int id)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Index", "Login");
            }

            var userId = int.Parse(userIdString);

            // Tìm địa chỉ theo id và thuộc về user
            var address = await _context.DiaChiNguoiDungs.FirstOrDefaultAsync(d => d.Id == id && d.MaNguoiDung == userId);
            if (address == null)
            {
                return NotFound();
            }
            if (address.TrangThai == 1) { TempData["ToastMessage"] = "Bạn cần đổi trạng thái mặc định sang địa chỉ khác trước khi xóa."; return RedirectToAction(nameof(Index)); }
            address.TrangThai = 2;
            _context.Update(address);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }




        private bool DiaChiNguoiDungExists(int id)
		{
			return _context.DiaChiNguoiDungs.Any(e => e.Id == id);
		}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetDefault(int id)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString))
            {
                return RedirectToAction("Index", "Login");
            }

            var userId = int.Parse(userIdString);

            // Tìm địa chỉ đang là mặc định
            var currentDefault = await _context.DiaChiNguoiDungs
                .FirstOrDefaultAsync(d => d.MaNguoiDung == userId && d.TrangThai == 1);

            if (currentDefault != null && currentDefault.Id != id)
            {
                currentDefault.TrangThai = 0;
            }

            // Set mặc định cho địa chỉ mới
            var newDefault = await _context.DiaChiNguoiDungs.FindAsync(id);
            if (newDefault != null && newDefault.MaNguoiDung == userId)
            {
                newDefault.TrangThai = 1;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


    }
}
