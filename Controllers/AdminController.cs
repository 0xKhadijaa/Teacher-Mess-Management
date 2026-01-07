using MessManagementSystem.Data;
using MessManagementSystem.Models.Shared;
using MessManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;

namespace MessManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : BaseController
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly BillingService _billingService;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            BillingService billingService) : base(context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _billingService = billingService;
        }

        // Admin dashboard
        public async Task<IActionResult> Dashboard()
        {
            // total teachers
            var teacherRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Teacher");
            var teacherCount = 0;
            if (teacherRole != null)
            {
                teacherCount = await _context.UserRoles.CountAsync(ur => ur.RoleId == teacherRole.Id);
            }

            var totalBills = await _context.Bills.CountAsync();
            var totalRevenue = await _context.Bills.SumAsync(b => (decimal?)b.AmountPaid) ?? 0m;
            var unpaidAmount = await _context.Bills.Where(b => b.AmountPaid < b.TotalAmount).SumAsync(b => (decimal?)(b.TotalAmount - b.AmountPaid)) ?? 0m;
            var openIssues = await _context.BillIssues.CountAsync(i => !i.IsResolved);

            // monthly revenue for last 6 months
            var sixMonths = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-5));
            var monthly = await _context.Bills
                .Where(b => b.BillingPeriodStart >= new DateOnly(sixMonths.Year, sixMonths.Month, 1))
                .GroupBy(b => new { b.BillingPeriodStart.Year, b.BillingPeriodStart.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Revenue = g.Sum(x => x.AmountPaid)
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();

            var vm = new AdminDashboardViewModel
            {
                TeacherCount = teacherCount,
                TotalBills = totalBills,
                TotalRevenue = totalRevenue,
                UnpaidAmount = unpaidAmount,
                OpenIssues = openIssues,
                MonthlyRevenue = monthly.Select(m => new MonthlyRevenue { Year = m.Year, Month = m.Month, Revenue = m.Revenue }).ToList()
            };

            return View(vm);
        }

        // ============= USER MANAGEMENT =============
        public async Task<IActionResult> Users()
        {
            var users = _userManager.Users.ToList();
            var userRoles = new Dictionary<string, IList<string>>();
            foreach (var user in users)
            {
                userRoles[user.Id] = await _userManager.GetRolesAsync(user);
            }
            ViewBag.UserRoles = userRoles;
            return View(users);
        }

        [HttpGet]
        public async Task<IActionResult> CreateUser()
        {
            var roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            ViewBag.Roles = roles;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(string email, string password, string fullName, string department, string[]? roles)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                TempData["Error"] = "Email and password are required.";
                return RedirectToAction(nameof(CreateUser));
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName ?? string.Empty,
                Department = department ?? string.Empty,
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                TempData["Error"] = "Failed to create user: " + string.Join(", ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(CreateUser));
            }

            if (roles != null && roles.Any())
            {
                var addRoleResult = await _userManager.AddToRolesAsync(user, roles);
                if (!addRoleResult.Succeeded)
                {
                    TempData["Error"] = "User created but failed to assign roles: " + string.Join(", ", addRoleResult.Errors.Select(e => e.Description));
                }
            }

            TempData["Success"] = "User created successfully.";
            return RedirectToAction(nameof(Users));
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            var userRoles = await _userManager.GetRolesAsync(user);

            ViewBag.AllRoles = roles;
            ViewBag.UserRoles = userRoles;

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(string id, string fullName, string department, bool isActive, string[]? roles, string? newPassword)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.FullName = fullName ?? user.FullName;
            user.Department = department ?? user.Department;
            user.IsActive = isActive;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                TempData["Error"] = "Failed to update user: " + string.Join(", ", updateResult.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(EditUser), new { id = id });
            }

            // Roles: sync
            var currentRoles = await _userManager.GetRolesAsync(user);
            roles = roles ?? Array.Empty<string>();
            var rolesToAdd = roles.Except(currentRoles).ToArray();
            var rolesToRemove = currentRoles.Except(roles).ToArray();

            if (rolesToAdd.Any())
            {
                var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
                if (!addResult.Succeeded)
                {
                    TempData["Error"] = "Failed to add roles: " + string.Join(", ", addResult.Errors.Select(e => e.Description));
                }
            }
            if (rolesToRemove.Any())
            {
                var remResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                if (!remResult.Succeeded)
                {
                    TempData["Error"] = "Failed to remove roles: " + string.Join(", ", remResult.Errors.Select(e => e.Description));
                }
            }

            // Reset password if provided
            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var pwResult = await _userManager.ResetPasswordAsync(user, token, newPassword);
                if (!pwResult.Succeeded)
                {
                    TempData["Error"] = "Failed to reset password: " + string.Join(", ", pwResult.Errors.Select(e => e.Description));
                }
            }

            TempData["Success"] = "User updated.";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null && currentUser.Id == id)
            {
                TempData["Error"] = "You cannot delete your own account.";
                return RedirectToAction(nameof(Users));
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Users));
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                var msg = "Failed to delete user: " + string.Join(", ", result.Errors.Select(e => e.Description));
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = msg });
                }
                TempData["Error"] = msg;
            }
            else
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "User deleted." });
                }
                TempData["Success"] = "User deleted.";
            }

            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRole(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !await _roleManager.RoleExistsAsync(roleName))
            {
                TempData["Error"] = "Invalid user or role.";
                return RedirectToAction(nameof(Users));
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (!currentRoles.Contains(roleName))
            {
                var result = await _userManager.AddToRoleAsync(user, roleName);
                if (!result.Succeeded)
                {
                    TempData["Error"] = "Failed to assign role: " + string.Join(", ", result.Errors.Select(e => e.Description));
                }
                else
                {
                    TempData["Success"] = $"Role '{roleName}' assigned to {user.Email}.";
                }
            }
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveRole(string userId, string roleName)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Users));
            }

            if (user.Id == currentUser.Id && roleName == "Admin")
            {
                TempData["Error"] = "You cannot remove your own Admin role.";
                return RedirectToAction(nameof(Users));
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Contains(roleName))
            {
                if (currentRoles.Count == 1)
                {
                    TempData["Error"] = "User must have at least one role.";
                    return RedirectToAction(nameof(Users));
                }

                var result = await _userManager.RemoveFromRoleAsync(user, roleName);
                if (!result.Succeeded)
                {
                    TempData["Error"] = "Failed to remove role: " + string.Join(", ", result.Errors.Select(e => e.Description));
                }
                else
                {
                    TempData["Success"] = $"Role '{roleName}' removed from {user.Email}.";
                }
            }
            return RedirectToAction(nameof(Users));
        }

        // ============= BILLING =============
        public async Task<IActionResult> Bills(string? search, string? status, string? month, int page = 1, int pageSize = 10)
        {
            var query = _context.Bills
                .Include(b => b.Student)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(b =>
                    (b.Student != null && (b.Student.FullName.Contains(search) || b.Student.Email.Contains(search))));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status.Equals("paid", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(b => b.AmountPaid >= b.TotalAmount);
                }
                else if (status.Equals("pending", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(b => b.AmountPaid < b.TotalAmount);
                }
            }

            if (!string.IsNullOrWhiteSpace(month) &&
                DateTime.TryParseExact(month + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var monthDate))
            {
                var first = new DateOnly(monthDate.Year, monthDate.Month, 1);
                var last = first.AddMonths(1).AddDays(-1);
                query = query.Where(b => b.BillingPeriodStart >= first && b.BillingPeriodEnd <= last);
            }

            var totalCount = await query.CountAsync();
            var bills = await query
                .OrderByDescending(b => b.BillingPeriodStart)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.Month = month;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;

            return View(bills);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateBills(string month)
        {
            if (string.IsNullOrWhiteSpace(month) || !DateTime.TryParseExact(month + "-01", "yyyy-MM-dd", null, DateTimeStyles.None, out var dt))
            {
                TempData["Error"] = "Invalid month.";
                return RedirectToAction(nameof(Bills));
            }

            try
            {
                var monthDate = DateOnly.FromDateTime(dt);
                await _billingService.GenerateBillsForMonthAsync(monthDate);
                TempData["Success"] = $"Bills generated for {monthDate:MMMM yyyy}!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error generating bills: " + ex.Message;
            }

            return RedirectToAction(nameof(Bills));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsPaid(int id)
        {
            try
            {
                var bill = await _context.Bills
                    .Include(b => b.Student)
                    .FirstOrDefaultAsync(b => b.Id == id);

                if (bill == null)
                {
                    return Json(new { success = false, message = "Bill not found." });
                }

                if (bill.IsPaid) // This still works (computed property)
                {
                    return Json(new { success = false, message = "Bill is already paid." });
                }

                // ✅ JUST UPDATE AmountPaid - IsPaid will auto-compute
                bill.AmountPaid = bill.TotalAmount;
                bill.PaymentDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Bill marked as paid successfully!",
                    billId = id,
                    totalAmount = bill.TotalAmount.ToString("N2")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // ============= BILL ISSUE REVIEW =============
        public async Task<IActionResult> BillIssues()
        {
            var issues = await _context.BillIssues
                .Include(i => i.Teacher)
                .Include(i => i.Bill)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            return View(issues);
        }

        public async Task<IActionResult> ResolveIssue(int id)
        {
            var issue = await _context.BillIssues
                .Include(i => i.Teacher)
                .Include(i => i.Bill)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (issue == null)
            {
                TempData["Error"] = "Issue not found.";
                return RedirectToAction(nameof(BillIssues));
            }

            return View(issue);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveIssue(int id, string resolutionNotes)
        {
            var issue = await _context.BillIssues.FindAsync(id);
            if (issue == null)
            {
                TempData["Error"] = "Issue not found.";
                return RedirectToAction(nameof(BillIssues));
            }

            issue.ResolutionNotes = resolutionNotes;
            issue.IsResolved = true;
            issue.ResolvedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Issue resolved.";
            return RedirectToAction(nameof(BillIssues));
        }

        public async Task<IActionResult> ViewMessage(int id)
        {
            var message = await _context.ContactMessages.FindAsync(id);
            if (message == null) return NotFound();

            // Optional: Mark as read
            if (!message.IsRead)
            {
                message.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return View(message);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ContactMessages()
        {
            var messages = await _context.ContactMessages
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
            return View(messages);
        }
    }
}