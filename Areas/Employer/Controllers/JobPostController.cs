using DNTU_JOBS.Data;
using DNTU_JOBS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using DNTU_JOBS.ViewModel;
using System.Web;
using DNTU_JOBS.Hubs;
using Microsoft.AspNetCore.SignalR;


namespace DNTU_JOBS.Areas.Employer.Controllers
{
    [Area("Employer")]
    [Authorize(Roles = "Employer")]
    public class JobPostController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<NotificationHub> _notificationHubContext;

        public JobPostController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IHubContext<NotificationHub> notificationHubContext)
        {
            _context = context;
            _userManager = userManager;
            _notificationHubContext = notificationHubContext;
        }


        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            // Get the current user's ID
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }
            var employerId = currentUser.Id;

            // Đánh dấu các thông báo liên quan đến công việc của nhà tuyển dụng là "đã đọc"
            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == employerId && !n.IsRead &&
                            (n.Type == "Approve" || n.Type == "Rejected" || n.Type == "ApproveEdit" || n.Type == "RejectEdit"))
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
            }
            await _context.SaveChangesAsync();

            // Fetch jobs posted by the logged-in employer with pagination
            var jobs = await _context.Jobs
                .Where(j => j.ApplicationUserId == employerId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            int totalJobs = await _context.Jobs.CountAsync(j => j.ApplicationUserId == employerId);
            int totalPages = (int)Math.Ceiling(totalJobs / (double)pageSize);

            ViewBag.TotalJobs = totalJobs;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;

            return View(jobs);
        }


        [HttpGet]
        public async Task<IActionResult> GetWardsByDistrict(int districtId)
        {
            var wards = await _context.Wards
                .Where(w => w.DistrictId == districtId)
                .ToListAsync();

            return Json(wards);  // Return the wards in JSON format
        }

        [HttpGet]
        public async Task<IActionResult> PostJob()
        {
            var categories = await _context.JobCategories.ToListAsync();
            var districts = await _context.Districts.ToListAsync(); // Get all districts

            var viewModel = new JobViewModel
            {
                Districts = districts,
                // Populate Wards if needed
            };

            ViewBag.JobCategories = categories;
            return View(viewModel);
        }



        [HttpPost]
        public async Task<IActionResult> PostJob(JobViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var employerId = currentUser.Id;

                var userExists = await _context.Users.AnyAsync(u => u.Id == employerId);
                if (!userExists)
                {
                    ModelState.AddModelError("", "The user does not exist. Please ensure you are logged in with a valid account.");
                    return View(viewModel);
                }

                // Lấy Employer tương ứng với người dùng hiện tại
                var employer = await _context.Employers.FirstOrDefaultAsync(e => e.ApplicationUserId == employerId);
                if (employer == null)
                {
                    ModelState.AddModelError("", "Employer not found. Please ensure you are logged in with the correct account.");
                    return View(viewModel);
                }

                // Xử lý thể loại công việc
                int? jobCategoryId = viewModel.JobCategoryId;
                if (jobCategoryId == -1) // Nếu chọn "Khác"
                {
                    if (string.IsNullOrWhiteSpace(viewModel.OtherCategoryName))
                    {
                        ModelState.AddModelError("OtherCategoryName", "Vui lòng nhập thể loại công việc mới.");
                        return View(viewModel);
                    }

                    // Tạo thể loại mới trong cơ sở dữ liệu
                    var newCategory = new JobCategoryTable
                    {
                        CategoryName = viewModel.OtherCategoryName,
                        Description = viewModel.OtherCategoryName,
                    };
                    _context.JobCategories.Add(newCategory);
                    await _context.SaveChangesAsync();

                    jobCategoryId = newCategory.Id; // Gán ID của thể loại mới
                }

                // Tạo công việc mới và liên kết với Employer
                var job = new JobTable
                {
                    JobTitle = viewModel.JobTitle,
                    JobDescription = HttpUtility.HtmlDecode(viewModel.JobDescription),
                    JobRequirements = HttpUtility.HtmlDecode(viewModel.JobRequirements),
                    Education = HttpUtility.HtmlDecode(viewModel.Education),
                    Benefits = HttpUtility.HtmlDecode(viewModel.Benefits),
                    SalaryMin = viewModel.SalaryMin,
                    SalaryMax = viewModel.SalaryMax,
                    JobCategoryId = jobCategoryId,
                    DistrictId = viewModel.DistrictId,
                    WardId = viewModel.WardId,
                    IsActive = true,
                    Status = ApprovalStatus.Pending,
                    ApplicationUserId = employerId,
                    EmployerId = employer.Id,
                    Address = viewModel.Address,
                    WorkingTime = viewModel.WorkingTime,
                    HiringQuantity = viewModel.HiringQuantity,
                    Gender = viewModel.Gender,
                    UpdatedAt = null
                };

                // Thêm công việc vào cơ sở dữ liệu
                _context.Jobs.Add(job);
                await _context.SaveChangesAsync();

                // Tạo thông báo cho Employer
                var employerNotification = new Notification
                {
                    UserId = employerId,
                    Message = $"Bài đăng công việc '{job.JobTitle}' của bạn đã được gửi và đang chờ phê duyệt.",
                    CreatedAt = DateTime.Now,
                    IsRead = false,
                    Type = "JobPost"
                };
                _context.Notifications.Add(employerNotification);

                // Gửi thông báo qua SignalR cho Employer
                await _notificationHubContext.Clients.User(employerId).SendAsync("ReceiveNotification", employerNotification.Message);

                // Tạo thông báo cho mỗi Admin
                var adminIds = await _context.Admins
                    .Select(a => a.ApplicationUserId)
                    .ToListAsync();

                foreach (var adminId in adminIds)
                {
                    var adminNotification = new Notification
                    {
                        UserId = adminId,
                        Message = $"Một bài đăng công việc mới '{job.JobTitle}' từ nhà tuyển dụng '{employer.Name}' đang chờ phê duyệt.",
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false,
                        Type = "JobPost"
                    };
                    _context.Notifications.Add(adminNotification);

                    // Gửi thông báo qua SignalR cho Admin
                    await _notificationHubContext.Clients.User(adminId).SendAsync("ReceiveNotification", adminNotification.Message);
                }

                await _context.SaveChangesAsync();

                TempData["Message"] = "Your job post has been submitted and is pending approval.";
                return RedirectToAction("Index");
            }

            // Re-populate ViewModel nếu có lỗi
            viewModel.Districts = await _context.Districts.ToListAsync();
            ViewBag.JobCategories = await _context.JobCategories.ToListAsync();
            return View(viewModel);
        }





        [HttpGet]
        public async Task<IActionResult> EditJob(int id)
        {
            var job = await _context.Jobs
                .FirstOrDefaultAsync(j => j.Id == id && j.ApplicationUserId == _userManager.GetUserId(User));
            if (job == null)
            {
                return NotFound();
            }

            var viewModel = new JobViewModel
            {
                JobTitle = job.JobTitle,
                JobRequirements = HttpUtility.HtmlDecode(job.JobRequirements),
                JobDescription = HttpUtility.HtmlDecode(job.JobDescription),
                Education = HttpUtility.HtmlDecode(job.Education),
                Benefits = HttpUtility.HtmlDecode(job.Benefits),              
                IsActive = job.IsActive,
                SalaryMin = job.SalaryMin,
                SalaryMax = job.SalaryMax,
                Address = job.Address,
                WorkingTime = job.WorkingTime,
                HiringQuantity = job.HiringQuantity,
                Gender = job.Gender,
                JobCategoryId = job.JobCategoryId,
                DistrictId = job.DistrictId,
                WardId = job.WardId,
                CreationDate = job.CreationDate,
                Districts = await _context.Districts.ToListAsync(),
                Wards = await _context.Wards
                    .Where(w => w.DistrictId == job.DistrictId)
                    .ToListAsync()
            };

            // Pass Job Categories list to ViewBag
            ViewBag.JobCategories = await _context.JobCategories.ToListAsync();
            return View(viewModel);
        }


        public async Task<IActionResult> JobDetail(int id)
        {
            var job = await _context.Jobs
                .Include(j => j.District)
                .Include(j => j.Ward)
                .Include(j => j.JobCategory)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (job == null)
            {
                return NotFound();
            }

            var jobDetailViewModel = new JobDetailViewModel
            {
                Id = job.Id,
                JobTitle = job.JobTitle,
                JobRequirements = job.JobRequirements,
                JobDescription = job.JobDescription,
                Education = job.Education,
                SalaryMin = job.SalaryMin,
                SalaryMax = job.SalaryMax,
                District = job.District?.Name,
                Ward = job.Ward?.Name,
                Status = job.Status,
                CategoryName = job.JobCategory?.CategoryName,
                Benefits = job.Benefits,
                Address = job.Address,
                WorkingTime = job.WorkingTime,
                HiringQuantity = job.HiringQuantity,
                Gender = job.Gender,
                RejectionReason = job.RejectionReason
            };

            return View(jobDetailViewModel);
        }






        [HttpPost]
        public async Task<IActionResult> EditJob(int id, JobViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                // Lấy công việc cần chỉnh sửa
                var job = await _context.Jobs
                    .FirstOrDefaultAsync(j => j.Id == id && j.ApplicationUserId == _userManager.GetUserId(User));

                if (job == null)
                {
                    return NotFound();
                }

                // Kiểm tra nếu công việc đã ở trạng thái PendingEdit
                if (job.Status == ApprovalStatus.PendingEdit)
                {
                    TempData["Message"] = "Yêu cầu chỉnh sửa công việc này đã được gửi và đang chờ phê duyệt.";
                    return RedirectToAction("Index");
                }

                var jobEditDetails = new List<string>();

                // Helper method to clean strings
                string CleanString(string input) => input?.Trim().Replace("\r\n", "\n").Replace("\n", "").Trim();

                // Xử lý danh mục công việc
                if (viewModel.JobCategoryId == -1) // Nếu chọn "Khác"
                {
                    if (string.IsNullOrWhiteSpace(viewModel.OtherCategoryName))
                    {
                        ModelState.AddModelError("OtherCategoryName", "Vui lòng nhập danh mục công việc mới.");
                        return View(viewModel);
                    }

                    // Tạo danh mục mới trong cơ sở dữ liệu
                    var newCategory = new JobCategoryTable
                    {
                        CategoryName = viewModel.OtherCategoryName
                    };
                    _context.JobCategories.Add(newCategory);
                    await _context.SaveChangesAsync();

                    viewModel.JobCategoryId = newCategory.Id; // Gán ID danh mục mới
                }

                // So sánh các thay đổi và thêm vào jobEditDetails
                if (CleanString(job.JobTitle) != CleanString(viewModel.JobTitle))
                    jobEditDetails.Add($"Tiêu đề: {CleanString(job.JobTitle)} → {CleanString(viewModel.JobTitle)}");
                if (CleanString(job.JobRequirements) != CleanString(viewModel.JobRequirements))
                    jobEditDetails.Add($"Yêu cầu công việc: {CleanString(job.JobRequirements)} → {CleanString(viewModel.JobRequirements)}");
                if (CleanString(job.JobDescription) != CleanString(viewModel.JobDescription))
                    jobEditDetails.Add($"Mô tả công việc: {CleanString(job.JobDescription)} → {CleanString(viewModel.JobDescription)}");
                if (CleanString(job.Education) != CleanString(viewModel.Education))
                    jobEditDetails.Add($"Trình độ học vấn: {CleanString(job.Education)} → {CleanString(viewModel.Education)}");
                if (job.SalaryMin != viewModel.SalaryMin || job.SalaryMax != viewModel.SalaryMax)
                {
                    var oldSalary = $"{(job.SalaryMin.HasValue ? job.SalaryMin.Value.ToString("N0") : "Không xác định")} - {(job.SalaryMax.HasValue ? job.SalaryMax.Value.ToString("N0") : "Không xác định")}";
                    var newSalary = $"{(viewModel.SalaryMin.HasValue ? viewModel.SalaryMin.Value.ToString("N0") : "Không xác định")} - {(viewModel.SalaryMax.HasValue ? viewModel.SalaryMax.Value.ToString("N0") : "Không xác định")}";
                    jobEditDetails.Add($"Mức lương: {oldSalary} → {newSalary}");
                }
                if (CleanString(job.Benefits) != CleanString(viewModel.Benefits))
                    jobEditDetails.Add($"Phúc lợi: {CleanString(job.Benefits)} → {CleanString(viewModel.Benefits)}");
                if (CleanString(job.Address) != CleanString(viewModel.Address))
                    jobEditDetails.Add($"Địa chỉ: {CleanString(job.Address)} → {CleanString(viewModel.Address)}");
                if (CleanString(job.WorkingTime) != CleanString(viewModel.WorkingTime))
                    jobEditDetails.Add($"Thời gian làm việc: {CleanString(job.WorkingTime)} → {CleanString(viewModel.WorkingTime)}");
                if (job.HiringQuantity != viewModel.HiringQuantity)
                    jobEditDetails.Add($"Số lượng tuyển: {job.HiringQuantity} → {viewModel.HiringQuantity}");
                if (CleanString(job.Gender) != CleanString(viewModel.Gender))
                    jobEditDetails.Add($"Giới tính yêu cầu: {CleanString(job.Gender)} → {CleanString(viewModel.Gender)}");
                if (job.JobCategoryId != viewModel.JobCategoryId)
                {
                    var oldCategory = (await _context.JobCategories.FindAsync(job.JobCategoryId))?.CategoryName ?? "Không có";
                    var newCategory = (await _context.JobCategories.FindAsync(viewModel.JobCategoryId))?.CategoryName ?? "Không có";
                    jobEditDetails.Add($"Danh mục công việc: {oldCategory} → {newCategory}");
                }
                if (job.DistrictId != viewModel.DistrictId)
                {
                    var oldDistrict = (await _context.Districts.FindAsync(job.DistrictId))?.Name ?? "Không có";
                    var newDistrict = (await _context.Districts.FindAsync(viewModel.DistrictId))?.Name ?? "Không có";
                    jobEditDetails.Add($"Thành phố / huyện: {oldDistrict} → {newDistrict}");
                }
                if (job.WardId != viewModel.WardId)
                {
                    var oldWard = (await _context.Wards.FindAsync(job.WardId))?.Name ?? "Không có";
                    var newWard = (await _context.Wards.FindAsync(viewModel.WardId))?.Name ?? "Không có";
                    jobEditDetails.Add($"Phường: {oldWard} → {newWard}");
                }



                if (jobEditDetails.Count == 0)
                {
                    TempData["Message"] = "Không có thay đổi nào để yêu cầu sửa.";
                    return RedirectToAction("Index");
                }

                // Cập nhật công việc với dữ liệu từ viewModel
                job.JobTitle = viewModel.JobTitle;
                job.JobRequirements = viewModel.JobRequirements;
                job.JobDescription = viewModel.JobDescription;
                job.Education = viewModel.Education;
                job.SalaryMin = viewModel.SalaryMin;
                job.SalaryMax = viewModel.SalaryMax;
                job.JobCategoryId = viewModel.JobCategoryId;
                job.DistrictId = viewModel.DistrictId;
                job.WardId = viewModel.WardId;
                job.Benefits = viewModel.Benefits;
                job.Address = viewModel.Address;
                job.WorkingTime = viewModel.WorkingTime;
                job.HiringQuantity = viewModel.HiringQuantity;
                job.Gender = viewModel.Gender;

                job.UpdatedAt = DateTime.Now;

                // Chuyển trạng thái thành PendingEdit và lưu chi tiết yêu cầu sửa
                job.Status = ApprovalStatus.PendingEdit;
                job.JobEditRequestDetails = string.Join("\n", jobEditDetails);

                // Thêm thông báo cho người dùng (Employer)
                var notificationForUser = new Notification
                {
                    UserId = job.ApplicationUserId,
                    Message = $"Yêu cầu chỉnh sửa bài đăng công việc '{job.JobTitle}' của bạn đã được gửi và đang chờ phê duyệt.",
                    CreatedAt = DateTime.Now,
                    IsRead = false,
                    Type = "JobEdit"
                };
                _context.Notifications.Add(notificationForUser);

                // Thêm thông báo cho Admin
                var adminIds = await _context.Admins
                    .Select(a => a.ApplicationUserId)
                    .ToListAsync();

                foreach (var adminId in adminIds)
                {
                    var notificationForAdmin = new Notification
                    {
                        UserId = adminId,
                        Message = $"Yêu cầu chỉnh sửa công việc '{job.JobTitle}' đang chờ phê duyệt.",
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false,
                        Type = "JobEdit"
                    };
                    _context.Notifications.Add(notificationForAdmin);
                }

                await _context.SaveChangesAsync();

                TempData["Message"] = "Yêu cầu chỉnh sửa công việc của bạn đã được gửi đi.";
                return RedirectToAction("Index");
            }

            // Cập nhật lại ViewBag nếu có lỗi
            ViewBag.JobCategories = await _context.JobCategories.ToListAsync();
            ViewBag.Districts = await _context.Districts.ToListAsync();
            return View(viewModel);
        }






        [HttpPost]
        public async Task<IActionResult> DeleteJob(int id)
        {
            // Tìm công việc cần xóa
            var job = await _context.Jobs
                .FirstOrDefaultAsync(j => j.Id == id && j.ApplicationUserId == _userManager.GetUserId(User));

            if (job == null)
            {
                return NotFound();
            }

            // Xóa các bản ghi trong bảng FavoriteJobs liên quan đến công việc này
            var favoriteJobs = _context.FavoriteJobs.Where(f => f.JobId == id);
            _context.FavoriteJobs.RemoveRange(favoriteJobs);

            // Xóa công việc
            _context.Jobs.Remove(job);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Job post has been deleted successfully.";
            return RedirectToAction("Index");
        }








    }
}