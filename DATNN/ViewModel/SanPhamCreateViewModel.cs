using DATNN.Models;
using System.ComponentModel.DataAnnotations;

namespace DATNN.ViewModel
{
    public class SanPhamCreateViewModel
    {
        [Required(ErrorMessage = "Tên sản phẩm không được để trống.")]
        [Display(Name = "Tên sản phẩm")]
        public string TenSanPham { get; set; }

        // SỬA Ở ĐÂY: Thêm '?' và đổi sang [Required]
        [Required(ErrorMessage = "Vui lòng chọn danh mục.")]
        [Display(Name = "Danh mục")]
        public int? MaDanhMuc { get; set; }

        // SỬA Ở ĐÂY: Thêm '?' và đổi sang [Required]
        [Required(ErrorMessage = "Vui lòng chọn chất liệu.")]
        [Display(Name = "Chất liệu")]
        public int? MaChatLieu { get; set; }

        // SỬA Ở ĐÂY: Thêm '?' và đổi sang [Required]
        [Required(ErrorMessage = "Vui lòng chọn xuất xứ.")]
        [Display(Name = "Xuất xứ")]
        public int? MaXuatXu { get; set; }

        public string? MoTa { get; set; }

        public int TrangThai { get; set; }

        // === SỬA Ở ĐÂY ===
        // Sử dụng ViewModel mới và không yêu cầu phải có ít nhất 1 phiên bản
        public List<SanPhamChiTietCreateViewModel> SanPhamChiTiets { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ảnh đại diện sản phẩm.")]
        public IFormFile? file { get; set; }
        public SanPhamCreateViewModel()
        {
            // Khởi tạo danh sách mới
            SanPhamChiTiets = new List<SanPhamChiTietCreateViewModel>();
            TrangThai = 1;
        }
    }
}
