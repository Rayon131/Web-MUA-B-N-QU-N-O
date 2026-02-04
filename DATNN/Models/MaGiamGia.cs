using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;

namespace DATNN.Models
{
    public class MaGiamGia
    {
        [Key]
        public int MaGiamGiaID { get; set; } // Thay thế MaKhuyenMai

        [Required(ErrorMessage = "Vui lòng nhập Mã Giảm Giá.")]
        [StringLength(50)]
        public string MaCode { get; set; } // <<< TRƯỜNG CỐT LÕI (Mã Voucher)

        [StringLength(255)]
        public string? TenChuongTrinh { get; set; } // Tên nội bộ cho dễ quản lý. VD: "Voucher Khai Trương"

        // "SanPham" hoặc "DonHang" (Voucher thường là DonHang, nhưng vẫn giữ tùy chọn)
        [Required]
        public string LoaiApDung { get; set; }

        [Required]
        [Display(Name = "Kênh áp dụng")]
        public string KenhApDung { get; set; }

        // Số lượt sử dụng tối đa trên toàn bộ hệ thống (Thay thế SoLuong của KhuyenMai)
        public int? TongLuotSuDungToiDa { get; set; }
        public int DaSuDung { get; set; } = 0; // Số lượt đã được sử dụng

        [Required]
        public string LoaiGiamGia { get; set; } // "PhanTram" hoặc "SoTien"

        [Required]
        public decimal GiaTriGiamGia { get; set; } // Giá trị giảm (VD: 10 cho 10%, 50000 cho 50.000đ)

        // Điều kiện áp dụng
        public decimal? DieuKienDonHangToiThieu { get; set; }
      

        // Dành cho khuyến mãi SẢN PHẨM (Ít gặp, nhưng vẫn giữ lại)
        public string? DanhSachSanPhamApDung { get; set; }

        public string? GhiChu { get; set; }

        public int TrangThai { get; set; } // 1: Đang hoạt động, 0: Vô hiệu hóa

        [Required(ErrorMessage = "Vui lòng chọn Ngày bắt đầu.")]
        public DateTime NgayBatDau { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Ngày kết thúc.")]
        public DateTime NgayKetThuc { get; set; }
        [BindNever]
        public virtual ICollection<DonHang> DonHangs { get; set; } = new List<DonHang>();
    }
}
