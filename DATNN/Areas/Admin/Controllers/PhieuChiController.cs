using DATNN.Models;
using DATNN.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DATNN.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin")]
    public class PhieuChiController : Controller
    {
        private readonly MyDbContext _context;

        public PhieuChiController(MyDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var listPhieuChi = await _context.PhieuChis
                .Include(p => p.NguoiDung)       // Người lập phiếu
                .Include(p => p.DonHang)         // Đơn hàng liên quan
                .Include(p => p.YeuCauDoiTra)    // Yêu cầu đổi trả liên quan
                .OrderByDescending(p => p.NgayTao)
                .ToListAsync();

            return View(listPhieuChi);
        }

        // 2. GET: MỞ FORM TẠO MỚI
        public IActionResult Create()
        {
            var model = new PhieuChiViewModel();
            LoadDropdownData(model);
            return View(model);
        }

        // 3. POST: XỬ LÝ LƯU
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PhieuChiViewModel model)
        {
            if (ModelState.IsValid)
            {
                var phieuChi = new PhieuChi
                {
                    SoTien = model.SoTien,
                    NoiDung = model.NoiDung,
                    GhiChu = model.GhiChu,
                    LoaiChiPhi = model.LoaiChiPhi,
                    NgayTao = DateTime.Now,
                    TrangThai = true,
                    // Lưu ý: ID người dùng này là người đang đăng nhập hệ thống (Nhân viên/Admin)
                    // Bạn cần thay số 1 bằng code lấy User ID thật: int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier))
                    MaNguoiDung = 1
                };

                // Xử lý logic liên kết
                switch (model.LoaiChiPhi)
                {
                    case LoaiPhieuChi.ChiPhiChung:
                        phieuChi.MaDonHang = null;
                        phieuChi.MaYeuCauDoiTra = null;
                        break;

                    case LoaiPhieuChi.LienQuanDonHang:
                        phieuChi.MaDonHang = model.MaDonHang;
                        phieuChi.MaYeuCauDoiTra = null;
                        break;

                    case LoaiPhieuChi.LienQuanDoiTra:
                        phieuChi.MaYeuCauDoiTra = model.MaYeuCauDoiTra;
                        phieuChi.MaDonHang = null;
                        break;
                }

                _context.PhieuChis.Add(phieuChi);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Nếu lỗi validate, load lại dropdown
            LoadDropdownData(model);
            return View(model);
        }

        // --- HÀM LOAD DỮ LIỆU (ĐÃ SỬA KHỚP VỚI MODEL CỦA BẠN) ---
        private void LoadDropdownData(PhieuChiViewModel model)
        {
            // 1. Lấy danh sách Đơn Hàng
            // Sửa: Id -> MaDonHang
            // Sửa: NgayDat -> ThoiGianTao
            // Sửa: NguoiDung.HoTen -> HoTenNguoiNhan (Tên người nhận hàng)
            var listDonHang = _context.DonHangs
                .OrderByDescending(d => d.ThoiGianTao)
                .Select(d => new {
                    Id = d.MaDonHang, // Khóa chính là MaDonHang
                    Text = $"Đơn #{d.MaDonHang} - {d.HoTenNguoiNhan} ({d.TongTien:N0}đ)"
                }).ToList();

            // 2. Lấy danh sách Yêu Cầu Đổi Trả
            // Sửa: NgayYeuCau -> NgayTao
            // Sửa: LyDoDoiTra -> LyDo
            var listDoiTra = _context.YeuCauDoiTras
                .OrderByDescending(y => y.NgayTao)
                .Select(y => new {
                    Id = y.Id, // Khóa chính là Id
                    Text = $"YC #{y.Id} - {y.LyDo}"
                }).ToList();

            model.DSDonHang = new SelectList(listDonHang, "Id", "Text", model.MaDonHang);
            model.DSYeuCauDoiTra = new SelectList(listDoiTra, "Id", "Text", model.MaYeuCauDoiTra);
        }
    }
}
