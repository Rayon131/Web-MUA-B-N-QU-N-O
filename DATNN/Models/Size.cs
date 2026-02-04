using System.ComponentModel.DataAnnotations;

namespace DATNN.Models
{
    public class Size
    {
        [Key]
        public int MaSize { get; set; }

        public string TenSize { get; set; }

        public string MoTa { get; set; }

        public int TrangThai { get; set; }

        // Navigation property
        public virtual ICollection<SanPhamChiTiet> SanPhamChiTiets { get; set; }
    }
}
