namespace DATNN.ViewModel
{
    public class ProductGroupViewModel
    {
        public string ProductName { get; set; }
        public List<VariantViewModel> Variants { get; set; } = new List<VariantViewModel>();
    }

    public class VariantViewModel
    {
        public int Id { get; set; }
        public string DisplayText { get; set; } // E.g., "Màu: Đỏ, Size: S (Tồn: 50)"
        public string FullTextForSearch { get; set; } // E.g., "Áo thun - Màu: Đỏ, Size: S (Tồn: 50)"
        public decimal Price { get; set; }
    }
}
