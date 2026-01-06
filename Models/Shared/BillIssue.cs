using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MessManagementSystem.Models.Shared
{
    public class BillIssue
    {
        public int Id { get; set; }

        [Required]
        public string TeacherId { get; set; }

        [ForeignKey(nameof(TeacherId))]
        public ApplicationUser? Teacher { get; set; } 

        [Required]
        public int BillId { get; set; }

        [ForeignKey(nameof(BillId))]
        public Bill? Bill { get; set; }

        [Required]
        [StringLength(500)]
        public string IssueDescription { get; set; }

        public bool IsResolved { get; set; } = false;
        public string? ResolutionNotes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }
    }
}