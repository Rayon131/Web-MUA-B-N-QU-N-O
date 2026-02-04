using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DATNN.Models
{
    public class ChiTietGioHang
    {
        [Key]
        public int MaChiTietGioHang { get; set; }

        public int MaGioHang { get; set; }

        public int MaSanPhamChiTiet { get; set; }

        public int SoLuong { get; set; }

        public int TrangThai { get; set; }

        // Navigation properties
        [ForeignKey("MaGioHang")]
        public virtual GioHang GioHang { get; set; }

        [ForeignKey("MaSanPhamChiTiet")]
        public virtual SanPhamChiTiet SanPhamChiTiet { get; set; }
    }
}
