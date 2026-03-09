using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using System.Text.Encodings.Web;
using eays.Models;
using eays.Data;
using eays.Services;

namespace eays.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;

        public AccountController(UserManager<ApplicationUser> userManager,
                                 SignInManager<ApplicationUser> signInManager,
                                 AppDbContext context,
                                 IEmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _emailService = emailService;
        }

        // ? USER PROFILE PAGE
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            var userId = user.Id;

            var recentOrders = await _context.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .Take(10)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .ToListAsync();

            var totalOrders = await _context.Orders.CountAsync(o => o.UserId == userId);
            var totalSpent = await _context.Orders
                .Where(o => o.UserId == userId)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
            var wishlistCount = await _context.WishlistItems.CountAsync(w => w.UserId == userId);
            var cartCount = await _context.CartItems.CountAsync(c => c.UserId == userId);

            var vm = new ProfileViewModel
            {
                FullName = user.FullName,
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber ?? "",
                Address = user.Address,
                TotalOrders = totalOrders,
                TotalSpent = totalSpent,
                WishlistCount = wishlistCount,
                CartItemsCount = cartCount,
                RecentOrders = recentOrders
            };

            return View(vm);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            user.Address = model.Address;

            await _userManager.UpdateAsync(user);
            TempData["ProfileMessage"] = "Profile updated successfully!";
            return RedirectToAction("Profile");
        }

        public IActionResult Register()
        {
            return View();
        }

        // ? CHECKOUT PAGE (FIXED)
        [Authorize]
        public async Task<IActionResult> Checkout()
        {
            var userId = _userManager.GetUserId(User);

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            var vm = new CheckoutViewModel
            {
                CartItems = cartItems
            };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = new ApplicationUser
            {
                FullName = model.FullName,
                UserName = model.Email,
                Email = model.Email,
                Address = model.Address
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, false);
                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // ADMIN LOGIN
            if (model.Email == "admin@gmail.com" && model.Password == "123456")
            {
                var user = await _userManager.FindByEmailAsync(model.Email);

                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = model.Email,
                        Email = model.Email,
                        FullName = "Admin"
                    };

                    await _userManager.CreateAsync(user, model.Password);
                }

                await _signInManager.SignInAsync(user, false);
                return RedirectToAction("Users", "Admin");
            }

            // NORMAL USER LOGIN
            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                false,
                false);

            if (result.Succeeded)
                return RedirectToAction("Index", "Home");

            ModelState.AddModelError("", "Invalid login");
            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        // ========== FORGOT PASSWORD =========
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            
            // Don't reveal that the user does not exist
            if (user == null)
            {
                return RedirectToAction("ForgotPasswordConfirmation");
            }

            // Generate password reset token
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            // Create reset password link
            var callbackUrl = Url.Action(
                "ResetPassword",
                "Account",
                new { email = model.Email, token = encodedToken },
                protocol: Request.Scheme);

            // Send email
            var emailBody = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
                        .content {{ background: #f8f9fa; padding: 30px; border-radius: 0 0 10px 10px; }}
                        .button {{ display: inline-block; background: linear-gradient(135deg, #667eea, #764ba2); color: white; padding: 12px 30px; text-decoration: none; border-radius: 8px; margin: 20px 0; font-weight: bold; }}
                        .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>?? EasyCart</h1>
                            <p>Password Reset Request</p>
                        </div>
                        <div class='content'>
                            <h2>Hello, {user.FullName}!</h2>
                            <p>We received a request to reset your password. Click the button below to reset it:</p>
                            <p style='text-align: center;'>
                                <a href='{HtmlEncoder.Default.Encode(callbackUrl)}' class='button'>Reset Password</a>
                            </p>
                            <p>If you didn't request a password reset, you can safely ignore this email.</p>
                            <p><strong>Note:</strong> This link will expire in 1 hour.</p>
                        </div>
                        <div class='footer'>
                            <p>© 2026 EasyCart. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>";

            await _emailService.SendEmailAsync(model.Email, "Reset Your Password - EasyCart", emailBody);

            return RedirectToAction("ForgotPasswordConfirmation");
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        // ========== RESET PASSWORD =========
        [HttpGet]
        public IActionResult ResetPassword(string email, string token)
        {
            if (email == null || token == null)
            {
                return RedirectToAction("Login");
            }

            var model = new ResetPasswordViewModel
            {
                Email = email,
                Token = token
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return RedirectToAction("ResetPasswordConfirmation");
            }

            // Decode token
            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Token));

            var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.Password);

            if (result.Succeeded)
            {
                return RedirectToAction("ResetPasswordConfirmation");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }
    }
}