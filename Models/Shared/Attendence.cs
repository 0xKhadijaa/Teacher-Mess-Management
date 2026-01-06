using System.ComponentModel.DataAnnotations;
using MessManagementSystem.Models.Shared;

namespace MessManagementSystem.Models.Shared
{
    /// <summary>
    /// Tracks which meals a teacher consumed on a specific date.
    /// Used for accurate per-meal billing (₹200 per meal).
    /// </summary>
    public class Attendance
    {
        public int Id { get; set; }

        /// <summary>
        /// The teacher this attendance record belongs to.
        /// </summary>
        [Required]
        public string TeacherId { get; set; } = string.Empty;

        /// <summary>
        /// Navigation property to the teacher (ApplicationUser).
        /// </summary>
        public ApplicationUser? Teacher { get; set; }

        /// <summary>
        /// The date of the attendance record.
        /// </summary>
        [Required]
        public DateOnly Date { get; set; }
        // Models/Shared/Attendance.cs

        /// <summary>
        /// Whether the teacher had breakfast on this date.
        /// Default: true (auto-generated as present).
        /// </summary>
        public bool HadBreakfast { get; set; } = true;

        /// <summary>
        /// Whether the teacher had lunch on this date.
        /// Default: true.
        /// </summary>
        public bool HadLunch { get; set; } = true;

        /// <summary>
        /// Whether the teacher had dinner on this date.
        /// Default: true.
        /// </summary>
        public bool HadDinner { get; set; } = true;

        /// <summary>
        /// Timestamp when this record was last updated (e.g., when teacher marked absence).
        /// </summary>
        public DateTime? MarkedAt { get; set; }

        /// <summary>
        /// Optional reason for skipping meals (for admin reference).
        /// </summary>
        public string? SkipReason { get; set; }
    }
}