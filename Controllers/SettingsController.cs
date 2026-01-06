using MessManagementSystem.Data;
using MessManagementSystem.Models.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MessManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SettingsController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public SettingsController(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var settings = await _context.AppSettings.ToListAsync();
            return View(settings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(List<AppSetting> model)
        {
            foreach (var item in model)
            {
                var existing = await _context.AppSettings.FindAsync(item.Id);
                if (existing != null)
                {
                    existing.Value = item.Value;
                }
            }
            await _context.SaveChangesAsync();
            TempData["Success"] = "Settings updated.";
            return RedirectToAction(nameof(Index));
        }
    }
}
