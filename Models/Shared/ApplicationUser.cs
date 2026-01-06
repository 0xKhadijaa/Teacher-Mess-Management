using Microsoft.AspNetCore.Identity;

namespace MessManagementSystem.Models.Shared
{
    /// <summary>
    /// Extended IdentityUser with mess-specific properties.
    /// Used for Admin, MessManager, and Teacher roles.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;

        public string Department { get; set; } = "Computer Science";

        /// <summary>
        /// Whether this user is an active mess member (affects billing).
        /// Set to false for leaves, resignations, etc.
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}