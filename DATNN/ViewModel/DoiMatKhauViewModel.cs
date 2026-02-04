using System.ComponentModel.DataAnnotations;

namespace DATNN.ViewModel
{
    public class DoiMatKhauViewModel
    {
        public string? TenDangNhap { get; set; }
        public string? Email { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại.")]
        [DataType(DataType.Password)]
        public string MatKhauCu { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
        [StringLength(15, MinimumLength = 8, ErrorMessage = "Mật khẩu mới phải từ 8 đến 15 ký tự.")]
        [RegularExpression("^(?=.*[a-z])(?=.*[A-Z])[a-zA-Z0-9]{8,15}$", ErrorMessage = "Mật khẩu mới phải có chữ hoa, chữ thường và không chứa ký tự đặc biệt.")]
        [DataType(DataType.Password)]
        public string MatKhauMoi { get; set; }

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới.")]
        [DataType(DataType.Password)]
        [Compare("MatKhauMoi", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        public string XacNhanMatKhauMoi { get; set; }
    }
}
