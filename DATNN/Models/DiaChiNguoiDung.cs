using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DATNN.Models
{
    public class DiaChiNguoiDung
    {
        [Key]
        public int Id { get; set; }

        public int MaNguoiDung { get; set; }

        public string Ten { get; set; }

        public string SoDienThoai { get; set; }

        public string Phuong { get; set; }

        public string Quan { get; set; }

        public string ThanhPho { get; set; }

        public string ChiTietDiaChi { get; set; }

        public int TrangThai { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        // Navigation property
        [ForeignKey("MaNguoiDung")]
        public virtual NguoiDung NguoiDung { get; set; }
    }
}
