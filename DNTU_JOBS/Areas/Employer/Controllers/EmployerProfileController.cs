using Microsoft.AspNetCore.Mvc;
using DNTU_JOBS.Data;
using DNTU_JOBS.ViewModel;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using DNTU_JOBS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using DNTU_JOBS.Helper;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace DNTU_JOBS.Areas.Employer.Controllers
{
    [Area("Employer")]
    [Authorize(Roles = "Employer")]
    public class EmployerProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;

        public EmployerProfileController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var employer = await _context.Employers.FirstOrDefaultAsync(e => e.ApplicationUserId == user.Id);

            if (employer == null)
            {
                return NotFound();
            }

            var applicationUser = await _userManager.FindByIdAsync(employer.ApplicationUserId);
            if (applicationUser == null)
            {
                return NotFound();
            }

            // Get the 2FA status for the ApplicationUser
            var isTwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(applicationUser);

            // Pass both employer and 2FA status to the view
            var viewModel = new EmployerProfileViewModel
            {
                Employer = employer,
                IsTwoFactorEnabled = isTwoFactorEnabled
            };

            return View(viewModel);
        }



        [HttpPost]
        public async Task<IActionResult> UpdateProfileDetails(EmployerTable employer)
        {
            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine(error.ErrorMessage);
                }
                return View("Index", employer); // Return view with errors
            }

            var existingEmployer = await _context.Employers.FindAsync(employer.Id);
            if (existingEmployer == null)
            {
                return NotFound();
            }

            // Fetch the associated ApplicationUser using ApplicationUserId
            var applicationUser = await _userManager.FindByIdAsync(existingEmployer.ApplicationUserId);
            if (applicationUser == null)
            {
                return NotFound();
            }

            // Update EmployerTable profile details
            existingEmployer.Name = employer.Name;
            existingEmployer.Email = employer.Email;
            existingEmployer.Phone = employer.Phone;
            existingEmployer.Address = employer.Address;
            existingEmployer.Description = employer.Description;

            // Update ApplicationUser details
            applicationUser.Name = employer.Name;
            applicationUser.Email = employer.Email;
            applicationUser.PhoneNumber = employer.Phone; 
            applicationUser.Address = employer.Address;

            try
            {
                // Update both entities in the context
                _context.Employers.Update(existingEmployer);
                await _userManager.UpdateAsync(applicationUser); 

                await _context.SaveChangesAsync(); 
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving changes: {ex.Message}");
            }

            return RedirectToAction(nameof(Index)); 
        }
        [HttpPost]
        public async Task<IActionResult> UpdateSocialLinks(EmployerTable employer)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(Index)); // Quay lại trang Index nếu model không hợp lệ
            }

            var existingEmployer = await _context.Employers.FindAsync(employer.Id);
            if (existingEmployer == null)
            {
                return NotFound();
            }

            // Cập nhật liên kết mạng xã hội
            existingEmployer.FacebookLink = employer.FacebookLink;
            existingEmployer.TwitterLink = employer.TwitterLink;
            existingEmployer.LinkedInLink = employer.LinkedInLink;
            existingEmployer.InstagramLink = employer.InstagramLink;

            try
            {
                // Lưu thay đổi vào cơ sở dữ liệu
                _context.Employers.Update(existingEmployer);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating social links: {ex.Message}");
            }

            return RedirectToAction(nameof(Index)); // Quay lại trang profile
        }


        // Separate action to upload profile image (POST)
        [HttpPost]
        public async Task<IActionResult> UploadProfileImage(IFormFile uploadedImage)
        {
            var user = await _userManager.GetUserAsync(User);
            var employer = await _context.Employers.FirstOrDefaultAsync(e => e.ApplicationUserId == user.Id);

            if (employer == null)
            {
                return NotFound();
            }

            if (uploadedImage != null && uploadedImage.Length > 0)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await uploadedImage.CopyToAsync(memoryStream);
                    var imageBytes = memoryStream.ToArray();

                    // Update employer's profile photo
                    employer.Photo = imageBytes;

                    // Also update the ApplicationUser profile photo
                    var applicationUser = await _userManager.FindByIdAsync(employer.ApplicationUserId);
                    if (applicationUser != null)
                    {
                        applicationUser.Photo = imageBytes;

                        try
                        {
                            // Save the image to both EmployerTable and ApplicationUser
                            _context.Employers.Update(employer);
                            await _userManager.UpdateAsync(applicationUser); // Update the ApplicationUser using UserManager
                            await _context.SaveChangesAsync(); // Save changes in the context
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error saving image: {ex.Message}");
                        }
                    }
                }

                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, "File upload failed. Please try again.");
            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordForUserViewModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.CurrentPassword) || string.IsNullOrEmpty(model.NewPassword))
            {
                return BadRequest("Thông tin không hợp lệ.");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            // Kiểm tra mật khẩu hiện tại
            var passwordCheck = await _userManager.CheckPasswordAsync(user, model.CurrentPassword);
            if (!passwordCheck)
            {
                return BadRequest("Mật khẩu hiện tại không đúng.");
            }

            // Thay đổi mật khẩu
            var resetResult = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!resetResult.Succeeded)
            {
                var errors = string.Join(", ", resetResult.Errors.Select(e => e.Description));
                return BadRequest(errors);
            }

            return Ok("Mật khẩu đã được cập nhật thành công.");
        }

        [HttpPost]
        public async Task<IActionResult> VerifyTwoFactorAuthentication([FromBody] TwoFactorVerificationRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.VerificationCode))
            {
                return Json(new { success = false, message = "Không tìm thấy mã xác minh. Vui lòng thử lại." });
            }

            var verificationCode = request.VerificationCode;
            Console.WriteLine($"DEBUG: Mã xác minh nhận được từ frontend: '{verificationCode}'");

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var key = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(key))
            {
                return Json(new { success = false, message = "Không tìm thấy mã bí mật. Vui lòng thử lại." });
            }

            if (!TwoFactorAuthHelper.IsValidOtp(key, verificationCode))
            {
                return Json(new { success = false, message = "Mã xác minh không hợp lệ." });
            }

            await _userManager.SetTwoFactorEnabledAsync(user, true);
            return Json(new { success = true, message = "Xác minh thành công. 2FA đã được kích hoạt." });
        }



        public class TwoFactorVerificationRequest
        {
            public string VerificationCode { get; set; }
        }


        [HttpGet]
        public async Task<IActionResult> GetTwoFactorSetup()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var key = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(key))
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);
                key = await _userManager.GetAuthenticatorKeyAsync(user);
            }

            const string issuer = "DNTU_JOBS";
            var otpAuthUrl = $"otpauth://totp/{issuer}:{user.Email}?secret={key}&issuer={issuer}&digits=6";

            var qrCodeImage = TwoFactorAuthHelper.GenerateQrCodeBase64(otpAuthUrl);

            var sharedKey = TwoFactorAuthHelper.FormatKey(key);

            return Json(new { qrCodeUri = qrCodeImage, sharedKey });
        }


        [HttpPost]
        public async Task<IActionResult> DisableTwoFactorAuthentication([FromBody] TwoFactorVerificationRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.VerificationCode))
            {
                return Json(new { success = false, message = "Không tìm thấy mã xác minh. Vui lòng thử lại." });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            // Kiểm tra xem 2FA đã được kích hoạt chưa
            if (!await _userManager.GetTwoFactorEnabledAsync(user))
            {
                return Json(new { success = false, message = "2FA chưa được kích hoạt." });
            }

            // Kiểm tra mã OTP do người dùng nhập
            var key = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(key))
            {
                return Json(new { success = false, message = "Không tìm thấy mã bí mật. Vui lòng thử lại." });
            }

            if (!TwoFactorAuthHelper.IsValidOtp(key, request.VerificationCode))
            {
                return Json(new { success = false, message = "Mã xác minh không hợp lệ." });
            }

            // Hủy kích hoạt 2FA
            await _userManager.SetTwoFactorEnabledAsync(user, false);

            return Json(new { success = true, message = "2FA đã được hủy kích hoạt thành công." });
        }

        [HttpPost]
        public async Task<IActionResult> SendOtpByEmail()
        {
            // Lấy thông tin người dùng hiện tại
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "Người dùng không tồn tại." });
            }

            // Kiểm tra email liên kết với tài khoản
            var email = await _userManager.GetEmailAsync(user);
            if (string.IsNullOrEmpty(email))
            {
                return Json(new { success = false, message = "Không tìm thấy email liên kết với tài khoản này." });
            }

            try
            {
                // Tạo mã OTP
                var token = await _userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);
                if (string.IsNullOrEmpty(token))
                {
                    return Json(new { success = false, message = "Không thể tạo mã OTP. Vui lòng thử lại." });
                }

                // Cấu hình nội dung email
                var subject = "Mã OTP xác thực hai yếu tố";
                var message = $@"
            <html>
            <body>
                <p>Xin chào {user.Name},</p>
                <p>Mã OTP xác thực hai yếu tố của bạn là:</p>
                <h2 style='color: #007bff;'>{token}</h2>
                <p>Vui lòng sử dụng mã này để tiếp tục.</p>
                <p>Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email.</p>
                <p>Trân trọng,<br>DNTU Jobs</p>
            </body>
            </html>";

                // Gửi email
                await _emailSender.SendEmailAsync(email, subject, message);

                // Gửi phản hồi thành công
                return Json(new { success = true, message = "Mã OTP đã được gửi qua email." });
            }
            catch (Exception ex)
            {
                // Ghi log chi tiết lỗi (nếu có logger)
                Console.WriteLine($"Error sending OTP email: {ex.Message}");
                return Json(new { success = false, message = "Lỗi khi gửi email: " + ex.Message });
            }
        }
    }
}
