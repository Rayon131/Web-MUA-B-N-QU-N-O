using DATNN.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DATNN.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin")]
    public class MaGiamGiaController : Controller
    {
        private readonly MyDbContext _context;

        public MaGiamGiaController(MyDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index(string statusFilter = "active")
        {
            var query = _context.MaGiamGias.AsQueryable();
            var now = DateTime.Now.Date;

            switch (statusFilter)
            {
                case "upcoming":
                    query = query.Where(mg => mg.TrangThai == 1 && mg.NgayBatDau > now);
                    break;
                case "expired":
                    query = query.Where(mg => mg.NgayKetThuc < now);
                    break;
                case "inactive":
                    query = query.Where(mg => mg.TrangThai == 0 && mg.NgayKetThuc >= now);
                    break;
                default: // "active" (Đang diễn ra)
                    statusFilter = "active";
                    query = query.Where(mg => mg.TrangThai == 1 && mg.NgayBatDau <= now && mg.NgayKetThuc >= now);
                    break;
            }

            var maGiamGias = await query.OrderByDescending(mg => mg.NgayBatDau).ToListAsync();
            ViewBag.CurrentStatusFilter = statusFilter;

            return View(maGiamGias);
        }

        public async Task<IActionResult> Create()
        {
            // Khởi tạo Model với các giá trị mặc định cho form
            var newMaGiamGia = new MaGiamGia()
            {
                NgayBatDau = DateTime.Now.Date,
                NgayKetThuc = DateTime.Now.Date.AddDays(7),
                LoaiApDung = "DonHang",
                LoaiGiamGia = "PhanTram",
                KenhApDung = "TatCa",
                GiaTriGiamGia = 10
            };
            return View(newMaGiamGia);
        }

        // POST: Admin/MaGiamGia/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaCode,TenChuongTrinh,LoaiApDung,KenhApDung,TongLuotSuDungToiDa,LoaiGiamGia,GiaTriGiamGia,DieuKienDonHangToiThieu,GhiChu,NgayBatDau,NgayKetThuc")] MaGiamGia maGiamGia, string[] selectedItems)
        {
            if (maGiamGia.LoaiApDung == "FreeShip")
            {
                maGiamGia.KenhApDung = "Online";
                ModelState.Remove("KenhApDung");
            }
            if (maGiamGia.NgayKetThuc < maGiamGia.NgayBatDau)
            {
                ModelState.AddModelError("NgayKetThuc", "Ngày kết thúc không được nhỏ hơn ngày bắt đầu.");
            }
            if (maGiamGia.GiaTriGiamGia <= 0)
            {
                ModelState.AddModelError("GiaTriGiamGia", "Giá trị giảm giá phải lớn hơn 0.");
            }

            // Xử lý và Validation cho Mã Code
            if (string.IsNullOrWhiteSpace(maGiamGia.MaCode))
            {
                ModelState.AddModelError("MaCode", "Mã Voucher không được để trống.");
            }
            else
            {
                maGiamGia.MaCode = maGiamGia.MaCode.ToUpper().Trim();
                if (await _context.MaGiamGias.AnyAsync(mg => mg.MaCode == maGiamGia.MaCode))
                {
                    ModelState.AddModelError("MaCode", "Mã Voucher đã tồn tại.");
                }
            }
            // Xử lý giới hạn sử dụng
            if (maGiamGia.TongLuotSuDungToiDa.HasValue && maGiamGia.TongLuotSuDungToiDa.Value <= 0)
            {
                ModelState.AddModelError("TongLuotSuDungToiDa", "Tổng lượt sử dụng tối đa phải lớn hơn 0 hoặc để trống.");
            }
            // --- KẾT THÚC VALIDATION THỦ CÔNG ---
            // 2. Kiểm tra Giá trị giảm giá
            if (maGiamGia.GiaTriGiamGia <= 0)
            {
                ModelState.AddModelError("GiaTriGiamGia", "Giá trị giảm giá phải lớn hơn 0.");
            }
            else // Chỉ kiểm tra các điều kiện tiếp theo nếu giá trị > 0
            {
                // VALIDATION MỚI: Nếu là phần trăm, giá trị không được vượt quá 100
                if (maGiamGia.LoaiGiamGia == "PhanTram" && maGiamGia.GiaTriGiamGia > 100)
                {
                    ModelState.AddModelError("GiaTriGiamGia", "Giá trị giảm theo % không được vượt quá 100.");
                }
                // VALIDATION MỚI: Nếu là số tiền, giá trị không được vượt quá 100,000
                else if (maGiamGia.LoaiGiamGia == "SoTien" && maGiamGia.GiaTriGiamGia > 100000)
                {
                    ModelState.AddModelError("GiaTriGiamGia", "Giá trị giảm theo tiền không được vượt quá 100.000 VNĐ.");
                }
            }
            if (string.IsNullOrWhiteSpace(maGiamGia.TenChuongTrinh))
            {
                ModelState.AddModelError("TenChuongTrinh", "Vui lòng nhập Tên chương trình.");
            }
            if (maGiamGia.NgayBatDau.Year == 1)
            {
                ModelState.AddModelError("NgayBatDau", "Vui lòng chọn Ngày bắt đầu.");
            }
            // 2. Kiểm tra ngày bắt đầu có phải là ngày trong quá khứ không
            else if (maGiamGia.NgayBatDau.Date < DateTime.Now.Date)
            {
                ModelState.AddModelError("NgayBatDau", "Ngày bắt đầu không được là ngày trong quá khứ.");
            }
            // ==========================================================
            // *** KẾT THÚC SỬA ĐỔI ***
            // ==========================================================

            if (maGiamGia.NgayKetThuc.Year == 1)
            {
                ModelState.AddModelError("NgayKetThuc", "Vui lòng chọn Ngày kết thúc.");
            }
            if (ModelState.IsValid)
            {
                maGiamGia.TrangThai = 1;
                maGiamGia.DaSuDung = 0; // Khởi tạo số lượt đã dùng

                // Xử lý danh sách sản phẩm (nếu có, Voucher thường là DonHang)
                if (maGiamGia.LoaiApDung == "SanPham")
                {
                    // Dữ liệu selectedItems cần được lấy từ form, nhưng View hiện tại không có, tạm thời để NULL
                    maGiamGia.DanhSachSanPhamApDung = null;
                    maGiamGia.DieuKienDonHangToiThieu = null;
                }
                else
                {
                    maGiamGia.DanhSachSanPhamApDung = null;
                }

                _context.Add(maGiamGia);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Nếu model không hợp lệ, trả về View
            return View(maGiamGia);
        }

        // ==========================================================
        // 3. EDIT (Chỉnh sửa)
        // ==========================================================
        // GET: Admin/MaGiamGia/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var maGiamGia = await _context.MaGiamGias.FindAsync(id);
            if (maGiamGia == null) return NotFound();
            return View(maGiamGia);
        }

        // POST: Admin/MaGiamGia/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaGiamGiaID,MaCode,TenChuongTrinh,LoaiApDung,KenhApDung,TongLuotSuDungToiDa,DaSuDung,LoaiGiamGia,GiaTriGiamGia,DieuKienDonHangToiThieu,GhiChu,NgayBatDau,NgayKetThuc,TrangThai")] MaGiamGia maGiamGia, string[] selectedItems)
        {
            if (id != maGiamGia.MaGiamGiaID) return NotFound();

            // Lấy Mã Code gốc và DaSuDung gốc để kiểm tra và bảo toàn
            var originalData = await _context.MaGiamGias.AsNoTracking()
                .Where(mg => mg.MaGiamGiaID == id)
                .Select(mg => new { mg.MaCode, mg.DaSuDung, mg.TrangThai })
                .FirstOrDefaultAsync();

            if (originalData == null) return NotFound();

            // --- VALIDATION THỦ CÔNG ---
            if (maGiamGia.NgayKetThuc < maGiamGia.NgayBatDau)
            {
                ModelState.AddModelError("NgayKetThuc", "Ngày kết thúc không được nhỏ hơn ngày bắt đầu.");
            }
            if (maGiamGia.GiaTriGiamGia <= 0)
            {
                ModelState.AddModelError("GiaTriGiamGia", "Giá trị giảm giá phải lớn hơn 0.");
            }

            // Xử lý và Validation cho Mã Code
            if (string.IsNullOrWhiteSpace(maGiamGia.MaCode))
            {
                ModelState.AddModelError("MaCode", "Mã Voucher không được để trống.");
            }
            else
            {
                maGiamGia.MaCode = maGiamGia.MaCode.ToUpper().Trim();
                // Kiểm tra trùng lặp (chỉ khi Mã Code thay đổi)
                if (originalData.MaCode != maGiamGia.MaCode)
                {
                    if (await _context.MaGiamGias.AnyAsync(mg => mg.MaCode == maGiamGia.MaCode && mg.MaGiamGiaID != id))
                    {
                        ModelState.AddModelError("MaCode", "Mã Voucher đã tồn tại.");
                    }
                }
            }
            // Xử lý giới hạn sử dụng
            if (maGiamGia.TongLuotSuDungToiDa.HasValue && maGiamGia.TongLuotSuDungToiDa.Value <= 0)
            {
                ModelState.AddModelError("TongLuotSuDungToiDa", "Tổng lượt sử dụng tối đa phải lớn hơn 0 hoặc để trống.");
            }
            // Đảm bảo giới hạn mới không nhỏ hơn số lượt đã dùng
            if (maGiamGia.TongLuotSuDungToiDa.HasValue && originalData.DaSuDung > maGiamGia.TongLuotSuDungToiDa.Value)
            {
                ModelState.AddModelError("TongLuotSuDungToiDa", $"Tổng lượt sử dụng tối đa không thể nhỏ hơn số lượt đã sử dụng ({originalData.DaSuDung}).");
            }
            // --- KẾT THÚC VALIDATION THỦ CÔNG ---


            if (ModelState.IsValid)
            {
                try
                {
                    // BẢO TOÀN GIÁ TRỊ TỪ DB: DaSuDung và TrangThai
                    maGiamGia.DaSuDung = originalData.DaSuDung;
                    maGiamGia.TrangThai = originalData.TrangThai;

                    // Xử lý danh sách sản phẩm
                    if (maGiamGia.LoaiApDung == "SanPham")
                    {
                        // Dữ liệu selectedItems cần được lấy từ form, nhưng View hiện tại không có, tạm thời để NULL
                        maGiamGia.DanhSachSanPhamApDung = null;
                        maGiamGia.DieuKienDonHangToiThieu = null;
                    }
                    else
                    {
                        maGiamGia.DanhSachSanPhamApDung = null;
                    }

                    _context.Update(maGiamGia);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.MaGiamGias.Any(e => e.MaGiamGiaID == maGiamGia.MaGiamGiaID))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            // Nếu model không hợp lệ, trả về View
            return View(maGiamGia);
        }

        // ==========================================================
        // 4. DELETE (Xóa)
        // ==========================================================
        // GET: Admin/MaGiamGia/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var maGiamGia = await _context.MaGiamGias.FindAsync(id);
            if (maGiamGia == null)
                return Json(new { success = false, message = "Không tìm thấy mã giảm giá." });

            if (maGiamGia.TrangThai == 1) // Nếu đang bật -> Tắt (Vô hiệu hóa)
            {
                maGiamGia.TrangThai = 0;
            }
            else // Nếu đang tắt -> Bật lại
            {
                // Chỉ cho phép kích hoạt lại nếu chưa hết hạn
                if (maGiamGia.NgayKetThuc < DateTime.Now.Date)
                {
                    return Json(new { success = false, message = "Không thể kích hoạt lại mã giảm giá đã hết hạn." });
                }
                maGiamGia.TrangThai = 1;
            }

            _context.Update(maGiamGia);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}
