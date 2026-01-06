using MessManagementSystem.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace MessManagementSystem.Controllers
{
    public class BaseController : Controller
    {
        protected readonly ApplicationDbContext _context;

        public BaseController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ✅ CORRECT METHOD FOR MVC: OnActionExecuting (synchronous)
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (User?.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                {
                    var unread = _context.Notifications
                        .Count(n => n.UserId == userId && !n.IsRead);
                    ViewBag.UnreadCount = unread;
                }
            }
            base.OnActionExecuting(context);
        }
    }
}