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
using DATNN.ViewModel;
using Microsoft.AspNetCore.Identity;

namespace DATNN.Controllers
{
    public class NguoiDungController : Controller
    {
        private readonly MyDbContext _context;

        public NguoiDungController(MyDbContext context)
        {
            _context = context;
        }
		public async Task<IActionResult> Edit()
		{
			// Lấy ID của người dùng hiện tại từ Claims
			var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (string.IsNullOrEmpty(userIdClaim))
			{
				return RedirectToAction("Index", "Login"); // Nếu chưa đăng nhập thì quay lại Login
			}

			int userId = int.Parse(userIdClaim);

			var nguoiDung = await _context.NguoiDungs.FindAsync(userId);
			if (nguoiDung == null)
			{
				return NotFound();
			}

			// Truyền dữ liệu ra View
			return View(nguoiDung);
		}

		// POST: NguoiDung/Edit
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit([Bind("MaNguoiDung,HoTen,TenDangNhap,MatKhau,SoDienThoai,Email,NgaySinh,GioiTinh")] NguoiDung nguoiDung)
		{
			var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (string.IsNullOrEmpty(userIdClaim))
			{
				return RedirectToAction("Index", "Login");
			}

			int userId = int.Parse(userIdClaim);

			if (userId != nguoiDung.MaNguoiDung)
			{
				return Unauthorized(); // Không cho sửa tài khoản khác
			}

			try
			{
				// Chỉ update các field được phép (tránh ghi đè Quyen, NgayTao, TrangThai...)
				var userDb = await _context.NguoiDungs.FindAsync(userId);
				if (userDb == null) return NotFound();

				userDb.HoTen = nguoiDung.HoTen;
				userDb.SoDienThoai = nguoiDung.SoDienThoai;
				userDb.Email = nguoiDung.Email;
				userDb.NgaySinh = nguoiDung.NgaySinh;
				userDb.GioiTinh = nguoiDung.GioiTinh;
				userDb.MatKhau = nguoiDung.MatKhau; // ❗Ở đây bạn đang lưu plaintext, sau này nên mã hóa

				await _context.SaveChangesAsync();
			}
			catch (DbUpdateConcurrencyException)
			{
				if (!_context.NguoiDungs.Any(e => e.MaNguoiDung == nguoiDung.MaNguoiDung))
				{
					return NotFound();
				}
				else
				{
					throw;
				}
			}

			return RedirectToAction("Index", "Home"); // Sau khi sửa xong chuyển về trang chủ
		}

        public async Task<IActionResult> ChangePassword()
        {
            // Lấy ID người dùng hiện tại từ Claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return RedirectToAction("Index", "Login");
            }

            int userId = int.Parse(userIdClaim);

            // Tìm người dùng hiện tại
            var nguoiDung = await _context.NguoiDungs.FindAsync(userId);
            if (nguoiDung == null)
            {
                return NotFound();
            }

            // Tạo ViewModel để hiển thị thông tin cơ bản
            var model = new DoiMatKhauViewModel
            {
                TenDangNhap = nguoiDung.TenDangNhap,
                Email = nguoiDung.Email
            };

            return View(model);
        }


        // POST: xử lý đổi mật khẩu
        [HttpPost]
        public async Task<IActionResult> ChangePassword(DoiMatKhauViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Lấy ID người dùng từ Claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                TempData["ToastMessage"] = "⚠️ Bạn cần đăng nhập để đổi mật khẩu.";
                return RedirectToAction("Index", "Login");
            }

            int userId = int.Parse(userIdClaim);
            var user = await _context.NguoiDungs.FindAsync(userId);
            if (user == null)
            {
                TempData["ToastMessage"] = "❌ Không tìm thấy người dùng.";
                return RedirectToAction("Edit");
            }

            var hasher = new PasswordHasher<NguoiDung>();

            // ✅ Kiểm tra mật khẩu cũ có khớp với mật khẩu đã hash trong DB không
            var verifyResult = hasher.VerifyHashedPassword(user, user.MatKhau, model.MatKhauCu);
            if (verifyResult == PasswordVerificationResult.Failed)
            {
                TempData["ToastMessage"] = "❌ Mật khẩu cũ không đúng.";
                return View(model);
            }

            // ✅ Hash lại mật khẩu mới trước khi lưu
            user.MatKhau = hasher.HashPassword(user, model.MatKhauMoi);
            await _context.SaveChangesAsync();

            TempData["ToastMessage"] = "✅ Đổi mật khẩu thành công!";
            return RedirectToAction("Edit");
        }


    }
}
