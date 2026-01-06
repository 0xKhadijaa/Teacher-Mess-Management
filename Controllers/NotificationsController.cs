using MessManagementSystem.Data;
using MessManagementSystem.Models.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MessManagementSystem.Controllers  // ✅ Correct namespace
{
    [Authorize(Roles = "Admin")]
    public class NotificationsController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public NotificationsController(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        // GET: /Notification
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId) // 👈 ONLY current user's notifications
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
            return View(notifications);
        }

        // POST: /Notification/MarkRead
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: /Notification/MarkAllRead
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.IsRead, true));
            return RedirectToAction(nameof(Index));
        }

        //public async Task<IActionResult> Details(int id)
        //{
        //    var notification = await _context.Notifications.FindAsync(id);
        //    if (notification == null || notification.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier))
        //    {
        //        return NotFound();
        //    }

        //    notification.IsRead = true;
        //    await _context.SaveChangesAsync();

        //    // ✅ REDIRECT TO SOURCE BASED ON TYPE
        //    if (notification.BillIssueId.HasValue)
        //    {
        //        return RedirectToAction("ResolveIssue", "Admin", new { id = notification.BillIssueId.Value });
        //    }
        //    if (notification.ContactMessageId.HasValue)
        //    {
        //        return RedirectToAction("ViewMessage", "Admin", new { id = notification.ContactMessageId.Value });
        //    }
        //    if (notification.BillId.HasValue)
        //    {
        //        return RedirectToAction("Bills", "Admin", new { highlightBill = notification.BillId.Value });
        //    }

        //    return RedirectToAction("Index");
        //}
        // GET: /Notification/Details/1
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (notification == null)
            {
                return NotFound();
            }

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            // 🔗 REDIRECT BASED ON NOTIFICATION TYPE
            if (notification.BillIssueId.HasValue)
            {
                return RedirectToAction("ResolveIssue", "Admin", new { id = notification.BillIssueId.Value });
            }
            if (notification.ContactMessageId.HasValue)
            {
                return RedirectToAction("ViewMessage", "Admin", new { id = notification.ContactMessageId.Value });
            }
            if (notification.BillId.HasValue)
            {
                return RedirectToAction("Bills", "Admin");
            }

            return RedirectToAction("Index");
        }
        // POST: /Notification/Delete/5
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Delete(int id)
        //{
        //    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        //    var notification = await _context.Notifications
        //        .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

        //    if (notification != null)
        //    {
        //        _context.Notifications.Remove(notification);
        //        await _context.SaveChangesAsync();
        //    }

        //    return RedirectToAction(nameof(Index));
        //}
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> ClearAll()
        //{
        //    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        //    await _context.Notifications
        //        .Where(n => n.UserId == userId)
        //        .ExecuteDeleteAsync();
        //    return RedirectToAction(nameof(Index));
        //}

        // For Admin: Delete single notification (any user)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteAny(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification != null)
            {
                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // For Admin: Delete multiple notifications
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteSelected(int[] selectedIds)
        {
            if (selectedIds != null && selectedIds.Length > 0)
            {
                var notifications = await _context.Notifications
                    .Where(n => selectedIds.Contains(n.Id))
                    .ToListAsync();
                _context.Notifications.RemoveRange(notifications);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}