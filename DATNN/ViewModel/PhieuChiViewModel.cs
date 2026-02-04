using DATNN.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace DATNN.ViewModel
{
    public class PhieuChiViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập số tiền")]
        [Range(1000, double.MaxValue, ErrorMessage = "Số tiền tối thiểu là 1,000đ")]
        public decimal SoTien { get; set; }

        [Display(Name = "Loại chi phí")]
        public LoaiPhieuChi LoaiChiPhi { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung chi")]
        public string NoiDung { get; set; }

        public string? GhiChu { get; set; }

        // --- Các trường hứng dữ liệu ID ---
        public int? MaDonHang { get; set; }
        public int? MaYeuCauDoiTra { get; set; }

        // --- Danh sách để đổ dữ liệu vào Combobox (Dropdown) ---
        public SelectList? DSDonHang { get; set; }
        public SelectList? DSYeuCauDoiTra { get; set; }
    }
}
