using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DATNN.Models
{
    public class DonHang
    {
        [Key]
        public int MaDonHang { get; set; }

        public int? MaKhuyenMai { get; set; }

        public int? MaNguoiDung { get; set; }
        public int? MaKhachHang { get; set; }
        public string? HoTenNguoiNhan { get; set; }
        public decimal TongTien { get; set; }

        public string? SoDienThoai { get; set; }

        public string? Email { get; set; }

        public string? DiaChi { get; set; }

        public decimal? PhiVanChuyen { get; set; }

        public string? GhiChu { get; set; }

        public int TrangThaiThanhToan { get; set; }

        public DateTime ThoiGianTao { get; set; }
        public string? LichSuTrangThai { get; set; } // lưu dưới dạng JSON hoặc văn bản

        public string? LyDoHuy { get; set; }

        public int TrangThaiDonHang { get; set; }

        public decimal? SoTienDuocGiam { get; set; }

        public string? PhuongThucThanhToan { get; set; }

        public decimal? TienMatDaNhan { get; set; }
        public string? VnpTxnRef { get; set; } // Mã giao dịch của VNPAY
        public string? VnpTransactionNo { get; set; } // Mã giao dịch trong hệ thống VNPAY
        public DateTime? VnpPayDate { get; set; } // Thời gian thanh toán thành công
        public int? MaGiamGiaID { get; set; }

        [ForeignKey("MaGiamGiaID")]
        public virtual MaGiamGia MaGiamGia { get; set; } // Navigation Property
        // Navigation properties
        [ForeignKey("MaKhuyenMai")]
        public virtual KhuyenMai KhuyenMai { get; set; }

        [ForeignKey("MaNguoiDung")]
        [InverseProperty("DonHangsNhanVien")] // 👈 đặt tên cho collection bên kia
        public virtual NguoiDung NguoiDung { get; set; } // nhân viên

        [ForeignKey("MaKhachHang")]
        [InverseProperty("DonHangsKhachHang")] // 👈 đặt tên cho collection bên kia
        public virtual NguoiDung KhachHang { get; set; } // khách hàng mua
        public virtual ICollection<DonHangChiTiet> DonHangChiTiets { get; set; }
    }
}
