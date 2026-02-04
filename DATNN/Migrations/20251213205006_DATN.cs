using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DATNN.Migrations
{
    /// <inheritdoc />
    public partial class DATN : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatLieus",
                columns: table => new
                {
                    MaChatLieu = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenChatLieu = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TrangThai = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatLieus", x => x.MaChatLieu);
                });

            migrationBuilder.CreateTable(
                name: "ChinhSaches",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenChinhSach = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChinhSaches", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "DanhMucs",
                columns: table => new
                {
                    MaDanhMuc = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenDanhMuc = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TrangThai = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DanhMucs", x => x.MaDanhMuc);
                });

            migrationBuilder.CreateTable(
                name: "DiaChiCuaHangs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Phuong = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quan = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ThanhPho = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChiTietDiaChi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiaChiCuaHangs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KhuyenMais",
                columns: table => new
                {
                    MaKhuyenMai = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenKhuyenMai = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    KenhApDung = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LoaiGiamGia = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GiaTriGiamGia = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DanhSachSanPhamApDung = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TrangThai = table.Column<int>(type: "int", nullable: false),
                    NgayBatDau = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayKetThuc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KhuyenMais", x => x.MaKhuyenMai);
                });

            migrationBuilder.CreateTable(
                name: "MaGiamGias",
                columns: table => new
                {
                    MaGiamGiaID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TenChuongTrinh = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    LoaiApDung = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    KenhApDung = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TongLuotSuDungToiDa = table.Column<int>(type: "int", nullable: true),
                    DaSuDung = table.Column<int>(type: "int", nullable: false),
                    LoaiGiamGia = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GiaTriGiamGia = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DieuKienDonHangToiThieu = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DanhSachSanPhamApDung = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TrangThai = table.Column<int>(type: "int", nullable: false),
                    NgayBatDau = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayKetThuc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaGiamGias", x => x.MaGiamGiaID);
                });

            migrationBuilder.CreateTable(
                name: "MauSacs",
                columns: table => new
                {
                    MaMauSac = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenMau = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TrangThai = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MauSacs", x => x.MaMauSac);
                });

            migrationBuilder.CreateTable(
                name: "Quyens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaVaiTro = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ten = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TrangThai = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quyens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sizes",
                columns: table => new
                {
                    MaSize = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenSize = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TrangThai = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sizes", x => x.MaSize);
                });

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    SettingKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SettingValue = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.SettingKey);
                });

            migrationBuilder.CreateTable(
                name: "ThongTinCuaHangs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MoCua = table.Column<TimeOnly>(type: "time", nullable: false),
                    DongCua = table.Column<TimeOnly>(type: "time", nullable: false),
                    TenCuaHang = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SoDienThoai = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Gmail = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThongTinCuaHangs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ThongTinGiaoHangs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BanKinhGiaoHang = table.Column<double>(type: "float", nullable: false),
                    BanKinhfree = table.Column<double>(type: "float", nullable: false),
                    PhiGiaoHang = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DonHangToiThieu = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThongTinGiaoHangs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "XuatXus",
                columns: table => new
                {
                    MaXuatXu = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenXuatXu = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TrangThai = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XuatXus", x => x.MaXuatXu);
                });

            migrationBuilder.CreateTable(
                name: "NguoiDungs",
                columns: table => new
                {
                    MaNguoiDung = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaQuyen = table.Column<int>(type: "int", nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TenDangNhap = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MatKhau = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SoDienThoai = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TrangThai = table.Column<int>(type: "int", nullable: false),
                    NgaySinh = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResetToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TokenExpiry = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GioiTinh = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NguoiDungs", x => x.MaNguoiDung);
                    table.ForeignKey(
                        name: "FK_NguoiDungs_Quyens_MaQuyen",
                        column: x => x.MaQuyen,
                        principalTable: "Quyens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SanPhams",
                columns: table => new
                {
                    MaSanPham = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDanhMuc = table.Column<int>(type: "int", nullable: false),
                    MaChatLieu = table.Column<int>(type: "int", nullable: false),
                    MaXuatXu = table.Column<int>(type: "int", nullable: false),
                    TenSanPham = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AnhSanPham = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ThoiGianTao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TrangThai = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SanPhams", x => x.MaSanPham);
                    table.ForeignKey(
                        name: "FK_SanPhams_ChatLieus_MaChatLieu",
                        column: x => x.MaChatLieu,
                        principalTable: "ChatLieus",
                        principalColumn: "MaChatLieu",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SanPhams_DanhMucs_MaDanhMuc",
                        column: x => x.MaDanhMuc,
                        principalTable: "DanhMucs",
                        principalColumn: "MaDanhMuc",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SanPhams_XuatXus_MaXuatXu",
                        column: x => x.MaXuatXu,
                        principalTable: "XuatXus",
                        principalColumn: "MaXuatXu",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DiaChiNguoiDungs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaNguoiDung = table.Column<int>(type: "int", nullable: false),
                    Ten = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SoDienThoai = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phuong = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quan = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ThanhPho = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChiTietDiaChi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TrangThai = table.Column<int>(type: "int", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiaChiNguoiDungs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiaChiNguoiDungs_NguoiDungs_MaNguoiDung",
                        column: x => x.MaNguoiDung,
                        principalTable: "NguoiDungs",
                        principalColumn: "MaNguoiDung",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DonHangs",
                columns: table => new
                {
                    MaDonHang = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaKhuyenMai = table.Column<int>(type: "int", nullable: true),
                    MaNguoiDung = table.Column<int>(type: "int", nullable: true),
                    MaKhachHang = table.Column<int>(type: "int", nullable: true),
                    HoTenNguoiNhan = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TongTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SoDienThoai = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DiaChi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhiVanChuyen = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TrangThaiThanhToan = table.Column<int>(type: "int", nullable: false),
                    ThoiGianTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LichSuTrangThai = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LyDoHuy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TrangThaiDonHang = table.Column<int>(type: "int", nullable: false),
                    SoTienDuocGiam = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PhuongThucThanhToan = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TienMatDaNhan = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    VnpTxnRef = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VnpTransactionNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VnpPayDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MaGiamGiaID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonHangs", x => x.MaDonHang);
                    table.ForeignKey(
                        name: "FK_DonHangs_KhuyenMais_MaKhuyenMai",
                        column: x => x.MaKhuyenMai,
                        principalTable: "KhuyenMais",
                        principalColumn: "MaKhuyenMai");
                    table.ForeignKey(
                        name: "FK_DonHangs_MaGiamGias_MaGiamGiaID",
                        column: x => x.MaGiamGiaID,
                        principalTable: "MaGiamGias",
                        principalColumn: "MaGiamGiaID");
                    table.ForeignKey(
                        name: "FK_DonHangs_NguoiDungs_MaKhachHang",
                        column: x => x.MaKhachHang,
                        principalTable: "NguoiDungs",
                        principalColumn: "MaNguoiDung");
                    table.ForeignKey(
                        name: "FK_DonHangs_NguoiDungs_MaNguoiDung",
                        column: x => x.MaNguoiDung,
                        principalTable: "NguoiDungs",
                        principalColumn: "MaNguoiDung");
                });

            migrationBuilder.CreateTable(
                name: "GioHangs",
                columns: table => new
                {
                    MaGioHang = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaNguoiDung = table.Column<int>(type: "int", nullable: false),
                    TrangThai = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GioHangs", x => x.MaGioHang);
                    table.ForeignKey(
                        name: "FK_GioHangs_NguoiDungs_MaNguoiDung",
                        column: x => x.MaNguoiDung,
                        principalTable: "NguoiDungs",
                        principalColumn: "MaNguoiDung",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SanPhamChiTiets",
                columns: table => new
                {
                    MaSanPhamChiTiet = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaSanPham = table.Column<int>(type: "int", nullable: false),
                    MaMauSac = table.Column<int>(type: "int", nullable: false),
                    MaSize = table.Column<int>(type: "int", nullable: false),
                    GiaBan = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HinhAnh = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TrangThai = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SanPhamChiTiets", x => x.MaSanPhamChiTiet);
                    table.ForeignKey(
                        name: "FK_SanPhamChiTiets_MauSacs_MaMauSac",
                        column: x => x.MaMauSac,
                        principalTable: "MauSacs",
                        principalColumn: "MaMauSac",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SanPhamChiTiets_SanPhams_MaSanPham",
                        column: x => x.MaSanPham,
                        principalTable: "SanPhams",
                        principalColumn: "MaSanPham",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SanPhamChiTiets_Sizes_MaSize",
                        column: x => x.MaSize,
                        principalTable: "Sizes",
                        principalColumn: "MaSize",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietGioHangs",
                columns: table => new
                {
                    MaChiTietGioHang = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaGioHang = table.Column<int>(type: "int", nullable: false),
                    MaSanPhamChiTiet = table.Column<int>(type: "int", nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false),
                    TrangThai = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietGioHangs", x => x.MaChiTietGioHang);
                    table.ForeignKey(
                        name: "FK_ChiTietGioHangs_GioHangs_MaGioHang",
                        column: x => x.MaGioHang,
                        principalTable: "GioHangs",
                        principalColumn: "MaGioHang",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChiTietGioHangs_SanPhamChiTiets_MaSanPhamChiTiet",
                        column: x => x.MaSanPhamChiTiet,
                        principalTable: "SanPhamChiTiets",
                        principalColumn: "MaSanPhamChiTiet",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DonHangChiTiets",
                columns: table => new
                {
                    MaDonHangChiTiet = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaSanPhamChiTiet = table.Column<int>(type: "int", nullable: false),
                    MaDonHang = table.Column<int>(type: "int", nullable: false),
                    TenSanPham_Luu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenMau_Luu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenSize_Luu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HinhAnh_Luu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DonGia = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonHangChiTiets", x => x.MaDonHangChiTiet);
                    table.ForeignKey(
                        name: "FK_DonHangChiTiets_DonHangs_MaDonHang",
                        column: x => x.MaDonHang,
                        principalTable: "DonHangs",
                        principalColumn: "MaDonHang",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DonHangChiTiets_SanPhamChiTiets_MaSanPhamChiTiet",
                        column: x => x.MaSanPhamChiTiet,
                        principalTable: "SanPhamChiTiets",
                        principalColumn: "MaSanPhamChiTiet",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "YeuCauDoiTras",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDonHangChiTiet = table.Column<int>(type: "int", nullable: false),
                    MaNguoiDung = table.Column<int>(type: "int", nullable: false),
                    LoaiYeuCau = table.Column<int>(type: "int", nullable: false),
                    LyDo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    GhiChuKhachHang = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HinhAnhBangChung = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SoLuongYeuCau = table.Column<int>(type: "int", nullable: false),
                    TrangThai = table.Column<int>(type: "int", nullable: false),
                    GhiChuAdmin = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenNganHang = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TenChuTaiKhoan = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SoTaiKhoan = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ChiNhanh = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MaSanPhamChiTietMoi = table.Column<int>(type: "int", nullable: true),
                    TenSanPhamMoi_Luu = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TenMauMoi_Luu = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TenSizeMoi_Luu = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    HinhAnhMoi_Luu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GiaSanPhamMoi_Luu = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    BenChiuPhiShip = table.Column<int>(type: "int", nullable: false),
                    ChiPhiShip = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TienChenhLech = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    HinhThucHoanTien = table.Column<int>(type: "int", nullable: true),
                    TongTienThanhToan = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YeuCauDoiTras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_YeuCauDoiTras_DonHangChiTiets_MaDonHangChiTiet",
                        column: x => x.MaDonHangChiTiet,
                        principalTable: "DonHangChiTiets",
                        principalColumn: "MaDonHangChiTiet",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_YeuCauDoiTras_NguoiDungs_MaNguoiDung",
                        column: x => x.MaNguoiDung,
                        principalTable: "NguoiDungs",
                        principalColumn: "MaNguoiDung",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PhieuChis",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SoTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LoaiChiPhi = table.Column<int>(type: "int", nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false),
                    MaDonHang = table.Column<int>(type: "int", nullable: true),
                    MaYeuCauDoiTra = table.Column<int>(type: "int", nullable: true),
                    MaNguoiDung = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhieuChis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhieuChis_DonHangs_MaDonHang",
                        column: x => x.MaDonHang,
                        principalTable: "DonHangs",
                        principalColumn: "MaDonHang");
                    table.ForeignKey(
                        name: "FK_PhieuChis_NguoiDungs_MaNguoiDung",
                        column: x => x.MaNguoiDung,
                        principalTable: "NguoiDungs",
                        principalColumn: "MaNguoiDung");
                    table.ForeignKey(
                        name: "FK_PhieuChis_YeuCauDoiTras_MaYeuCauDoiTra",
                        column: x => x.MaYeuCauDoiTra,
                        principalTable: "YeuCauDoiTras",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "ChatLieus",
                columns: new[] { "MaChatLieu", "MoTa", "TenChatLieu", "TrangThai" },
                values: new object[,]
                {
                    { 1, "Chất liệu mềm mại, thoáng khí, phù hợp thời tiết nóng", "Cotton", 1 },
                    { 2, "Chất liệu nhân tạo, bền, ít nhăn, dễ giặt", "Polyester", 1 },
                    { 3, "Vải lanh mát mẻ, sang trọng, phù hợp mùa hè", "Linen", 1 },
                    { 4, "Giữ ấm tốt, dùng cho mùa đông", "Len", 1 },
                    { 5, "Chất liệu dày, bền, thường dùng cho quần áo cá tính", "Jean (Denim)", 1 },
                    { 6, "Mềm mịn, bóng đẹp, thường dùng cho đồ cao cấp", "Lụa", 1 },
                    { 7, "Co giãn tốt, thoải mái khi vận động", "Vải thun", 1 },
                    { 8, "Giữ nhiệt tốt, thường dùng cho áo khoác, hoodie", "Nỉ", 1 },
                    { 9, "Mỏng nhẹ, bay bổng, thường dùng cho váy nữ", "Voan", 1 },
                    { 10, "Dày dặn, đứng form, thường dùng cho quần áo công sở", "Kaki", 1 },
                    { 11, "Chất liệu cao cấp, bền, sang trọng", "Da thật", 1 },
                    { 12, "Thay thế da thật, giá rẻ hơn, dễ bảo quản", "Giả da", 1 }
                });

            migrationBuilder.InsertData(
                table: "ChinhSaches",
                columns: new[] { "id", "NoiDung", "TenChinhSach" },
                values: new object[,]
                {
                    { 1, "Chúng tôi cam kết bảo vệ thông tin cá nhân của người dùng. \r\n                Thông tin như tên, email, số điện thoại được thu thập nhằm mục đích xác thực tài khoản, hỗ trợ kỹ thuật và cải thiện trải nghiệm người dùng. \r\n                Dữ liệu sẽ không được chia sẻ với bên thứ ba nếu không có sự đồng ý rõ ràng từ người dùng. \r\n                Người dùng có quyền yêu cầu truy cập, chỉnh sửa hoặc xóa thông tin cá nhân bất cứ lúc nào. \r\n                Chúng tôi áp dụng các biện pháp bảo mật như mã hóa, kiểm soát truy cập và giám sát hệ thống để đảm bảo an toàn dữ liệu.", "Chính sách bảo mật tài khoản" },
                    { 2, "Khách hàng có thể yêu cầu đổi hoặc trả sản phẩm trong vòng 7 ngày kể từ ngày nhận hàng. \r\n                Điều kiện đổi trả bao gồm: sản phẩm chưa qua sử dụng, còn nguyên tem niêm phong, đầy đủ phụ kiện và hóa đơn. \r\n                Nếu sản phẩm bị lỗi kỹ thuật do nhà sản xuất, chúng tôi sẽ hỗ trợ đổi mới hoặc hoàn tiền 100%. \r\n                Quá trình xử lý đổi trả sẽ được thực hiện trong vòng 3-5 ngày làm việc kể từ khi nhận được yêu cầu. \r\n                Chúng tôi không chấp nhận đổi trả đối với các sản phẩm đã qua sử dụng hoặc bị hư hỏng do người dùng.", "Chính sách đổi trả hàng" },
                    { 3, "[...]</li></ul>", "Chính sách hủy hàng" }
                });

            migrationBuilder.InsertData(
                table: "DanhMucs",
                columns: new[] { "MaDanhMuc", "MoTa", "TenDanhMuc", "TrangThai" },
                values: new object[,]
                {
                    { 1, "Áo thun đơn giản, dễ phối đồ", "Áo thun", 1 },
                    { 2, "Áo sơ mi công sở, lịch sự", "Áo sơ mi", 1 },
                    { 3, "Áo khoác giữ ấm, thời trang", "Áo khoác", 1 },
                    { 4, "Quần jeans năng động, cá tính", "Quần jeans", 1 },
                    { 5, "Quần tây lịch lãm, phù hợp công sở", "Quần tây", 1 },
                    { 6, "Váy nữ tính, đa dạng kiểu dáng", "Váy đầm", 1 },
                    { 7, "Trang phục thể thao thoải mái", "Đồ thể thao", 1 },
                    { 8, "Đồ ngủ mềm mại, dễ chịu", "Đồ ngủ", 1 },
                    { 9, "Đồ lót nam nữ các loại", "Đồ lót", 1 },
                    { 10, "Áo hoodie trẻ trung, cá tính", "Áo hoodie", 1 },
                    { 11, "Áo len giữ ấm mùa đông", "Áo len", 1 },
                    { 12, "Quần short mát mẻ, năng động", "Quần short", 1 }
                });

            migrationBuilder.InsertData(
                table: "DiaChiCuaHangs",
                columns: new[] { "Id", "ChiTietDiaChi", "Latitude", "Longitude", "Phuong", "Quan", "ThanhPho" },
                values: new object[] { 1, "Tổ 6", 21.182207399999999, 105.72168360000001, "Thị trấn Quang Minh", "Huyện Mê Linh", "Thành phố Hà Nội" });

            migrationBuilder.InsertData(
                table: "MauSacs",
                columns: new[] { "MaMauSac", "MoTa", "TenMau", "TrangThai" },
                values: new object[,]
                {
                    { 1, "#FF0000", "Đỏ", 1 },
                    { 2, "#0000FF", "Xanh dương", 1 },
                    { 3, "#FFFF00", "Vàng", 1 },
                    { 4, "#00FF00", "Xanh lá cây", 1 },
                    { 5, "#FFA500", "Cam", 1 },
                    { 6, "#800080", "Tím", 1 },
                    { 7, "#FFC0CB", "Hồng", 1 },
                    { 8, "#A52A2A", "Nâu", 1 },
                    { 9, "#000000", "Đen", 1 },
                    { 10, "#FFFFFF", "Trắng", 1 },
                    { 11, "#808080", "Xám", 1 },
                    { 12, "#00CED1", "Xanh ngọc", 1 }
                });

            migrationBuilder.InsertData(
                table: "Quyens",
                columns: new[] { "Id", "MaVaiTro", "Ten", "TrangThai" },
                values: new object[,]
                {
                    { 1, "ADMIN", "Quản trị viên", 1 },
                    { 2, "NHANVIEN", "Nhân viên", 1 },
                    { 3, "KHACHHANG", "Khách hàng", 1 }
                });

            migrationBuilder.InsertData(
                table: "Sizes",
                columns: new[] { "MaSize", "MoTa", "TenSize", "TrangThai" },
                values: new object[,]
                {
                    { 1, "Extra Small – Rất nhỏ", "XS", 1 },
                    { 2, "Small – Nhỏ", "S", 1 },
                    { 3, "Medium – Vừa", "M", 1 },
                    { 4, "Large – Lớn", "L", 1 },
                    { 5, "Extra Large – Rất lớn", "XL", 1 },
                    { 6, "Double Extra Large – Siêu lớn", "XXL", 1 },
                    { 7, "Triple Extra Large – Cực lớn", "3XL", 1 },
                    { 8, "Kích thước tự do – phù hợp nhiều vóc dáng", "Free Size", 1 },
                    { 9, "Size quần 28 – nhỏ", "28", 1 },
                    { 10, "Size quần 30 – trung bình", "30", 1 },
                    { 11, "Size quần 32 – lớn", "32", 1 },
                    { 12, "Size quần 34 – rất lớn", "34", 1 }
                });

            migrationBuilder.InsertData(
                table: "ThongTinCuaHangs",
                columns: new[] { "Id", "DongCua", "Gmail", "MoCua", "SoDienThoai", "TenCuaHang" },
                values: new object[] { 1, new TimeOnly(23, 0, 0), "hienngoc@example.com", new TimeOnly(6, 0, 0), "0388118966", "hiện ngọc" });

            migrationBuilder.InsertData(
                table: "ThongTinGiaoHangs",
                columns: new[] { "Id", "BanKinhGiaoHang", "BanKinhfree", "DonHangToiThieu", "PhiGiaoHang" },
                values: new object[] { 1, 15.0, 3.0, 50000m, 5000m });

            migrationBuilder.InsertData(
                table: "XuatXus",
                columns: new[] { "MaXuatXu", "MoTa", "TenXuatXu", "TrangThai" },
                values: new object[,]
                {
                    { 1, "Sản xuất trong nước, chất lượng ổn định", "Việt Nam", 1 },
                    { 2, "Giá thành cạnh tranh, sản xuất quy mô lớn", "Trung Quốc", 1 },
                    { 3, "Thiết kế hiện đại, thời trang cao cấp", "Hàn Quốc", 1 },
                    { 4, "Chất lượng cao, độ bền tốt", "Nhật Bản", 1 },
                    { 5, "Mẫu mã đẹp, giá hợp lý", "Thái Lan", 1 },
                    { 6, "Tiêu chuẩn quốc tế, thương hiệu uy tín", "Mỹ", 1 },
                    { 7, "Phong cách sang trọng, thời trang cao cấp", "Pháp", 1 },
                    { 8, "Thời trang đẳng cấp, thiết kế tinh tế", "Ý", 1 },
                    { 9, "Kỹ thuật chính xác, độ bền cao", "Đức", 1 },
                    { 10, "Phong cách cổ điển, chất lượng tốt", "Anh", 1 },
                    { 11, "Nguyên liệu tự nhiên, giá tốt", "Ấn Độ", 1 },
                    { 12, "Sản xuất khu vực Đông Nam Á", "Indonesia", 1 }
                });

            migrationBuilder.InsertData(
                table: "NguoiDungs",
                columns: new[] { "MaNguoiDung", "Email", "GioiTinh", "HoTen", "MaQuyen", "MatKhau", "NgaySinh", "NgayTao", "ResetToken", "SoDienThoai", "TenDangNhap", "TokenExpiry", "TrangThai" },
                values: new object[,]
                {
                    { 1, "admin@example.com", "Nam", "Quản trị viên", 1, "AQAAAAIAAYagAAAAENa6tfOCNlmIuKKfP5aBy3xmrAvlx93io3bqEuNCmdQgG1u+vvHR0Zc0Q3i9OeAHlg==", new DateTime(1990, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 8, 11, 15, 0, 0, 0, DateTimeKind.Unspecified), null, "0900000001", "admin", null, 1 },
                    { 2, "nhanvien@example.com", "Nam", "Nguyễn Văn Nhân", 2, "AQAAAAIAAYagAAAAENa6tfOCNlmIuKKfP5aBy3xmrAvlx93io3bqEuNCmdQgG1u+vvHR0Zc0Q3i9OeAHlg==", new DateTime(1995, 5, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 8, 11, 15, 0, 0, 0, DateTimeKind.Unspecified), null, "0900000002", "nhanvien", null, 1 },
                    { 3, "khach@example.com", "Nữ", "Trần Thị Khách", 3, "AQAAAAIAAYagAAAAENa6tfOCNlmIuKKfP5aBy3xmrAvlx93io3bqEuNCmdQgG1u+vvHR0Zc0Q3i9OeAHlg==", new DateTime(2000, 10, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2025, 8, 11, 15, 0, 0, 0, DateTimeKind.Unspecified), null, "0900000003", "khachhang", null, 1 }
                });

            migrationBuilder.InsertData(
                table: "SanPhams",
                columns: new[] { "MaSanPham", "AnhSanPham", "MaChatLieu", "MaDanhMuc", "MaXuatXu", "MoTa", "TenSanPham", "ThoiGianTao", "TrangThai" },
                values: new object[,]
                {
                    { 1, "34239af7-9bc2-4259-b9aa-5c4d6d982383.webp", 1, 1, 1, "Áo thun cotton thoáng mát, phù hợp mùa hè", "Áo thun nam basic", new DateTime(2025, 8, 11, 12, 0, 0, 0, DateTimeKind.Unspecified), 1 },
                    { 2, "e3494b8b-9ee4-43a2-8b97-51ac8b5d269d.webp", 2, 2, 2, "Áo sơ mi lịch sự, phù hợp môi trường văn phòng", "Áo sơ mi trắng công sở", new DateTime(2025, 8, 11, 12, 0, 0, 0, DateTimeKind.Unspecified), 1 },
                    { 3, "db61a7eb-6e34-4ca1-8c32-da30bf68edfd.webp", 3, 3, 3, "Áo hoodie nỉ ấm, phong cách trẻ trung", "Áo khoác hoodie unisex", new DateTime(2025, 8, 11, 12, 0, 0, 0, DateTimeKind.Unspecified), 1 },
                    { 4, "5a60f6f7-e20c-44eb-9cb5-88fdb944f13c.webp", 4, 4, 4, "Quần jeans cá tính, phong cách đường phố", "Quần jeans rách gối", new DateTime(2025, 8, 11, 12, 0, 0, 0, DateTimeKind.Unspecified), 1 },
                    { 5, "2693f29b-9566-4972-ae7e-9dc3bbb9117a.webp", 5, 5, 5, "Váy nữ tính, chất liệu voan nhẹ nhàng", "Váy voan xếp ly", new DateTime(2025, 8, 11, 12, 0, 0, 0, DateTimeKind.Unspecified), 1 },
                    { 6, "6f339380-7919-4e1d-87b7-76bd54febf5b.webp", 6, 6, 6, "Áo len giữ ấm, phù hợp mùa đông", "Áo len cổ lọ", new DateTime(2025, 8, 11, 12, 0, 0, 0, DateTimeKind.Unspecified), 1 }
                });

            migrationBuilder.InsertData(
                table: "SanPhamChiTiets",
                columns: new[] { "MaSanPhamChiTiet", "GhiChu", "GiaBan", "HinhAnh", "MaMauSac", "MaSanPham", "MaSize", "SoLuong", "TrangThai" },
                values: new object[,]
                {
                    { 1, "Màu đỏ - Size S", 199000.00m, "e35219ca-1040-42ab-ad5a-292be7750b18.webp", 1, 1, 1, 50, 1 },
                    { 2, "Màu xanh dương - Size M", 199000.00m, "4a5e7e4b-27f6-437c-8874-5e03278df64f.webp", 2, 1, 2, 40, 1 },
                    { 3, "Màu vàng - Size L", 199000.00m, "a1695aa7-705e-48d0-b28b-464aa5c570f3.webp", 3, 1, 3, 30, 1 },
                    { 4, "Size S", 249000.00m, "95918186-de09-413f-9518-77100c34f79d.webp", 10, 2, 1, 30, 1 },
                    { 5, "Size M", 249000.00m, "05ea2664-5ed0-4272-aaa2-66de76935ed3.webp", 10, 2, 2, 45, 1 },
                    { 6, "Size L", 249000.00m, "1a97e244-2280-470c-9f42-c6cca8d17f29.webp", 6, 2, 3, 35, 1 },
                    { 7, "Màu hồng - Size M", 299000.00m, "7c236dee-12f7-43fd-bc06-7330c651ead1.webp", 7, 3, 2, 70, 1 },
                    { 8, "Màu nâu - Size L", 299000.00m, "98aad352-f160-443e-8bc7-df0bb1254223.webp", 8, 3, 3, 50, 1 },
                    { 9, "Màu đen - Size XL", 299000.00m, "28a18cbb-b318-4234-b96c-d51d0eb8dba6.webp", 9, 3, 4, 40, 1 },
                    { 10, "Màu xám - Size S", 279000.00m, "d7e66c3a-a1e0-48f5-b37c-9384babe5f22.webp", 11, 4, 1, 55, 1 },
                    { 11, "Màu xám - Size M", 279000.00m, "56d9df18-36fc-4c2c-b6f9-7a77275dad64.webp", 11, 4, 2, 45, 1 },
                    { 12, "Màu xám - Size L", 279000.00m, "3b010930-6373-4c3f-afbe-f2c08374534d.webp", 11, 4, 3, 35, 1 },
                    { 13, "Màu đỏ - Size M", 319000.00m, "ffa63165-c6be-4c9e-98b6-febfbe1fae71.webp", 1, 5, 3, 40, 1 },
                    { 14, "Màu xanh dương - Size L", 319000.00m, "04131487-a5f2-4750-8fad-faaf1d173617.webp", 2, 5, 3, 30, 1 },
                    { 15, "Màu vàng - Size XL", 319000.00m, "c6c7731d-bfea-4056-b54d-1353eb200919.webp", 3, 5, 4, 20, 1 },
                    { 16, "Màu xanh lá - Size M", 259000.00m, "869f6d1d-17ee-4745-b1c8-b06a6d773c7a.webp", 4, 6, 2, 50, 1 },
                    { 17, "Màu cam - Size L", 259000.00m, "6bc27c82-af10-4a6d-a378-bb27569e3f9f.webp", 4, 6, 3, 40, 1 },
                    { 18, "Màu tím - Size XL", 259000.00m, "550f7a41-1020-4847-85bc-1bc9454192e0.webp", 6, 6, 4, 30, 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietGioHangs_MaGioHang",
                table: "ChiTietGioHangs",
                column: "MaGioHang");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietGioHangs_MaSanPhamChiTiet",
                table: "ChiTietGioHangs",
                column: "MaSanPhamChiTiet");

            migrationBuilder.CreateIndex(
                name: "IX_DiaChiNguoiDungs_MaNguoiDung",
                table: "DiaChiNguoiDungs",
                column: "MaNguoiDung");

            migrationBuilder.CreateIndex(
                name: "IX_DonHangChiTiets_MaDonHang",
                table: "DonHangChiTiets",
                column: "MaDonHang");

            migrationBuilder.CreateIndex(
                name: "IX_DonHangChiTiets_MaSanPhamChiTiet",
                table: "DonHangChiTiets",
                column: "MaSanPhamChiTiet");

            migrationBuilder.CreateIndex(
                name: "IX_DonHangs_MaGiamGiaID",
                table: "DonHangs",
                column: "MaGiamGiaID");

            migrationBuilder.CreateIndex(
                name: "IX_DonHangs_MaKhachHang",
                table: "DonHangs",
                column: "MaKhachHang");

            migrationBuilder.CreateIndex(
                name: "IX_DonHangs_MaKhuyenMai",
                table: "DonHangs",
                column: "MaKhuyenMai");

            migrationBuilder.CreateIndex(
                name: "IX_DonHangs_MaNguoiDung",
                table: "DonHangs",
                column: "MaNguoiDung");

            migrationBuilder.CreateIndex(
                name: "IX_GioHangs_MaNguoiDung",
                table: "GioHangs",
                column: "MaNguoiDung",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NguoiDung_Email",
                table: "NguoiDungs",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NguoiDung_TenDangNhap",
                table: "NguoiDungs",
                column: "TenDangNhap",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NguoiDungs_MaQuyen",
                table: "NguoiDungs",
                column: "MaQuyen");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuChis_MaDonHang",
                table: "PhieuChis",
                column: "MaDonHang");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuChis_MaNguoiDung",
                table: "PhieuChis",
                column: "MaNguoiDung");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuChis_MaYeuCauDoiTra",
                table: "PhieuChis",
                column: "MaYeuCauDoiTra");

            migrationBuilder.CreateIndex(
                name: "IX_SanPhamChiTiets_MaMauSac",
                table: "SanPhamChiTiets",
                column: "MaMauSac");

            migrationBuilder.CreateIndex(
                name: "IX_SanPhamChiTiets_MaSanPham",
                table: "SanPhamChiTiets",
                column: "MaSanPham");

            migrationBuilder.CreateIndex(
                name: "IX_SanPhamChiTiets_MaSize",
                table: "SanPhamChiTiets",
                column: "MaSize");

            migrationBuilder.CreateIndex(
                name: "IX_SanPhams_MaChatLieu",
                table: "SanPhams",
                column: "MaChatLieu");

            migrationBuilder.CreateIndex(
                name: "IX_SanPhams_MaDanhMuc",
                table: "SanPhams",
                column: "MaDanhMuc");

            migrationBuilder.CreateIndex(
                name: "IX_SanPhams_MaXuatXu",
                table: "SanPhams",
                column: "MaXuatXu");

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauDoiTras_MaDonHangChiTiet",
                table: "YeuCauDoiTras",
                column: "MaDonHangChiTiet");

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauDoiTras_MaNguoiDung",
                table: "YeuCauDoiTras",
                column: "MaNguoiDung");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChinhSaches");

            migrationBuilder.DropTable(
                name: "ChiTietGioHangs");

            migrationBuilder.DropTable(
                name: "DiaChiCuaHangs");

            migrationBuilder.DropTable(
                name: "DiaChiNguoiDungs");

            migrationBuilder.DropTable(
                name: "PhieuChis");

            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropTable(
                name: "ThongTinCuaHangs");

            migrationBuilder.DropTable(
                name: "ThongTinGiaoHangs");

            migrationBuilder.DropTable(
                name: "GioHangs");

            migrationBuilder.DropTable(
                name: "YeuCauDoiTras");

            migrationBuilder.DropTable(
                name: "DonHangChiTiets");

            migrationBuilder.DropTable(
                name: "DonHangs");

            migrationBuilder.DropTable(
                name: "SanPhamChiTiets");

            migrationBuilder.DropTable(
                name: "KhuyenMais");

            migrationBuilder.DropTable(
                name: "MaGiamGias");

            migrationBuilder.DropTable(
                name: "NguoiDungs");

            migrationBuilder.DropTable(
                name: "MauSacs");

            migrationBuilder.DropTable(
                name: "SanPhams");

            migrationBuilder.DropTable(
                name: "Sizes");

            migrationBuilder.DropTable(
                name: "Quyens");

            migrationBuilder.DropTable(
                name: "ChatLieus");

            migrationBuilder.DropTable(
                name: "DanhMucs");

            migrationBuilder.DropTable(
                name: "XuatXus");
        }
    }
}
