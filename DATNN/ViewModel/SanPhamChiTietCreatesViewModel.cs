namespace DATNN.ViewModel
{
    public class SanPhamChiTietCreatesViewModel
    {
        public int MaSanPham { get; set; }
        public string? TenSanPham { get; set; }

        public List<SanPhamChiTietItem> Items { get; set; } = new();
    }

}
