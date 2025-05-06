// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using DNTU_JOBS.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using DNTU_JOBS.Data;
using Microsoft.EntityFrameworkCore;

namespace DNTU_JOBS.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ExternalLoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<ExternalLoginModel> _logger;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public ExternalLoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            ILogger<ExternalLoginModel> logger,
            IEmailSender emailSender,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
            
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _logger = logger;
            _emailSender = emailSender;
            _context = context;
            _roleManager = roleManager;
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
        public string ProviderDisplayName { get; set; }

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
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [Display(Name = "Role")]
            public string Role { get; set; }

            public IList<SelectListItem> Roles { get; set; }

            [Required]
            [Display(Name = "Full Name")]
            public string Name { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Confirm Password")]
            [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
            public string ConfirmPassword { get; set; }
        }


        public IActionResult OnGet() => RedirectToPage("./Login");

        public IActionResult OnPost(string provider, string returnUrl = null)
        {
            // Ensure that the redirect URL matches the one registered in Google Developer Console
            var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return new ChallengeResult(provider, properties);
        }



        //public async Task<IActionResult> OnGetCallbackAsync(string returnUrl = null, string remoteError = null)
        //{
        //    returnUrl = returnUrl ?? Url.Content("~/");

        //    if (remoteError != null)
        //    {
        //        ErrorMessage = $"Error from external provider: {remoteError}";
        //        return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
        //    }

        //    var info = await _signInManager.GetExternalLoginInfoAsync();
        //    if (info == null)
        //    {
        //        ErrorMessage = "Error loading external login information.";
        //        return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
        //    }

        //    var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

        //    if (result.Succeeded)
        //    {
        //        var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
        //        if (user != null)
        //        {
        //            var roles = await _userManager.GetRolesAsync(user);
        //            if (!roles.Any())
        //            {
        //                // Người dùng chưa có vai trò, chuyển đến trang cập nhật vai trò
        //                ReturnUrl = returnUrl;
        //                ProviderDisplayName = info.ProviderDisplayName;
        //                Input = new InputModel
        //                {
        //                    Email = info.Principal.FindFirstValue(ClaimTypes.Email),
        //                    Roles = await GetRolesAsync(), // Lấy danh sách vai trò từ hệ thống
        //                                                   // Lấy và gán tên người dùng từ thông tin đăng nhập
        //                    Name = info.Principal.FindFirstValue(ClaimTypes.Name) ?? "Unknown User"
        //                };
        //                return Page();
        //            }
        //        }
        //        return LocalRedirect(returnUrl);
        //    }
        //    else
        //    {
        //        // Nếu người dùng chưa có tài khoản, yêu cầu tạo tài khoản mới
        //        ReturnUrl = returnUrl;
        //        ProviderDisplayName = info.ProviderDisplayName;
        //        Input = new InputModel
        //        {
        //            Email = info.Principal.FindFirstValue(ClaimTypes.Email),
        //            Roles = await GetRolesAsync(), // Lấy danh sách vai trò từ hệ thống
        //                                           // Lấy và gán tên người dùng từ thông tin đăng nhập
        //            Name = info.Principal.FindFirstValue(ClaimTypes.Name) ?? "Unknown User"
        //        };
        //        return Page();
        //    }
        //}

        public async Task<IActionResult> OnGetCallbackAsync(string returnUrl = null, string remoteError = null)
        {
            returnUrl ??= Url.Content("~/");

            if (remoteError != null)
            {
                ErrorMessage = $"Error from external provider: {remoteError}";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Lỗi khi tải thông tin đăng nhập từ provider.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            // Lấy email từ Google thông tin
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(email))
            {
                ErrorMessage = "Không thể lấy email từ tài khoản Google của bạn.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            // Kiểm tra xem email này đã tồn tại hay chưa
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                // Người dùng đã tồn tại, đăng nhập
                if (!existingUser.EmailConfirmed)
                {
                    ErrorMessage = "Email này chưa được xác nhận. Vui lòng kiểm tra email.";
                    return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
                }

                var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
                if (result.Succeeded)
                {
                    return LocalRedirect(returnUrl);
                }
                else if (result.IsLockedOut)
                {
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    ErrorMessage = "Lỗi khi đăng nhập. Vui lòng thử lại.";
                    return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
                }
            }

            // Nếu người dùng chưa tồn tại, chuyển đến trang xác nhận thông tin
            ReturnUrl = returnUrl;
            ProviderDisplayName = info.ProviderDisplayName;
            Input = new InputModel
            {
                Email = email,
                Name = info.Principal.FindFirstValue(ClaimTypes.Name) ?? email.Split('@')[0],
                Roles = await GetRolesAsync()
            };

            return Page();
        }




        private async Task<IList<SelectListItem>> GetRolesAsync()
        {
            // Lấy danh sách vai trò từ RoleManager và lọc chỉ lấy vai trò "Employer" và "User"
            var roles = await _roleManager.Roles
                                          .Where(role => role.Name == "Employer" || role.Name == "User")
                                          .ToListAsync();

            return roles.Select(role => new SelectListItem
            {
                Value = role.Name,
                Text = role.Name
            }).ToList();
        }









        public async Task<IActionResult> OnPostConfirmationAsync(string returnUrl = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Lỗi khi tải thông tin đăng nhập từ provider.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            if (ModelState.IsValid)
            {
                var user = CreateUser();
                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

                // Gán tên từ thông tin Google (hoặc sử dụng tên mặc định)
                user.Name = info.Principal.FindFirstValue(ClaimTypes.Name) ?? "Unknown User";

                // Đặt EmailConfirmed và IsActive
                user.EmailConfirmed = true;
                user.IsActive = true;

                // Tạo tài khoản người dùng với mật khẩu
                var result = await _userManager.CreateAsync(user, Input.Password); 
                if (result.Succeeded)
                {
                    result = await _userManager.AddLoginAsync(user, info);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);

                        // Gán vai trò cho người dùng
                        var roleResult = await _userManager.AddToRoleAsync(user, Input.Role);
                        if (roleResult.Succeeded)
                        {
                            // Xử lý gán ảnh mặc định dựa trên vai trò
                            if (Input.Role == "Employer")
                            {
                                var defaultEmployerImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/logo/EmployerLogo.png");
                                if (System.IO.File.Exists(defaultEmployerImagePath))
                                {
                                    user.Photo = await System.IO.File.ReadAllBytesAsync(defaultEmployerImagePath);
                                }

                                var employer = new EmployerTable
                                {
                                    ApplicationUserId = user.Id,
                                    Name = Input.Name,
                                    Email = Input.Email,
                                    Photo = user.Photo // Gán ảnh mặc định
                                };

                                _context.Employers.Add(employer);
                            }
                            else if (Input.Role == "User")
                            {
                                var defaultUserImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/logo/UserLogo.png");
                                if (System.IO.File.Exists(defaultUserImagePath))
                                {
                                    user.Photo = await System.IO.File.ReadAllBytesAsync(defaultUserImagePath);
                                }

                                var userTable = new UserTable
                                {
                                    ApplicationUserId = user.Id,
                                    Name = Input.Name,
                                    Email = Input.Email,
                                    Photo = user.Photo // Gán ảnh mặc định
                                };

                                _context.UserJobs.Add(userTable);
                            }

                            await _context.SaveChangesAsync();

                            // Đăng nhập người dùng ngay sau khi tạo
                            await _signInManager.SignInAsync(user, isPersistent: false);

                            return LocalRedirect(returnUrl);
                        }
                        else
                        {
                            // Nếu có lỗi khi thêm vai trò
                            foreach (var error in roleResult.Errors)
                            {
                                ModelState.AddModelError(string.Empty, error.Description);
                            }
                        }
                    }
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ProviderDisplayName = info.ProviderDisplayName;
            ReturnUrl = returnUrl;
            return Page();
        }









        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(IdentityUser)}'. " +
                    $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the external login page in /Areas/Identity/Pages/Account/ExternalLogin.cshtml");
            }
        }

        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<ApplicationUser>)_userStore;
        }
    }
}
