using System.ComponentModel.DataAnnotations;

namespace DATNN.ViewModel
{
    public class DangKyViewModel
    {
    
        [Required(ErrorMessage = "Tên đăng nhập không được để trống.")]
        [RegularExpression("^[a-z]{6,20}$", ErrorMessage = "Tên đăng nhập phải là chữ thường, từ 6–20 ký tự, không chứa ký tự đặc biệt.")]
        public string TenDangNhap { get; set; }

        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được để trống.")]
        [StringLength(15, MinimumLength = 8, ErrorMessage = "Mật khẩu phải từ 8 đến 15 ký tự.")]
        [RegularExpression("^(?=.*[a-z])(?=.*[A-Z])[a-zA-Z0-9]{8,15}$", ErrorMessage = "Mật khẩu phải có chữ hoa, chữ thường và không chứa ký tự đặc biệt.")]
        public string MatKhau { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare("MatKhau", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        public string XacNhanMatKhau { get; set; }
      
        public string? NoiDungChinhSach { get; set; }
    }

}
