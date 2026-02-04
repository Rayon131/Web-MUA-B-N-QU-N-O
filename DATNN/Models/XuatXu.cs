using System.ComponentModel.DataAnnotations;

namespace DATNN.Models
{
    public class XuatXu
    {
        [Key]
        public int MaXuatXu { get; set; }

        public string TenXuatXu { get; set; }

        public string MoTa { get; set; }

        public int TrangThai { get; set; }

        // Navigation property
        public virtual ICollection<SanPham> SanPhams { get; set; }
    }
}
