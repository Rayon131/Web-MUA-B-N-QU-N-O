using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DATNN.Models
{
    public class SanPham
    {
        [Key]
        public int MaSanPham { get; set; }

        public int MaDanhMuc { get; set; }

        public int MaChatLieu { get; set; }

        public int MaXuatXu { get; set; }
        [Required(ErrorMessage = "Tên sản phẩm không được để trống.")]
        public string TenSanPham { get; set; }

        public string? MoTa { get; set; }

        public string? AnhSanPham { get; set; }

        public DateTime? ThoiGianTao { get; set; }

		public int TrangThai { get; set; }

        // Navigation properties
        [ForeignKey("MaDanhMuc")]
        public virtual DanhMuc DanhMuc { get; set; }

        [ForeignKey("MaChatLieu")]
        public virtual ChatLieu ChatLieu { get; set; }

        [ForeignKey("MaXuatXu")]
        public virtual XuatXu XuatXu { get; set; }

        public virtual ICollection<SanPhamChiTiet> SanPhamChiTiets { get; set; }
    }
}
