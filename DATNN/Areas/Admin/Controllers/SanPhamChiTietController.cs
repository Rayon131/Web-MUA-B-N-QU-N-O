using DATNN;
using DATNN.Models;
using DATNN.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QRCoder; // using cho thư viện QR
using System;
using System.Collections.Generic;
using System.IO; // using cho MemoryStream
using System.Linq;
using System.Threading.Tasks;


namespace DATNN.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin")]
    public class SanPhamChiTietController : Controller
    {
        private readonly MyDbContext _context;

        public SanPhamChiTietController(MyDbContext context)
        {
            _context = context;
        }

        // GET: Admin/SanPhamChiTiet
        public async Task<IActionResult> Index(int? openSanPhamId)
        {
            var myDbContext = _context.SanPhamChiTiets.Include(s => s.MauSac).Include(s => s.SanPham).Include(s => s.Size);
			ViewBag.OpenSanPhamId = openSanPhamId;
			return View(await myDbContext.ToListAsync());
        }

        public IActionResult GenerateQrCode(int id)
        {
            // Dữ liệu cần mã hóa chính là ID của sản phẩm chi tiết
            string dataToEncode = id.ToString();

            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(dataToEncode, QRCodeGenerator.ECCLevel.Q);

            // SỬ DỤNG LỚP RENDERER MỚI
            PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeAsPngByteArr = qrCode.GetGraphic(20);

            // Trả về file ảnh PNG
            return File(qrCodeAsPngByteArr, "image/png");
        }
        // GET: Admin/SanPhamChiTiet/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sanPhamChiTiet = await _context.SanPhamChiTiets
                .Include(s => s.MauSac)
                .Include(s => s.SanPham)
                .Include(s => s.Size)
                .FirstOrDefaultAsync(m => m.MaSanPhamChiTiet == id);
            if (sanPhamChiTiet == null)
            {
                return NotFound();
            }

            return View(sanPhamChiTiet);
        }
        public async Task<IActionResult> Create(int? maSanPham)
        {
            if (maSanPham == null)
            {
                return NotFound("Không tìm thấy sản phẩm cha.");
            }

            var sanPham = await _context.SanPhams.FindAsync(maSanPham.Value);
            if (sanPham == null)
            {
                return NotFound("Sản phẩm cha không tồn tại.");
            }

            // Tạo ViewModel và gán các giá trị ban đầu
            var viewModel = new SanPhamChiTietCreateViewModel
            {
                MaSanPham = sanPham.MaSanPham,
                TenSanPham = sanPham.TenSanPham,
                TrangThai = 1 // Mặc định là Đang kinh doanh
            };

            // Dữ liệu cho các dropdown
            ViewData["MaMauSac"] = new SelectList(_context.MauSacs.Where(m => m.TrangThai == 1), "MaMauSac", "TenMau");
            ViewData["MaSize"] = new SelectList(_context.Sizes.Where(s => s.TrangThai == 1), "MaSize", "TenSize");

            return View(viewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SanPhamChiTietCreateViewModel viewModel)
        {
            // Luôn tải lại tên sản phẩm từ DB để tránh mất dữ liệu khi validation thất bại
            var sanPham = await _context.SanPhams.AsNoTracking().FirstOrDefaultAsync(p => p.MaSanPham == viewModel.MaSanPham);
            if (sanPham != null)
            {
                viewModel.TenSanPham = sanPham.TenSanPham;
            }
            else
            {
                // Xử lý trường hợp không tìm thấy sản phẩm cha (rất hiếm khi xảy ra)
                ModelState.AddModelError("", "Sản phẩm cha không hợp lệ.");
            }

            // --- VALIDATION TÙY CHỈNH ---
            if (viewModel.MaMauSac.HasValue && viewModel.MaSize.HasValue)
            {
                bool isDuplicate = await _context.SanPhamChiTiets.AnyAsync(ct =>
                    ct.MaSanPham == viewModel.MaSanPham &&
                    ct.MaMauSac == viewModel.MaMauSac.Value &&
                    ct.MaSize == viewModel.MaSize.Value);

                if (isDuplicate)
                {
                    ModelState.AddModelError("", "Phiên bản với Màu sắc và Size này đã tồn tại cho sản phẩm.");
                }
            }

            if (viewModel.file != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var ext = Path.GetExtension(viewModel.file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(ext))
                {
                    ModelState.AddModelError("file", "Định dạng ảnh không hợp lệ.");
                }
            }

            // --- KIỂM TRA KẾT QUẢ ---
            if (ModelState.IsValid)
            {
                // (Phần xử lý lưu vào DB giữ nguyên như cũ...)
                var sanPhamChiTiet = new SanPhamChiTiet
                {
                    MaSanPham = viewModel.MaSanPham,
                    MaMauSac = viewModel.MaMauSac.Value,
                    MaSize = viewModel.MaSize.Value,
                    GiaBan = viewModel.GiaBan.Value,
                    SoLuong = viewModel.SoLuong.Value,
                    GhiChu = string.IsNullOrEmpty(viewModel.GhiChu) ? "Không có ghi chú" : viewModel.GhiChu,
                    TrangThai = viewModel.TrangThai,
                    HinhAnh = "default.png"
                };

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(viewModel.file.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await viewModel.file.CopyToAsync(stream);
                }
                sanPhamChiTiet.HinhAnh = uniqueFileName;

                _context.Add(sanPhamChiTiet);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Thêm phiên bản mới thành công!";
                return RedirectToAction("Index", "SanPham", new { area = "Admin", openSanPhamId = sanPhamChiTiet.MaSanPham });
            }

            // --- XỬ LÝ KHI CÓ LỖI ---
            // Tải lại dữ liệu cho các DropDownList
            ViewData["MaMauSac"] = new SelectList(_context.MauSacs.Where(m => m.TrangThai == 1), "MaMauSac", "TenMau", viewModel.MaMauSac);
            ViewData["MaSize"] = new SelectList(_context.Sizes.Where(s => s.TrangThai == 1), "MaSize", "TenSize", viewModel.MaSize);

            // Trả về viewModel đã được cập nhật lại TenSanPham
            return View(viewModel);
        }
        public async Task<IActionResult> CreateMulti(int id)
        {
            var sp = await _context.SanPhams.FindAsync(id);

            if (sp == null)
            {
                return NotFound("Không tìm thấy sản phẩm với ID = " + id);
            }

            var vm = new SanPhamChiTietCreatesViewModel
            {
                MaSanPham = id,
                TenSanPham = sp.TenSanPham
            };

            // ✅ Chỉ lấy màu sắc đang kích hoạt
            ViewBag.MaMauSac = new SelectList(
                _context.MauSacs.Where(x => x.TrangThai == 1),
                "MaMauSac",
                "TenMau"
            );

            // ✅ Chỉ lấy size đang kích hoạt
            ViewBag.MaSize = new SelectList(
                _context.Sizes.Where(x => x.TrangThai == 1),
                "MaSize",
                "TenSize"
            );

            return View(vm);
        }


        [HttpPost]
        public async Task<IActionResult> CreateMulti(SanPhamChiTietCreatesViewModel model)
        {
            var duplicateItems = model.Items
      .GroupBy(x => new { x.MaMauSac, x.MaSize })
      .Where(g => g.Count() > 1)
      .Select(y => y.Key)
      .ToList();

            if (duplicateItems.Any())
            {
                ModelState.AddModelError("", "Danh sách có các phiên bản trùng nhau. Vui lòng kiểm tra lại.");
                PopulateViewBags();
                return View(model);
            }
            if (model.Items == null || !model.Items.Any())
            {
                ModelState.AddModelError("", "Vui lòng thêm ít nhất 1 sản phẩm chi tiết.");
                PopulateViewBags();
                return View(model);
            }

            // 2. VALIDATION CHI TIẾT TỪNG DÒNG
            for (int i = 0; i < model.Items.Count; i++)
            {
                var item = model.Items[i];

                // Validate Màu
                if (item.MaMauSac == null || item.MaMauSac == 0)
                {
                    ModelState.AddModelError($"Items[{i}].MaMauSac", "Chọn màu.");
                }

                // Validate Size
                if (item.MaSize == null || item.MaSize == 0)
                {
                    ModelState.AddModelError($"Items[{i}].MaSize", "Chọn size.");
                }

                // Validate Giá bán (Check null hoặc <= 0)
                if (item.GiaBan == null || item.GiaBan <= 0)
                {
                    ModelState.AddModelError($"Items[{i}].GiaBan", "Giá phải > 0.");
                }

                // Validate Số lượng (Check null hoặc < 0)
                if (item.SoLuong == null || item.SoLuong < 0)
                {
                    ModelState.AddModelError($"Items[{i}].SoLuong", "SL không hợp lệ.");
                }

                // Validate Ảnh (Chỉ bắt buộc nếu tạo mới, khi reload trang file sẽ bị mất nên phải chọn lại)
                if (item.file == null)
                {
                    ModelState.AddModelError($"Items[{i}].file", "Chọn ảnh.");
                }
            }

            // Nếu có bất kỳ lỗi nào -> Trả về View ngay lập tức
            if (!ModelState.IsValid)
            {
                PopulateViewBags(); // Nạp lại dropdown
                return View(model);
            }
            // 3. Nếu mọi thứ OK thì lưu vào DB
            foreach (var item in model.Items)
            {
                var ct = new SanPhamChiTiet
                {
                    MaSanPham = model.MaSanPham,



                    MaMauSac = item.MaMauSac.Value,
                    MaSize = item.MaSize.Value,

                    // Với Giá và Số lượng, dùng ?? 0 để phòng hờ
                    GiaBan = item.GiaBan ?? 0,
                    SoLuong = item.SoLuong ?? 0,

                    GhiChu = item.GhiChu,
                    TrangThai = item.TrangThai
                };
                bool existsInDb = await _context.SanPhamChiTiets.AnyAsync(sp =>
            sp.MaSanPham == model.MaSanPham &&
            sp.MaMauSac == item.MaMauSac &&
            sp.MaSize == item.MaSize);

                if (existsInDb)
                {
                    // Lấy tên màu và size để báo lỗi chi tiết
                    var mau = await _context.MauSacs.FindAsync(item.MaMauSac);
                    var size = await _context.Sizes.FindAsync(item.MaSize);

                    ModelState.AddModelError("", $"Phiên bản {mau?.TenMau} - {size?.TenSize} đã tồn tại trong hệ thống.");
                    PopulateViewBags();
                    return View(model);
                }
                if (item.file != null)
                {
                    string fileName = Guid.NewGuid() + Path.GetExtension(item.file.FileName);
                    string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);

                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await item.file.CopyToAsync(stream);
                    }
                    ct.HinhAnh = fileName;
                }
                else
                {
                    ct.HinhAnh = "default.png"; // Xử lý nếu không có ảnh (phòng hờ)
                }

                _context.SanPhamChiTiets.Add(ct);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Details", "SanPham", new { id = model.MaSanPham });
        }

        // --- HÀM PHỤ TRỢ (Để cuối Controller) ---
        private void PopulateViewBags()
        {
            // Dùng AsNoTracking() để tăng tốc độ truy vấn vì chỉ lấy dữ liệu hiển thị
            ViewBag.MaMauSac = new SelectList(_context.MauSacs.AsNoTracking().Where(x => x.TrangThai == 1), "MaMauSac", "TenMau");
            ViewBag.MaSize = new SelectList(_context.Sizes.AsNoTracking().Where(x => x.TrangThai == 1), "MaSize", "TenSize");
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

			//var sanPhamChiTiet = await _context.SanPhamChiTiets.FindAsync(id);
			var sanPhamChiTiet = await _context.SanPhamChiTiets.Include(x => x.SanPham).FirstOrDefaultAsync(x => x.MaSanPhamChiTiet == id);
			if (sanPhamChiTiet == null)
            {
                return NotFound();
            }

			ViewBag.MaSanPham = sanPhamChiTiet.MaSanPham; //

			ViewData["MaMauSac"] = new SelectList(_context.MauSacs, "MaMauSac", "TenMau", sanPhamChiTiet.MaMauSac);
            ViewData["MaSanPham"] = new SelectList(_context.SanPhams, "MaSanPham", "TenSanPham", sanPhamChiTiet.MaSanPham);
            ViewData["MaSize"] = new SelectList(_context.Sizes, "MaSize", "TenSize", sanPhamChiTiet.MaSize);
            return View(sanPhamChiTiet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, IFormFile? file)
        {
            // Bước 1: Tải đối tượng gốc từ database
            var chiTietToUpdate = await _context.SanPhamChiTiets.FindAsync(id);
            if (chiTietToUpdate == null)
            {
                return NotFound();
            }

            // Bước 2: Cố gắng cập nhật các giá trị từ form vào đối tượng đã tải
            await TryUpdateModelAsync(chiTietToUpdate, "",
                ct => ct.MaMauSac, ct => ct.MaSize, ct => ct.GiaBan, ct => ct.GhiChu);

            // === BƯỚC 3: XÓA CÁC LỖI VALIDATION KHÔNG MONG MUỐN ===
            // Đây là bước quan trọng nhất để sửa lỗi
            ModelState.Remove("SanPham");
            ModelState.Remove("MauSac");
            ModelState.Remove("Size");
            ModelState.Remove("DonHangChiTiets");
            ModelState.Remove("ChiTietGioHangs");
            // =======================================================

            // Bước 4: Thực hiện các validation tùy chỉnh của bạn
            bool isDuplicate = await _context.SanPhamChiTiets.AnyAsync(ct =>
                ct.MaSanPham == chiTietToUpdate.MaSanPham &&
                ct.MaMauSac == chiTietToUpdate.MaMauSac &&
                ct.MaSize == chiTietToUpdate.MaSize &&
                ct.MaSanPhamChiTiet != id);

            if (isDuplicate)
            {
                ModelState.AddModelError("", "Một phiên bản khác với Màu sắc và Size này đã tồn tại.");
            }

            if (file != null && file.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(ext))
                {
                    ModelState.AddModelError("HinhAnh", "Định dạng ảnh không hợp lệ (.jpg, .jpeg, .png).");
                }
            }

            // Bước 5: Kiểm tra ModelState lần cuối và lưu
            if (ModelState.IsValid)
            {
                try
                {
                    // Xử lý upload file ảnh mới
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
                        chiTietToUpdate.HinhAnh = uniqueFileName;
                    }

                    if (string.IsNullOrEmpty(chiTietToUpdate.GhiChu))
                    {
                        chiTietToUpdate.GhiChu = "Không";
                    }

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật phiên bản thành công!";
                    return RedirectToAction("Index", "SanPham", new { area = "Admin", openSanPhamId = chiTietToUpdate.MaSanPham });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SanPhamChiTietExists(chiTietToUpdate.MaSanPhamChiTiet)) { return NotFound(); } else { throw; }
                }
            }

            // Bước 6: Xử lý khi có lỗi thực sự
            ViewData["MaMauSac"] = new SelectList(_context.MauSacs, "MaMauSac", "TenMau", chiTietToUpdate.MaMauSac);
            // Tải lại cả TenSanPham để hiển thị đúng trên dropdown bị disable
            ViewData["MaSanPham"] = new SelectList(_context.SanPhams, "MaSanPham", "TenSanPham", chiTietToUpdate.MaSanPham);
            ViewData["MaSize"] = new SelectList(_context.Sizes, "MaSize", "TenSize", chiTietToUpdate.MaSize);
            return View(chiTietToUpdate);
        }


        // GET: Admin/SanPhamChiTiet/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sanPhamChiTiet = await _context.SanPhamChiTiets
                .Include(s => s.MauSac)
                .Include(s => s.SanPham)
                .Include(s => s.Size)
                .FirstOrDefaultAsync(m => m.MaSanPhamChiTiet == id);
            if (sanPhamChiTiet == null)
            {
                return NotFound();
            }

            return View(sanPhamChiTiet);
        }

        // POST: Admin/SanPhamChiTiet/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var sanPhamChiTiet = await _context.SanPhamChiTiets.FindAsync(id);
            var maSanPhamCha = sanPhamChiTiet?.MaSanPham;
            if (sanPhamChiTiet != null)
            {
                _context.SanPhamChiTiets.Remove(sanPhamChiTiet);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "SanPham", new { area = "Admin", openSanPhamId = maSanPhamCha });
        }

        private bool SanPhamChiTietExists(int id)
        {
            return _context.SanPhamChiTiets.Any(e => e.MaSanPhamChiTiet == id);
        }
        public async Task<IActionResult> ListBySanPham(int sanPhamId, string statusFilter = "active")
        {
            var sanPham = await _context.SanPhams.FindAsync(sanPhamId); // Thêm dòng này
            ViewBag.TenSanPham = sanPham?.TenSanPham ?? "Sản phẩm không xác định"; // Thêm dòng này
            var query = _context.SanPhamChiTiets
                .Include(s => s.MauSac)
                .Include(s => s.Size)
                .Where(s => s.MaSanPham == sanPhamId);

            // Lọc các phiên bản theo trạng thái
            if (statusFilter == "inactive")
            {
                query = query.Where(ct => ct.TrangThai == 0);
            }
            else if (statusFilter == "not_yet_active") // <-- THÊM MỚI
            {
                query = query.Where(ct => ct.TrangThai == 2);
            }
            else
            {
                statusFilter = "active"; // Đảm bảo giá trị mặc định
                query = query.Where(ct => ct.TrangThai == 1);
            }

            var chiTietList = await query.ToListAsync();

            const string settingKey = "PromotionRule";
            var promotionRuleSetting = await _context.SystemSettings.FindAsync(settingKey);
            string currentRule = promotionRuleSetting?.SettingValue ?? "BestValue";

            var allActivePromotions = await _context.KhuyenMais
                 .Where(km => km.TrangThai == 1 &&
                                km.NgayBatDau <= DateTime.Now &&
                                km.NgayKetThuc >= DateTime.Now &&
                                !string.IsNullOrEmpty(km.DanhSachSanPhamApDung) &&
                                (km.KenhApDung == "TaiQuay" || km.KenhApDung == "TatCa"))
                 .ToListAsync();

            var giaDaGiamDict = new Dictionary<int, decimal>();
            foreach (var ct in chiTietList)
            {
                var promotionsForThisVariant = allActivePromotions.Where(promo =>
                {
                    var appliedIds = promo.DanhSachSanPhamApDung.Split(',');
                    return appliedIds.Contains($"v-{ct.MaSanPhamChiTiet}") || appliedIds.Contains($"p-{ct.MaSanPham}");
                }).ToList();

                decimal finalPrice = ct.GiaBan;

                if (currentRule == "Stackable" && promotionsForThisVariant.Any())
                {
                    decimal stackedPrice = ct.GiaBan;
                    foreach (var promo in promotionsForThisVariant)
                    {
                        stackedPrice = CalculateDiscountedPrice(stackedPrice, promo);
                    }
                    finalPrice = stackedPrice;
                }
                else // BestValue
                {
                    if (promotionsForThisVariant.Any())
                    {
                        var bestPromotion = promotionsForThisVariant
                            .OrderBy(km => CalculateDiscountedPrice(ct.GiaBan, km))
                            .FirstOrDefault();

                        if (bestPromotion != null)
                        {
                            finalPrice = CalculateDiscountedPrice(ct.GiaBan, bestPromotion);
                        }
                    }
                }

                if (finalPrice < ct.GiaBan)
                {
                    giaDaGiamDict[ct.MaSanPhamChiTiet] = finalPrice;
                }
            }

            ViewBag.GiaDaGiam = giaDaGiamDict;
            ViewBag.CurrentChiTietStatusFilter = statusFilter; // <-- Quan trọng: Truyền bộ lọc hiện tại của phiên bản
            ViewBag.SanPhamId = sanPhamId; // <-- Quan trọng: Truyền ID sản phẩm cha
            return PartialView("_SanPhamChiTietList", chiTietList);
        }
        // THÊM HÀM HELPER NÀY VÀO TRONG CONTROLLER NẾU CHƯA CÓ
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var sanPhamChiTiet = await _context.SanPhamChiTiets
                                             .Include(spct => spct.SanPham)
                                             .ThenInclude(sp => sp.SanPhamChiTiets)
                                             .FirstOrDefaultAsync(spct => spct.MaSanPhamChiTiet == id);

            if (sanPhamChiTiet == null)
            {
                return Json(new { success = false, message = "Không tìm thấy phiên bản sản phẩm." });
            }

            // Logic mới: Nếu đang KD (1) -> Ngừng KD (0). Mọi trường hợp khác (0, 2) -> Đang KD (1)
            sanPhamChiTiet.TrangThai = (sanPhamChiTiet.TrangThai == 1) ? 0 : 1;

            try
            {
                _context.Update(sanPhamChiTiet);
                await _context.SaveChangesAsync();

                var parentProduct = sanPhamChiTiet.SanPham;

                // --- BẮT ĐẦU PHẦN TÍNH TOÁN MỚI ---
                // Kiểm tra xem sản phẩm cha có còn phù hợp với bộ lọc "Đang kinh doanh" không
                bool parentHasActiveVariants = parentProduct.SanPhamChiTiets.Any(ct => ct.TrangThai == 1);
                // Tính toán lại số lượng để trả về cho giao diện
                int newActiveStock = parentProduct.SanPhamChiTiets.Where(ct => ct.TrangThai == 1).Sum(ct => ct.SoLuong);
                int newTotalStock = parentProduct.SanPhamChiTiets.Sum(ct => ct.SoLuong);
                // --- KẾT THÚC PHẦN TÍNH TOÁN MỚI ---

                return Json(new
                {
                    success = true,
                    parentHasActiveVariants = parentHasActiveVariants,
                    newActiveStock = newActiveStock, // Trả về số lượng đang KD mới
                    newTotalStock = newTotalStock    // Trả về tổng số lượng mới
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi cập nhật trạng thái: " + ex.Message });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStock(int id, int quantityToAdd)
        {
            if (quantityToAdd <= 0)
            {
                return Json(new { success = false, message = "Số lượng nhập thêm phải lớn hơn 0." });
            }

            var sanPhamChiTiet = await _context.SanPhamChiTiets.FindAsync(id);
            if (sanPhamChiTiet == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy phiên bản sản phẩm." });
            }

            try
            {
                // Cộng dồn số lượng
                sanPhamChiTiet.SoLuong += quantityToAdd;

                // (Gợi ý nâng cao) Tại đây, bạn có thể tạo một bản ghi lịch sử trong bảng PhieuNhapKho
                // để theo dõi chi tiết các lần nhập hàng.

                await _context.SaveChangesAsync();

                // Trả về số lượng mới và ID sản phẩm cha để cập nhật giao diện
                return Json(new
                {
                    success = true,
                    newQuantity = sanPhamChiTiet.SoLuong,
                    parentId = sanPhamChiTiet.MaSanPham
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi server: " + ex.Message });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdjustStock(int id, int actualQuantity, string reason)
        {
            if (actualQuantity < 0)
            {
                return Json(new { success = false, message = "Số lượng thực tế không thể là số âm." });
            }

            var sanPhamChiTiet = await _context.SanPhamChiTiets.FindAsync(id);
            if (sanPhamChiTiet == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy phiên bản sản phẩm." });
            }

            try
            {
                int oldQuantity = sanPhamChiTiet.SoLuong;
                // Cập nhật số lượng về đúng giá trị thực tế
                sanPhamChiTiet.SoLuong = actualQuantity;

                // (Nâng cao) Ghi lại lịch sử kiểm kê
                // var adjustmentLog = new LichSuKiemKe 
                // { 
                //     MaSanPhamChiTiet = id,
                //     SoLuongTruoc = oldQuantity,
                //     SoLuongThucTe = actualQuantity,
                //     ChenhLech = actualQuantity - oldQuantity,
                //     LyDo = reason,
                //     NgayThucHien = DateTime.Now,
                //     MaNhanVien = // Lấy ID người dùng đang đăng nhập
                // };
                // _context.LichSuKiemKes.Add(adjustmentLog);

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    newQuantity = sanPhamChiTiet.SoLuong,
                    parentId = sanPhamChiTiet.MaSanPham
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi server: " + ex.Message });
            }
        }
    }
}
