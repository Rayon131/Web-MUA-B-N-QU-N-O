using System.ComponentModel.DataAnnotations;

namespace DATNN.ViewModel
{
    public class EditNhanVienViewModel
    {
        // Bắt buộc phải có ID để biết đang sửa nhân viên nào
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
        public int TrangThai { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Ngày sinh")]
        public DateTime? NgaySinh { get; set; }

        [Display(Name = "Giới tính")]
        public string GioiTinh { get; set; }
        // THÊM 2 DÒNG NÀY
        [Display(Name = "Tên đăng nhập")]
        public string TenDangNhap { get; set; } // Chỉ để hiển thị

        [Display(Name = "Ngày tạo")]
        public DateTime? NgayTao { get; set; } // Chỉ để hiển thị

		// Không bao gồm: TenDangNhap, MatKhau, MaQuyen...
		// vì chúng ta không cho phép sửa chúng trên form này.
	}
}
