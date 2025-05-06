using Microsoft.AspNetCore.Mvc;
using DNTU_JOBS.Data;
using DNTU_JOBS.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using DNTU_JOBS.ViewModel;
using System.Security.Claims;
using System.Text;
using System.Drawing.Imaging;
using System.Drawing;
using ZXing.Common;
using ZXing;
using OtpNet;
using DNTU_JOBS.Helper;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace DNTU_JOBS.Areas.User.Controllers
{
    [Area("User")]
    [Authorize(Roles = "User")]
    public class UserProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;

        public UserProfileController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IEmailSender emailSender)
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
        }

        // Display user profile (GET)
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var favoriteJobs = await _context.FavoriteJobs
                .Include(f => f.Job) // Include the Job relationship
                .ThenInclude(j => j.ApplicationUser) // Include the ApplicationUser of the job
                .Where(f => f.ApplicationUserId == user.Id) // Filter by the current user
                .ToListAsync();

            var userProfile = await _context.UserJobs
                .Include(u => u.UserJobCategories)
                .Include(u => u.JobCategory)
                .FirstOrDefaultAsync(u => u.ApplicationUserId == user.Id);


            ViewBag.JobCategories = await _context.JobCategories.ToListAsync();

            if (userProfile == null)
            {
                return NotFound();
            }

            // Populate the UserProfileViewModel
            var model = new UserProfileViewModel
            {
                Id = userProfile.Id,
                Name = userProfile.Name,
                Phone = userProfile.Phone,
                DateOfBirth = userProfile.DateOfBirth,
                Nationality = userProfile.Nationality,
                Email = userProfile.Email,
                Gender = userProfile.Gender,
                Photo = userProfile.Photo,
                Qualification = userProfile.Qualification,
                Address = userProfile.Address,
                AttachCv = userProfile.AttachCv,
                Description = userProfile.Description,
                FacebookLink = userProfile.FacebookLink,
                TwitterLink = userProfile.TwitterLink,
                LinkedInLink = userProfile.LinkedInLink,
                InstagramLink = userProfile.InstagramLink,
                Applications = await _context.JobApplications
                    .Include(a => a.Job)
                    .ThenInclude(j => j.ApplicationUser)
                    .Where(a => a.ApplicationUserId == user.Id)
                    .ToListAsync(),
               FavoriteJobs = favoriteJobs,
                SelectedJobCategoryIds = userProfile.UserJobCategories.Select(ujc => ujc.JobCategoryId).ToList(), // Lấy danh sách các JobCategoryId đã chọn
                JobCategoryNames = userProfile.UserJobCategories
            .Select(ujc => ujc.JobCategory.CategoryName).ToList(),

                IsTwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user),
            };


            return View(model);
        }

        public IActionResult UserProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var applications = _context.JobApplications
                                       .Include(a => a.Job)
                                       .ThenInclude(j => j.ApplicationUser)
                                       .Where(a => a.ApplicationUserId == userId)
                                       .ToList();

            var model = new UserProfileViewModel
            {
                Applications = applications
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetApplicationStatus(int applicationId)
        {
            var application = await _context.JobApplications
                                            .Where(a => a.Id == applicationId)
                                            .FirstOrDefaultAsync();

            if (application == null)
            {
                return NotFound("Đơn ứng tuyển không tồn tại.");
            }

            // Trả về thông tin đơn ứng tuyển (bao gồm trạng thái)
            return Ok(new
            {
                applicationId = application.Id,
                status = application.Status.ToString(),
                jobTitle = application.Job?.JobTitle ?? "N/A",
                applicationDate = application.ApplicationDate.ToString("dd/MM/yyyy")
            });
        }


        [HttpPost]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> CancelApplication(int applicationId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var jobApplication = await _context.JobApplications
                .FirstOrDefaultAsync(a => a.Id == applicationId && a.ApplicationUserId == user.Id);

            if (jobApplication == null)
            {
                return NotFound(); // If the application doesn't exist or doesn't belong to the user
            }

            jobApplication.Status = ApplicationStatus.Cancelled; 

            try
            {
                _context.Update(jobApplication); 
                await _context.SaveChangesAsync();
                TempData["SuccessCancelJobMessage"] = "Đơn ứng tuyển đã bị hủy thành công.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi hủy đơn: " + ex.Message;
            }

            return RedirectToAction("Index"); // Redirect back to the profile or applications page
        }











        [HttpPost]
        public async Task<IActionResult> RemoveFromFavorites([FromBody] int jobId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Unauthorized();
            }

            var favoriteJob = await _context.FavoriteJobs
                .FirstOrDefaultAsync(f => f.JobId == jobId && f.ApplicationUserId == user.Id);

            if (favoriteJob != null)
            {
                _context.FavoriteJobs.Remove(favoriteJob);
                await _context.SaveChangesAsync();
                return Ok();
            }

            return BadRequest("Không tìm thấy công việc đã lưu.");
        }





        [HttpPost]
        public async Task<IActionResult> UpdateProfile(UserTable userProfile)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", userProfile);
            }

            // Tìm UserTable
            var existingUser = await _context.UserJobs
                .Include(u => u.UserJobCategories) // Bao gồm liên kết nhiều-nhiều
                .FirstOrDefaultAsync(u => u.Id == userProfile.Id);

            if (existingUser == null) return NotFound();

            // Tìm ApplicationUser
            var applicationUser = await _userManager.FindByIdAsync(existingUser.ApplicationUserId);
            if (applicationUser == null) return NotFound();

            // Cập nhật thông tin cá nhân
            existingUser.Name = userProfile.Name;
            existingUser.Email = userProfile.Email;
            existingUser.Phone = userProfile.Phone;
            existingUser.Address = userProfile.Address;
            existingUser.Description = userProfile.Description;
            existingUser.Nationality = userProfile.Nationality;
            existingUser.DateOfBirth = userProfile.DateOfBirth;
            existingUser.Gender = userProfile.Gender;
            existingUser.Qualification = userProfile.Qualification;
            existingUser.AttachCv = userProfile.AttachCv;

            // Cập nhật thông tin ApplicationUser
            applicationUser.Name = userProfile.Name;
            applicationUser.Email = userProfile.Email;
            applicationUser.Address = userProfile.Address;
            applicationUser.PhoneNumber = userProfile.Phone;

            // Thêm trạng thái EntityState để theo dõi
            _context.Entry(existingUser).State = EntityState.Modified;

            try
            {
                // Cập nhật cả hai đối tượng
                _context.UserJobs.Update(existingUser);
                await _userManager.UpdateAsync(applicationUser);

                // Lưu thay đổi vào cơ sở dữ liệu
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving changes: {ex.Message}");
                ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật thông tin.");
                return View("Index", userProfile);
            }

            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        public async Task<IActionResult> UpdateUserCategories(int userId, List<int> selectedJobCategoryIds)
        {
            // Lấy thông tin UserTable
            var user = await _context.UserJobs
                .Include(u => u.UserJobCategories)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound("User không tồn tại.");

            // Xóa các danh mục hiện tại
            user.UserJobCategories.Clear();

            // Thêm các danh mục được chọn
            foreach (var categoryId in selectedJobCategoryIds)
            {
                user.UserJobCategories.Add(new UserJobCategory
                {
                    UserTableId = userId,
                    JobCategoryId = categoryId
                });
            }

            // Lưu thay đổi
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }



        [HttpPost]
        public async Task<IActionResult> UpdateSocialLinks(UserTable userProfile)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("Index");
            }

            var existingUser = await _context.UserJobs.FindAsync(userProfile.Id);
            if (existingUser == null)
            {
                return NotFound();
            }

            // Cập nhật các liên kết xã hội
            existingUser.FacebookLink = userProfile.FacebookLink;
            existingUser.TwitterLink = userProfile.TwitterLink;
            existingUser.LinkedInLink = userProfile.LinkedInLink;
            existingUser.InstagramLink = userProfile.InstagramLink;

            // Cập nhật các liên kết xã hội trong ApplicationUser nếu có
            var applicationUser = await _userManager.FindByIdAsync(existingUser.ApplicationUserId);
            if (applicationUser != null)
            {
                applicationUser.FacebookLink = userProfile.FacebookLink;
                applicationUser.TwitterLink = userProfile.TwitterLink;
                applicationUser.LinkedInLink = userProfile.LinkedInLink;
                applicationUser.InstagramLink = userProfile.InstagramLink;
            }

            try
            {
                _context.UserJobs.Update(existingUser);
                if (applicationUser != null)
                {
                    _context.ApplicationUser.Update(applicationUser);  // Cập nhật cả ApplicationUser
                }
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving changes: {ex.Message}");
            }

            return RedirectToAction(nameof(Index));
        }





        public async Task<IActionResult> DownloadCV(int userId)
        {
            var user = await _context.UserJobs.FindAsync(userId);
            if (user?.AttachCv == null)
            {
                return NotFound();
            }

            return File(user.AttachCv, "application/pdf", "CV.pdf");
        }

        



        [HttpPost]
        public async Task<IActionResult> UploadProfileImage(IFormFile uploadedImage)
        {
            var user = await _userManager.GetUserAsync(User);
            var userProfile = await _context.UserJobs.FirstOrDefaultAsync(u => u.ApplicationUserId == user.Id);

            if (userProfile == null)
            {
                return NotFound();
            }

            if (uploadedImage != null && uploadedImage.Length > 0)
            {
                var permittedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var ext = Path.GetExtension(uploadedImage.FileName).ToLowerInvariant();
                if (string.IsNullOrEmpty(ext) || !permittedExtensions.Contains(ext))
                {
                    ModelState.AddModelError(string.Empty, "Invalid file type. Please upload JPG or PNG images.");
                    return RedirectToAction(nameof(Index));
                }

                // Kiểm tra kích thước file
                if (uploadedImage.Length > 5 * 1024 * 1024) 
                {
                    ModelState.AddModelError(string.Empty, "File size exceeds the limit of 5 MB.");
                    return RedirectToAction(nameof(Index));
                }

                using (var memoryStream = new MemoryStream())
                {
                    await uploadedImage.CopyToAsync(memoryStream);
                    var imageBytes = memoryStream.ToArray();

                    userProfile.Photo = imageBytes;

                    var applicationUser = await _userManager.FindByIdAsync(userProfile.ApplicationUserId);
                    if (applicationUser != null)
                    {
                        applicationUser.Photo = imageBytes;

                        try
                        {
                            _context.UserJobs.Update(userProfile);
                            await _userManager.UpdateAsync(applicationUser);
                            await _context.SaveChangesAsync();
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

            var passwordCheck = await _userManager.CheckPasswordAsync(user, model.CurrentPassword);
            if (!passwordCheck)
            {
                return BadRequest("Mật khẩu hiện tại không đúng.");
            }

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

            if (!await _userManager.GetTwoFactorEnabledAsync(user))
            {
                return Json(new { success = false, message = "2FA chưa được kích hoạt." });
            }

            var key = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(key))
            {
                return Json(new { success = false, message = "Không tìm thấy mã bí mật. Vui lòng thử lại." });
            }

            if (!TwoFactorAuthHelper.IsValidOtp(key, request.VerificationCode))
            {
                return Json(new { success = false, message = "Mã xác minh không hợp lệ." });
            }

            await _userManager.SetTwoFactorEnabledAsync(user, false);

            return Json(new { success = true, message = "2FA đã được hủy kích hoạt thành công." });
        }

        [HttpPost]
        public async Task<IActionResult> SendOtpByEmail()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "Người dùng không tồn tại." });
            }

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
