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
            var requiredKeys = new[] { "MealRate", "UtilityCharge" };

            // Check what exists in DB
            var existingInDb = await _context.AppSettings
                .Where(s => requiredKeys.Contains(s.Key))
                .ToListAsync();

            // Create missing settings IN DATABASE
            bool needsSave = false;
            foreach (var key in requiredKeys)
            {
                if (!existingInDb.Any(s => s.Key == key))
                {
                    var defaultValue = key switch
                    {
                        "MealRate" => "150",
                        "UtilityCharge" => "500",
                        _ => "0"
                    };

                    _context.AppSettings.Add(new AppSetting { Key = key, Value = defaultValue });
                    needsSave = true;
                }
            }

            // Save to DB if we added anything
            if (needsSave)
            {
                await _context.SaveChangesAsync();
                // Refresh the list from DB
                existingInDb = await _context.AppSettings
                    .Where(s => requiredKeys.Contains(s.Key))
                    .ToListAsync();
            }

            return View(existingInDb.OrderBy(s => s.Key).ToList());
        }       // POST: /Settings/Save
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(List<AppSetting> settings)
        {
            if (settings == null)
            {
                TempData["Error"] = "No settings provided.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var setting in settings)
            {
                if (string.IsNullOrWhiteSpace(setting.Key) || string.IsNullOrWhiteSpace(setting.Value))
                    continue;

                // Validate numeric values
                if (setting.Key == "MealRate" || setting.Key == "UtilityCharge")
                {
                    if (!decimal.TryParse(setting.Value, out var numericValue) || numericValue < 0)
                    {
                        TempData["Error"] = $"{setting.Key} must be a valid positive number.";
                        return RedirectToAction(nameof(Index));
                    }
                }

                var existing = await _context.AppSettings
                    .FirstOrDefaultAsync(s => s.Key == setting.Key);

                if (existing != null)
                {
                    existing.Value = setting.Value;
                }
                else
                {
                    _context.AppSettings.Add(new AppSetting
                    {
                        Key = setting.Key,
                        Value = setting.Value
                    });
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Settings updated successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}