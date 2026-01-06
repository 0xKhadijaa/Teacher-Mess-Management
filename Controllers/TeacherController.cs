using MessManagementSystem.Data;
using MessManagementSystem.Models.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Mess_Management_System.Constants;

namespace MessManagementSystem.Controllers
{
    [Authorize(Roles = "Teacher,MessManager,Admin")]
    public class TeacherController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager; // 👈 ADDED

        // 👇 UPDATED CONSTRUCTOR
        public TeacherController(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : base(context)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Teacher/Menu
        public async Task<IActionResult> Menu()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var menuItem = await _context.MenuItems
                .FirstOrDefaultAsync(m => m.Date == today);

            if (menuItem == null)
            {
                menuItem = new MenuItem { Date = today };
            }

            return View(menuItem);
        }

        // GET: /Teacher/Attendance
        public async Task<IActionResult> Attendance()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var attendance = await _context.Attendances
                .FirstOrDefaultAsync(a => a.TeacherId == userId && a.Date == today);

            if (attendance == null)
            {
                attendance = new Attendance
                {
                    Date = today,
                    TeacherId = userId,
                    HadBreakfast = true,
                    HadLunch = true,
                    HadDinner = true
                };
            }
            // Determine if editing is allowed
            bool isTeacher = User.IsInRole("Teacher");
            bool before9AM = DateTime.Now.Hour < 9;
            bool canEdit = isTeacher && before9AM;

            ViewBag.CanEdit = canEdit;
            ViewBag.EditCutoffPassed = isTeacher && !before9AM;
            ViewBag.IsSelf = true;
            ViewBag.TeacherName = "You";

            return View(attendance);
        }

        // POST: /Teacher/Attendance
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Attendance(Attendance model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isTeacher = User.IsInRole("Teacher");
            var isAdmin = User.IsInRole("Admin");
            var isBefore9AM = DateTime.Now.Hour < 9;

            if (userId != model.TeacherId)
            {
                TempData["Error"] = "You can only edit your own attendance.";
                return RedirectToAction(nameof(Attendance));
            }

            if (isTeacher && !isBefore9AM)
            {
                TempData["Error"] = "Attendance can only be updated before 9:00 AM.";
                return RedirectToAction(nameof(Attendance));
            }

            if (!isAdmin && !isTeacher)
            {
                TempData["Error"] = "You are not authorized to edit attendance.";
                return RedirectToAction(nameof(Attendance));
            }

            if (!ModelState.IsValid)
            {
                ViewBag.CanEdit = (isTeacher && isBefore9AM) || isAdmin;
                return View(model);
            }

            var today = DateOnly.FromDateTime(DateTime.Today);
            model.Date = today;
            model.MarkedAt = DateTime.UtcNow;
            model.TeacherId = userId;

            var existing = await _context.Attendances
                .FirstOrDefaultAsync(a => a.TeacherId == userId && a.Date == today);

            if (existing != null)
            {
                existing.HadBreakfast = model.HadBreakfast;
                existing.HadLunch = model.HadLunch;
                existing.HadDinner = model.HadDinner;
                existing.SkipReason = model.SkipReason;
                existing.MarkedAt = model.MarkedAt;
            }
            else
            {
                _context.Attendances.Add(model);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Attendance updated successfully!";
            return RedirectToAction(nameof(Attendance));
        }

        // GET: /Teacher/AllAttendance
        [Authorize(Roles = "Admin,MessManager")]
        public async Task<IActionResult> AllAttendance()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            var teacherRole = await _context.Roles.FirstAsync(r => r.Name == "Teacher");
            var teacherUserIds = await _context.UserRoles
                .Where(ur => ur.RoleId == teacherRole.Id)
                .Select(ur => ur.UserId)
                .ToListAsync();

            var teachers = await _context.Users
                .Where(u => teacherUserIds.Contains(u.Id))
                .ToListAsync();

            var attendances = new List<Attendance>();
            foreach (var teacher in teachers)
            {
                var att = await _context.Attendances
                    .FirstOrDefaultAsync(a => a.TeacherId == teacher.Id && a.Date == today);
                if (att == null)
                {
                    att = new Attendance
                    {
                        Date = today,
                        TeacherId = teacher.Id,
                        Teacher = teacher,
                        HadBreakfast = true,
                        HadLunch = true,
                        HadDinner = true
                    };
                }
                else
                {
                    att.Teacher = teacher;
                }
                attendances.Add(att);
            }

            ViewBag.CanEdit = User.IsInRole("Admin");
            return View(attendances);
        }

        // POST: /Teacher/AllAttendance
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AllAttendance(List<Attendance> model)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            foreach (var item in model)
            {
                item.Date = today;
                item.MarkedAt = DateTime.UtcNow;

                var existing = await _context.Attendances
                    .FirstOrDefaultAsync(a => a.TeacherId == item.TeacherId && a.Date == today);

                if (existing != null)
                {
                    existing.HadBreakfast = item.HadBreakfast;
                    existing.HadLunch = item.HadLunch;
                    existing.HadDinner = item.HadDinner;
                    existing.SkipReason = item.SkipReason;
                    existing.MarkedAt = item.MarkedAt;
                }
                else
                {
                    item.Id = 0;
                    _context.Attendances.Add(item);
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "All attendance records updated!";
            return RedirectToAction(nameof(AllAttendance));
        }

        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> MyBills()
        {
            var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var bills = await _context.Bills
                .Where(b => b.StudentId == teacherId)
                .OrderByDescending(b => b.BillingPeriodStart)
                .ToListAsync();

            var teacher = await _context.Users.FirstOrDefaultAsync(u => u.Id == teacherId);

            var vm = new List<BillDetailsViewModel>();
            foreach (var bill in bills)
            {
                var attendances = await _context.Attendances
                    .Where(a => a.TeacherId == teacherId
                                && a.Date >= bill.BillingPeriodStart
                                && a.Date <= bill.BillingPeriodEnd)
                    .ToListAsync();

                int totalMeals = attendances.Sum(a =>
                    (a.HadBreakfast ? 1 : 0) +
                    (a.HadLunch ? 1 : 0) +
                    (a.HadDinner ? 1 : 0));

                var mealTotal = totalMeals * BillingConstants.MealRate;

                vm.Add(new BillDetailsViewModel
                {
                    BillId = bill.Id,
                    BillingPeriodStart = bill.BillingPeriodStart,
                    BillingPeriodEnd = bill.BillingPeriodEnd,
                    TeacherName = teacher?.FullName ?? teacher?.UserName ?? string.Empty,
                    TeacherEmail = teacher?.Email ?? string.Empty,
                    TeacherDepartment = teacher?.Department ?? string.Empty,
                    TotalMeals = totalMeals,
                    MealTotal = mealTotal,
                    UtilityCharge = BillingConstants.UtilityCharge,
                    PreviousDues = bill.PreviousDues,
                    TotalAmount = bill.TotalAmount,
                    AmountPaid = bill.AmountPaid,
                    IsPaid = bill.IsPaid,
                    PaymentDate = bill.PaymentDate
                });
            }

            return View(vm);
        }

        // GET: /Teacher/ReportBillIssue
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> ReportBillIssue(int billId)
        {
            var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var bill = await _context.Bills
                .Include(b => b.Student)
                .FirstOrDefaultAsync(b => b.Id == billId);

            if (bill == null)
            {
                TempData["Error"] = "Bill not found.";
                return RedirectToAction(nameof(MyBills));
            }

            if (bill.StudentId != teacherId)
            {
                TempData["Error"] = "You can only report issues on your own bills.";
                return RedirectToAction(nameof(MyBills));
            }

            ViewBag.Bill = bill;
            var issue = new BillIssue { BillId = billId };
            return View(issue);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> ReportBillIssue(BillIssue model)
        {
            var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!ModelState.IsValid)
            {
                ViewBag.Bill = await _context.Bills.FindAsync(model.BillId);
                return View(model);
            }

            var bill = await _context.Bills.FindAsync(model.BillId);
            if (bill == null || bill.StudentId != teacherId)
            {
                TempData["Error"] = "Invalid bill.";
                return RedirectToAction(nameof(MyBills));
            }

            model.TeacherId = teacherId;
            model.CreatedAt = DateTime.UtcNow;
            model.IsResolved = false;

            _context.BillIssues.Add(model);
            if (!ModelState.IsValid)
            {
                var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                System.Diagnostics.Debug.WriteLine("ModelError: " + errors);
                ViewBag.Bill = await _context.Bills.FindAsync(model.BillId);
                return View(model);
            }
            await _context.SaveChangesAsync();

            // 🔔 DEBUG: Check if Admin exists
            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            var adminId = adminUsers.FirstOrDefault()?.Id;

            if (string.IsNullOrEmpty(adminId))
            {
                // Log to console (visible in VS Output)
                System.Diagnostics.Debug.WriteLine("⚠️ NO ADMIN USER FOUND! Notification not sent.");
                TempData["Warning"] = "Issue saved, but no admin found to notify.";
            }
            else
            {
                var notif = new Notification
                {
                    UserId = adminId,
                    Title = "New bill issue reported",
                    Message = $"{User.Identity?.Name} reported an issue for bill {bill.BillingPeriodStart:MMMM yyyy}.",
                    BillIssueId = model.Id, // 👈 Link to issue
                    CreatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(notif);

                await _context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine($"✅ Notification sent to Admin ID: {adminId}");
            }

            TempData["Success"] = "Issue reported. The admin will review it.";
            return RedirectToAction(nameof(MyBills));
        }
        // GET: /Teacher/DownloadBill
        [Authorize(Roles = "Teacher,MessManager,Admin")]
        public async Task<IActionResult> DownloadBill(int id)
        {
            var bill = await _context.Bills
                .Include(b => b.Student)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (bill == null)
            {
                TempData["Error"] = "Bill not found.";
                return RedirectToAction(nameof(MyBills));
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (User.IsInRole("Teacher") && bill.StudentId != currentUserId)
            {
                TempData["Error"] = "You are not authorized to download this bill.";
                return RedirectToAction(nameof(MyBills));
            }

            var attendances = await _context.Attendances
                .Where(a => a.TeacherId == bill.StudentId
                            && a.Date >= bill.BillingPeriodStart
                            && a.Date <= bill.BillingPeriodEnd)
                .ToListAsync();

            int totalMeals = attendances.Sum(a =>
                (a.HadBreakfast ? 1 : 0) +
                (a.HadLunch ? 1 : 0) +
                (a.HadDinner ? 1 : 0));

            var (mealRate, utilityCharge) = (BillingConstants.MealRate, BillingConstants.UtilityCharge);
            var mealSetting = await _context.AppSettings.FirstOrDefaultAsync(s => s.Key == "MealRate");
            var utilSetting = await _context.AppSettings.FirstOrDefaultAsync(s => s.Key == "UtilityCharge");
            if (mealSetting != null && decimal.TryParse(mealSetting.Value, out var m)) mealRate = m;
            if (utilSetting != null && decimal.TryParse(utilSetting.Value, out var u)) utilityCharge = u;

            decimal mealTotal = totalMeals * mealRate;
            var student = bill.Student ?? await _context.Users.FindAsync(bill.StudentId);

            var viewModel = new BillDetailsViewModel
            {
                BillId = bill.Id,
                BillingPeriodStart = bill.BillingPeriodStart,
                BillingPeriodEnd = bill.BillingPeriodEnd,
                TeacherName = student?.FullName ?? student?.UserName ?? "N/A",
                TeacherEmail = student?.Email ?? string.Empty,
                TeacherDepartment = student?.Department ?? string.Empty,
                TotalMeals = totalMeals,
                MealTotal = mealTotal,
                UtilityCharge = utilityCharge,
                PreviousDues = bill.PreviousDues,
                TotalAmount = bill.TotalAmount,
                AmountPaid = bill.AmountPaid,
                IsPaid = bill.IsPaid,
                PaymentDate = bill.PaymentDate
            };

            return View("PrintableBill", viewModel);
        }

        // ===== Demo Online Payment =====
        [Authorize(Roles = "Teacher")]
        [HttpGet]
        public async Task<IActionResult> PayBillDemo(int id)
        {
            var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var bill = await _context.Bills
                .Include(b => b.Student)
                .FirstOrDefaultAsync(b => b.Id == id && b.StudentId == teacherId);

            if (bill == null)
            {
                TempData["Error"] = "Bill not found.";
                return RedirectToAction(nameof(MyBills));
            }

            if (bill.IsPaid)
            {
                TempData["Info"] = "This bill is already marked as paid.";
                return RedirectToAction(nameof(MyBills));
            }

            return View(bill);
        }

        [Authorize(Roles = "Teacher")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayBillDemoConfirmed(int id)
        {
            var teacherId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var bill = await _context.Bills
                .FirstOrDefaultAsync(b => b.Id == id && b.StudentId == teacherId);

            if (bill == null)
            {
                TempData["Error"] = "Bill not found.";
                return RedirectToAction(nameof(MyBills));
            }

            if (!bill.IsPaid)
            {
                bill.AmountPaid = bill.TotalAmount;
                bill.PaymentDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Demo payment successful. Bill marked as paid.";
            return RedirectToAction(nameof(MyBills));
        }
    }
}