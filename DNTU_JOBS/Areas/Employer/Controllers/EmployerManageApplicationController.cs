 using DNTU_JOBS.Data;
using DNTU_JOBS.Hubs;
using DNTU_JOBS.Models;
using DNTU_JOBS.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace DNTU_JOBS.Areas.Employer.Controllers
{
    [Area("Employer")]
    [Authorize(Roles = "Employer")]
    public class EmployerManageApplicationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly IHubContext<NotificationHub> _notificationHubContext;

        public EmployerManageApplicationController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IHubContext<ChatHub> hubContext, IHubContext<NotificationHub> notificationHubContext)
        {
            _context = context;
            _userManager = userManager;
            _hubContext = hubContext;
            _notificationHubContext = notificationHubContext;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            const int pageSize = 7; // Số lượng bản ghi mỗi trang

            // Lấy thông tin người dùng hiện tại
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var employerId = user.Id;

            // Lấy tổng số đơn ứng tuyển của công việc thuộc nhà tuyển dụng hiện tại
            var totalApplications = await _context.JobApplications
                .Where(a => a.Job.ApplicationUserId == employerId)
                .CountAsync();

            // Lấy danh sách đơn ứng tuyển (kèm phân trang)
            var applications = await _context.JobApplications
                .Include(a => a.Job)
                .ThenInclude(j => j.ApplicationUser) // Nhà tuyển dụng
                .Include(a => a.ApplicationUser) // Ứng viên
                .Where(a => a.Job.ApplicationUserId == employerId) // Chỉ lấy đơn thuộc employer hiện tại
                .OrderByDescending(a => a.ApplicationDate) // Sắp xếp theo ngày nộp đơn
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Đánh dấu các thông báo liên quan là đã đọc
            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == employerId && !n.IsRead && n.Type == "JobApplication")
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true; // Đánh dấu là đã đọc
            }

            await _context.SaveChangesAsync(); // Lưu thay đổi

            // Tính tổng số trang
            int totalPages = (int)Math.Ceiling(totalApplications / (double)pageSize);

            // Truyền thông tin phân trang vào ViewBag
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;

            // Trả về view với danh sách đơn ứng tuyển
            return View(applications);
        }





















        [HttpPost]
        public async Task<IActionResult> ApproveApplication(int id)
        {
            var application = await _context.JobApplications
                .Include(a => a.Job)
                .Include(a => a.ApplicationUser) // The applicant
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null)
            {
                return NotFound();
            }

            application.Status = ApplicationStatus.Accepted;

            if (application.Job != null && application.Job.HiringQuantity > 0)
            {
                application.Job.HiringQuantity--;

                if (application.Job.HiringQuantity <= 0)
                {
                    application.Job.Status = ApprovalStatus.Filled;
                    application.Job.IsActive = false;
                }
            }


            var sender = await _userManager.GetUserAsync(User);
            if (sender == null)
            {
                return Unauthorized();
            }
            var senderId = sender.Id;

            // Get the applicant (receiver of the message)
            var receiver = application.ApplicationUser;
            if (receiver == null)
            {
                return NotFound("Receiver not found.");
            }
            var receiverId = receiver.Id;

            // Prepare and save a chat message to the applicant
            var contentMessage = $"🎉 Chúc mừng bạn đã được ứng tuyển! 🎊 Hãy liên hệ với tôi để sắp xếp một buổi phỏng vấn. 📞 Số điện thoại của tôi là: {sender.Phone}. 😊";

            var chatMessage = new ChatMessage
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = contentMessage,
                SentAt = DateTime.Now,
                IsRead = false
            };

            _context.ChatMessages.Add(chatMessage);

            // Create a notification for the applicant
            var notificationMessage = $"Đơn ứng tuyển công việc '{application.Job.JobTitle}' đã được chấp nhận!";
            var notification = new Notification
            {
                UserId = receiverId,
                Message = notificationMessage,
                CreatedAt = DateTime.Now,
                IsRead = false,
                Type = "ApplicationApproval"
            };
            _context.Notifications.Add(notification);

            // Save all changes to the database
            await _context.SaveChangesAsync();

            // Send real-time notification via SignalR
            await _hubContext.Clients.User(receiverId).SendAsync("ReceiveMessage", new
            {
                SenderId = senderId,
                Message = chatMessage.Content,
                SentAt = chatMessage.SentAt
            });

            // Send the approval notification via SignalR as well
            await _hubContext.Clients.User(receiverId).SendAsync("ReceiveNotification", new
            {
                Message = notification.Message,
                CreatedAt = notification.CreatedAt
            });

            return RedirectToAction(nameof(Index));
        }

        // Từ chối đơn xin việc
        [HttpPost]
        public async Task<IActionResult> RejectApplication(int id, string reason)
        {
            var application = await _context.JobApplications
                .Include(a => a.Job)
                .Include(a => a.ApplicationUser) // The applicant
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null)
            {
                return NotFound();
            }

            // Cập nhật trạng thái đơn ứng tuyển thành Từ chối và lưu lý do
            application.Status = ApplicationStatus.Rejected;
            application.RejectionReason = reason;

            // Lấy thông tin người gửi (nhà tuyển dụng)
            var sender = await _userManager.GetUserAsync(User);
            if (sender == null)
            {
                return Unauthorized();
            }
            var senderId = sender.Id;

            var receiver = application.ApplicationUser;
            if (receiver == null)
            {
                return NotFound("Không tìm thấy ứng viên.");
            }
            var receiverId = receiver.Id;

            var contentMessage = $"❌ Rất tiếc, đơn ứng tuyển của bạn cho công việc '{application.Job.JobTitle}' đã bị từ chối. 📩 Lý do từ chối: {reason}. Hãy liên hệ với tôi qua số: {sender.Phone} nếu cần thêm thông tin.";

            var chatMessage = new ChatMessage
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = contentMessage,
                SentAt = DateTime.Now,
                IsRead = false
            };

            _context.ChatMessages.Add(chatMessage);

            // Tạo thông báo từ chối và lưu vào cơ sở dữ liệu
            var notificationMessage = $"Đơn ứng tuyển của bạn vào công việc '{application.Job.JobTitle}' đã bị từ chối: {reason}";
            var notification = new Notification
            {
                UserId = receiverId,
                Message = notificationMessage,
                CreatedAt = DateTime.Now,
                IsRead = false,
                Type = "ApplicationRejection"
            };
            _context.Notifications.Add(notification);

            // Lưu tất cả thay đổi vào cơ sở dữ liệu
            await _context.SaveChangesAsync();

            // Gửi tin nhắn qua SignalR cho ứng viên
            await _hubContext.Clients.User(receiverId).SendAsync("ReceiveMessage", new
            {
                SenderId = senderId,
                Message = chatMessage.Content,
                SentAt = chatMessage.SentAt
            });

            // Gửi thông báo từ chối qua SignalR cho ứng viên
            await _hubContext.Clients.User(receiverId).SendAsync("ReceiveNotification", new
            {
                Message = notification.Message,
                CreatedAt = notification.CreatedAt
            });

            return RedirectToAction(nameof(Index));
        }




        [HttpGet]
        [Route("Employer/EmployerManageApplication/GetApplicantDetails")]
        public async Task<IActionResult> GetApplicantDetails(string id)
        {
            // Lấy employer hiện tại
            var employerId = _userManager.GetUserId(User);

            var applicant = await _context.ApplicationUser
                .Where(u => u.Id == id)
                .Select(u => new
                {
                    Name = u.Name,
                    Email = u.Email,
                    DateOfBirth = u.UserJob.DateOfBirth.HasValue ? u.UserJob.DateOfBirth.Value.ToString("dd-MM-yyyy") : null,
                    Nationality = u.UserJob.Nationality,
                    Address = u.Address,
                    Phone = u.Phone,
                    Qualification = u.Qualification,
                    Description = u.Description,
                    FacebookLink = u.FacebookLink,
                    TwitterLink = u.TwitterLink,
                    LinkedInLink = u.LinkedInLink,
                    InstagramLink = u.InstagramLink,
                    // Chuyển đổi ảnh sang Base64 nếu có
                    Photo = u.Photo != null ? Convert.ToBase64String(u.Photo) : null, 
                    Applications = u.Applications
                        .Where(a => a.Job.ApplicationUserId == employerId) 
                        .Select(a => new
                        {
                            JobTitle = a.Job.JobTitle,
                            EmployerName = a.Job.ApplicationUser.Name,
                            Salary = $"{a.Job.SalaryMin} - {a.Job.SalaryMax}",
                            ApplicationDate = a.ApplicationDate.ToString("dd/MM/yyyy"),
                            Status = a.Status.ToString()
                        }).ToList()
                })
                .FirstOrDefaultAsync();

            if (applicant == null)
            {
                return NotFound();
            }

            return Json(applicant);
        }














        [HttpGet]
        public async Task<IActionResult> DownloadCV(int id)
        {
            var application = await _context.JobApplications
                .Include(a => a.ApplicationUser)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null || application.CV == null)
            {
                return NotFound();
            }

            var fileName = $"{application.ApplicationUser.Name}_CV.pdf";

            // Đặt Content-Disposition là inline để mở trực tiếp
            Response.Headers.Add("Content-Disposition", $"inline; filename={fileName}");

            return File(application.CV, "application/pdf");
        }








    }
}
