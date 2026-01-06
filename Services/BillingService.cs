using Mess_Management_System.Constants;
using MessManagementSystem.Data;
using MessManagementSystem.Models.Shared;
using Microsoft.EntityFrameworkCore;

namespace MessManagementSystem.Services
{
    public class BillingService
    {
        private readonly ApplicationDbContext _context;

        public BillingService(ApplicationDbContext context)
        {
            _context = context;
        }

        private async Task<(decimal MealRate, decimal Utility)> GetRatesAsync()
        {
            var mealSetting = await _context.AppSettings.FirstOrDefaultAsync(s => s.Key == "MealRate");
            var utilSetting = await _context.AppSettings.FirstOrDefaultAsync(s => s.Key == "UtilityCharge");

            decimal mealRate = BillingConstants.MealRate;
            decimal utility = BillingConstants.UtilityCharge;

            if (mealSetting != null && decimal.TryParse(mealSetting.Value, out var m)) mealRate = m;
            if (utilSetting != null && decimal.TryParse(utilSetting.Value, out var u)) utility = u;

            return (mealRate, utility);
        }

        public async Task GenerateBillsForMonthAsync(DateOnly monthDate)
        {
            var firstDay = new DateOnly(monthDate.Year, monthDate.Month, 1);
            var lastDay = firstDay.AddMonths(1).AddDays(-1);

            var (mealRate, utilityCharge) = await GetRatesAsync();

            var teacherRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Teacher");
            if (teacherRole == null)
            {
                throw new InvalidOperationException("No 'Teacher' role found. Make sure roles are initialized.");
            }

            var teacherIds = await _context.UserRoles
                .Where(ur => ur.RoleId == teacherRole.Id)
                .Select(ur => ur.UserId)
                .ToListAsync();

            if (!teacherIds.Any())
            {
                // Nothing to bill
                return;
            }

            foreach (var teacherId in teacherIds)
            {
                var existingBill = await _context.Bills
                    .FirstOrDefaultAsync(b => b.StudentId == teacherId &&
                                             b.BillingPeriodStart == firstDay &&
                                             b.BillingPeriodEnd == lastDay);
                if (existingBill != null) continue;

                // Attendance in month
                var attendances = await _context.Attendances
                    .Where(a => a.TeacherId == teacherId &&
                               a.Date >= firstDay &&
                               a.Date <= lastDay)
                    .ToListAsync();

                int totalMeals = attendances.Sum(a =>
                    (a.HadBreakfast ? 1 : 0) +
                    (a.HadLunch ? 1 : 0) +
                    (a.HadDinner ? 1 : 0));

                decimal mealTotal = totalMeals * mealRate;
                decimal utility = utilityCharge;

                // Carry-forward: sum of unpaid amounts from prior months (do NOT include already paid)
                decimal previousDues = await _context.Bills
                    .Where(b => b.StudentId == teacherId
                                && b.BillingPeriodEnd < firstDay
                                && b.AmountPaid < b.TotalAmount)
                    .SumAsync(b => (b.TotalAmount - b.AmountPaid));

                decimal totalAmount = mealTotal + utility + previousDues;

                var bill = new Bill
                {
                    StudentId = teacherId,
                    BillingPeriodStart = firstDay,
                    BillingPeriodEnd = lastDay,
                    PreviousDues = previousDues,
                    TotalAmount = totalAmount,
                    AmountPaid = 0.00m
                };

                _context.Bills.Add(bill);
            }

            await _context.SaveChangesAsync();
        }
    }
}