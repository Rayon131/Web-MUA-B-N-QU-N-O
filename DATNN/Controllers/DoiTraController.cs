using DATNN.Models;
using DATNN.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DATNN.Controllers
{
    [Authorize]
    public class DoiTraController : Controller
    {
        private readonly MyDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        // Bỏ IVnPayService vì khách hàng không cần dùng đến
        public DoiTraController(MyDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // KHÁCH HÀNG: Lịch sử các yêu cầu đổi trả
        public async Task<IActionResult> LichSuYeuCau()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var yeuCaus = await _context.YeuCauDoiTras
                .Include(yc => yc.DonHangChiTiet.SanPhamChiTiet.SanPham)
                .Where(yc => yc.MaNguoiDung.ToString() == userId)
                .OrderByDescending(yc => yc.NgayTao)
                .ToListAsync();

            return View(yeuCaus);
        }

        [HttpGet]
        public async Task<IActionResult> TaoYeuCau(int id) // id là MaDonHangChiTiet
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // BƯỚC 1: Lấy thông tin sản phẩm khách đã mua TRƯỚC
            var donHangChiTiet = await _context.DonHangChiTiets
                .Include(ct => ct.DonHang)
                .Include(ct => ct.SanPhamChiTiet.SanPham) // Include để lấy MaSanPham
                .Include(ct => ct.SanPhamChiTiet.MauSac)
                .Include(ct => ct.SanPhamChiTiet.Size)
                .FirstOrDefaultAsync(ct => ct.MaDonHangChiTiet == id && ct.DonHang.MaKhachHang.ToString() == userId);

            if (donHangChiTiet == null || donHangChiTiet.DonHang.TrangThaiDonHang != 5)
            {
                TempData["ErrorMessage"] = "Sản phẩm không hợp lệ để yêu cầu đổi trả.";
                return RedirectToAction("LichSuDatHang", "GioHang");
            }

            // BƯỚC 2: Kiểm tra yêu cầu cũ (giữ nguyên logic của bạn)
            var existingRequest = await _context.YeuCauDoiTras.FirstOrDefaultAsync(yc => yc.MaDonHangChiTiet == id);
            if (existingRequest != null)
            {
                TempData["ErrorMessage"] = "Bạn đã gửi yêu cầu đổi/trả cho sản phẩm này rồi.";
                return RedirectToAction("ChiTietDonHang", "GioHang", new { id = donHangChiTiet.DonHang.MaDonHang });
            }

            // BƯỚC 3: Lấy danh sách sản phẩm để đổi (CHỈ LẤY CÙNG LOẠI)
            // Lấy MaSanPham gốc
            var currentProductId = donHangChiTiet.SanPhamChiTiet.MaSanPham;

            var availableVariants = await _context.SanPhamChiTiets
                .Where(spct => spct.SoLuong > 0
                            && spct.TrangThai == 1
                            && spct.MaSanPham == currentProductId) // <--- QUAN TRỌNG: Chỉ lấy cùng cha
                .Include(s => s.SanPham)
                .Include(s => s.MauSac)
                .Include(s => s.Size)
                .ToListAsync();

            // Tạo ViewBag (Lúc này list chỉ chứa 1 ProductGroup là sản phẩm hiện tại)
            ViewBag.ProductGroups = availableVariants
                .GroupBy(spct => spct.MaSanPham)
                .Select(group => new ProductGroupViewModel
                {
                    ProductName = group.First().SanPham.TenSanPham,
                    Variants = group.Select(variant => new VariantViewModel
                    {
                        Id = variant.MaSanPhamChiTiet,
                        // Hiển thị rõ Màu - Size
                        DisplayText = $"Màu: {variant.MauSac.TenMau} - Size: {variant.Size.TenSize}",
                        Price = variant.GiaBan,
                        FullTextForSearch = $"{group.First().SanPham.TenSanPham} {variant.MauSac.TenMau} {variant.Size.TenSize}"
                    }).ToList()
                })
                .ToList();

            // Các phần còn lại giữ nguyên
            var chinhSach = _context.ChinhSaches.FirstOrDefault(x => x.id == 2);

            // Nạp lại các include cần thiết nếu bị thiếu (đoạn logic cũ của bạn)
            await _context.Entry(donHangChiTiet.DonHang)
              .Collection(dh => dh.DonHangChiTiets)
              .Query()
              .Include(dhct => dhct.SanPhamChiTiet.SanPham)
              .Include(dhct => dhct.SanPhamChiTiet.MauSac)
              .Include(dhct => dhct.SanPhamChiTiet.Size)
              .LoadAsync();

            var isCOD = donHangChiTiet.DonHang.PhuongThucThanhToan == "COD";
            var viewModel = new TaoYeuCauViewModel
            {
                MaDonHangChiTiet = id,
                TenSanPham = donHangChiTiet.TenSanPham_Luu ?? donHangChiTiet.SanPhamChiTiet.SanPham.TenSanPham,
                TenMau = donHangChiTiet.TenMau_Luu ?? donHangChiTiet.SanPhamChiTiet.MauSac.TenMau,
                TenSize = donHangChiTiet.TenSize_Luu ?? donHangChiTiet.SanPhamChiTiet.Size.TenSize,
                SoLuong = donHangChiTiet.SoLuong,
                GiaTri = donHangChiTiet.DonGia * donHangChiTiet.SoLuong,
                IsCOD = isCOD,
                NoiDungChinhSach = chinhSach?.NoiDung ?? "Không tìm thấy chính sách đổi/trả.",
                MaDonHangGoc = donHangChiTiet.DonHang.MaDonHang,
                ThoiGianTaoDonHangGoc = donHangChiTiet.DonHang.ThoiGianTao,
                TongTienDonHangGoc = donHangChiTiet.DonHang.TongTien
            };

            return View(viewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TaoYeuCau(TaoYeuCauViewModel model, IFormFile hinhAnh) // Nhận ViewModel từ form
        {
            // Bước 1: Kiểm tra validation của form
            if (ModelState.IsValid)
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

                // Bước 2: Xác thực lại logic nghiệp vụ trên server
                var donHangChiTiet = await _context.DonHangChiTiets
                    .AsNoTracking()
                    .Include(ct => ct.DonHang)
                    .FirstOrDefaultAsync(ct => ct.MaDonHangChiTiet == model.MaDonHangChiTiet && ct.DonHang.MaKhachHang == userId && ct.DonHang.TrangThaiDonHang == 5);

                // THÊM KIỂM TRA SỐ LƯỢNG YÊU CẦU SO VỚI SỐ LƯỢNG ĐÃ MUA
                if (donHangChiTiet == null || model.SoLuongYeuCau > donHangChiTiet.SoLuong)
                {
                    ModelState.AddModelError("SoLuongYeuCau", "Số lượng yêu cầu không thể lớn hơn số lượng đã mua.");
                }
                else
                {
                    // Bước 3: Nếu mọi thứ đều hợp lệ, tạo yêu cầu và chuyển hướng
                    var newYeuCau = new YeuCauDoiTra
                    {
                        MaDonHangChiTiet = model.MaDonHangChiTiet,
                        MaNguoiDung = userId,
                        LoaiYeuCau = model.LoaiYeuCau,
                        LyDo = model.LyDo,
                        GhiChuKhachHang = model.GhiChuKhachHang,

                        // =============================================================
                        // === SỬA LỖI Ở ĐÂY: THÊM DÒNG NÀY VÀO ===
                        // =============================================================
                        SoLuongYeuCau = model.SoLuongYeuCau,
                        // =============================================================

                        TrangThai = TrangThaiYeuCauDoiTra.ChoXacNhan,
                        NgayTao = DateTime.Now
                    };
                    if (newYeuCau.LoaiYeuCau == LoaiYeuCau.DoiHang)
                    {
                        newYeuCau.MaSanPhamChiTietMoi = model.MaSanPhamChiTietMoi;

                        // === BẮT ĐẦU: LƯU CỨNG THÔNG TIN SẢN PHẨM MỚI ===
                        if (model.MaSanPhamChiTietMoi.HasValue)
                        {
                            var sanPhamMoi = await _context.SanPhamChiTiets
                                .Include(sp => sp.SanPham)
                                .Include(sp => sp.MauSac)
                                .Include(sp => sp.Size)
                                .FirstOrDefaultAsync(sp => sp.MaSanPhamChiTiet == model.MaSanPhamChiTietMoi.Value);

                            if (sanPhamMoi != null)
                            {
                                newYeuCau.TenSanPhamMoi_Luu = sanPhamMoi.SanPham.TenSanPham;
                                newYeuCau.TenMauMoi_Luu = sanPhamMoi.MauSac.TenMau;
                                newYeuCau.TenSizeMoi_Luu = sanPhamMoi.Size.TenSize;

                                // Ưu tiên lấy ảnh phiên bản, nếu không có lấy ảnh cha
                                newYeuCau.HinhAnhMoi_Luu = !string.IsNullOrEmpty(sanPhamMoi.HinhAnh)
                                                            ? sanPhamMoi.HinhAnh
                                                            : sanPhamMoi.SanPham.AnhSanPham;

                                newYeuCau.GiaSanPhamMoi_Luu = sanPhamMoi.GiaBan;
                            }
                        }
                        // === KẾT THÚC ===
                    }
                    if (newYeuCau.LoaiYeuCau == LoaiYeuCau.TraHang)
                    {
                        // Luồng COD: Khách hàng bắt buộc phải nhập thông tin ngân hàng
                        if (donHangChiTiet.DonHang.PhuongThucThanhToan == "COD")
                        {
                            newYeuCau.HinhThucHoanTien = HinhThucHoanTien.ChuyenKhoan; // Mặc định là chuyển khoản cho đơn COD
                            newYeuCau.TenNganHang = model.TenNganHang;
                            newYeuCau.TenChuTaiKhoan = model.TenChuTaiKhoan;
                            newYeuCau.SoTaiKhoan = model.SoTaiKhoan;
                            newYeuCau.ChiNhanh = model.ChiNhanh;
                        }
                        // Luồng VNPAY: Khách hàng có lựa chọn
                        else
                        {
                            newYeuCau.HinhThucHoanTien = model.HinhThucHoanTien;

                            // Chỉ lưu thông tin ngân hàng nếu khách chọn "Ngân hàng khác"
                            if (model.HinhThucHoanTien == HinhThucHoanTien.NganHangKhac)
                            {
                                newYeuCau.TenNganHang = model.TenNganHang;
                                newYeuCau.TenChuTaiKhoan = model.TenChuTaiKhoan;
                                newYeuCau.SoTaiKhoan = model.SoTaiKhoan;
                                newYeuCau.ChiNhanh = model.ChiNhanh;
                            }
                        }
                    }
                    if (hinhAnh != null && hinhAnh.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images/returns");
                        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(hinhAnh.FileName);
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await hinhAnh.CopyToAsync(fileStream);
                        }
                        newYeuCau.HinhAnhBangChung = uniqueFileName;
                    }

                    _context.YeuCauDoiTras.Add(newYeuCau);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Gửi yêu cầu đổi/trả thành công. Vui lòng chờ quản trị viên xác nhận.";
                    return RedirectToAction("LichSuYeuCau");
                }
            }

          
            var chiTiet = await _context.DonHangChiTiets
             .AsNoTracking()
             .Include(ct => ct.SanPhamChiTiet.SanPham)
             .FirstOrDefaultAsync(ct => ct.MaDonHangChiTiet == model.MaDonHangChiTiet);

            if (chiTiet != null)
            {
                model.TenSanPham = chiTiet.SanPhamChiTiet.SanPham.TenSanPham;
                model.SoLuong = chiTiet.SoLuong;
                model.GiaTri = chiTiet.DonGia * chiTiet.SoLuong;
            }
            await PopulateTaoYeuCauData(model);
            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> ChiTietYeuCau(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var yeuCau = await _context.YeuCauDoiTras
                .Include(yc => yc.DonHangChiTiet.SanPhamChiTiet.SanPham)
                .Include(yc => yc.DonHangChiTiet.SanPhamChiTiet.MauSac)
                .Include(yc => yc.DonHangChiTiet.SanPhamChiTiet.Size)
                .Include(yc => yc.DonHangChiTiet.DonHang)
                    .ThenInclude(dh => dh.DonHangChiTiets)
                        .ThenInclude(dhct => dhct.SanPhamChiTiet)
                            .ThenInclude(spct => spct.SanPham)
                .Include(yc => yc.DonHangChiTiet.DonHang) // Lặp lại Include DonHang
                    .ThenInclude(dh => dh.DonHangChiTiets)
                        .ThenInclude(dhct => dhct.SanPhamChiTiet)
                            .ThenInclude(spct => spct.MauSac) // Để nạp MauSac
                .Include(yc => yc.DonHangChiTiet.DonHang) // Lặp lại Include DonHang
                    .ThenInclude(dh => dh.DonHangChiTiets)
                        .ThenInclude(dhct => dhct.SanPhamChiTiet)
                            .ThenInclude(spct => spct.Size) // Để nạp Size
                .FirstOrDefaultAsync(yc => yc.Id == id && yc.MaNguoiDung.ToString() == userId);

            if (yeuCau == null) return NotFound();
            decimal tienHangHoan = yeuCau.DonHangChiTiet.DonGia * yeuCau.SoLuongYeuCau;
            decimal tienShipHoan = 0;
            bool daHoanShip = false;

            // Chỉ tính nếu trạng thái là Hoàn thành và là Trả hàng
            if (yeuCau.TrangThai == TrangThaiYeuCauDoiTra.HoanThanh && yeuCau.LoaiYeuCau == LoaiYeuCau.TraHang)
            {
                // Tìm phiếu chi liên quan
                var phieuChi = await _context.PhieuChis
                    .FirstOrDefaultAsync(p => p.MaYeuCauDoiTra == id);

                if (phieuChi != null)
                {
                    // Nếu tổng tiền chi > tiền hàng => Có hoàn ship
                    if (phieuChi.SoTien > tienHangHoan)
                    {
                        tienShipHoan = phieuChi.SoTien - tienHangHoan;
                        daHoanShip = true;
                    }
                }
            }

            decimal tongTienHoanThucTe = tienHangHoan + tienShipHoan;

            // Truyền qua ViewBag
            ViewBag.TongTienHoanThucTe = tongTienHoanThucTe;
            ViewBag.TienHangHoan = tienHangHoan;
            ViewBag.TienShipHoan = tienShipHoan;
            return View(yeuCau);
        }
        private async Task PopulateTaoYeuCauData(TaoYeuCauViewModel model)
        {
            // 1. Lấy thông tin chi tiết đơn hàng TRƯỚC để biết ID sản phẩm cha
            var donHangChiTiet = await _context.DonHangChiTiets
                 .Include(ct => ct.DonHang)
                 .Include(ct => ct.SanPhamChiTiet.SanPham)
                 .Include(ct => ct.SanPhamChiTiet.MauSac) // Include thêm để hiển thị lại
                 .Include(ct => ct.SanPhamChiTiet.Size)   // Include thêm để hiển thị lại
                 .FirstOrDefaultAsync(ct => ct.MaDonHangChiTiet == model.MaDonHangChiTiet);

            if (donHangChiTiet != null)
            {
                // 2. Lấy danh sách biến thể CÙNG LOẠI
                var currentProductId = donHangChiTiet.SanPhamChiTiet.MaSanPham;

                var availableVariants = await _context.SanPhamChiTiets
                   .Where(spct => spct.SoLuong > 0
                               && spct.TrangThai == 1
                               && spct.MaSanPham == currentProductId) // <--- Lọc theo MaSanPham
                   .Include(s => s.SanPham).Include(s => s.MauSac).Include(s => s.Size)
                   .ToListAsync();

                ViewBag.ProductGroups = availableVariants
                    .GroupBy(spct => spct.MaSanPham)
                    .Select(group => new ProductGroupViewModel
                    {
                        ProductName = group.First().SanPham.TenSanPham,
                        Variants = group.Select(variant => new VariantViewModel
                        {
                            Id = variant.MaSanPhamChiTiet,
                            DisplayText = $"Màu: {variant.MauSac.TenMau} - Size: {variant.Size.TenSize}",
                            Price = variant.GiaBan,
                            FullTextForSearch = $"{group.First().SanPham.TenSanPham} {variant.MauSac.TenMau} {variant.Size.TenSize}"
                        }).ToList()
                    }).ToList();

                // Nạp lại các thông tin khác cho DonHangChiTiet (giữ nguyên code cũ của bạn)
                await _context.Entry(donHangChiTiet.DonHang)
                    .Collection(dh => dh.DonHangChiTiets)
                    .Query()
                    .Include(dhct => dhct.SanPhamChiTiet.SanPham)
                    .Include(dhct => dhct.SanPhamChiTiet.MauSac)
                    .Include(dhct => dhct.SanPhamChiTiet.Size)
                    .LoadAsync();

                // Gán lại data vào model để View hiển thị
                model.TenSanPham = donHangChiTiet.TenSanPham_Luu ?? donHangChiTiet.SanPhamChiTiet.SanPham.TenSanPham;
                model.TenMau = donHangChiTiet.TenMau_Luu ?? donHangChiTiet.SanPhamChiTiet.MauSac.TenMau;
                model.TenSize = donHangChiTiet.TenSize_Luu ?? donHangChiTiet.SanPhamChiTiet.Size.TenSize;
                model.SoLuong = donHangChiTiet.SoLuong;
                model.GiaTri = donHangChiTiet.DonGia * model.SoLuong; // Lưu ý: dùng giá lúc mua
                model.IsCOD = donHangChiTiet.DonHang.PhuongThucThanhToan == "COD";
                model.NoiDungChinhSach = _context.ChinhSaches.FirstOrDefault(x => x.id == 2)?.NoiDung ?? "Không tìm thấy.";
                model.MaDonHangGoc = donHangChiTiet.DonHang.MaDonHang;
                model.ThoiGianTaoDonHangGoc = donHangChiTiet.DonHang.ThoiGianTao;
                model.TongTienDonHangGoc = donHangChiTiet.DonHang.TongTien;
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CapNhatHinhThucHoanTien(int id, HinhThucHoanTien hinhThucHoanTien, string tenNganHang, string tenChuTaiKhoan, string soTaiKhoan, string chiNhanh)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var yeuCau = await _context.YeuCauDoiTras.FirstOrDefaultAsync(yc => yc.Id == id && yc.MaNguoiDung == userId);

            if (yeuCau == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy yêu cầu.";
                return RedirectToAction("LichSuYeuCau");
            }

            // Chỉ cho phép cập nhật nếu yêu cầu đã được duyệt và chưa chọn hình thức
            if (yeuCau.TrangThai == TrangThaiYeuCauDoiTra.DaDuyet && yeuCau.HinhThucHoanTien == null)
            {
                yeuCau.HinhThucHoanTien = hinhThucHoanTien;
                if (hinhThucHoanTien == HinhThucHoanTien.ChuyenKhoan)
                {
                    // Thêm validate ở đây cho chắc chắn
                    if (string.IsNullOrWhiteSpace(tenNganHang) || string.IsNullOrWhiteSpace(tenChuTaiKhoan) || string.IsNullOrWhiteSpace(soTaiKhoan))
                    {
                        TempData["ErrorMessage"] = "Vui lòng điền đầy đủ thông tin ngân hàng.";
                        return RedirectToAction("ChiTietYeuCau", new { id });
                    }
                    yeuCau.TenNganHang = tenNganHang;
                    yeuCau.TenChuTaiKhoan = tenChuTaiKhoan;
                    yeuCau.SoTaiKhoan = soTaiKhoan;
                    yeuCau.ChiNhanh = chiNhanh;
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật hình thức hoàn tiền thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể cập nhật hình thức hoàn tiền cho yêu cầu này.";
            }

            return RedirectToAction("ChiTietYeuCau", new { id });
        }
    }
}
