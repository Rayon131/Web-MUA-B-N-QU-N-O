using DATNN.Models;
using DATNN.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DATNN.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin")]
    public class ThongKeController : Controller
    {
        private readonly MyDbContext _context;

        public ThongKeController(MyDbContext context)
        {
            _context = context;
        }


        public async Task<IActionResult> Index(DateTime? tuNgay, DateTime? denNgay)
        {
            // 1. Xác định khoảng thời gian
            var start = tuNgay ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var endParam = denNgay ?? DateTime.Now;
            var end = endParam.Date.AddDays(1).AddTicks(-1);

            // =========================================================================
            // 1. TÍNH TỔNG DOANH THU BÁN HÀNG (BAO GỒM CẢ TẠI QUẦY)
            // =========================================================================
            var doanhThuBanHang = await _context.DonHangs
                 .Where(d => d.ThoiGianTao >= start && d.ThoiGianTao <= end)
                 .Where(d =>
                     // Trường hợp A: Đơn COD / Online thường -> Phải Hoàn thành (5) mới tính tiền
                     (d.TrangThaiDonHang == 5 && d.TrangThaiThanhToan == 0)

                     || // HOẶC

                     // Trường hợp B: Đơn VNPAY -> Tính ngay khi tiền về (Trạng thái TT = 1)
                     // (Bất kể đơn đang giao hay đã hủy, vì đơn hủy đã có Phiếu Chi cân bằng lại)
                     (d.PhuongThucThanhToan == "VnPay" && d.TrangThaiThanhToan == 1)

                     || // HOẶC

                     // Trường hợp C: Đơn TẠI QUẦY -> Tính ngay khi thanh toán (Trạng thái TT = 2)
                     (d.TrangThaiThanhToan == 2)
                 )
                 .SumAsync(d => d.TongTien);

            // 2. DOANH THU TỪ ĐỔI HÀNG (Tiền khách trả thêm)
            var doanhThuDoiHang = await _context.YeuCauDoiTras
                .Where(y => y.NgayCapNhat >= start && y.NgayCapNhat <= end
                            && y.TrangThai == TrangThaiYeuCauDoiTra.HoanThanh
                            && y.LoaiYeuCau == LoaiYeuCau.DoiHang
                            && y.TienChenhLech > 0)
                .SumAsync(y => y.TienChenhLech ?? 0);

            // 3. DOANH THU TỪ ĐƠN HỦY (Tiền ship giữ lại - Nếu có)
            // (Phần này quan trọng: Khi bạn hủy đơn COD mà bắt khách chịu ship, 
            // bạn nhập Phiếu chi trả ship cho bưu điện, thì phải có khoản thu này bù vào mới hòa vốn)
            var doanhThuGiuLai = await _context.DonHangs
                .Where(d => d.ThoiGianTao >= start && d.ThoiGianTao <= end
                            && d.TrangThaiDonHang == 6 // Đã hủy
                            && d.TienMatDaNhan > 0)    // Có giữ lại tiền
                .SumAsync(d => d.TienMatDaNhan ?? 0);

            // => TỔNG DOANH THU THỰC TẾ
            var tongDoanhThu = doanhThuBanHang + doanhThuDoiHang + doanhThuGiuLai;


            // =========================================================================
            // 4. TỔNG CHI PHÍ (TỪ BẢNG PHIẾU CHI)
            // =========================================================================
            var listPhieuChi = await _context.PhieuChis
                .Where(p => p.NgayTao >= start && p.NgayTao <= end && p.TrangThai == true)
                .ToListAsync();

            var chiPhiChung = listPhieuChi.Where(p => p.LoaiChiPhi == LoaiPhieuChi.ChiPhiChung).Sum(p => p.SoTien);
            var chiPhiLienQuanDon = listPhieuChi.Where(p => p.LoaiChiPhi == LoaiPhieuChi.LienQuanDonHang).Sum(p => p.SoTien);
            var chiPhiLienQuanDoiTra = listPhieuChi.Where(p => p.LoaiChiPhi == LoaiPhieuChi.LienQuanDoiTra).Sum(p => p.SoTien);

            var tongChiPhi = listPhieuChi.Sum(p => p.SoTien);


            // =========================================================================
            // 5. CÁC CHỈ SỐ KHÁC (ĐẾM SỐ LƯỢNG)
            // =========================================================================
            var soDonThanhCong = await _context.DonHangs.CountAsync(d => d.ThoiGianTao >= start && d.ThoiGianTao <= end && d.TrangThaiDonHang == 5);
            var soDonHuy = await _context.DonHangs.CountAsync(d => d.ThoiGianTao >= start && d.ThoiGianTao <= end && d.TrangThaiDonHang == 6);
            var soYeuCauDoiTra = await _context.YeuCauDoiTras.CountAsync(y => y.NgayTao >= start && y.NgayTao <= end);


            var model = new ThongKeViewModel
            {
                TuNgay = start,
                DenNgay = endParam,

                TongDoanhThu = tongDoanhThu,
                TongChiTieu = tongChiPhi,
                LoiNhuan = tongDoanhThu - tongChiPhi,

                ChiPhiVanHanh = chiPhiChung,
                ChiPhiRuiRoDonHang = chiPhiLienQuanDon,
                ChiPhiRuiRoDoiTra = chiPhiLienQuanDoiTra,

                SoDonHangThanhCong = soDonThanhCong,
                SoDonHangHuy = soDonHuy,
                SoYeuCauDoiTra = soYeuCauDoiTra
            };

            // (Optional) Debug dữ liệu
            ViewBag.DoanhThuTaiQuay = await _context.DonHangs
                .Where(d => d.ThoiGianTao >= start && d.ThoiGianTao <= end && d.TrangThaiThanhToan == 2)
                .SumAsync(d => d.TongTien);

            return View(model);
        }
    }
}
