using System.ComponentModel.DataAnnotations;

namespace DATNN.Models
{
    public class ThongTinCuaHang
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Giờ mở cửa không được để trống.")]
        [Display(Name = "Giờ mở cửa")]
        public TimeOnly MoCua { get; set; }

        [Required(ErrorMessage = "Giờ đóng cửa không được để trống.")]
        [Display(Name = "Giờ đóng cửa")]
        public TimeOnly DongCua { get; set; }

        [Required(ErrorMessage = "Tên cửa hàng không được để trống.")]
        [StringLength(100, ErrorMessage = "Tên cửa hàng tối đa 100 ký tự.")]
        [Display(Name = "Tên cửa hàng")]
        public string TenCuaHang { get; set; }

        [Required(ErrorMessage = "Số điện thoại không được để trống.")]
        [RegularExpression(@"^(032|033|034|035|036|037|038|039|096|097|098|086|083|084|085|081|082|088|091|094|070|079|077|076|078|090|093|089|056|058|092|059|099)\d{7}$",
    ErrorMessage = "Số điện thoại không phải là số di động Việt Nam hợp lệ.")]
        [Display(Name = "Số điện thoại")]
        public string SoDienThoai { get; set; }

        [Required(ErrorMessage = "Gmail không được để trống.")]
        [EmailAddress(ErrorMessage = "Địa chỉ Gmail không hợp lệ.")]
        [StringLength(100, ErrorMessage = "Gmail tối đa 100 ký tự.")]
        [Display(Name = "Gmail")]
        public string Gmail { get; set; }
    }


}
