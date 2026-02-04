using System.ComponentModel.DataAnnotations;

namespace DATNN.Models
{
    public class KhuyenMai
    {
        public KhuyenMai()
        {
            DonHangs = new HashSet<DonHang>();
        }
        [Key]
        public int MaKhuyenMai { get; set; }

        [Display(Name = "Tên khuyến mãi")]
        [Required(ErrorMessage = "Tên khuyến mãi không được để trống.")]
        [StringLength(100, ErrorMessage = "Tên khuyến mãi không được vượt quá 100 ký tự.")]
        public string TenKhuyenMai { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn kênh áp dụng.")]
        [Display(Name = "Kênh áp dụng")]
        public string KenhApDung { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn loại giảm giá.")]
        [Display(Name = "Loại giảm giá")]
        public string LoaiGiamGia { get; set; }

        [Display(Name = "Giá trị giảm giá")]
        [Required(ErrorMessage = "Giá trị giảm giá không được để trống.")]
        [Range(1, double.MaxValue, ErrorMessage = "Giá trị giảm giá phải lớn hơn 0.")]
        public decimal GiaTriGiamGia { get; set; }

        public string? DanhSachSanPhamApDung { get; set; }

        [Display(Name = "Ghi chú")]
        [StringLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự.")]
        public string? GhiChu { get; set; }

        public int TrangThai { get; set; }

        [Display(Name = "Ngày bắt đầu")]
        [Required(ErrorMessage = "Ngày bắt đầu không được để trống.")]
        public DateTime NgayBatDau { get; set; }

        [Display(Name = "Ngày kết thúc")]
        [Required(ErrorMessage = "Ngày kết thúc không được để trống.")]
        public DateTime NgayKetThuc { get; set; }

        public virtual ICollection<DonHang> DonHangs { get; set; }
    }
}
