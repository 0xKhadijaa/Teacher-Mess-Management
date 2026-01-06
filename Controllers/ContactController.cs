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
        private readonly UserManager<ApplicationUser> _userManager;

        public ContactController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            if (User.IsInRole("Admin"))
            {
                TempData["Info"] = "Contact messages are available in the Admin area.";
                return RedirectToAction("ContactMessages", "Admin");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ContactMessage model)
        {
            if (User.IsInRole("Admin"))
            {
                TempData["Info"] = "Admins can review contact messages directly from the Admin panel.";
                return RedirectToAction("ContactMessages", "Admin");
            }

            if (!ModelState.IsValid) return View(model);

            // 1. SAVE ContactMessage FIRST → gets an ID
            _context.ContactMessages.Add(model);
            await _context.SaveChangesAsync(); // 👈 NOW model.Id is valid

            // 2. Notify Admin
            var admin = (await _userManager.GetUsersInRoleAsync("Admin")).FirstOrDefault();
            if (admin != null)
            {
                var notif = new Notification
                {
                    UserId = admin.Id,
                    Title = "New Contact Message",
                    Message = $"From: {model.Email}\nSubject: {model.Subject}",
                    ContactMessageId = model.Id, // ✅ SET THIS
                    CreatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(notif);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Message sent! Admin will respond soon.";
            return RedirectToAction(nameof(Index));
        }
    }
}