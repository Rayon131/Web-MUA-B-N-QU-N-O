using System.ComponentModel.DataAnnotations;

namespace DATNN.ViewModel
{
    public class CreateNhanVienViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn vai trò.")]
        public int MaQuyen { get; set; }

        [Required(ErrorMessage = "Họ và tên không được để trống.")]
        public string HoTen { get; set; }

        [Required(ErrorMessage = "Tên đăng nhập không được để trống.")]
        public string TenDangNhap { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được để trống.")]
        [DataType(DataType.Password)]
        public string MatKhau { get; set; }

        [Required(ErrorMessage = "Số điện thoại không được để trống.")]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại không hợp lệ.")]
        public string SoDienThoai { get; set; }

        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress]
        public string Email { get; set; }

		[Required(ErrorMessage = "Vui lòng nhập ngày sinh.")]
		[DataType(DataType.Date)]
		public DateTime? NgaySinh { get; set; }
        public string GioiTinh { get; set; }

	}
}
