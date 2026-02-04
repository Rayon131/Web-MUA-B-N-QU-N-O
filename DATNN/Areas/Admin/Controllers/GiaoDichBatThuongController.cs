using DATNN.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DATNN.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin")]
    public class GiaoDichBatThuongController : Controller
    {
        private readonly MyDbContext _context;
        public GiaoDichBatThuongController(MyDbContext context) => _context = context;
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate, int? statusFilter)
        {
            // 1. Xử lý ngày lọc (Mặc định lấy trong ngày hôm nay)
            var start = startDate ?? DateTime.Today;
            var end = endDate ?? DateTime.Today.AddDays(1);
            int currentStatus = statusFilter ?? 1;
            // 2. Truy vấn danh sách khách hàng
            var query = _context.NguoiDungs
                .Include(n => n.Quyen)
                .Where(n => n.Quyen.MaVaiTro == "KHACHHANG");

            // 3. Thống kê dữ liệu
            var dataQuery = query.Select(kh => new AbnormalTransactionViewModel
            {
                MaKhachHang = kh.MaNguoiDung,
                TenKhachHang = kh.HoTen ?? kh.TenDangNhap,
                TrangThaiHienTai = kh.TrangThai,
                ResetTokenLuuTru = kh.ResetToken,

                SoDonDaDat = kh.DonHangsKhachHang.Count(d =>
                    d.ThoiGianTao >= start && d.ThoiGianTao < end &&
                    d.PhuongThucThanhToan == "COD" &&
                    d.TrangThaiDonHang != 5),

                SoDonDaHuy = kh.DonHangsKhachHang.Count(d =>
                    d.ThoiGianTao >= start && d.ThoiGianTao < end &&
                    d.PhuongThucThanhToan == "COD" &&
                    d.TrangThaiDonHang == 6)
            });
            if (currentStatus == 1) // Hoạt động (Không bị cấm)
            {
                dataQuery = dataQuery.Where(x => x.TrangThaiHienTai != 2);
            }
            else if (currentStatus == 2) // Bị cấm
            {
                dataQuery = dataQuery.Where(x => x.TrangThaiHienTai == 2);
            }

            // Luôn ẩn khách hàng không có hoạt động gì và không bị cấm để tránh rác danh sách
            var data = await dataQuery
                .Where(x => x.SoDonDaDat > 0 || x.SoDonDaHuy > 0 || x.TrangThaiHienTai == 2)
                .ToListAsync();

            ViewBag.StartDate = start.ToString("yyyy-MM-dd");
            ViewBag.EndDate = end.ToString("yyyy-MM-dd");
            ViewBag.StatusFilter = currentStatus;

            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CamKhachHang(int id, string lyDo, string lyDoChiTiet, int? soNgayCam, bool isPermanent)
        {
            var kh = await _context.NguoiDungs.FindAsync(id);
            if (kh == null) return NotFound();

            kh.TrangThai = 2; // Trạng thái cấm COD
            string finalReason = !string.IsNullOrWhiteSpace(lyDoChiTiet) ? lyDoChiTiet : lyDo;

            DateTime expiryDate;
            if (isPermanent)
            {
                expiryDate = new DateTime(9999, 12, 31);
            }
            else
            {
                expiryDate = DateTime.Now.AddDays(soNgayCam ?? 30);
            }

            kh.ResetToken = $"{finalReason}|{expiryDate:yyyy-MM-dd}";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = isPermanent ? "Đã cấm vĩnh viễn." : $"Đã cấm đến {expiryDate:dd/MM/yyyy}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HuyCam(int id)
        {
            var kh = await _context.NguoiDungs.FindAsync(id);
            if (kh == null) return NotFound();

            // Khôi phục về trạng thái 1 (Hoạt động bình thường)
            kh.TrangThai = 1;

            // Xóa toàn bộ chuỗi Lý do|Ngày lưu trong ResetToken
            kh.ResetToken = null;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã gỡ bỏ hạn chế giao dịch cho khách hàng.";

            return RedirectToAction(nameof(Index));
        }
        [HttpGet]
        public async Task<IActionResult> GetOrderDetails(int userId, DateTime startDate, DateTime endDate)
        {
            // Đảm bảo lấy đến hết ngày kết thúc (23:59:59)
            var end = endDate.AddDays(1);

            var orders = await _context.DonHangs
                .Include(d => d.KhachHang)
                .Include(d => d.DonHangChiTiets)
                .Where(d => d.MaKhachHang == userId &&
                            d.PhuongThucThanhToan == "COD" &&
                            d.ThoiGianTao >= startDate &&
                            d.ThoiGianTao < end &&
                            d.TrangThaiDonHang != 5) // Theo logic bạn muốn: không tính đơn hoàn thành
                .Select(d => new
                {
                    maDonHang = d.MaDonHang,
                    tenKhachHang = d.HoTenNguoiNhan ?? d.KhachHang.HoTen,
                    tongTien = d.TongTien,
                    trangThai = d.TrangThaiDonHang,
                    ngayTao = d.ThoiGianTao.ToString("dd/MM/yyyy HH:mm"),
                    // Lấy thông tin sản phẩm (Tên, màu, size, số lượng)
                    sanPhams = d.DonHangChiTiets.Select(ct =>
                        $"{ct.TenSanPham_Luu} ({ct.TenMau_Luu}/{ct.TenSize_Luu}) x{ct.SoLuong}"
                    ).ToList()
                })
                .OrderByDescending(d => d.maDonHang)
                .ToListAsync();

            return Json(orders);
        }
    }
}
