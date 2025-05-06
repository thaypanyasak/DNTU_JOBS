using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using DNTU_JOBS.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using DNTU_JOBS.Data;
using System.Net.Http;
using System.Threading.Tasks;

namespace DNTU_JOBS.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            IWebHostEnvironment env)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _roleManager = roleManager;
            _context = context;
            _env = env;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            // Common Fields
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }

            [Required]
            [Display(Name = "Full Name")]
            public string Name { get; set; }

            //public string? Address { get; set; }

            public string? Phone { get; set; }

            [Required]
            [Display(Name = "Role")]
            public string Role { get; set; }

            // Fields for Company


            // Fields for Employee
            public DateOnly? DateOfBirth { get; set; }
            public string? Nationality { get; set; }


            [Display(Name = "Gender")]
            public Gender? Gender { get; set; }


        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!_roleManager.RoleExistsAsync("Admin").GetAwaiter().GetResult())
            {
                await _roleManager.CreateAsync(new IdentityRole("Admin"));
                await _roleManager.CreateAsync(new IdentityRole("Employer"));
                await _roleManager.CreateAsync(new IdentityRole("User"));
            }

            // Create default admin accounts
            await CreateDefaultAdminAccount("admin@dntujobs.com", "Admin", true, true, true);
            await CreateDefaultAdminAccount("adminaccount@dntujobs.com", "Admin", true, false, false);
            await CreateDefaultAdminAccount("adminjob@dntujobs.com", "Admin", false, true, false);
            await CreateDefaultAdminAccount("adminchat@dntujobs.com", "Admin", false, false, true);

            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        private async Task CreateDefaultAdminAccount(string email, string role, bool canManageAccount, bool canManageJobs, bool canManageChats)
        {
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser == null)
            {
                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    Name = email.Split('@')[0], // Use part of the email as the name
                    IsActive = true
                };

                var result = await _userManager.CreateAsync(user, "kingslove3G!");
                if (result.Succeeded)
                {
                    // Add user to role
                    await _userManager.AddToRoleAsync(user, "Admin");

                    // Assign image path based on the email
                    string imagePath = string.Empty;
                    if (email == "admin@dntujobs.com")
                        imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/logo/AdminLogo.png");
                    else if (email == "adminaccount@dntujobs.com")
                        imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/logo/AdminLogo.png");
                    else if (email == "adminjob@dntujobs.com")
                        imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/logo/AdminLogo.png");
                    else if (email == "adminchat@dntujobs.com")
                        imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/logo/AdminLogo.png");
                    else
                        imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/logo/AdminLogo.png");

                    byte[] photo = null;
                    if (System.IO.File.Exists(imagePath))
                    {
                        photo = await System.IO.File.ReadAllBytesAsync(imagePath);
                        user.Photo = photo; // Assign the photo to ApplicationUser
                    }
                    user.IsActive = true; // Đảm bảo IsActive là true
                    user.EmailConfirmed = true; // Đảm bảo EmailConfirmed là true
                    await _userManager.UpdateAsync(user);
                    // Create AdminTable and add photo to it as well
                    var admin = new AdminTable
                    {
                        ApplicationUserId = user.Id,
                        Name = user.Name,
                        Email = user.Email,
                        Phone = user.Phone,
                        Photo = user.Photo, // Assign the photo to AdminTable as well
                        CanManageAccount = canManageAccount,
                        CanManageJobs = canManageJobs,
                        CanManageChats = canManageChats
                    };

                    _context.Admins.Add(admin);
                    await _context.SaveChangesAsync();


                }
            }
        }



        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                var existingUser = await _userManager.FindByEmailAsync(Input.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError(string.Empty, "Email đã được đăng ký. Vui lòng sử dụng email khác.");
                    return Page(); 
                }

                var user = CreateUser();

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

                user.Name = Input.Name;
                user.Phone = Input.Phone;

                user.IsActive = false;

                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, Input.Role);

                    if (Input.Role == "Admin")
                    {
                        var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/logo/AdminLogo.png");

                        if (System.IO.File.Exists(imagePath))
                        {
                            user.Photo = await System.IO.File.ReadAllBytesAsync(imagePath);
                        }

                        var admin = new AdminTable
                        {
                            ApplicationUserId = user.Id,
                            Name = Input.Name,
                            Email = Input.Email,
                            Phone = Input.Phone,
                            Photo = user.Photo, 
                            CanManageAccount = true,
                            CanManageJobs = true,
                            CanManageChats = true
                        };

                        _context.Admins.Add(admin);
                    }
                    else if (Input.Role == "Employer")
                    {
                        var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/logo/EmployerLogo.png");

                        if (System.IO.File.Exists(imagePath))
                        {
                            user.Photo = await System.IO.File.ReadAllBytesAsync(imagePath);
                        }

                        var company = new EmployerTable
                        {
                            ApplicationUserId = user.Id,
                            Name = Input.Name,
                            Email = Input.Email,
                            Phone = Input.Phone,
                            Photo = user.Photo
                        };

                        _context.Employers.Add(company);
                    }
                    else if (Input.Role == "User")
                    {
                        var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/logo/UserLogo.png");
                        if (System.IO.File.Exists(imagePath))
                        {
                            user.Photo = await System.IO.File.ReadAllBytesAsync(imagePath);
                        }

                        var employee = new UserTable
                        {
                            ApplicationUserId = user.Id,
                            Name = Input.Name,
                            Email = Input.Email,
                            Phone = Input.Phone,
                            Photo = user.Photo 
                        };

                        _context.UserJobs.Add(employee);
                    }

                    await _context.SaveChangesAsync();

                    _logger.LogInformation("User created a new account with password.");

                    await SendEmailConfirmationAsync(user, returnUrl);

                    // Hiển thị thông báo yêu cầu xác nhận
                    return RedirectToPage("RegisterConfirmation", new { email = Input.Email });



                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return Page();
        }


        private async Task SendEmailConfirmationAsync(ApplicationUser user, string returnUrl)
        {
            var userId = await _userManager.GetUserIdAsync(user);
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                protocol: Request.Scheme);

            var emailContent = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        @import url('https://fonts.googleapis.com/css2?family=Manrope:wght@400;600;700&display=swap');

        body {{
            font-family: 'Manrope', Arial, sans-serif;
            margin: 0;
            padding: 0;
            background-color: #eef2f6;
        }}
        .email-container {{
            max-width: 600px;
            margin: 20px auto;
            background: #ffffff;
            border-radius: 12px;
            box-shadow: 0 6px 12px rgba(0, 0, 0, 0.1);
            overflow: hidden;
            border: 1px solid #e5e7eb;
        }}
        .email-header {{
            background-color: #3b82f6;
            color: #ffffff;
            text-align: center;
            padding: 30px 20px;
            font-size: 26px;
            font-weight: 700;
        }}
        .email-body {{
            padding: 30px;
            color: #333333;
            line-height: 1.8;
        }}
        .email-body p {{
            margin: 0 0 15px;
            font-weight: 400;
            font-size: 16px;
            color: #555555;
        }}
        .email-body strong {{
            color: #3b82f6;
        }}
        .confirm-button {{
            display: inline-block;
            margin: 30px auto;
            padding: 12px 28px;
            font-size: 18px;
            font-weight: 600;
            color: #ffffff;
            background-color: #2563eb;
            text-decoration: none;
            border-radius: 8px;
            text-align: center;
        }}
        .confirm-button:hover {{
            background-color: #1e40af;
        }}
        .email-footer {{
            text-align: center;
            padding: 15px;
            background-color: #f9fafb;
            font-size: 14px;
            color: #888888;
        }}
        .email-footer a {{
            color: #3b82f6;
            text-decoration: none;
            font-weight: 500;
        }}
    </style>
</head>
<body>
    <div class='email-container'>
        <div class='email-header'>
            Xác nhận tài khoản
        </div>
        <div class='email-body'>
            <p>Xin chào <strong>{user.Name}</strong>,</p>
            <p>Cảm ơn bạn đã đăng ký tài khoản trên DNTU Jobs. Nhấn vào nút bên dưới để xác nhận tài khoản của bạn:</p>
            <p style='text-align: center;'>
                <a href='{HtmlEncoder.Default.Encode(callbackUrl)}' class='confirm-button' style='color: white;'>Xác nhận tài khoản</a>
            </p>

            <p>Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email. Liên hệ với chúng tôi nếu bạn cần hỗ trợ.</p>
            <p>Chúng tôi rất hân hạnh được phục vụ bạn!</p>
        </div>
        <div class='email-footer'>
            <p>© 2024 DNTU Jobs. Mọi quyền được bảo lưu.</p>
            <p><a href='#'>Chính sách bảo mật</a> | <a href='#'>Điều khoản dịch vụ</a></p>
        </div>
    </div>
</body>
</html>";


            await _emailSender.SendEmailAsync(user.Email, "Xác nhận tài khoản của bạn", emailContent);

        }




        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'. " +
                    $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
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
