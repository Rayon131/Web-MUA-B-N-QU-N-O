using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DATNN.Areas.NhanVien.Controllers
{
    [Area("NhanVien")]
    [Authorize(Roles = "nhanvien")]
    public class NhanVienDashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
