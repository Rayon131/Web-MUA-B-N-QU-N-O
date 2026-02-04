using System.ComponentModel.DataAnnotations;

namespace DATNN.Models
{
    public class Quyen
    {
        [Key]
        public int Id { get; set; }

        public string MaVaiTro { get; set; }

        public string Ten { get; set; }

        public int TrangThai { get; set; }

        // Navigation property
        public virtual ICollection<NguoiDung> NguoiDungs { get; set; }
    }
}
