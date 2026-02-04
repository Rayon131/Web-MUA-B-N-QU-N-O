using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DATNN.Models
{
    public class DonHangChiTiet
    {
        [Key]
        public int MaDonHangChiTiet { get; set; }

        public int MaSanPhamChiTiet { get; set; }

        public int MaDonHang { get; set; }
        public string? TenSanPham_Luu { get; set; }
        public string? TenMau_Luu { get; set; }
        public string? TenSize_Luu { get; set; }
        public string? HinhAnh_Luu { get; set; } // (Tùy chọn)
        public decimal DonGia { get; set; }

        public int SoLuong { get; set; }

        // Navigation properties
        [ForeignKey("MaDonHang")]
        public virtual DonHang DonHang { get; set; }

        [ForeignKey("MaSanPhamChiTiet")]
        public virtual SanPhamChiTiet SanPhamChiTiet { get; set; }
    }
}
