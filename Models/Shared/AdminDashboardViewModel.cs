using System.Collections.Generic;

namespace MessManagementSystem.Models.Shared
{
    public class AdminDashboardViewModel
    {
        public int TeacherCount { get; set; }
        public int TotalBills { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal UnpaidAmount { get; set; }
        public int OpenIssues { get; set; }
        public List<MonthlyRevenue> MonthlyRevenue { get; set; } = new List<MonthlyRevenue>();
    }

    public class MonthlyRevenue
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal Revenue { get; set; }
    }
}
