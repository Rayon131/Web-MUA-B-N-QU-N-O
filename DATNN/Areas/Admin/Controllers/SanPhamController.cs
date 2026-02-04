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
using DATNN.ViewModel;
using OfficeOpenXml;

namespace DATNN.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize(Roles = "admin")]
	public class SanPhamController : Controller
	{
		private readonly MyDbContext _context;

		public SanPhamController(MyDbContext context)
		{
			_context = context;
		}

        // GET: Admin/SanPham
        // == THAY THẾ TOÀN BỘ ACTION INDEX CŨ BẰNG ACTION NÀY ==
        // ==========================================================
        // GET: Admin/SanPham
        public async Task<IActionResult> Index(int? openSanPhamId, string statusFilter = "active", string searchString = "")
        {
            var query = _context.SanPhams.AsQueryable();
            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Trim(); // Xóa khoảng trắng thừa

                // Kiểm tra xem chuỗi nhập vào có phải là số không
                if (int.TryParse(searchString, out int searchId))
                {
                    // Nếu là số: Tìm theo Tên HOẶC Mã (ID)
                    query = query.Where(p => p.TenSanPham.Contains(searchString) || p.MaSanPham == searchId);
                }
                else
                {
                    // Nếu là chữ: Chỉ tìm theo Tên
                    query = query.Where(p => p.TenSanPham.Contains(searchString));
                }

                ViewBag.CurrentSearch = searchString;
            }
            if (statusFilter == "inactive")
            {
                query = query.Where(p => p.TrangThai == 0);
                ViewBag.FilterMessage = "Hiển thị danh sách các sản phẩm đã ngừng kinh doanh";
            }
            else if (statusFilter == "not_yet_active")
            {
                query = query.Where(p => p.TrangThai == 2);
                ViewBag.FilterMessage = "Hiển thị danh sách các sản phẩm chưa kinh doanh";
            }
            else
            {
                statusFilter = "active";
                query = query.Where(p => p.TrangThai == 1);
            }

            var sanPhams = await query
                .Include(s => s.ChatLieu)
                .Include(s => s.DanhMuc)
                .Include(s => s.XuatXu)
                .Include(s => s.SanPhamChiTiets)
                    .ThenInclude(ct => ct.MauSac)
                .Include(s => s.SanPhamChiTiets)
                    .ThenInclude(ct => ct.Size)
                .OrderByDescending(p => p.ThoiGianTao)
                .ToListAsync();

            // === SỬA ĐỔI Ở ĐÂY ===
            // Lấy tất cả khuyến mãi đang hoạt động mà không cần lọc theo LoaiApDung
            var allActivePromotions = await _context.KhuyenMais
                .Where(km => km.TrangThai == 1 &&
                               km.NgayBatDau <= DateTime.Now &&
                               km.NgayKetThuc >= DateTime.Now &&
                               !string.IsNullOrEmpty(km.DanhSachSanPhamApDung))
                .ToListAsync();

            var viewModelList = new List<SanPhamViewModel>();
            foreach (var sp in sanPhams)
            {
                var spViewModel = new SanPhamViewModel { SanPham = sp };

                // Logic này vẫn đúng: Lọc ra các khuyến mãi áp dụng cho sản phẩm này
                var promotionsForThisProduct = allActivePromotions
                    .Where(km => km.DanhSachSanPhamApDung.Split(',').Any(id => id == $"p-{sp.MaSanPham}" || sp.SanPhamChiTiets.Any(ct => id == $"v-{ct.MaSanPhamChiTiet}")))
                    .ToList();

                // Gán cờ HasPromotion nếu có bất kỳ KM nào áp dụng
                spViewModel.HasPromotion = promotionsForThisProduct.Any();

                // Logic tính giá đã giảm không cần thiết ở trang Index tổng, có thể bỏ để tối ưu
                // Nhưng nếu bạn cần nó, logic dưới đây vẫn đúng
                foreach (var ct in sp.SanPhamChiTiets)
                {
                    var applicablePromosForVariant = promotionsForThisProduct
                        .Where(km => km.DanhSachSanPhamApDung.Split(',').Contains($"p-{sp.MaSanPham}") || km.DanhSachSanPhamApDung.Split(',').Contains($"v-{ct.MaSanPhamChiTiet}"))
                        .ToList();

                    if (applicablePromosForVariant.Any())
                    {
                        var bestPrice = applicablePromosForVariant
                            .Select(p => CalculateDiscountedPrice(ct.GiaBan, p))
                            .Min();
                        spViewModel.GiaDaGiam[ct.MaSanPhamChiTiet] = bestPrice;
                    }
                }
                viewModelList.Add(spViewModel);
            }

            ViewBag.OpenSanPhamId = openSanPhamId;
            ViewBag.CurrentStatusFilter = statusFilter;
            return View(viewModelList);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            // 1. Tải sản phẩm và CÁC PHIÊN BẢN CON của nó bằng Include()
            var sanPham = await _context.SanPhams
                .Include(p => p.SanPhamChiTiets)
                .FirstOrDefaultAsync(p => p.MaSanPham == id);

            if (sanPham == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm." });
            }

            // 2. Xác định trạng thái mới (1 thành 0, và 0 thành 1)
            int newStatus = (sanPham.TrangThai == 1) ? 0 : 1;

            // 3. Cập nhật trạng thái cho sản phẩm cha
            sanPham.TrangThai = newStatus;

            // 4. DÙNG VÒNG LẶP để cập nhật trạng thái cho TẤT CẢ phiên bản con
            foreach (var chiTiet in sanPham.SanPhamChiTiets)
            {
                chiTiet.TrangThai = newStatus;
            }

            try
            {
                // 5. Lưu tất cả các thay đổi (cả cha và con) vào database
                // _context.Update(sanPham); // Không cần thiết khi đã tracking
                await _context.SaveChangesAsync();

                return Json(new { success = true, newStatus = sanPham.TrangThai });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi cập nhật trạng thái: " + ex.Message });
            }
        }


        // ==========================================================
        // == THÊM HÀM HELPER NÀY NẾU CHƯA CÓ ==
        // ==========================================================
        private decimal CalculateDiscountedPrice(decimal originalPrice, KhuyenMai promotion)
        {
            if (promotion.LoaiGiamGia == "PhanTram")
            {
                return originalPrice * (1 - (promotion.GiaTriGiamGia / 100));
            }
            if (promotion.LoaiGiamGia == "SoTien")
            {
                var newPrice = originalPrice - promotion.GiaTriGiamGia;
                return newPrice > 0 ? newPrice : 0;
            }
            return originalPrice;
        }
        // GET: Admin/SanPham/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sanPham = await _context.SanPhams
                .Include(s => s.ChatLieu)
                .Include(s => s.DanhMuc)
                .Include(s => s.XuatXu)
                .FirstOrDefaultAsync(m => m.MaSanPham == id);
            if (sanPham == null)
            {
                return NotFound();
            }

            return View(sanPham);
        }


        public IActionResult Create()
        {
            // ===== Lọc MÀU SẮC + SIZE theo TrangThai = 1 =====
            var mauSacData = _context.MauSacs
                                     .Where(m => m != null
                                              && m.TenMau != null
                                              && m.TrangThai == 1)
                                     .ToList();

            var sizeData = _context.Sizes
                                   .Where(s => s != null
                                            && s.TenSize != null
                                            && s.TrangThai == 1)
                                   .ToList();

            ViewData["MauSacItems"] = mauSacData
                .Select(m => new { value = m.MaMauSac.ToString(), text = m.TenMau })
                .ToList();

            ViewData["SizeItems"] = sizeData
                .Select(s => new { value = s.MaSize.ToString(), text = s.TenSize })
                .ToList();


            // ===== Lọc dropdown DANH MỤC – CHẤT LIỆU – XUẤT XỨ =====
            ViewData["MaDanhMuc"] = new SelectList(
                _context.DanhMucs.Where(x => x.TrangThai == 1),
                "MaDanhMuc", "TenDanhMuc"
            );

            ViewData["MaChatLieu"] = new SelectList(
                _context.ChatLieus.Where(x => x.TrangThai == 1),
                "MaChatLieu", "TenChatLieu"
            );

            ViewData["MaXuatXu"] = new SelectList(
                _context.XuatXus.Where(x => x.TrangThai == 1),
                "MaXuatXu", "TenXuatXu"
            );

            // ===== Dropdown trạng thái sản phẩm =====
            var statusList = new List<SelectListItem>
    {
        new SelectListItem { Value = "1", Text = "Đang kinh doanh" },
        new SelectListItem { Value = "2", Text = "Chưa kinh doanh" }
    };

            ViewBag.TrangThaiList = statusList;

            // ===== Trả ViewModel =====
            var viewModel = new SanPhamCreateViewModel();
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SanPhamCreateViewModel viewModel)
        {
            // Kiểm tra tên sản phẩm cha trùng lặp (giữ nguyên)
            if (!string.IsNullOrEmpty(viewModel.TenSanPham))
            {
                var existingProduct = await _context.SanPhams
                    .FirstOrDefaultAsync(p => p.TenSanPham.ToUpper() == viewModel.TenSanPham.ToUpper());
                if (existingProduct != null)
                {
                    ModelState.AddModelError("TenSanPham", "Tên sản phẩm này đã tồn tại. Vui lòng chọn tên khác.");
                }
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };

            // Kiểm tra ảnh đại diện sản phẩm cha (giữ nguyên)
            if (viewModel.file != null)
            {
                var ext = Path.GetExtension(viewModel.file.FileName).ToLowerInvariant();
                if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
                {
                    ModelState.AddModelError("file", "Vui lòng chọn tệp hình ảnh có định dạng .jpg, .png hoặc .jpeg.");
                }
            }

            // === PHẦN SỬA LỖI BẮT ĐẦU TỪ ĐÂY ===
            // Logic validate ảnh của các phiên bản con
            if (viewModel.SanPhamChiTiets != null && viewModel.SanPhamChiTiets.Any())
            {
                for (int i = 0; i < viewModel.SanPhamChiTiets.Count; i++)
                {
                    var chiTietVM = viewModel.SanPhamChiTiets[i];
                    // Sửa tên thuộc tính để hiển thị lỗi đúng chỗ
                    var propertyName = $"SanPhamChiTiets[{i}].file";

                    // Sửa 'VariantFile' thành 'file'
                    if (chiTietVM.file == null || chiTietVM.file.Length == 0)
                    {
                        ModelState.AddModelError(propertyName, "Vui lòng chọn ảnh cho phiên bản.");
                    }
                    else
                    {
                        // Sửa 'VariantFile' thành 'file'
                        var ext = Path.GetExtension(chiTietVM.file.FileName).ToLowerInvariant();
                        if (!allowedExtensions.Contains(ext))
                        {
                            ModelState.AddModelError(propertyName, "Định dạng ảnh không hợp lệ.");
                        }
                    }
                }
            }

            // Xử lý lưu dữ liệu nếu không có lỗi
            if (ModelState.IsValid)
            {
                // Tạo và lưu sản phẩm cha (giữ nguyên)
                var sanPham = new SanPham
                {
                    TenSanPham = viewModel.TenSanPham,
                    MaDanhMuc = viewModel.MaDanhMuc.Value,
                    MaChatLieu = viewModel.MaChatLieu.Value,
                    MaXuatXu = viewModel.MaXuatXu.Value,
                    MoTa = string.IsNullOrEmpty(viewModel.MoTa) ? "Không có mô tả" : viewModel.MoTa,
                    TrangThai = viewModel.TrangThai,
                    ThoiGianTao = DateTime.Now
                };
                if (viewModel.file != null && viewModel.file.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                    var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(viewModel.file.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var stream = new FileStream(filePath, FileMode.Create)) { await viewModel.file.CopyToAsync(stream); }
                    sanPham.AnhSanPham = uniqueFileName;
                }
                _context.Add(sanPham);
                await _context.SaveChangesAsync();

                // Logic lưu ảnh các phiên bản con
                if (viewModel.SanPhamChiTiets != null && viewModel.SanPhamChiTiets.Any())
                {
                    foreach (var chiTietVM in viewModel.SanPhamChiTiets)
                    {
                        var chiTietModel = new SanPhamChiTiet
                        {
                            MaSanPham = sanPham.MaSanPham,
                            MaMauSac = chiTietVM.MaMauSac.Value,
                            MaSize = chiTietVM.MaSize.Value,
                            GiaBan = chiTietVM.GiaBan.Value,
                            SoLuong = chiTietVM.SoLuong.Value,
                            GhiChu = string.IsNullOrEmpty(chiTietVM.GhiChu) ? "Không" : chiTietVM.GhiChu,
                            TrangThai = sanPham.TrangThai,
                            HinhAnh = "default_image.png"
                        };

                        // Sửa 'VariantFile' thành 'file'
                        var chiTietFile = chiTietVM.file;
                        if (chiTietFile != null && chiTietFile.Length > 0)
                        {
                            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(chiTietFile.FileName);
                            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                            using (var stream = new FileStream(filePath, FileMode.Create)) { await chiTietFile.CopyToAsync(stream); }
                            chiTietModel.HinhAnh = uniqueFileName;
                        }
                        _context.Add(chiTietModel);
                    }
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Index));
            }
            // === PHẦN SỬA LỖI KẾT THÚC TẠI ĐÂY ===

            // Trả về View nếu có lỗi (giữ nguyên)
            ViewData["MaChatLieu"] = new SelectList(_context.ChatLieus, "MaChatLieu", "TenChatLieu", viewModel.MaChatLieu);
            ViewData["MaDanhMuc"] = new SelectList(_context.DanhMucs, "MaDanhMuc", "TenDanhMuc", viewModel.MaDanhMuc);
            ViewData["MaXuatXu"] = new SelectList(_context.XuatXus, "MaXuatXu", "TenXuatXu", viewModel.MaXuatXu);
            var mauSacData = _context.MauSacs.Where(m => m != null && m.TenMau != null).ToList();
            var sizeData = _context.Sizes.Where(s => s != null && s.TenSize != null).ToList();
            ViewData["MauSacItems"] = mauSacData.Select(m => new { value = m.MaMauSac.ToString(), text = m.TenMau }).ToList();
            ViewData["SizeItems"] = sizeData.Select(s => new { value = s.MaSize.ToString(), text = s.TenSize }).ToList();
            var statusList = new List<SelectListItem>
    {
        new SelectListItem { Value = "1", Text = "Đang kinh doanh" },
        new SelectListItem { Value = "2", Text = "Chưa kinh doanh" }
    };
            ViewBag.TrangThaiList = new SelectList(statusList, "Value", "Text", viewModel.TrangThai);

            return View(viewModel);
        }
        // GET: Admin/SanPham/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sanPham = await _context.SanPhams.FindAsync(id);
            if (sanPham == null)
            {
                return NotFound();
            }

                // SỬA Ở ĐÂY: Tham số thứ 3 là cột TÊN để hiển thị
             ViewBag.MaDanhMuc = new SelectList(
                 _context.DanhMucs.Where(x => x.TrangThai == 1),
                 "MaDanhMuc", "TenDanhMuc", sanPham.MaDanhMuc
             );

            ViewBag.MaChatLieu = new SelectList(
                _context.ChatLieus.Where(x => x.TrangThai == 1),
                "MaChatLieu", "TenChatLieu", sanPham.MaChatLieu
            );

            ViewBag.MaXuatXu = new SelectList(
                _context.XuatXus.Where(x => x.TrangThai == 1),
                "MaXuatXu", "TenXuatXu", sanPham.MaXuatXu
            );
            return View(sanPham);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, IFormFile? file)
        {
            // Bước 1: Tải đối tượng sản phẩm gốc và đầy đủ từ database
            var sanPhamToUpdate = await _context.SanPhams.FindAsync(id);
            if (sanPhamToUpdate == null)
            {
                return NotFound();
            }

            // Bước 2: Cố gắng cập nhật các giá trị từ form vào đối tượng vừa tải
            await TryUpdateModelAsync(sanPhamToUpdate, "",
                s => s.MaDanhMuc, s => s.MaChatLieu, s => s.MaXuatXu, s => s.TenSanPham, s => s.MoTa);

            // === BƯỚC QUAN TRỌNG NHẤT ĐỂ SỬA LỖI ===
            // Sau khi TryUpdateModel, hệ thống có thể đã tự động thêm lỗi cho các thuộc tính đối tượng.
            // Chúng ta sẽ xóa các lỗi không mong muốn này một cách thủ công.
            ModelState.Remove("DanhMuc");
            ModelState.Remove("ChatLieu");
            ModelState.Remove("XuatXu");
            ModelState.Remove("SanPhamChiTiets");
            // =========================================

            // Bước 3: Bây giờ, ModelState đã "sạch", ta tiếp tục với các validation tùy chỉnh
            var tenSanPhamUpper = sanPhamToUpdate.TenSanPham.ToUpper();
            var existingProduct = await _context.SanPhams
                .FirstOrDefaultAsync(p => p.TenSanPham.ToUpper() == tenSanPhamUpper && p.MaSanPham != id);

            if (existingProduct != null)
            {
                ModelState.AddModelError("TenSanPham", "Tên sản phẩm này đã tồn tại. Vui lòng chọn tên khác.");
            }

            if (file != null && file.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
                {
                    ModelState.AddModelError("AnhSanPham", "Định dạng ảnh không hợp lệ. Chỉ chấp nhận .jpg, .jpeg, .png.");
                }
            }

            // Bước 4: Kiểm tra ModelState lần cuối cùng.
            if (ModelState.IsValid)
            {
                try
                {
                    // Xử lý upload file ảnh mới (nếu có)
                    if (file != null && file.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                        var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }
                        sanPhamToUpdate.AnhSanPham = uniqueFileName;
                    }

                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index), new { openSanPhamId = id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SanPhamExists(sanPhamToUpdate.MaSanPham))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            // Bước 5: Nếu vẫn có lỗi (lỗi thực sự từ validation tùy chỉnh), tải lại dropdowns và hiển thị lại view
            ViewData["MaChatLieu"] = new SelectList(_context.ChatLieus, "MaChatLieu", "TenChatLieu", sanPhamToUpdate.MaChatLieu);
            ViewData["MaDanhMuc"] = new SelectList(_context.DanhMucs, "MaDanhMuc", "TenDanhMuc", sanPhamToUpdate.MaDanhMuc);
            ViewData["MaXuatXu"] = new SelectList(_context.XuatXus, "MaXuatXu", "TenXuatXu", sanPhamToUpdate.MaXuatXu);
            return View(sanPhamToUpdate);
        }

        // GET: Admin/SanPham/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sanPham = await _context.SanPhams
                .Include(s => s.ChatLieu)
                .Include(s => s.DanhMuc)
                .Include(s => s.XuatXu)
                .FirstOrDefaultAsync(m => m.MaSanPham == id);
            if (sanPham == null)
            {
                return NotFound();
            }

            return View(sanPham);
        }

        // POST: Admin/SanPham/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var sanPham = await _context.SanPhams.FindAsync(id);
            if (sanPham != null)
            {
                _context.SanPhams.Remove(sanPham);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> DownloadExcelTemplate()
        {
          
            using (var package = new ExcelPackage())
            {
                // Tên của sheet
                var worksheet = package.Workbook.Worksheets.Add("SanPhamTemplate");

                // === TẠO HEADER CHO CÁC CỘT ===
                // Dòng 1 sẽ là tiêu đề
                worksheet.Cells["A1"].Value = "Tên Sản Phẩm*";
                worksheet.Cells["B1"].Value = "Mô Tả";
                worksheet.Cells["C1"].Value = "Danh Mục*";
                worksheet.Cells["D1"].Value = "Chất Liệu*";
                worksheet.Cells["E1"].Value = "Xuất Xứ*";
                worksheet.Cells["F1"].Value = "Màu Sắc*";
                worksheet.Cells["G1"].Value = "Size*";
                worksheet.Cells["H1"].Value = "Giá Bán*";
                worksheet.Cells["I1"].Value = "Số Lượng Tồn*";
                worksheet.Cells["J1"].Value = "Tên File Ảnh";

                // In đậm header
                using (var range = worksheet.Cells["A1:J1"])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                // === TẠO DATA VALIDATION (Dropdown list) ĐỂ HẠN CHẾ NHẬP SAI ===
                // Lấy dữ liệu từ database
                var danhMucList = await _context.DanhMucs.Select(d => d.TenDanhMuc).ToArrayAsync();
                var chatLieuList = await _context.ChatLieus.Select(c => c.TenChatLieu).ToArrayAsync();
                var xuatXuList = await _context.XuatXus.Select(x => x.TenXuatXu).ToArrayAsync();
                var mauSacList = await _context.MauSacs.Select(m => m.TenMau).ToArrayAsync();
                var sizeList = await _context.Sizes.Select(s => s.TenSize).ToArrayAsync();

                // Áp dụng validation cho 1000 dòng tiếp theo
                // Danh mục
                var danhMucValidation = worksheet.DataValidations.AddListValidation("C2:C1001");
                danhMucList.ToList().ForEach(d => danhMucValidation.Formula.Values.Add(d));
                // Chất liệu
                var chatLieuValidation = worksheet.DataValidations.AddListValidation("D2:D1001");
                chatLieuList.ToList().ForEach(c => chatLieuValidation.Formula.Values.Add(c));
                // Xuất xứ
                var xuatXuValidation = worksheet.DataValidations.AddListValidation("E2:E1001");
                xuatXuList.ToList().ForEach(x => xuatXuValidation.Formula.Values.Add(x));
                // Màu sắc
                var mauSacValidation = worksheet.DataValidations.AddListValidation("F2:F1001");
                mauSacList.ToList().ForEach(m => mauSacValidation.Formula.Values.Add(m));
                // Size
                var sizeValidation = worksheet.DataValidations.AddListValidation("G2:G1001");
                sizeList.ToList().ForEach(s => sizeValidation.Formula.Values.Add(s));

                // Tự động điều chỉnh độ rộng cột
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                // Chuyển package thành dạng byte array để tải về
                var stream = new MemoryStream();
                await package.SaveAsAsync(stream);
                stream.Position = 0;

                // Đặt tên file khi tải về
                string excelName = "Mau_Nhap_San_Pham.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportFromExcel(IFormFile excelFile, List<IFormFile> imageFiles) // <-- Sửa chữ ký hàm
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                TempData["Error"] = "Vui lòng chọn một file Excel để tải lên.";
                return RedirectToAction(nameof(Index));
            }

            var sanPhamList = new List<SanPham>();
            var sanPhamDict = new Dictionary<string, SanPham>();

            try
            {
                // === LOGIC MỚI: Xử lý file ảnh trước ===
                // Chuyển danh sách file ảnh thành một Dictionary để tra cứu nhanh bằng tên file
                var imageFilesDict = new Dictionary<string, IFormFile>(StringComparer.OrdinalIgnoreCase);
                if (imageFiles != null)
                {
                    foreach (var imageFile in imageFiles)
                    {
                        if (imageFile.Length > 0 && !imageFilesDict.ContainsKey(imageFile.FileName))
                        {
                            imageFilesDict.Add(imageFile.FileName, imageFile);
                        }
                    }
                }

                using (var stream = new MemoryStream())
                {
                    await excelFile.CopyToAsync(stream);
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                        if (worksheet == null)
                        {
                            TempData["Error"] = "File Excel không hợp lệ hoặc không có sheet nào.";
                            return RedirectToAction(nameof(Index));
                        }

                        int rowCount = worksheet.Dimension.Rows;

                        var danhMucDict = await _context.DanhMucs.ToDictionaryAsync(d => d.TenDanhMuc, d => d.MaDanhMuc, StringComparer.OrdinalIgnoreCase);
                        var chatLieuDict = await _context.ChatLieus.ToDictionaryAsync(c => c.TenChatLieu, c => c.MaChatLieu, StringComparer.OrdinalIgnoreCase);
                        var xuatXuDict = await _context.XuatXus.ToDictionaryAsync(x => x.TenXuatXu, x => x.MaXuatXu, StringComparer.OrdinalIgnoreCase);
                        var mauSacDict = await _context.MauSacs.ToDictionaryAsync(m => m.TenMau, m => m.MaMauSac, StringComparer.OrdinalIgnoreCase);
                        var sizeDict = await _context.Sizes.ToDictionaryAsync(s => s.TenSize, s => s.MaSize, StringComparer.OrdinalIgnoreCase);

                        string currentProductName = "";

                        for (int row = 2; row <= rowCount; row++)
                        {
                            var tenSanPhamCell = worksheet.Cells[row, 1].Value?.ToString()?.Trim();
                            if (!string.IsNullOrEmpty(tenSanPhamCell))
                            {
                                currentProductName = tenSanPhamCell;
                            }
                            if (string.IsNullOrEmpty(currentProductName))
                            {
                                continue;
                            }

                            // Đọc tên file ảnh từ cột J (cột 10)
                            var imageFileName = worksheet.Cells[row, 10].Value?.ToString()?.Trim();
                            string savedImageName = "default_image.png"; // Giá trị mặc định

                            // === LOGIC MỚI: Lưu file ảnh nếu có ===
                            if (!string.IsNullOrEmpty(imageFileName) && imageFilesDict.ContainsKey(imageFileName))
                            {
                                var imageFile = imageFilesDict[imageFileName];
                                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                                // Tạo tên file duy nhất để tránh trùng lặp
                                savedImageName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                                var filePath = Path.Combine(uploadsFolder, savedImageName);
                                using (var fileStream = new FileStream(filePath, FileMode.Create))
                                {
                                    await imageFile.CopyToAsync(fileStream);
                                }
                            }

                            if (!sanPhamDict.ContainsKey(currentProductName))
                            {
                                var tenDanhMuc = worksheet.Cells[row, 3].Value?.ToString()?.Trim();
                                var tenChatLieu = worksheet.Cells[row, 4].Value?.ToString()?.Trim();
                                var tenXuatXu = worksheet.Cells[row, 5].Value?.ToString()?.Trim();

                                if (string.IsNullOrEmpty(tenDanhMuc) || !danhMucDict.ContainsKey(tenDanhMuc))
                                    throw new Exception($"Lỗi ở dòng {row}: Tên danh mục '{tenDanhMuc}' không tồn tại hoặc bị bỏ trống.");
                                if (string.IsNullOrEmpty(tenChatLieu) || !chatLieuDict.ContainsKey(tenChatLieu))
                                    throw new Exception($"Lỗi ở dòng {row}: Tên chất liệu '{tenChatLieu}' không tồn tại hoặc bị bỏ trống.");
                                if (string.IsNullOrEmpty(tenXuatXu) || !xuatXuDict.ContainsKey(tenXuatXu))
                                    throw new Exception($"Lỗi ở dòng {row}: Tên xuất xứ '{tenXuatXu}' không tồn tại hoặc bị bỏ trống.");

                                var sanPham = new SanPham
                                {
                                    TenSanPham = currentProductName,
                                    MoTa = worksheet.Cells[row, 2].Value?.ToString()?.Trim() ?? "Không có mô tả",
                                    MaDanhMuc = danhMucDict[tenDanhMuc],
                                    MaChatLieu = chatLieuDict[tenChatLieu],
                                    MaXuatXu = xuatXuDict[tenXuatXu],
                                    AnhSanPham = savedImageName, // Gán ảnh đại diện là ảnh của phiên bản đầu tiên
                                    ThoiGianTao = DateTime.Now,
                                    TrangThai = 1,
                                    SanPhamChiTiets = new List<SanPhamChiTiet>()
                                };

                                sanPhamDict.Add(currentProductName, sanPham);
                                sanPhamList.Add(sanPham);
                            }

                            var tenMauSac = worksheet.Cells[row, 6].Value?.ToString()?.Trim();
                            var tenSize = worksheet.Cells[row, 7].Value?.ToString()?.Trim();
                            var giaBanStr = worksheet.Cells[row, 8].Value?.ToString()?.Trim();
                            var soLuongStr = worksheet.Cells[row, 9].Value?.ToString()?.Trim();

                            if (string.IsNullOrEmpty(tenMauSac) || !mauSacDict.ContainsKey(tenMauSac))
                                throw new Exception($"Lỗi ở dòng {row}: Tên màu sắc '{tenMauSac}' không tồn tại hoặc bị bỏ trống.");
                            if (string.IsNullOrEmpty(tenSize) || !sizeDict.ContainsKey(tenSize))
                                throw new Exception($"Lỗi ở dòng {row}: Tên size '{tenSize}' không tồn tại hoặc bị bỏ trống.");
                            if (!decimal.TryParse(giaBanStr, out decimal giaBan) || giaBan < 0)
                                throw new Exception($"Lỗi ở dòng {row}: Giá bán '{giaBanStr}' không hợp lệ.");
                            if (!int.TryParse(soLuongStr, out int soLuong) || soLuong < 0)
                                throw new Exception($"Lỗi ở dòng {row}: Số lượng '{soLuongStr}' không hợp lệ.");

                            var chiTiet = new SanPhamChiTiet
                            {
                                MaMauSac = mauSacDict[tenMauSac],
                                MaSize = sizeDict[tenSize],
                                GiaBan = giaBan,
                                SoLuong = soLuong,
                                TrangThai = 1,
                                HinhAnh = savedImageName, // Gán ảnh cho phiên bản
                                GhiChu = "Không"
                            };

                            sanPhamDict[currentProductName].SanPhamChiTiets.Add(chiTiet);
                        }
                    }
                }

                if (!sanPhamList.Any())
                {
                    TempData["Warning"] = "Không tìm thấy dữ liệu hợp lệ trong file Excel.";
                    return RedirectToAction(nameof(Index));
                }

                _context.SanPhams.AddRange(sanPhamList);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Nhập thành công {sanPhamList.Count} sản phẩm mới từ file Excel.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Đã xảy ra lỗi khi xử lý file: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
        private bool SanPhamExists(int id)
        {
            return _context.SanPhams.Any(e => e.MaSanPham == id);
        }
    }
}
