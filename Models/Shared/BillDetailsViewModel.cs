using System;

namespace MessManagementSystem.Models.Shared
{
    public class BillDetailsViewModel
    {
        public int BillId { get; set; }
        public DateOnly BillingPeriodStart { get; set; }
        public DateOnly BillingPeriodEnd { get; set; }

        // Teacher / user details
        public string TeacherName { get; set; } = string.Empty;
        public string TeacherEmail { get; set; } = string.Empty;
        public string TeacherDepartment { get; set; } = string.Empty;

        // Calculated
        public int TotalMeals { get; set; }
        public decimal MealTotal { get; set; }
        public decimal UtilityCharge { get; set; }
        public decimal PreviousDues { get; set; }

        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public bool IsPaid { get; set; }
        public DateTime? PaymentDate { get; set; }
    }
}
