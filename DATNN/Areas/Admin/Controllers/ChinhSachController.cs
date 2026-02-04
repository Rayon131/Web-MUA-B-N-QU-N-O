using DATNN.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DATNN.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "admin")]
    public class ChinhSachController : Controller
    {
        private readonly MyDbContext _context;

        public ChinhSachController(MyDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            return View(await _context.ChinhSaches.ToListAsync());
        }
        // GET: Admin/ChinhSach/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var chinhSach = await _context.ChinhSaches.FindAsync(id);
            if (chinhSach == null)
                return NotFound();

            return View(chinhSach);
        }

        // POST: Admin/ChinhSach/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("id,TenChinhSach,NoiDung")] ChinhSach chinhSach)
        {
            if (id != chinhSach.id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(chinhSach);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.ChinhSaches.Any(e => e.id == chinhSach.id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(chinhSach);
        }

      
        // GET: Admin/ChinhSach/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var chinhSach = await _context.ChinhSaches
                .FirstOrDefaultAsync(m => m.id == id);

            if (chinhSach == null)
                return NotFound();

            return View(chinhSach);
        }
    }
}
