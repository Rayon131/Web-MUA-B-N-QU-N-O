using System.ComponentModel.DataAnnotations;

namespace DATNN.ViewModel
{
    public class EditKhachHangViewModel
    {
        // Luôn cần ID để biết đang sửa đối tượng nào
        public int MaNguoiDung { get; set; }

        [Required(ErrorMessage = "Họ và tên không được để trống.")]
        [Display(Name = "Họ và Tên")]
        public string HoTen { get; set; }

        [Required(ErrorMessage = "Số điện thoại không được để trống.")]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại không hợp lệ.")]
        [Display(Name = "Số điện thoại")]
        public string SoDienThoai { get; set; }

        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress]
        public string Email { get; set; }

        [Display(Name = "Trạng thái")]
        public int TrangThai { get; set; } // Ví dụ: 1 = Hoạt động, 0 = Bị khóa

        [DataType(DataType.Date)]
        [Display(Name = "Ngày sinh")]
        public DateTime? NgaySinh { get; set; }

        [Display(Name = "Giới tính")]
        public string GioiTinh { get; set; }
        // === THÊM CÁC THUỘC TÍNH NÀY VÀO ===
        [Display(Name = "Tên đăng nhập")]
        public string TenDangNhap { get; set; }

        [Display(Name = "Ngày tạo")]
        [DataType(DataType.Date)]
        public DateTime? NgayTao { get; set; }
    }
}
