// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using DNTU_JOBS.Models;

namespace DNTU_JOBS.Areas.Identity.Pages.Account
{
    public class LoginWith2faModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager; // Đổi IdentityUser thành ApplicationUser
        private readonly UserManager<ApplicationUser> _userManager; // Đổi IdentityUser thành ApplicationUser
        private readonly ILogger<LoginWith2faModel> _logger;
        private readonly IEmailSender _emailSender;

        public LoginWith2faModel(
            SignInManager<ApplicationUser> signInManager, // Cập nhật ApplicationUser
            UserManager<ApplicationUser> userManager, // Cập nhật ApplicationUser
            ILogger<LoginWith2faModel> logger,
            IEmailSender emailSender)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public bool RememberMe { get; set; }

        public string ReturnUrl { get; set; }

        public class InputModel
        {
            [Required]
            [StringLength(7, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Text)]
            [Display(Name = "Authenticator code")]
            public string TwoFactorCode { get; set; }

            [Display(Name = "Remember this machine")]
            public bool RememberMachine { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(bool rememberMe, string returnUrl = null)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();

            if (user == null)
            {
                throw new InvalidOperationException($"Unable to load two-factor authentication user.");
            }

            ReturnUrl = returnUrl;
            RememberMe = rememberMe;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(bool rememberMe, string returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            returnUrl = returnUrl ?? Url.Content("~/");

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                throw new InvalidOperationException($"Unable to load two-factor authentication user.");
            }

            var authenticatorCode = Input.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);

            var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(authenticatorCode, rememberMe, Input.RememberMachine);

            if (result.Succeeded)
            {
                _logger.LogInformation("User with ID '{UserId}' logged in with 2fa.", user.Id);
                return LocalRedirect(returnUrl);
            }
            else if (result.IsLockedOut)
            {
                _logger.LogWarning("User with ID '{UserId}' account locked out.", user.Id);
                return RedirectToPage("./Lockout");
            }
            else
            {
                await _userManager.AccessFailedAsync(user);
                var failedCount = await _userManager.GetAccessFailedCountAsync(user);

                if (failedCount >= 5)
                {
                    _logger.LogWarning("User with ID '{UserId}' account locked out after multiple failed attempts.", user.Id);
                    return RedirectToPage("./Lockout");
                }

                _logger.LogWarning("Invalid authenticator code entered for user with ID '{UserId}'.", user.Id);
                ModelState.AddModelError(string.Empty, $"Invalid authenticator code. You have {5 - failedCount} attempts left.");
                return Page();
            }
        }

        public async Task<IActionResult> OnPostSendOtpAsync()
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return new JsonResult(new { success = false, message = "Unable to load two-factor authentication user." });
            }

            var email = await _userManager.GetEmailAsync(user);

            if (string.IsNullOrEmpty(email))
            {
                return new JsonResult(new { success = false, message = "Cannot send OTP because no email is associated with this account." });
            }

            // Tạo mã OTP
            var otp = GenerateOtp();

            // Gửi mã OTP qua email
            await _emailSender.SendEmailAsync(email, "Your OTP Code", $"Your OTP code is: {otp}");

            TempData["Message"] = "A new OTP has been sent to your email.";
            _logger.LogInformation("OTP sent to user with ID '{UserId}'.", user.Id);

            return new JsonResult(new { success = true, message = "OTP has been sent to your email." });
        }


        private string GenerateOtp()
        {
            Random rand = new Random();
            var otp = rand.Next(100000, 999999).ToString(); 
            return otp;
        }


        public async Task<IActionResult> OnPostSend2faCodeAsync()
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                throw new InvalidOperationException($"Unable to load two-factor authentication user.");
            }

            var email = await _userManager.GetEmailAsync(user);

            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError(string.Empty, "Cannot send code because no email is associated with this account.");
                return Page();
            }

            var code = await _userManager.GenerateTwoFactorTokenAsync(user, "Email");
            await _emailSender.SendEmailAsync(email, "Your Two-Factor Authentication Code", $"Your code is: {code}");

            TempData["Message"] = "A new 2FA code has been sent to your email.";
            _logger.LogInformation("2FA code sent to user with ID '{UserId}'.", user.Id);

            return RedirectToPage();
        }
    }

}
