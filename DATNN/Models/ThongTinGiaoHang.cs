using System.ComponentModel.DataAnnotations;

namespace DATNN.Models
{
    public class ThongTinGiaoHang
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "Bán kính giao hàng (km)")]
        public double BanKinhGiaoHang { get; set; }
        [Display(Name = "Bán kính giao hàng miễn phí (km)")]
        public double BanKinhfree { get; set; }

        [Display(Name = "Phí giao hàng (VNĐ)")]
        public decimal PhiGiaoHang { get; set; }

        [Display(Name = "Đơn hàng tối thiểu (VNĐ)")]
        public decimal DonHangToiThieu { get; set; }

       
    }

}
