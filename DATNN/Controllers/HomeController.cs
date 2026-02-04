using System.Diagnostics;
using DATNN.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DATNN.Controllers
{
    public class HomeController : Controller
    {
         private readonly MyDbContext _context;

        public HomeController(MyDbContext context)
        {
            _context = context;
        }
        // S?a action Index ?? l?y và g?i danh sách s?n ph?m
        public async Task<IActionResult> Index(string keyword)
        {
            // ? Ki?m tra ??ng nh?p
            var username = HttpContext.Session.GetString("UserName");
            if (string.IsNullOrEmpty(username))
            {
                ViewBag.IsLoggedIn = false;
            }
            else
            {
                ViewBag.IsLoggedIn = true;
                ViewBag.UserName = username;
            }

            // ? T?o query ??ng
            var query = _context.SanPhams
                .Where(p => p.TrangThai == 1)
                .Include(p => p.DanhMuc)
                .Include(p => p.SanPhamChiTiets)
                .AsQueryable();

            // ? L?c theo keyword
            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(p => p.TenSanPham.Contains(keyword));
            }

            // ? L?y danh sách (n?u có keyword thì không Take(8))
            var danhSachSanPham = await query
                .OrderByDescending(p => p.ThoiGianTao)
                .Take(8)
                .ToListAsync();

            // ? Gi? l?i keyword ?? hi?n th? l?i trên View
            ViewBag.Keyword = keyword;

            return View(danhSachSanPham);
        }



        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
