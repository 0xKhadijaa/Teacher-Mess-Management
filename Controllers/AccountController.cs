using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MessManagementSystem.Models.Shared;
using MessManagementSystem.Services;
using System.Threading.Tasks;
using MessManagementSystem.Models.Account;

namespace MessManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly JwtTokenService _jwtTokenService;

        public AccountController(UserManager<ApplicationUser> userManager, JwtTokenService jwtTokenService)
        {
            _userManager = userManager;
            _jwtTokenService = jwtTokenService;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, string returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(string.Empty, "Email and password are required.");
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, password))
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View();
            }

            // Issue JWT access + refresh tokens in HttpOnly cookies
            var (accessToken, refreshToken) = await _jwtTokenService.GenerateTokensAsync(user);

            var isSecure = HttpContext.Request.IsHttps;

            Response.Cookies.Append("access_token", accessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = isSecure,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddMinutes(30)
            });

            Response.Cookies.Append("refresh_token", refreshToken.Token, new CookieOptions
            {
                HttpOnly = true,
                Secure = isSecure,
                SameSite = SameSiteMode.Strict,
                Expires = refreshToken.ExpiresAt
            });

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // Best-effort: revoke refresh token if present
            if (Request.Cookies.TryGetValue("refresh_token", out var refreshTokenValue))
            {
                var stored = await _jwtTokenService.GetRefreshTokenAsync(refreshTokenValue);
                if (stored != null)
                {
                    await _jwtTokenService.RevokeRefreshTokenAsync(stored);
                }
            }

            Response.Cookies.Delete("access_token");
            Response.Cookies.Delete("refresh_token");

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string email, string password, string fullName)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(string.Empty, "Email and password are required.");
                return View();
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                EmailConfirmed = true // Skip email confirmation for demo
            };

            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                // Auto-assign Teacher role on registration
                await _userManager.AddToRoleAsync(user, "Teacher");

                // Issue JWT tokens and sign-in via cookies
                var (accessToken, refreshToken) = await _jwtTokenService.GenerateTokensAsync(user);
                var isSecure = HttpContext.Request.IsHttps;

                Response.Cookies.Append("access_token", accessToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = isSecure,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddMinutes(30)
                });

                Response.Cookies.Append("refresh_token", refreshToken.Token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = isSecure,
                    SameSite = SameSiteMode.Strict,
                    Expires = refreshToken.ExpiresAt
                });

                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View();
        }


        // ===== Forgot / Reset Password (no email sending) =====
        // GET: /Account/ForgotPassword
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Don't reveal if email exists - but for demo, show error
                ModelState.AddModelError("Email", "No account found with this email.");
                return View(model);
            }

            // For demo: Store email in TempData and redirect to reset page
            TempData["ResetEmail"] = model.Email;
            return RedirectToAction(nameof(ResetPassword));
        }

        // GET: /Account/ResetPassword
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword()
        {
            var email = TempData["ResetEmail"]?.ToString();
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction(nameof(ForgotPassword));
            }

            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {

            // 🔍 DEBUG: Check if session exists
            var sessionEmail = HttpContext.Session.GetString("ResetEmail");
            System.Diagnostics.Debug.WriteLine($"Session Email: '{sessionEmail}'");
            System.Diagnostics.Debug.WriteLine($"Session Keys: {string.Join(", ", HttpContext.Session.Keys)}");

            if (string.IsNullOrEmpty(sessionEmail))
            {
                System.Diagnostics.Debug.WriteLine("❌ Session email is null - redirecting to ForgotPassword");
                ModelState.AddModelError("", "Session expired. Please try again.");
                return RedirectToAction(nameof(ForgotPassword));
            }


            var email = TempData["ResetEmail"]?.ToString();
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError("", "Session expired. Please try again.");
                return RedirectToAction(nameof(ForgotPassword));
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Email = email;
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid request.");
                return RedirectToAction(nameof(ForgotPassword));
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

            if (result.Succeeded)
            {
                TempData["Success"] = "Your password has been updated successfully!";
                return RedirectToAction(nameof(Login));
            }

            // ✅ CRITICAL: Add Identity errors to ModelState
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            ViewBag.Email = email;
            var errors = result.Errors.Select(e => e.Description).ToList();
            System.Diagnostics.Debug.WriteLine("Password errors: " + string.Join(", ", errors));
            return View(model); // Now shows WHY it failed
        }
    }
}