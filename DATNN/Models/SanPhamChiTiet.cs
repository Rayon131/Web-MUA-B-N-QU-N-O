using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DATNN.Models
{
    public class SanPhamChiTiet
    {
        [Key]
        public int MaSanPhamChiTiet { get; set; }

        public int MaSanPham { get; set; }

        public int MaMauSac { get; set; }

        public int MaSize { get; set; }

        public decimal GiaBan { get; set; }

        //public decimal? GiaSauKhiGiam { get; set; }

        public int SoLuong { get; set; }

        public string? GhiChu { get; set; }

        //public DateTime? ThoiGianTao { get; set; }

        //public string NguoiTao { get; set; }

        //public DateTime? ThoiGianSua { get; set; }

        //public string NguoiSua { get; set; }

        public string? HinhAnh { get; set; }

        public int TrangThai { get; set; }

        // Navigation properties
        [ForeignKey("MaSanPham")]
        public virtual SanPham SanPham { get; set; }

        [ForeignKey("MaMauSac")]
        public virtual MauSac MauSac { get; set; }

        [ForeignKey("MaSize")]
        public virtual Size Size { get; set; }

        public virtual ICollection<DonHangChiTiet> DonHangChiTiets { get; set; }
        public virtual ICollection<ChiTietGioHang> ChiTietGioHangs { get; set; }
    }
}
