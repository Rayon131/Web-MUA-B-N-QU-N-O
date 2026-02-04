using System.ComponentModel.DataAnnotations;

namespace DATNN.Models
{
    public class DanhMuc
    {
        [Key]
        public int MaDanhMuc { get; set; }

        public string TenDanhMuc { get; set; }

        public string MoTa { get; set; }

        public int TrangThai { get; set; }

        // Navigation property
        public virtual ICollection<SanPham> SanPhams { get; set; }
    }
}
