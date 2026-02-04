using DATNN.Models;
namespace DATNN.ViewModel
{
    public class HomeViewModel
    {
        public List<DanhMucViewModel> DanhMucs { get; set; }
        public List<SanPham> SanPhams { get; set; }

        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string? Keyword { get; set; }

        public int? SelectedCategoryId { get; set; }
        public int? MinPrice { get; set; }
        public int? MaxPrice { get; set; }
    }




}
