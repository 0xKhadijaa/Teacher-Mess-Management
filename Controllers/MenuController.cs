using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MessManagementSystem.Data;
using MessManagementSystem.Models.Shared;

namespace MessManagementSystem.Controllers
{
    [Authorize(Roles = "MessManager")]
    public class MenuController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public MenuController(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        // GET: /Menu
        public async Task<IActionResult> Index()
        {
            // Get menu for the next 7 days
            var startDate = DateOnly.FromDateTime(DateTime.Today);
            var menuItems = await _context.MenuItems
                .Where(m => m.Date >= startDate && m.Date < startDate.AddDays(7))
                .OrderBy(m => m.Date)
                .ToListAsync();

            // Ensure all 7 days exist (create if missing)
            for (int i = 0; i < 7; i++)
            {
                var date = startDate.AddDays(i);
                if (!menuItems.Any(m => m.Date == date))
                {
                    menuItems.Add(new MenuItem { Date = date });
                }
            }

            menuItems = menuItems.OrderBy(m => m.Date).ToList();
            return View(menuItems);
        }

        // GET: /Menu/Edit
        [HttpGet]
        public async Task<IActionResult> Edit(int id, DateOnly date)
        {
            MenuItem? item = null;

            if (id > 0)
            {
                item = await _context.MenuItems.FindAsync(id);
            }

            if (item == null)
            {
                if (date != default)
                {
                    item = await _context.MenuItems.FirstOrDefaultAsync(m => m.Date == date)
                           ?? new MenuItem { Date = date };
                }
                else
                {
                    // fallback: today's date
                    item = new MenuItem { Date = DateOnly.FromDateTime(DateTime.Today) };
                }
            }

            return View(item);
        }

        // POST: /Menu/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(MenuItem model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.Id > 0)
            {
                var existing = await _context.MenuItems.FindAsync(model.Id);
                if (existing == null)
                {
                    return NotFound();
                }

                existing.Breakfast = model.Breakfast;
                existing.Lunch = model.Lunch;
                existing.Dinner = model.Dinner;
            }
            else
            {
                // If a record for this date already exists, update it instead of creating duplicate
                var existingByDate = await _context.MenuItems.FirstOrDefaultAsync(m => m.Date == model.Date);
                if (existingByDate != null)
                {
                    existingByDate.Breakfast = model.Breakfast;
                    existingByDate.Lunch = model.Lunch;
                    existingByDate.Dinner = model.Dinner;
                }
                else
                {
                    _context.MenuItems.Add(model);
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Menu updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Menu/Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(List<MenuItem> menuItems)
        {
            if (ModelState.IsValid)
            {
                foreach (var item in menuItems)
                {
                    var existing = await _context.MenuItems.FindAsync(item.Id);
                    if (existing != null)
                    {
                        // Update existing
                        existing.Breakfast = item.Breakfast;
                        existing.Lunch = item.Lunch;
                        existing.Dinner = item.Dinner;
                    }
                    else
                    {
                        // New menu item
                        _context.MenuItems.Add(item);
                    }
                }
                await _context.SaveChangesAsync();
                TempData["Success"] = "Menu updated successfully!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}