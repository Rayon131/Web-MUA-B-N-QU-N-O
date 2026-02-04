using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DATNN.Models
{
    public class PhieuChi
    {
            [Key]
            public int Id { get; set; }

            [Required]
            public DateTime NgayTao { get; set; } = DateTime.Now;

            [Column(TypeName = "decimal(18, 2)")]
            [Range(0, double.MaxValue)]
            public decimal SoTien { get; set; }

            // Sử dụng Enum mới
            public LoaiPhieuChi LoaiChiPhi { get; set; } = LoaiPhieuChi.ChiPhiChung;

            [Required(ErrorMessage = "Vui lòng nhập nội dung")]
            [StringLength(500)]
            public string NoiDung { get; set; }

            [StringLength(1000)]
            public string? GhiChu { get; set; }

            public bool TrangThai { get; set; } = true;

            // --- LIÊN KẾT ---
            public int? MaDonHang { get; set; }
            [ForeignKey("MaDonHang")]
            public virtual DonHang? DonHang { get; set; }

            public int? MaYeuCauDoiTra { get; set; }
            [ForeignKey("MaYeuCauDoiTra")]
            public virtual YeuCauDoiTra? YeuCauDoiTra { get; set; }

            public int? MaNguoiDung { get; set; }
            [ForeignKey("MaNguoiDung")]
            public virtual NguoiDung? NguoiDung { get; set; }
        
    }
    public enum LoaiPhieuChi
    {
        [Display(Name = "Chi phí chung (Điện, nước, v.v...)")]
        ChiPhiChung = 0,

        [Display(Name = "Liên quan Đơn hàng (Bom hàng, đền bù)")]
        LienQuanDonHang = 1,

        [Display(Name = "Liên quan Đổi trả (Lỗi shop, sửa hàng)")]
        LienQuanDoiTra = 2
    }
}
