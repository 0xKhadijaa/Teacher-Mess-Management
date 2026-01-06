// Models/Bill.cs

using MessManagementSystem.Models.Shared;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MessManagementSystem.Models.Shared
{
    public class Bill
    {
        public int Id { get; set; }

        [Required]
        public string StudentId { get; set; } // Foreign key to ApplicationUser (Student)

        [ForeignKey("StudentId")]
        public ApplicationUser Student { get; set; }

        [Required]
        public DateOnly BillingPeriodStart { get; set; } // e.g., 2025-12-01

        [Required]
        public DateOnly BillingPeriodEnd { get; set; }   // e.g., 2025-12-31

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; } // Total charge for the period

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountPaid { get; set; } // Could be 0 if unpaid

        // New: previous unpaid dues carried forward into this bill
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PreviousDues { get; set; } = 0.00m;

        public bool IsPaid => AmountPaid >= TotalAmount;

        public DateTime? PaymentDate { get; set; } // Null if not paid

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}