namespace DATNN.ViewModel
{
    public class ProductPosViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ImageUrl { get; set; }
        public decimal Price { get; set; } // Giá bán cơ bản để hiển thị
        public bool HasPromotion { get; set; }
        public int TotalStock { get; set; }
    }
}
