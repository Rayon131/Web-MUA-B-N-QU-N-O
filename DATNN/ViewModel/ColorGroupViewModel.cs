namespace DATNN.ViewModel
{
    public class ColorGroupViewModel
    {
        public string ColorName { get; set; }
        public string ColorCode { get; set; } // Mã màu hex (vd: #FFFFFF)
        public List<SizeViewModel> Sizes { get; set; }
    }
}

