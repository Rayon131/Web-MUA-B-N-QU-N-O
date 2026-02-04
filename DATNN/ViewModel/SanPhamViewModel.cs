using DATNN.Models;

namespace DATNN.ViewModel
{
    public class SanPhamViewModel
    {
        public SanPham SanPham { get; set; }
        // Dictionary để lưu giá đã giảm, với key là MaSanPhamChiTiet và value là giá đã giảm
        public Dictionary<int, decimal> GiaDaGiam { get; set; }
        public bool HasPromotion { get; set; }
        public SanPhamViewModel()
        {
            GiaDaGiam = new Dictionary<int, decimal>();
        }
    }
}
