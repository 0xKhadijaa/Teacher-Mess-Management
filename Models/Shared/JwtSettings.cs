using System;

namespace MessManagementSystem.Models.Shared
{
    /// <summary>
    /// Strongly-typed JWT configuration, bound from appsettings.json ("Jwt" section).
    /// </summary>
    public class JwtSettings
    {
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        // Matches "Jwt:Key" in appsettings.json
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Access token lifetime in minutes.
        /// </summary>
        public int AccessTokenMinutes { get; set; } = 30;

        /// <summary>
        /// Refresh token lifetime in days.
        /// </summary>
        public int RefreshTokenDays { get; set; } = 7;
    }
}

