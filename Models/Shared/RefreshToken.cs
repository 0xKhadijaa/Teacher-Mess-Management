using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MessManagementSystem.Models.Shared
{
    /// <summary>
    /// Stores refresh tokens for JWT authentication.
    /// Tokens can be revoked and rotated for security.
    /// </summary>
    public class RefreshToken
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }

        [Required]
        [MaxLength(200)]
        public string Token { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// When set, this token is considered invalid even if not expired.
        /// </summary>
        public DateTime? RevokedAt { get; set; }

        [NotMapped]
        public bool IsActive => RevokedAt == null && DateTime.UtcNow <= ExpiresAt;
    }
}

