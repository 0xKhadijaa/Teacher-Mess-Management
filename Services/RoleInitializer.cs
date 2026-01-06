using Microsoft.AspNetCore.Identity;
using MessManagementSystem.Models.Shared;

namespace MessManagementSystem.Services
{
    public class RoleInitializer
    {
        public static async Task InitializeAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            // Step 1: Ensure roles exist
            
            string[] roleNames = { "Admin", "MessManager", "Teacher" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Step 2: Create default MessManager user if doesn't exist
            var messManagerEmail = "messmanager@dept.edu";
            var messManagerUser = await userManager.FindByEmailAsync(messManagerEmail);

            if (messManagerUser == null)
            {
                messManagerUser = new ApplicationUser
                {
                    UserName = messManagerEmail,
                    Email = messManagerEmail,
                    EmailConfirmed = true,
                    FullName = "Default Mess Manager"
                };

                var result = await userManager.CreateAsync(messManagerUser, "Mess@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(messManagerUser, "MessManager");
                }
            }

            // Optional: Create default Admin (uncomment if needed)
            var adminEmail = "admin@dept.edu";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true, FullName = "Admin User" };
                await userManager.CreateAsync(adminUser, "Admin@123");
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
    }
}