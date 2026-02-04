using System.ComponentModel.DataAnnotations;

namespace DATNN.Models
{
    public class MauSac
    {
        [Key]
        public int MaMauSac { get; set; }

        public string TenMau { get; set; }

        public string MoTa { get; set; }

        public int TrangThai { get; set; }

        // Navigation property
        public virtual ICollection<SanPhamChiTiet> SanPhamChiTiets { get; set; }
    }
}
