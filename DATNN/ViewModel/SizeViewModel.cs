namespace DATNN.ViewModel
{
    public class SizeViewModel
    {
        public int ProductDetailId { get; set; } // MaSanPhamChiTiet
        public string SizeName { get; set; }
        public int Stock { get; set; } // Số lượng tồn kho
        public decimal Price { get; set; }
        public decimal OriginalPrice { get; set; }
    }
}
