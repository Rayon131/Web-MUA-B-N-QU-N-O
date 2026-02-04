using DATNN.Models;
using Microsoft.EntityFrameworkCore;

namespace DATNN
{
    public class MyDbContext : DbContext
    {
        public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
        {
        }
        public DbSet<ChatLieu> ChatLieus { get; set; }
        public DbSet<ChiTietGioHang> ChiTietGioHangs { get; set; }
        public DbSet<DanhMuc> DanhMucs { get; set; }
        public DbSet<DiaChiNguoiDung> DiaChiNguoiDungs { get; set; }
        public DbSet<DonHang> DonHangs { get; set; }
        public DbSet<DonHangChiTiet> DonHangChiTiets { get; set; }
        public DbSet<GioHang> GioHangs { get; set; }
        public DbSet<KhuyenMai> KhuyenMais { get; set; }
        public DbSet<MauSac> MauSacs { get; set; }
        public DbSet<NguoiDung> NguoiDungs { get; set; }
        public DbSet<Quyen> Quyens { get; set; }
        public DbSet<SanPham> SanPhams { get; set; }
        public DbSet<SanPhamChiTiet> SanPhamChiTiets { get; set; }
        public DbSet<Size> Sizes { get; set; }
        public DbSet<XuatXu> XuatXus { get; set; }
        public DbSet<ThongTinCuaHang> ThongTinCuaHangs { get; set; }
        public DbSet<ThongTinGiaoHang> ThongTinGiaoHangs { get; set; }
        public DbSet<DiaChiCuaHang> DiaChiCuaHangs { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<MaGiamGia> MaGiamGias { get; set; }
        public DbSet<YeuCauDoiTra> YeuCauDoiTras { get; set; }
        public DbSet<ChinhSach> ChinhSaches { get; set; }
        public DbSet<PhieuChi> PhieuChis { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed Quyens
            modelBuilder.Entity<Quyen>().HasData(
                new Quyen { Id = 1, MaVaiTro = "ADMIN", Ten = "Quản trị viên", TrangThai = 1 },
                new Quyen { Id = 2, MaVaiTro = "NHANVIEN", Ten = "Nhân viên", TrangThai = 1 },
                new Quyen { Id = 3, MaVaiTro = "KHACHHANG", Ten = "Khách hàng", TrangThai = 1 }
            );

            // Seed NguoiDungs
            modelBuilder.Entity<NguoiDung>().HasData(
                new NguoiDung
                {
                    MaNguoiDung = 1,
                    MaQuyen = 1,
                    HoTen = "Quản trị viên",
                    TenDangNhap = "admin",
                    MatKhau = "AQAAAAIAAYagAAAAENa6tfOCNlmIuKKfP5aBy3xmrAvlx93io3bqEuNCmdQgG1u+vvHR0Zc0Q3i9OeAHlg==",
                    SoDienThoai = "0900000001",
                    Email = "admin@example.com",
                    TrangThai = 1,
                    NgaySinh = new DateTime(1990, 1, 1),
                    NgayTao = new DateTime(2025, 8, 11, 15, 0, 0),
                    GioiTinh = "Nam"
                },
                new NguoiDung
                {
                    MaNguoiDung = 2,
                    MaQuyen = 2,
                    HoTen = "Nguyễn Văn Nhân",
                    TenDangNhap = "nhanvien",
                    MatKhau = "AQAAAAIAAYagAAAAENa6tfOCNlmIuKKfP5aBy3xmrAvlx93io3bqEuNCmdQgG1u+vvHR0Zc0Q3i9OeAHlg==",
                    SoDienThoai = "0900000002",
                    Email = "nhanvien@example.com",
                    TrangThai = 1,
                    NgaySinh = new DateTime(1995, 5, 15),
                    NgayTao = new DateTime(2025, 8, 11, 15, 0, 0),
                    GioiTinh = "Nam"
                },
                new NguoiDung
                {
                    MaNguoiDung = 3,
                    MaQuyen = 3,
                    HoTen = "Trần Thị Khách",
                    TenDangNhap = "khachhang",
                    MatKhau = "AQAAAAIAAYagAAAAENa6tfOCNlmIuKKfP5aBy3xmrAvlx93io3bqEuNCmdQgG1u+vvHR0Zc0Q3i9OeAHlg==",
                    SoDienThoai = "0900000003",
                    Email = "khach@example.com",
                    TrangThai = 1,
                    NgaySinh = new DateTime(2000, 10, 20),
                    NgayTao = new DateTime(2025, 8, 11, 15, 0, 0),
                    GioiTinh = "Nữ"
                }
            );

            // Seed DiaChiCuaHang
            modelBuilder.Entity<DiaChiCuaHang>().HasData(
                 new DiaChiCuaHang
                 {
                     Id = 1,
                     Phuong = "Thị trấn Quang Minh",
                     Quan = "Huyện Mê Linh",
                     ThanhPho = "Thành phố Hà Nội",
                     ChiTietDiaChi = "Tổ 6",
                     Latitude = 21.1822074,
                     Longitude = 105.7216836
                 }
             );


            // Seed ThongTinGiaoHang
            modelBuilder.Entity<ThongTinGiaoHang>().HasData(
                new ThongTinGiaoHang
                {
                    Id = 1,
                    PhiGiaoHang = 5000,
                    DonHangToiThieu = 50000,
                    BanKinhfree=3,
                    BanKinhGiaoHang=15
                }
            );

            // Seed ThongTinCuaHang
            modelBuilder.Entity<ThongTinCuaHang>().HasData(
                new ThongTinCuaHang
                {
                    Id = 1,
                    TenCuaHang = "hiện ngọc",
                    SoDienThoai = "0388118966",
                    Gmail = "hienngoc@example.com",
                    DongCua = TimeOnly.Parse("23:00:00"),
                    MoCua = TimeOnly.Parse("06:00:00")
                }
            );
            modelBuilder.Entity<MauSac>().HasData(
               new MauSac { MaMauSac = 1, TenMau = "Đỏ", MoTa = "#FF0000", TrangThai = 1 },
               new MauSac { MaMauSac = 2, TenMau = "Xanh dương", MoTa = "#0000FF", TrangThai = 1 },
               new MauSac { MaMauSac = 3, TenMau = "Vàng", MoTa = "#FFFF00", TrangThai = 1 },
               new MauSac { MaMauSac = 4, TenMau = "Xanh lá cây", MoTa = "#00FF00", TrangThai = 1 },
               new MauSac { MaMauSac = 5, TenMau = "Cam", MoTa = "#FFA500", TrangThai = 1 },
               new MauSac { MaMauSac = 6, TenMau = "Tím", MoTa = "#800080", TrangThai = 1 },
               new MauSac { MaMauSac = 7, TenMau = "Hồng", MoTa = "#FFC0CB", TrangThai = 1 },
               new MauSac { MaMauSac = 8, TenMau = "Nâu", MoTa = "#A52A2A", TrangThai = 1 },
               new MauSac { MaMauSac = 9, TenMau = "Đen", MoTa = "#000000", TrangThai = 1 },
               new MauSac { MaMauSac = 10, TenMau = "Trắng", MoTa = "#FFFFFF", TrangThai = 1 },
               new MauSac { MaMauSac = 11, TenMau = "Xám", MoTa = "#808080", TrangThai = 1 },
               new MauSac { MaMauSac = 12, TenMau = "Xanh ngọc", MoTa = "#00CED1", TrangThai = 1 }
             );
            modelBuilder.Entity<Size>().HasData(
               new Size { MaSize = 1, TenSize = "XS", MoTa = "Extra Small – Rất nhỏ", TrangThai = 1 },
               new Size { MaSize = 2, TenSize = "S", MoTa = "Small – Nhỏ", TrangThai = 1 },
               new Size { MaSize = 3, TenSize = "M", MoTa = "Medium – Vừa", TrangThai = 1 },
               new Size { MaSize = 4, TenSize = "L", MoTa = "Large – Lớn", TrangThai = 1 },
               new Size { MaSize = 5, TenSize = "XL", MoTa = "Extra Large – Rất lớn", TrangThai = 1 },
               new Size { MaSize = 6, TenSize = "XXL", MoTa = "Double Extra Large – Siêu lớn", TrangThai = 1 },
               new Size { MaSize = 7, TenSize = "3XL", MoTa = "Triple Extra Large – Cực lớn", TrangThai = 1 },
               new Size { MaSize = 8, TenSize = "Free Size", MoTa = "Kích thước tự do – phù hợp nhiều vóc dáng", TrangThai = 1 },
               new Size { MaSize = 9, TenSize = "28", MoTa = "Size quần 28 – nhỏ", TrangThai = 1 },
               new Size { MaSize = 10, TenSize = "30", MoTa = "Size quần 30 – trung bình", TrangThai = 1 },
               new Size { MaSize = 11, TenSize = "32", MoTa = "Size quần 32 – lớn", TrangThai = 1 },
               new Size { MaSize = 12, TenSize = "34", MoTa = "Size quần 34 – rất lớn", TrangThai = 1 }
           );
            modelBuilder.Entity<DanhMuc>().HasData(
                new DanhMuc { MaDanhMuc = 1, TenDanhMuc = "Áo thun", MoTa = "Áo thun đơn giản, dễ phối đồ", TrangThai = 1 },
                new DanhMuc { MaDanhMuc = 2, TenDanhMuc = "Áo sơ mi", MoTa = "Áo sơ mi công sở, lịch sự", TrangThai = 1 },
                new DanhMuc { MaDanhMuc = 3, TenDanhMuc = "Áo khoác", MoTa = "Áo khoác giữ ấm, thời trang", TrangThai = 1 },
                new DanhMuc { MaDanhMuc = 4, TenDanhMuc = "Quần jeans", MoTa = "Quần jeans năng động, cá tính", TrangThai = 1 },
                new DanhMuc { MaDanhMuc = 5, TenDanhMuc = "Quần tây", MoTa = "Quần tây lịch lãm, phù hợp công sở", TrangThai = 1 },
                new DanhMuc { MaDanhMuc = 6, TenDanhMuc = "Váy đầm", MoTa = "Váy nữ tính, đa dạng kiểu dáng", TrangThai = 1 },
                new DanhMuc { MaDanhMuc = 7, TenDanhMuc = "Đồ thể thao", MoTa = "Trang phục thể thao thoải mái", TrangThai = 1 },
                new DanhMuc { MaDanhMuc = 8, TenDanhMuc = "Đồ ngủ", MoTa = "Đồ ngủ mềm mại, dễ chịu", TrangThai = 1 },
                new DanhMuc { MaDanhMuc = 9, TenDanhMuc = "Đồ lót", MoTa = "Đồ lót nam nữ các loại", TrangThai = 1 },
                new DanhMuc { MaDanhMuc = 10, TenDanhMuc = "Áo hoodie", MoTa = "Áo hoodie trẻ trung, cá tính", TrangThai = 1 },
                new DanhMuc { MaDanhMuc = 11, TenDanhMuc = "Áo len", MoTa = "Áo len giữ ấm mùa đông", TrangThai = 1 },
                new DanhMuc { MaDanhMuc = 12, TenDanhMuc = "Quần short", MoTa = "Quần short mát mẻ, năng động", TrangThai = 1 }
            );
            modelBuilder.Entity<XuatXu>().HasData(
                new XuatXu { MaXuatXu = 1, TenXuatXu = "Việt Nam", MoTa = "Sản xuất trong nước, chất lượng ổn định", TrangThai = 1 },
                new XuatXu { MaXuatXu = 2, TenXuatXu = "Trung Quốc", MoTa = "Giá thành cạnh tranh, sản xuất quy mô lớn", TrangThai = 1 },
                new XuatXu { MaXuatXu = 3, TenXuatXu = "Hàn Quốc", MoTa = "Thiết kế hiện đại, thời trang cao cấp", TrangThai = 1 },
                new XuatXu { MaXuatXu = 4, TenXuatXu = "Nhật Bản", MoTa = "Chất lượng cao, độ bền tốt", TrangThai = 1 },
                new XuatXu { MaXuatXu = 5, TenXuatXu = "Thái Lan", MoTa = "Mẫu mã đẹp, giá hợp lý", TrangThai = 1 },
                new XuatXu { MaXuatXu = 6, TenXuatXu = "Mỹ", MoTa = "Tiêu chuẩn quốc tế, thương hiệu uy tín", TrangThai = 1 },
                new XuatXu { MaXuatXu = 7, TenXuatXu = "Pháp", MoTa = "Phong cách sang trọng, thời trang cao cấp", TrangThai = 1 },
                new XuatXu { MaXuatXu = 8, TenXuatXu = "Ý", MoTa = "Thời trang đẳng cấp, thiết kế tinh tế", TrangThai = 1 },
                new XuatXu { MaXuatXu = 9, TenXuatXu = "Đức", MoTa = "Kỹ thuật chính xác, độ bền cao", TrangThai = 1 },
                new XuatXu { MaXuatXu = 10, TenXuatXu = "Anh", MoTa = "Phong cách cổ điển, chất lượng tốt", TrangThai = 1 },
                new XuatXu { MaXuatXu = 11, TenXuatXu = "Ấn Độ", MoTa = "Nguyên liệu tự nhiên, giá tốt", TrangThai = 1 },
                new XuatXu { MaXuatXu = 12, TenXuatXu = "Indonesia", MoTa = "Sản xuất khu vực Đông Nam Á", TrangThai = 1 }
            );
            modelBuilder.Entity<ChatLieu>().HasData(
               new ChatLieu { MaChatLieu = 1, TenChatLieu = "Cotton", MoTa = "Chất liệu mềm mại, thoáng khí, phù hợp thời tiết nóng", TrangThai = 1 },
               new ChatLieu { MaChatLieu = 2, TenChatLieu = "Polyester", MoTa = "Chất liệu nhân tạo, bền, ít nhăn, dễ giặt", TrangThai = 1 },
               new ChatLieu { MaChatLieu = 3, TenChatLieu = "Linen", MoTa = "Vải lanh mát mẻ, sang trọng, phù hợp mùa hè", TrangThai = 1 },
               new ChatLieu { MaChatLieu = 4, TenChatLieu = "Len", MoTa = "Giữ ấm tốt, dùng cho mùa đông", TrangThai = 1 },
               new ChatLieu { MaChatLieu = 5, TenChatLieu = "Jean (Denim)", MoTa = "Chất liệu dày, bền, thường dùng cho quần áo cá tính", TrangThai = 1 },
               new ChatLieu { MaChatLieu = 6, TenChatLieu = "Lụa", MoTa = "Mềm mịn, bóng đẹp, thường dùng cho đồ cao cấp", TrangThai = 1 },
               new ChatLieu { MaChatLieu = 7, TenChatLieu = "Vải thun", MoTa = "Co giãn tốt, thoải mái khi vận động", TrangThai = 1 },
               new ChatLieu { MaChatLieu = 8, TenChatLieu = "Nỉ", MoTa = "Giữ nhiệt tốt, thường dùng cho áo khoác, hoodie", TrangThai = 1 },
               new ChatLieu { MaChatLieu = 9, TenChatLieu = "Voan", MoTa = "Mỏng nhẹ, bay bổng, thường dùng cho váy nữ", TrangThai = 1 },
               new ChatLieu { MaChatLieu = 10, TenChatLieu = "Kaki", MoTa = "Dày dặn, đứng form, thường dùng cho quần áo công sở", TrangThai = 1 },
               new ChatLieu { MaChatLieu = 11, TenChatLieu = "Da thật", MoTa = "Chất liệu cao cấp, bền, sang trọng", TrangThai = 1 },
               new ChatLieu { MaChatLieu = 12, TenChatLieu = "Giả da", MoTa = "Thay thế da thật, giá rẻ hơn, dễ bảo quản", TrangThai = 1 }
           );
            modelBuilder.Entity<SanPham>().HasData(
                new SanPham
                {
                    MaSanPham = 1,
                    MaDanhMuc = 1,
                    MaChatLieu = 1,
                    MaXuatXu = 1,
                    TenSanPham = "Áo thun nam basic",
                    MoTa = "Áo thun cotton thoáng mát, phù hợp mùa hè",
                    AnhSanPham = "34239af7-9bc2-4259-b9aa-5c4d6d982383.webp",
                    ThoiGianTao = new DateTime(2025, 8, 11, 12, 0, 0),  // ví dụ ngày cố định
                    TrangThai = 1
                },
                new SanPham
                {
                    MaSanPham = 2,
                    MaDanhMuc = 2,
                    MaChatLieu = 2,
                    MaXuatXu = 2,
                    TenSanPham = "Áo sơ mi trắng công sở",
                    MoTa = "Áo sơ mi lịch sự, phù hợp môi trường văn phòng",
                    AnhSanPham = "e3494b8b-9ee4-43a2-8b97-51ac8b5d269d.webp",
                    ThoiGianTao = new DateTime(2025, 8, 11, 12, 0, 0),
                    TrangThai = 1
                },
                new SanPham
                {
                    MaSanPham = 3,
                    MaDanhMuc = 3,
                    MaChatLieu = 3,
                    MaXuatXu = 3,
                    TenSanPham = "Áo khoác hoodie unisex",
                    MoTa = "Áo hoodie nỉ ấm, phong cách trẻ trung",
                    AnhSanPham = "db61a7eb-6e34-4ca1-8c32-da30bf68edfd.webp",
                    ThoiGianTao = new DateTime(2025, 8, 11, 12, 0, 0),
                    TrangThai = 1
                },
                new SanPham
                {
                    MaSanPham = 4,
                    MaDanhMuc = 4,
                    MaChatLieu = 4,
                    MaXuatXu = 4,
                    TenSanPham = "Quần jeans rách gối",
                    MoTa = "Quần jeans cá tính, phong cách đường phố",
                    AnhSanPham = "5a60f6f7-e20c-44eb-9cb5-88fdb944f13c.webp",
                    ThoiGianTao = new DateTime(2025, 8, 11, 12, 0, 0),
                    TrangThai = 1
                },
                new SanPham
                {
                    MaSanPham = 5,
                    MaDanhMuc = 5,
                    MaChatLieu = 5,
                    MaXuatXu = 5,
                    TenSanPham = "Váy voan xếp ly",
                    MoTa = "Váy nữ tính, chất liệu voan nhẹ nhàng",
                    AnhSanPham = "2693f29b-9566-4972-ae7e-9dc3bbb9117a.webp",
                    ThoiGianTao = new DateTime(2025, 8, 11, 12, 0, 0),
                    TrangThai = 1
                },
                new SanPham
                {
                    MaSanPham = 6,
                    MaDanhMuc = 6,
                    MaChatLieu = 6,
                    MaXuatXu = 6,
                    TenSanPham = "Áo len cổ lọ",
                    MoTa = "Áo len giữ ấm, phù hợp mùa đông",
                    AnhSanPham = "6f339380-7919-4e1d-87b7-76bd54febf5b.webp",
                    ThoiGianTao = new DateTime(2025, 8, 11, 12, 0, 0),
                    TrangThai = 1
                }
            );
            modelBuilder.Entity<SanPhamChiTiet>().HasData(
                new SanPhamChiTiet
                {
                    MaSanPhamChiTiet = 1,
                    MaSanPham = 1,
                    MaMauSac = 1,
                    MaSize = 1,
                    GiaBan = 199000.00m,
                    SoLuong = 50,
                    GhiChu = "Màu đỏ - Size S",
                    HinhAnh = "e35219ca-1040-42ab-ad5a-292be7750b18.webp",
                    TrangThai = 1
                },
                new SanPhamChiTiet
                {
                    MaSanPhamChiTiet = 2,
                    MaSanPham = 1,
                    MaMauSac = 2,
                    MaSize = 2,
                    GiaBan = 199000.00m,
                    SoLuong = 40,
                    GhiChu = "Màu xanh dương - Size M",
                    HinhAnh = "4a5e7e4b-27f6-437c-8874-5e03278df64f.webp",
                    TrangThai = 1
                },
                new SanPhamChiTiet
                {
                    MaSanPhamChiTiet = 3,
                    MaSanPham = 1,
                    MaMauSac = 3,
                    MaSize = 3,
                    GiaBan = 199000.00m,
                    SoLuong = 30,
                    GhiChu = "Màu vàng - Size L",
                    HinhAnh = "a1695aa7-705e-48d0-b28b-464aa5c570f3.webp",
                    TrangThai = 1
                },
                new SanPhamChiTiet
                {
                    MaSanPhamChiTiet = 4,
                    MaSanPham = 2,
                    MaMauSac = 10,
                    MaSize = 1,
                    GiaBan = 249000.00m,
                    SoLuong = 30,
                    GhiChu = "Size S",
                    HinhAnh = "95918186-de09-413f-9518-77100c34f79d.webp",
                    TrangThai = 1
                },
                new SanPhamChiTiet
                {
                    MaSanPhamChiTiet = 5,
                    MaSanPham = 2,
                    MaMauSac = 10,
                    MaSize = 2,
                    GiaBan = 249000.00m,
                    SoLuong = 45,
                    GhiChu = "Size M",
                    HinhAnh = "05ea2664-5ed0-4272-aaa2-66de76935ed3.webp",
                    TrangThai = 1
                },
                new SanPhamChiTiet
                {
                    MaSanPhamChiTiet = 6,
                    MaSanPham = 2,
                    MaMauSac = 6,
                    MaSize = 3,
                    GiaBan = 249000.00m,
                    SoLuong = 35,
                    GhiChu = "Size L",
                    HinhAnh = "1a97e244-2280-470c-9f42-c6cca8d17f29.webp",
                    TrangThai = 1
                },
                new SanPhamChiTiet
                {
                    MaSanPhamChiTiet = 7,
                    MaSanPham = 3,
                    MaMauSac = 7,
                    MaSize = 2,
                    GiaBan = 299000.00m,
                    SoLuong = 70,
                    GhiChu = "Màu hồng - Size M",
                    HinhAnh = "7c236dee-12f7-43fd-bc06-7330c651ead1.webp",
                    TrangThai = 1
                },
                new SanPhamChiTiet
                {
                    MaSanPhamChiTiet = 8,
                    MaSanPham = 3,
                    MaMauSac = 8,
                    MaSize = 3,
                    GiaBan = 299000.00m,
                    SoLuong = 50,
                    GhiChu = "Màu nâu - Size L",
                    HinhAnh = "98aad352-f160-443e-8bc7-df0bb1254223.webp",
                    TrangThai = 1
                },
                new SanPhamChiTiet
                {
                    MaSanPhamChiTiet = 9,
                    MaSanPham = 3,
                    MaMauSac = 9,
                    MaSize = 4,
                    GiaBan = 299000.00m,
                    SoLuong = 40,
                    GhiChu = "Màu đen - Size XL",
                    HinhAnh = "28a18cbb-b318-4234-b96c-d51d0eb8dba6.webp",
                    TrangThai = 1
                },
                new SanPhamChiTiet
                {
                    MaSanPhamChiTiet = 10,
                    MaSanPham = 4,
                    MaMauSac = 11,
                    MaSize = 1,
                    GiaBan = 279000.00m,
                    SoLuong = 55,
                    GhiChu = "Màu xám - Size S",
                    HinhAnh = "d7e66c3a-a1e0-48f5-b37c-9384babe5f22.webp",
                    TrangThai = 1
                },
                new SanPhamChiTiet
                {
                    MaSanPhamChiTiet = 11,
                    MaSanPham = 4,
                    MaMauSac = 11,
                    MaSize = 2,
                    GiaBan = 279000.00m,
                    SoLuong = 45,
                    GhiChu = "Màu xám - Size M",
                    HinhAnh = "56d9df18-36fc-4c2c-b6f9-7a77275dad64.webp",
                    TrangThai = 1
                },
                new SanPhamChiTiet
                {
                    MaSanPhamChiTiet = 12,
                    MaSanPham = 4,
                    MaMauSac = 11,
                    MaSize = 3,
                    GiaBan = 279000.00m,
                    SoLuong = 35,
                    GhiChu = "Màu xám - Size L",
                    HinhAnh = "3b010930-6373-4c3f-afbe-f2c08374534d.webp",
                    TrangThai = 1
                },
                new SanPhamChiTiet
                {
                    MaSanPhamChiTiet = 13,
                    MaSanPham = 5,
                    MaMauSac = 1,
                    MaSize = 3,
                    GiaBan = 319000.00m,
                    SoLuong = 40,
                    GhiChu = "Màu đỏ - Size M",
                    HinhAnh = "ffa63165-c6be-4c9e-98b6-febfbe1fae71.webp",
                    TrangThai = 1
                },
                new SanPhamChiTiet
                {
                    MaSanPhamChiTiet = 14,
                    MaSanPham = 5,
                    MaMauSac = 2,
                    MaSize = 3,
                    GiaBan = 319000.00m,
                    SoLuong = 30,
                    GhiChu = "Màu xanh dương - Size L",
                    HinhAnh = "04131487-a5f2-4750-8fad-faaf1d173617.webp",
                    TrangThai = 1
                },
                new SanPhamChiTiet
                {
                    MaSanPhamChiTiet = 15,
                    MaSanPham = 5,
                    MaMauSac = 3,
                    MaSize = 4,
                    GiaBan = 319000.00m,
                    SoLuong = 20,
                    GhiChu = "Màu vàng - Size XL",
                    HinhAnh = "c6c7731d-bfea-4056-b54d-1353eb200919.webp",
                    TrangThai = 1
                },
                new SanPhamChiTiet
                {
                    MaSanPhamChiTiet = 16,
                    MaSanPham = 6,
                    MaMauSac = 4,
                    MaSize = 2,
                    GiaBan = 259000.00m,
                    SoLuong = 50,
                    GhiChu = "Màu xanh lá - Size M",
                    HinhAnh = "869f6d1d-17ee-4745-b1c8-b06a6d773c7a.webp",
                    TrangThai = 1
                },
                new SanPhamChiTiet
                {
                    MaSanPhamChiTiet = 17,
                    MaSanPham = 6,
                    MaMauSac = 4,
                    MaSize = 3,
                    GiaBan = 259000.00m,
                    SoLuong = 40,
                    GhiChu = "Màu cam - Size L",
                    HinhAnh = "6bc27c82-af10-4a6d-a378-bb27569e3f9f.webp",
                    TrangThai = 1
                },
                new SanPhamChiTiet
                {
                    MaSanPhamChiTiet = 18,
                    MaSanPham = 6,
                    MaMauSac = 6,
                    MaSize = 4,
                    GiaBan = 259000.00m,
                    SoLuong = 30,
                    GhiChu = "Màu tím - Size XL",
                    HinhAnh = "550f7a41-1020-4847-85bc-1bc9454192e0.webp",
                    TrangThai = 1
                }
            );
            modelBuilder.Entity<ChinhSach>().HasData(
                       new ChinhSach
                       {
                           id = 1,
                           TenChinhSach = "Chính sách bảo mật tài khoản",
                           NoiDung = @"Chúng tôi cam kết bảo vệ thông tin cá nhân của người dùng. 
                Thông tin như tên, email, số điện thoại được thu thập nhằm mục đích xác thực tài khoản, hỗ trợ kỹ thuật và cải thiện trải nghiệm người dùng. 
                Dữ liệu sẽ không được chia sẻ với bên thứ ba nếu không có sự đồng ý rõ ràng từ người dùng. 
                Người dùng có quyền yêu cầu truy cập, chỉnh sửa hoặc xóa thông tin cá nhân bất cứ lúc nào. 
                Chúng tôi áp dụng các biện pháp bảo mật như mã hóa, kiểm soát truy cập và giám sát hệ thống để đảm bảo an toàn dữ liệu."
                       },
                       new ChinhSach
                       {
                           id = 2,
                           TenChinhSach = "Chính sách đổi trả hàng",
                           NoiDung = @"Khách hàng có thể yêu cầu đổi hoặc trả sản phẩm trong vòng 7 ngày kể từ ngày nhận hàng. 
                Điều kiện đổi trả bao gồm: sản phẩm chưa qua sử dụng, còn nguyên tem niêm phong, đầy đủ phụ kiện và hóa đơn. 
                Nếu sản phẩm bị lỗi kỹ thuật do nhà sản xuất, chúng tôi sẽ hỗ trợ đổi mới hoặc hoàn tiền 100%. 
                Quá trình xử lý đổi trả sẽ được thực hiện trong vòng 3-5 ngày làm việc kể từ khi nhận được yêu cầu. 
                Chúng tôi không chấp nhận đổi trả đối với các sản phẩm đã qua sử dụng hoặc bị hư hỏng do người dùng."
                       },
                       new ChinhSach
                       {
                           id = 3,
                           TenChinhSach = "Chính sách hủy hàng",
                           NoiDung = @"[...]</li></ul>"
                       }
               );
        }
    }
}
