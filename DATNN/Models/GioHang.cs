using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DATNN.Models
{
    public class GioHang
    {
        [Key]
        public int MaGioHang { get; set; }

        [ForeignKey("NguoiDung")]
        public int MaNguoiDung { get; set; }

        public int TrangThai { get; set; }

        // Navigation properties
        public virtual NguoiDung NguoiDung { get; set; }
        public virtual ICollection<ChiTietGioHang> ChiTietGioHangs { get; set; }
    }
}
