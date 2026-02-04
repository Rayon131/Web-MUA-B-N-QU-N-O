using DATNN.Models;
using System.Collections.Generic; // Thêm using này
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace DATNN.ViewModel
{
    // BƯỚC 1: Thêm ": IValidatableObject" vào sau tên class
    public class TaoYeuCauViewModel : IValidatableObject
    {
        // --- Dữ liệu để hiển thị trên View ---
        [ValidateNever]
        public string TenSanPham { get; set; }
        public string? TenMau { get; set; }
        public string? TenSize { get; set; }
        [ValidateNever]
        public int SoLuong { get; set; }
        [ValidateNever]
        public decimal GiaTri { get; set; }
        [ValidateNever]
        public string? NoiDungChinhSach { get; set; }

        // Thêm [ValidateNever] vì trường này chỉ dùng để điều khiển logic, không phải do người dùng nhập
        [ValidateNever]
        public bool IsCOD { get; set; }

        [ValidateNever]
        public int MaDonHangGoc { get; set; }
        [ValidateNever]
        public DateTime ThoiGianTaoDonHangGoc { get; set; }
        [ValidateNever]
        public decimal TongTienDonHangGoc { get; set; }
        // --- Dữ liệu để form POST về ---
        public int MaDonHangChiTiet { get; set; }

        [Display(Name = "Số lượng đổi/trả")]
        [Required(ErrorMessage = "Vui lòng nhập số lượng.")]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải ít nhất là 1.")]
        public int SoLuongYeuCau { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn loại yêu cầu.")]
        public LoaiYeuCau LoaiYeuCau { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập lý do đổi/trả.")]
        [StringLength(500)]
        public string LyDo { get; set; }

        public string? GhiChuKhachHang { get; set; }

        // --- Các trường thông tin ngân hàng ---
        [Display(Name = "Tên Ngân Hàng")]
        public string? TenNganHang { get; set; }

        [Display(Name = "Tên Chủ Tài Khoản")]
        public string? TenChuTaiKhoan { get; set; }

        [Display(Name = "Số Tài Khoản")]
        public string? SoTaiKhoan { get; set; }

        [Display(Name = "Chi Nhánh")]
        public string? ChiNhanh { get; set; }

        [Display(Name = "Sản phẩm muốn đổi")]
        public int? MaSanPhamChiTietMoi { get; set; }
        [Display(Name = "Hình thức nhận hoàn tiền")]
        public HinhThucHoanTien? HinhThucHoanTien { get; set; }

        // BƯỚC 2: Thêm toàn bộ phương thức Validate này vào cuối class
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (LoaiYeuCau == LoaiYeuCau.DoiHang && !MaSanPhamChiTietMoi.HasValue)
            {
                yield return new ValidationResult(
                       "Vui lòng chọn sản phẩm bạn muốn đổi.",
                       new[] { nameof(MaSanPhamChiTietMoi) });
            }
            // Kiểm tra điều kiện: Là đơn COD VÀ khách hàng chọn Trả hàng/Hoàn tiền
            if (IsCOD && LoaiYeuCau == LoaiYeuCau.TraHang)
            {
                // Nếu thỏa mãn điều kiện, thì các trường ngân hàng là bắt buộc
                if (string.IsNullOrWhiteSpace(TenNganHang))
                {
                    yield return new ValidationResult(
                        "Vui lòng nhập tên ngân hàng để nhận hoàn tiền.",
                        new[] { nameof(TenNganHang) });
                }

                if (string.IsNullOrWhiteSpace(TenChuTaiKhoan))
                {
                    yield return new ValidationResult(
                        "Vui lòng nhập tên chủ tài khoản.",
                        new[] { nameof(TenChuTaiKhoan) });
                }

                if (string.IsNullOrWhiteSpace(SoTaiKhoan))
                {
                    yield return new ValidationResult(
                        "Vui lòng nhập số tài khoản.",
                        new[] { nameof(SoTaiKhoan) });
                }
                if (string.IsNullOrWhiteSpace(ChiNhanh))
                {
                    yield return new ValidationResult(
                        "Vui lòng nhập chi nhánh ngân hàng.",
                        new[] { nameof(ChiNhanh) });
                }
            }
        }
    }
}