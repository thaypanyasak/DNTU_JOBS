// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using DNTU_JOBS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace DNTU_JOBS.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;

        public ForgotPasswordModel(UserManager<ApplicationUser> userManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
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
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return RedirectToPage("./ForgotPasswordConfirmation");
                }

                // For more information on how to enable account confirmation and password reset please
                // visit https://go.microsoft.com/fwlink/?LinkID=532713
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ResetPassword",
                    pageHandler: null,
                    values: new { area = "Identity", code },
                    protocol: Request.Scheme);

                await SendResetPasswordEmailAsync(user, Input.Email, callbackUrl);


                return RedirectToPage("./ForgotPasswordConfirmation");
            }

            return Page();
        }

        private async Task SendResetPasswordEmailAsync(ApplicationUser user, string email, string callbackUrl)
        {
            // Nội dung email
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
            background-color: #f9f9f9;
        }}
        .email-container {{
            max-width: 600px;
            margin: 30px auto;
            background: #ffffff;
            border-radius: 8px;
            border: 1px solid #e0e0e0;
            box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);
            overflow: hidden;
        }}
        .email-header {{
            background-color: #4caf50;
            color: #ffffff;
            text-align: center;
            padding: 30px 20px;
            font-size: 24px;
            font-weight: 600;
        }}
        .email-body {{
            padding: 30px;
            color: #333333;
            line-height: 1.6;
            font-size: 16px;
        }}
        .email-body p {{
            margin: 0 0 16px;
            color: #555555;
        }}
        .email-body strong {{
            color: #4caf50;
        }}
        .confirm-button {{
            display: inline-block;
            margin: 20px auto;
            padding: 12px 25px;
            font-size: 16px;
            font-weight: 600;
            color: #ffffff;
            background-color: #4caf50;
            text-decoration: none;
            border-radius: 6px;
            text-align: center;
            box-shadow: 0 1px 4px rgba(0, 0, 0, 0.1);
            transition: background-color 0.3s ease;
        }}
        .confirm-button:hover {{
            background-color: #388e3c;
        }}
        .email-footer {{
            text-align: center;
            padding: 15px;
            background-color: #f1f1f1;
            font-size: 14px;
            color: #888888;
        }}
        .email-footer a {{
            color: #4caf50;
            text-decoration: none;
            font-weight: 500;
        }}
    </style>
</head>
<body>
    <div class='email-container'>
        <div class='email-header'>
            Đặt lại mật khẩu
        </div>
        <div class='email-body'>
            <p>Chào <strong>{user.Name}</strong>,</p>
            <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn trên DNTU Jobs. Nhấn vào nút bên dưới để đặt lại mật khẩu:</p>
            <p style='text-align: center;'>
                <a href='{HtmlEncoder.Default.Encode(callbackUrl)}' class='confirm-button' style='color: white;'>Đặt lại mật khẩu</a>
            </p>

            <p>Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này. Liên hệ với chúng tôi nếu bạn cần thêm hỗ trợ.</p>
            <p>Cảm ơn bạn đã lựa chọn DNTU Jobs!</p>
        </div>
        <div class='email-footer'>
            <p>© 2024 DNTU Jobs. Mọi quyền được bảo lưu.</p>
            <p><a href='#'>Chính sách bảo mật</a> | <a href='#'>Điều khoản dịch vụ</a></p>
        </div>
    </div>
</body>
</html>";


            // Gửi email
            await _emailSender.SendEmailAsync(email, "Reset Your Password", emailContent);
        }




    }
}
