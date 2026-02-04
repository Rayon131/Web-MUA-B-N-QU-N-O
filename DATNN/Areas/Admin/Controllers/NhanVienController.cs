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
using System.Net;
using System.Text.RegularExpressions;
using DATNN.ViewModel;
using Microsoft.AspNetCore.Identity;

namespace DATNN.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize(Roles = "admin")]
	public class NhanVienController : Controller
	{
		private readonly MyDbContext _context;
		private readonly PasswordHasher<NguoiDung> _passwordHasher;
		public NhanVienController(MyDbContext context)
		{
			_context = context;
			_passwordHasher = new PasswordHasher<NguoiDung>();
		}
		// GET: Admin/NhanVien
		// GET: Admin/NhanVien
		//public async Task<IActionResult> Index(string statusFilter = "active")
		//{
		//	// Lấy query cơ bản
		//	var query = _context.NguoiDungs
		//						.Include(n => n.Quyen)
		//						.Where(n => n.Quyen.MaVaiTro == "ADMIN" || n.Quyen.MaVaiTro == "NHANVIEN");

		//	// Áp dụng bộ lọc trạng thái
		//	if (statusFilter == "inactive")
		//	{
		//		query = query.Where(u => u.TrangThai == 0);
		//	}
		//	else if (statusFilter == "all")
		//	{
		//		// Không làm gì cả, hiển thị tất cả
		//	}
		//	else // Mặc định là "active"
		//	{
		//		query = query.Where(u => u.TrangThai == 1);
		//	}

		//	ViewBag.CurrentFilter = statusFilter;
		//	return View(await query.ToListAsync());
		//}

		public async Task<IActionResult> Index(string statusFilter = "active", string searchString = "")
		{
			// Query cơ bản: chỉ lấy ADMIN + NHANVIEN
			var query = _context.NguoiDungs
								.Include(n => n.Quyen)
								.Where(n => n.Quyen.MaVaiTro == "ADMIN" || n.Quyen.MaVaiTro == "NHANVIEN");

			// Lọc theo trạng thái
			if (statusFilter == "inactive")
			{
				query = query.Where(u => u.TrangThai == 0);
			}
			else if (statusFilter == "active")
			{
				query = query.Where(u => u.TrangThai == 1);
			}

			// Lọc theo tìm kiếm
			if (!string.IsNullOrWhiteSpace(searchString))
			{
				query = query.Where(u =>
					u.HoTen.Contains(searchString) ||
					u.TenDangNhap.Contains(searchString) ||
					u.SoDienThoai.Contains(searchString)
				);
			}

			// Truyền lại filter để giữ nguyên khi reload
			ViewBag.CurrentFilter = statusFilter;
			ViewBag.SearchString = searchString;

			return View(await query.ToListAsync());
		}


		// GET: Admin/NhanVien/Details/5
		public async Task<IActionResult> Details(int? id)
		{
			if (id == null) return NotFound();
			var nguoiDung = await _context.NguoiDungs
				.Include(n => n.Quyen)
				.FirstOrDefaultAsync(m => m.MaNguoiDung == id);
			if (nguoiDung == null) return NotFound();
			return View(nguoiDung);
		}

		// GET Action
		public IActionResult Create()
		{
			ViewData["MaQuyen"] = new SelectList(_context.Quyens.Where(q => q.MaVaiTro == "ADMIN" || q.MaVaiTro == "NHANVIEN"), "Id", "Ten");
			return View(); // Không cần truyền gì vào View
		}

		// POST Action
		[HttpPost]
		[ValidateAntiForgeryToken]
		// Sửa ở đây: Dùng ViewModel làm tham số
		public async Task<IActionResult> Create(CreateNhanVienViewModel viewModel)
		{
			// Kiểm tra trùng lặp vẫn thực hiện trên _context
			if (!string.IsNullOrWhiteSpace(viewModel.TenDangNhap))
			{
				if (await _context.NguoiDungs.AnyAsync(u => u.TenDangNhap == viewModel.TenDangNhap))
				{
					ModelState.AddModelError("TenDangNhap", "Tên đăng nhập này đã tồn tại.");
				}
			}
			// ... kiểm tra trùng Email tương tự ...

			// Kiểm tra trùng Email
			if (!string.IsNullOrWhiteSpace(viewModel.Email))
			{
				if (await _context.NguoiDungs.AnyAsync(u => u.Email == viewModel.Email))
				{
					ModelState.AddModelError("Email", "Email này đã được đăng ký cho tài khoản khác.");
				}
			}
			// Validate tuổi >= 18
			if (viewModel.NgaySinh != null)
			{
				int tuoi = DateTime.Today.Year - viewModel.NgaySinh.Value.Year;
				if (viewModel.NgaySinh.Value.Date > DateTime.Today.AddYears(-tuoi)) tuoi--;

				if (tuoi < 18)
				{
					ModelState.AddModelError("NgaySinh", "Nhân viên phải từ 18 tuổi trở lên.");
				}
			}

			// Bây giờ ModelState sẽ chỉ validate các trường có trong ViewModel
			if (ModelState.IsValid)
			{
				// "Ánh xạ" (map) dữ liệu từ ViewModel sang Model thật
				var nguoiDung = new NguoiDung
				{
					MaQuyen = viewModel.MaQuyen,
					HoTen = viewModel.HoTen,
					TenDangNhap = viewModel.TenDangNhap,
					MatKhau = _passwordHasher.HashPassword(null, viewModel.MatKhau),
					SoDienThoai = viewModel.SoDienThoai,
					Email = viewModel.Email,
					NgaySinh = viewModel.NgaySinh,
					GioiTinh = viewModel.GioiTinh,
					TrangThai = 1, // Gán giá trị mặc định
					NgayTao = DateTime.Now
				};

				_context.Add(nguoiDung);
				await _context.SaveChangesAsync();
				TempData["SuccessMessage"] = "Thêm mới nhân viên thành công!";
				return RedirectToAction(nameof(Index));
			}

			// Nếu lỗi, trả về View
			ViewData["MaQuyen"] = new SelectList(_context.Quyens.Where(q => q.MaVaiTro == "ADMIN" || q.MaVaiTro == "NHANVIEN"), "Id", "Ten", viewModel.MaQuyen);
			return View(viewModel); // Trả về viewModel để hiển thị lại dữ liệu đã nhập
		}
		// GET: Admin/NhanVien/Edit/5
		public async Task<IActionResult> Edit(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			// 1. Lấy dữ liệu gốc từ database
			var nguoiDung = await _context.NguoiDungs.AsNoTracking().FirstOrDefaultAsync(u => u.MaNguoiDung == id);

			if (nguoiDung == null)
			{
				return NotFound();
			}

			// 2. Ánh xạ (map) dữ liệu từ Model NguoiDung sang EditNhanVienViewModel
			var viewModel = new EditNhanVienViewModel
			{
				MaNguoiDung = nguoiDung.MaNguoiDung,
				HoTen = nguoiDung.HoTen,
				SoDienThoai = nguoiDung.SoDienThoai,
				Email = nguoiDung.Email,
				TrangThai = nguoiDung.TrangThai,
				NgaySinh = nguoiDung.NgaySinh,
				GioiTinh = nguoiDung.GioiTinh,
				// BỔ SUNG 2 DÒNG NÀY
				TenDangNhap = nguoiDung.TenDangNhap,
				NgayTao = nguoiDung.NgayTao
			};

			// 3. Trả về View với đối tượng viewModel
			return View(viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		// Sửa ở đây: Tham số đầu vào là EditNhanVienViewModel
		public async Task<IActionResult> Edit(int id, EditNhanVienViewModel viewModel)
		{
			// Kiểm tra xem ID từ URL có khớp với ID trong form không
			if (id != viewModel.MaNguoiDung)
			{
				return NotFound();
			}

			// === KHỐI VALIDATION CHO EDIT ===
			// Kiểm tra trùng Email với những người dùng KHÁC
			if (!string.IsNullOrWhiteSpace(viewModel.Email))
			{
				var emailDaTonTai = await _context.NguoiDungs
					.AnyAsync(u => u.Email == viewModel.Email && u.MaNguoiDung != viewModel.MaNguoiDung);

				if (emailDaTonTai)
				{
					ModelState.AddModelError("Email", "Email này đã được sử dụng bởi một người dùng khác.");
				}
			}

			// Validate tuổi >= 18
			if (viewModel.NgaySinh != null)
			{
				int tuoi = DateTime.Today.Year - viewModel.NgaySinh.Value.Year;
				if (viewModel.NgaySinh.Value.Date > DateTime.Today.AddYears(-tuoi)) tuoi--;

				if (tuoi < 18)
				{
					ModelState.AddModelError("NgaySinh", "Nhân viên phải từ 18 tuổi trở lên.");
				}
			}


			// Sửa lại lỗi logic quan trọng: chỉ lưu khi ModelState HỢP LỆ
			if (ModelState.IsValid)
			{
				try
				{
					// 1. Lấy đối tượng gốc từ DB để cập nhật
					var userInDb = await _context.NguoiDungs.FirstOrDefaultAsync(u => u.MaNguoiDung == id);
					if (userInDb == null)
					{
						return NotFound();
					}

					// 2. Cập nhật các thuộc tính từ viewModel vào đối tượng gốc
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
					if (!_context.NguoiDungs.Any(e => e.MaNguoiDung == viewModel.MaNguoiDung))
					{
						return NotFound();
					}
					else
					{
						throw;
					}
				}
				TempData["SuccessMessage"] = "Cập nhật thành công!";
				return RedirectToAction(nameof(Index));
			}

			// Nếu có lỗi validation, trả về lại form với dữ liệu người dùng đã nhập
			return View(viewModel);
		}
		// GET: Admin/NhanVien/Delete/5
		public async Task<IActionResult> Delete(int? id)
		{
			if (id == null) return NotFound();
			var nguoiDung = await _context.NguoiDungs
				.Include(n => n.Quyen)
				.FirstOrDefaultAsync(m => m.MaNguoiDung == id);
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

			// QUY TẮC BẢO MẬT: Không cho phép thay đổi trạng thái của Quản trị viên
			if (userInDb.Quyen.MaVaiTro == "ADMIN")
			{
				return Forbid(); // Trả về lỗi 403 Access Denied
			}

			// Đảo ngược trạng thái
			userInDb.TrangThai = (userInDb.TrangThai == 1) ? 0 : 1;

			_context.Update(userInDb);
			await _context.SaveChangesAsync();

			// Quay trở lại trang Index để xem kết quả
			return RedirectToAction(nameof(Index));
		}
	}
}
