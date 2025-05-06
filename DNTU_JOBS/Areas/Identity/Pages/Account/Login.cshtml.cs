// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using DNTU_JOBS.Models;

namespace DNTU_JOBS.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public LoginModel(SignInManager<ApplicationUser> signInManager, ILogger<LoginModel> logger, UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _logger = logger;
            _userManager = userManager;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string ErrorMessage { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }



        //public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        //{
        //    returnUrl ??= Url.Content("~/");

        //    if (ModelState.IsValid)
        //    {
        //        var user = await _userManager.FindByEmailAsync(Input.Email);

        //        if (user == null)
        //        {
        //            ModelState.AddModelError(string.Empty, "Tài khoản không tồn tại.");
        //            return Page();
        //        }

        //        if (!user.IsActive)
        //        {
        //            ModelState.AddModelError(string.Empty, "Tài khoản chưa được kích hoạt. Vui lòng kiểm tra email để xác nhận.");
        //            return Page();
        //        }

        //        // Thử đăng nhập với `UserName`
        //        var result = await _signInManager.PasswordSignInAsync(user.UserName, Input.Password, Input.RememberMe, false);

        //        if (result.Succeeded)
        //        {
        //            _logger.LogInformation("User logged in successfully.");

        //            if (await _userManager.IsInRoleAsync(user, "Admin"))
        //            {
        //                return RedirectToAction("Index", "AdminDashboard", new { area = "Admin" });
        //            }

        //            return LocalRedirect(returnUrl);
        //        }
        //        else if (result.IsLockedOut)
        //        {
        //            _logger.LogWarning("User account locked out.");
        //            ModelState.AddModelError(string.Empty, "Tài khoản đã bị khóa. Vui lòng liên hệ hỗ trợ.");
        //            return Page();
        //        }
        //        else if (result.RequiresTwoFactor)
        //        {
        //            _logger.LogWarning("User requires two-factor authentication.");
        //            return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
        //        }
        //        else
        //        {
        //            _logger.LogWarning("Invalid login attempt.");
        //            ModelState.AddModelError(string.Empty, "Thông tin đăng nhập không chính xác.");
        //            return Page();
        //        }
        //    }

        //    return Page();
        //}



        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);

                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Tài khoản không tồn tại.");
                    return Page();
                }

                if (!user.IsActive)
                {
                    ModelState.AddModelError(string.Empty, "Tài khoản chưa được kích hoạt. Vui lòng kiểm tra email để xác nhận.");
                    return Page();
                }

                var result = await _signInManager.PasswordSignInAsync(user.UserName, Input.Password, Input.RememberMe, false);

                if (result.RequiresTwoFactor)
                {
                    _logger.LogInformation("User requires two-factor authentication.");
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }

                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in successfully.");

                    // Kiểm tra nếu user có quyền Admin
                    var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                    if (isAdmin)
                    {
                        _logger.LogInformation("Redirecting Admin user to Admin Dashboard.");
                        return RedirectToAction("Index", "AdminDashboard", new { area = "Admin" });
                    }

                    return LocalRedirect(returnUrl);
                }
                else if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    ModelState.AddModelError(string.Empty, "Tài khoản đã bị khóa. Vui lòng liên hệ hỗ trợ.");
                    return Page();
                }
                else
                {
                    _logger.LogWarning("Invalid login attempt.");
                    ModelState.AddModelError(string.Empty, "Thông tin đăng nhập không chính xác.");
                    return Page();
                }
            }

            return Page();
        }




    }
}
