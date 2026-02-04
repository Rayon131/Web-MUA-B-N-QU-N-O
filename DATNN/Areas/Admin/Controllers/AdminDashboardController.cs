using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DATNN.Areas.Admin.Controllers
{
    
    [Area("Admin")]
    [Authorize(Roles = "admin")]
    public class AdminDashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
