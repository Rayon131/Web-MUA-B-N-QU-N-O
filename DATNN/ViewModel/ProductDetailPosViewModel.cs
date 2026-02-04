namespace DATNN.ViewModel
{
    public class ProductDetailPosViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        // Nhóm các chi tiết theo màu sắc
        public List<ColorGroupViewModel> Colors { get; set; }
    }
}
