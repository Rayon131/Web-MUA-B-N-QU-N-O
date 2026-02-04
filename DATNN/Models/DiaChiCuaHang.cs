namespace DATNN.Models
{
    public class DiaChiCuaHang
    {
        public int Id { get; set; }
        public string Phuong { get; set; }

        public string Quan { get; set; }

        public string ThanhPho { get; set; }

        public string ChiTietDiaChi { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}
