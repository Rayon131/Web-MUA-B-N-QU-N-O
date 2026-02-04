using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace DATNN.Models
{// Thêm Attribute này ngay trên class để định nghĩa Index
    [Index(nameof(TenDangNhap), IsUnique = true, Name = "IX_NguoiDung_TenDangNhap")]
    [Index(nameof(Email), IsUnique = true, Name = "IX_NguoiDung_Email")]
    public class NguoiDung
    {
        [Key]
        public int MaNguoiDung { get; set; }

        public int MaQuyen { get; set; }

        [StringLength(100)]
        [Display(Name = "Họ và Tên")]
        public string? HoTen { get; set; }
        [Required(ErrorMessage = "Tên đăng nhập không được để trống.")]
        [StringLength(50)]
        [Display(Name = "Tên đăng nhập")]
        public string TenDangNhap { get; set; }
        [Required(ErrorMessage = "Mật khẩu không được để trống.")]
        //[StringLength(15, MinimumLength = 8, ErrorMessage = "Mật khẩu phải từ 8 đến 15 ký tự.")]
        //[RegularExpression("^(?=.*[a-z])(?=.*[A-Z])[a-zA-Z0-9]{8,15}$", ErrorMessage = "Mật khẩu phải có chữ hoa, chữ thường và không chứa ký tự đặc biệt.")]
        [Display(Name = "Mật khẩu")]
        public string MatKhau { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại không hợp lệ. Phải có 10 chữ số và bắt đầu bằng 0.")]
        [Display(Name = "Số điện thoại")]
        public string? SoDienThoai { get; set; }
        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ.")]
        [StringLength(100)]
        public string Email { get; set; }

        public int TrangThai { get; set; }

        public DateTime? NgaySinh { get; set; }

        public DateTime? NgayTao { get; set; }
        public string? ResetToken { get; set; }

        public DateTime? TokenExpiry { get; set; }

        public string? GioiTinh { get; set; }

        // Navigation properties
        [ForeignKey("MaQuyen")]
        public virtual Quyen Quyen { get; set; }

        public virtual ICollection<DiaChiNguoiDung> DiaChiNguoiDungs { get; set; }
        [InverseProperty("NguoiDung")] // tương ứng với DonHang.NguoiDung (nhân viên)
        public virtual ICollection<DonHang> DonHangsNhanVien { get; set; }

        [InverseProperty("KhachHang")] // tương ứng với DonHang.KhachHang (khách mua)
        public virtual ICollection<DonHang> DonHangsKhachHang { get; set; }
        public virtual GioHang GioHang { get; set; }
    }
}
