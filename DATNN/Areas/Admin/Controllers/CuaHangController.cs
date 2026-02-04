using DATNN.Areas.Admin.Models;
using DATNN.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DATNN.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin")]
    public class CuaHangController : Controller
    {
       
        private readonly MyDbContext _context;
        private readonly GeocodingService _geocodingService;
        public CuaHangController(MyDbContext context, GeocodingService geocodingService)
        {
            _context = context;
            _geocodingService = geocodingService; // <-- THÊM DÒNG NÀY
        }

        public async Task<IActionResult> Edit()
        {
            var viewModel = new CuaHangViewModel
            {
                ThongTinCuaHang = await _context.ThongTinCuaHangs.FirstOrDefaultAsync(t => t.Id == 1),
                DiaChiCuaHang = await _context.DiaChiCuaHangs.FirstOrDefaultAsync(d => d.Id == 1),
                ThongTinGiaoHang = await _context.ThongTinGiaoHangs.FirstOrDefaultAsync(g => g.Id == 1)
            };

            if (viewModel.ThongTinCuaHang == null || viewModel.DiaChiCuaHang == null || viewModel.ThongTinGiaoHang == null)
            {
                // Xử lý trường hợp dữ liệu ban đầu chưa có
                // Bạn có thể tạo mới các đối tượng ở đây nếu cần
                return NotFound();
            }

            return View(viewModel);
        }

        // POST: CuaHang/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CuaHangViewModel model)
        {
            // Lấy tên Tỉnh, Quận, Phường từ hidden input
            model.DiaChiCuaHang.ThanhPho = Request.Form["DiaChiCuaHang.ThanhPho"];
            model.DiaChiCuaHang.Quan = Request.Form["DiaChiCuaHang.Quan"];
            model.DiaChiCuaHang.Phuong = Request.Form["DiaChiCuaHang.Phuong"];

            // VALIDATE THÊM
            if (model.ThongTinGiaoHang.PhiGiaoHang <= 0)
                ModelState.AddModelError("ThongTinGiaoHang.PhiGiaoHang", "Phí giao hàng phải lớn hơn 0");

            if (model.ThongTinGiaoHang.BanKinhGiaoHang < 0)
                ModelState.AddModelError("ThongTinGiaoHang.BanKinhGiaoHang", "Bán kính giao hàng không được âm");

            if (model.ThongTinGiaoHang.BanKinhfree < 0)
                ModelState.AddModelError("ThongTinGiaoHang.BanKinhfree", "Bán kính miễn phí không được âm");

            if (model.ThongTinGiaoHang.BanKinhfree > model.ThongTinGiaoHang.BanKinhGiaoHang)
                ModelState.AddModelError("ThongTinGiaoHang.BanKinhfree", "Bán kính miễn phí không được lớn hơn bán kính giao hàng");

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                model.ThongTinCuaHang.Id = 1;
                model.DiaChiCuaHang.Id = 1;
                model.ThongTinGiaoHang.Id = 1;

                var (lat, lon) = await _geocodingService.LayToaDoTuDiaChiAsync(
                    model.DiaChiCuaHang.ChiTietDiaChi,
                    model.DiaChiCuaHang.Phuong,
                    model.DiaChiCuaHang.Quan,
                    model.DiaChiCuaHang.ThanhPho
                );

                model.DiaChiCuaHang.Latitude = lat;
                model.DiaChiCuaHang.Longitude = lon;

                _context.Update(model.ThongTinCuaHang);
                _context.Update(model.DiaChiCuaHang);
                _context.Update(model.ThongTinGiaoHang);

                await _context.SaveChangesAsync();

                TempData["Success"] = "Cập nhật thành công!";
                return RedirectToAction("Edit");
            }
            catch
            {
                ModelState.AddModelError("", "Lỗi khi cập nhật dữ liệu.");
                return View(model);
            }
        }

    }
}
