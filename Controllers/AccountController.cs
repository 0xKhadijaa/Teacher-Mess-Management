using MessManagementSystem.Models.Account;
using MessManagementSystem.Models.Shared;
using MessManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using System.Threading.Tasks;

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
                // For demo: Still show success to avoid user enumeration
                TempData["Success"] = "If an account exists with this email, you'll receive reset instructions.";
                return RedirectToAction(nameof(Login));
            }

            // Generate reset token
            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            // For demo: Redirect directly to reset page (no email sent)
            return RedirectToAction(nameof(ResetPassword), new { code = encodedCode, email = model.Email });
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string code = null, string email = null)
        {
            if (code == null || email == null)
            {
                TempData["Error"] = "Invalid reset link.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            var model = new ResetPasswordViewModel
            {
                Code = code,
                Email = email
            };
            return View(model);
        }


        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) return RedirectToAction("ResetPasswordConfirmation");

            // 1. Decode the token back to its original form
            var decodedCode = System.Text.Encoding.UTF8.GetString(Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlDecode(model.Code));

            // 2. Perform the reset
            var result = await _userManager.ResetPasswordAsync(user, decodedCode, model.NewPassword);

            if (result.Succeeded)
            {
                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }
    }
   }