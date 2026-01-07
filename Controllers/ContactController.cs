using MessManagementSystem.Data;
using MessManagementSystem.Models.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MessManagementSystem.Controllers
{
    public class ContactController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ContactController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ADDED: This is required to load the view initially
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ContactMessage model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    );
                return Json(new { success = false, errors = errors });
            }

            try
            {
                model.CreatedAt = DateTime.UtcNow;
                _context.ContactMessages.Add(model);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Your message has been sent successfully! We'll get back to you soon."
                });
            }
            catch
            {
                return Json(new { success = false, message = "Failed to send message." });
            }
        }
    }
}