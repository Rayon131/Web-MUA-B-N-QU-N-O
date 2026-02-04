using DATNN.Models;
using DATNN.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace DATNN.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin")]
    public class KhachHangController : Controller
    {
       
        private readonly MyDbContext _context;

        public KhachHangController(MyDbContext context)
        {
            _context = context;
        }
		//GET: KhachHang
		//public async Task<IActionResult> Index()
		//{
		//    var khachHangs = _context.NguoiDungs
		//                             .Include(n => n.Quyen)
		//                             .Where(n => n.Quyen.MaVaiTro == "KHACHHANG");
		//    return View(await khachHangs.ToListAsync());
		//}

		// GET: KhachHang
		public async Task<IActionResult> Index(string searchString = "")
		{
			const string roleKH = "KHACHHANG";

			var query = _context.NguoiDungs
								.Include(n => n.Quyen)
								.Where(n => n.Quyen.MaVaiTro == roleKH);

			// Nếu có nhập tìm kiếm
			if (!string.IsNullOrWhiteSpace(searchString))
			{
				string keyword = searchString.Trim();

				query = query.Where(u =>
					u.HoTen.Contains(keyword) ||
					u.TenDangNhap.Contains(keyword) ||
					u.Email.Contains(keyword) ||
					u.SoDienThoai.Contains(keyword)
				);
			}

			ViewBag.SearchString = searchString;

			return View(await query.ToListAsync());
		}


		// GET: KhachHang/Details/5
		public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var nguoiDung = await _context.NguoiDungs
                .Include(n => n.Quyen)
                .FirstOrDefaultAsync(m => m.MaNguoiDung == id && m.Quyen.MaVaiTro == "KHACHHANG");
            if (nguoiDung == null) return NotFound();
            return View(nguoiDung);
        }

        // GET: KhachHang/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            // 1. Lấy dữ liệu gốc từ DB
            var nguoiDung = await _context.NguoiDungs.AsNoTracking()
                .FirstOrDefaultAsync(u => u.MaNguoiDung == id && u.Quyen.MaVaiTro == "KHACHHANG");

            if (nguoiDung == null) return NotFound();

            // 2. Map dữ liệu từ Model sang ViewModel
            var viewModel = new EditKhachHangViewModel
            {
                MaNguoiDung = nguoiDung.MaNguoiDung,
                HoTen = nguoiDung.HoTen,
                SoDienThoai = nguoiDung.SoDienThoai,
                Email = nguoiDung.Email,
                TrangThai = nguoiDung.TrangThai,
                NgaySinh = nguoiDung.NgaySinh,
                GioiTinh = nguoiDung.GioiTinh,
                TenDangNhap = nguoiDung.TenDangNhap,
                NgayTao = nguoiDung.NgayTao
            };

            // 3. Trả về View với ViewModel
            return View(viewModel);
        }

        // === BẮT ĐẦU SỬA LẠI HOÀN TOÀN ACTION POST EDIT ===
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditKhachHangViewModel viewModel)
        {
            if (id != viewModel.MaNguoiDung)
            {
                return NotFound();
            }

            // Kiểm tra trùng lặp Email với những người dùng KHÁC
            if (!string.IsNullOrWhiteSpace(viewModel.Email))
            {
                if (await _context.NguoiDungs.AnyAsync(u => u.Email == viewModel.Email && u.MaNguoiDung != id))
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng bởi một tài khoản khác.");
                }
            }

            // SỬA LỖI LOGIC: Chỉ lưu khi Model HỢP LỆ
            if (ModelState.IsValid)
            {
                try
                {
                    // SỬA LỖI TRUY VẤN: Dùng FirstOrDefaultAsync và Include để lấy cả Quyen
                    var userInDb = await _context.NguoiDungs
                                                .Include(u => u.Quyen)
                                                .FirstOrDefaultAsync(u => u.MaNguoiDung == id);

                    // Bây giờ userInDb.Quyen sẽ không còn null nữa
                    if (userInDb == null || userInDb.Quyen?.MaVaiTro != "KHACHHANG")
                    {
                        return NotFound();
                    }

                    // Cập nhật các trường từ ViewModel
                    userInDb.HoTen = viewModel.HoTen;
                    userInDb.SoDienThoai = viewModel.SoDienThoai;
                    userInDb.Email = viewModel.Email;
                    userInDb.TrangThai = viewModel.TrangThai;
                    userInDb.NgaySinh = viewModel.NgaySinh;
                    userInDb.GioiTinh = viewModel.GioiTinh;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.NguoiDungs.Any(e => e.MaNguoiDung == viewModel.MaNguoiDung)) return NotFound();
                    else throw;
                }
                TempData["SuccessMessage"] = "Cập nhật thông tin khách hàng thành công!";
                return RedirectToAction(nameof(Index));
            }

            // Nếu có lỗi validation, trả lại View với dữ liệu đã nhập
            return View(viewModel);
        }
        // GET: KhachHang/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var nguoiDung = await _context.NguoiDungs
                .Include(n => n.Quyen)
                .FirstOrDefaultAsync(m => m.MaNguoiDung == id && m.Quyen.MaVaiTro == "KHACHHANG");
            if (nguoiDung == null) return NotFound();
            return View(nguoiDung);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var nguoiDung = await _context.NguoiDungs.FindAsync(id);
            _context.NguoiDungs.Remove(nguoiDung);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ToggleStatus(int id)
		{
			var userInDb = await _context.NguoiDungs
				.Include(u => u.Quyen)
				.FirstOrDefaultAsync(u => u.MaNguoiDung == id);

			if (userInDb == null)
			{
				return NotFound();
			}

			// Chỉ cho phép đổi trạng thái khách hàng
			if (userInDb.Quyen.MaVaiTro != "KHACHHANG")
			{
				return Forbid(); // Không phải khách hàng → không cho phép thao tác
			}

			// Đảo trạng thái
			userInDb.TrangThai = userInDb.TrangThai == 1 ? 0 : 1;

			_context.Update(userInDb);
			await _context.SaveChangesAsync();

			return RedirectToAction(nameof(Index));
		}

	}
}
