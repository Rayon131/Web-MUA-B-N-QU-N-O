using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DATNN.Models
{
    public class YeuCauDoiTra
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int MaDonHangChiTiet { get; set; }
        [ForeignKey("MaDonHangChiTiet")]
        public virtual DonHangChiTiet DonHangChiTiet { get; set; }

        [Required]
        public int MaNguoiDung { get; set; }
        [ForeignKey("MaNguoiDung")]
        public virtual NguoiDung NguoiDung { get; set; }

        [Required]
        public LoaiYeuCau LoaiYeuCau { get; set; } // Enum: Đổi hàng hoặc Trả hàng

        [Required]
        [StringLength(500)]
        public string LyDo { get; set; }

        public string? GhiChuKhachHang { get; set; }
        public string? HinhAnhBangChung { get; set; } // Lưu đường dẫn ảnh
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0.")]
        public int SoLuongYeuCau { get; set; }
        [Required]
        public TrangThaiYeuCauDoiTra TrangThai { get; set; } // Enum: Chờ xác nhận, Đã duyệt,...

        public string? GhiChuAdmin { get; set; }
        public DateTime NgayTao { get; set; }
        public DateTime? NgayCapNhat { get; set; }
        [StringLength(100)]
        public string? TenNganHang { get; set; }

        [StringLength(100)]
        public string? TenChuTaiKhoan { get; set; }

        [StringLength(20)]
        public string? SoTaiKhoan { get; set; }

        [StringLength(200)]
        public string? ChiNhanh { get; set; }
        // Dùng khi khách muốn đổi sang sản phẩm khác
        public int? MaSanPhamChiTietMoi { get; set; }
        [StringLength(200)]
        public string? TenSanPhamMoi_Luu { get; set; } // Tên sản phẩm đổi
        [StringLength(50)]
        public string? TenMauMoi_Luu { get; set; }     // Màu sản phẩm đổi
        [StringLength(50)]
        public string? TenSizeMoi_Luu { get; set; }    // Size sản phẩm đổi
        public string? HinhAnhMoi_Luu { get; set; }    // Ảnh sản phẩm đổi

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? GiaSanPhamMoi_Luu { get; set; } // Giá tại thời điểm đổi
        public BenChiuPhi BenChiuPhiShip { get; set; } = BenChiuPhi.KhachHang; // Mặc định là khách chịu

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? ChiPhiShip { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? TienChenhLech { get; set; }
        public HinhThucHoanTien? HinhThucHoanTien { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? TongTienThanhToan { get; set; }
    }
    public enum LoaiYeuCau
    {
        [Display(Name = "Đổi hàng")]
        DoiHang,
        [Display(Name = "Trả hàng / Hoàn tiền")]
        TraHang
    }
    public enum BenChiuPhi
    {
        [Display(Name = "Cửa hàng")]
        CuaHang,
        [Display(Name = "Khách hàng")]
        KhachHang
    }
    // Thêm Enum này
    public enum HinhThucHoanTien
    {
        [Display(Name = "Chưa xác định")]
        ChuaXacDinh,
        [Display(Name = "Tiền mặt")] // Dùng cho đơn COD
        TienMat,
        [Display(Name = "Chuyển khoản")] // Dùng cho đơn COD
        ChuyenKhoan,
        [Display(Name = "Hoàn qua cổng VNPAY")] // Dùng cho đơn VNPAY
        VNPAY,
        [Display(Name = "Hoàn qua TK Ngân hàng khác")] // Dùng cho đơn VNPAY
        NganHangKhac
    }
    public enum TrangThaiYeuCauDoiTra
    {
        [Display(Name = "Chờ xác nhận")]
        ChoXacNhan,
        [Display(Name = "Đã từ chối")]
        DaTuChoi,
        [Display(Name = "Đã duyệt - Chờ nhận hàng")]
        DaDuyet,
        [Display(Name = "Đã nhận hàng - Đang xử lý")]
        DaNhanHang,
        [Display(Name = "Đang giao hàng đổi")]
        DangGiaoHangDoi,
        [Display(Name = "Hoàn thành")]
        HoanThanh
    }
}
