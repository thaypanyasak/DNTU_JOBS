using DNTU_JOBS.Data;
using DNTU_JOBS.Helper;
using DNTU_JOBS.Hubs;
using DNTU_JOBS.Models;
using DNTU_JOBS.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DNTU_JOBS.Areas.User.Controllers
{
    [Area("User")]
    public class UserJobController : Controller
    {
        public readonly ApplicationDbContext _context;
        public readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<NotificationHub> _notificationHubContext;


        public UserJobController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IHubContext<NotificationHub> notificationHubContext)
        {
            _context = context;
            _userManager = userManager;
            _notificationHubContext = notificationHubContext;
        }
        public async Task<IActionResult> Index(int page = 1, int pageSize = 5, int? categoryId = null)
        {
            // Get all categories for the dropdown
            //var categories = await _context.JobCategories.ToListAsync();
            var categories = await _context.JobCategories
            .Where(category => _context.Jobs.Any(job => job.JobCategoryId == category.Id
                                                   && (job.Status == ApprovalStatus.Approved || job.Status == ApprovalStatus.ApprovedEdit || job.Status == ApprovalStatus.Working)))
            .ToListAsync();

            ViewBag.JobCategories = categories;
            //ViewBag.Districts = await _context.Districts.ToListAsync();
            //ViewBag.JobCategories = await _context.JobCategories.ToListAsync();

            var districts = await _context.Districts
        .Where(district => _context.Jobs.Any(job => job.DistrictId == district.Id
                                                && (job.Status == ApprovalStatus.Approved || job.Status == ApprovalStatus.ApprovedEdit || job.Status == ApprovalStatus.Working)))
        .ToListAsync();

            ViewBag.Districts = districts;

            // Initialize job query
            var jobQuery = _context.Jobs
                .Include(job => job.ApplicationUser)
                .Where(job => (job.Status == ApprovalStatus.Approved || job.Status == ApprovalStatus.ApprovedEdit || job.Status == ApprovalStatus.Working)
                              && job.HiringQuantity > 0); // Only jobs with hiring quantity > 0

            // If a category is selected, filter jobs by category
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                jobQuery = jobQuery.Where(job => job.JobCategoryId == categoryId.Value);
                ViewBag.SelectedCategoryId = categoryId.Value; // Store the selected category to highlight in the dropdown
            }
            else
            {
                ViewBag.SelectedCategoryId = null;
            }

            // Get the list of approved jobs
            var approvedJobs = await jobQuery.ToListAsync();

            // Count the number of approved jobs
            var approvedJobCount = await jobQuery.CountAsync();

            // Prepare job view models for the approved jobs
            var jobViewModels = approvedJobs.Select(job => new UserJobViewModel
            {
                Id = job.Id,
                JobTitle = job.JobTitle,
                DistrictName = GetDistrictName(job.DistrictId),
                WardName = GetWardName(job.WardId),
                SalaryMin = job.SalaryMin,
                SalaryMax = job.SalaryMax,
                CreationDate = job.CreationDate,
                CompanyName = job.ApplicationUser.Name,
                EmployerId = job.ApplicationUser.Id,
                CompanyLogo = job.ApplicationUser.Photo
            }).ToList();

            // Get the list of jobs with status PendingEdit
            var pendingEditJobs = await _context.Jobs
                .Where(job => job.Status == ApprovalStatus.PendingEdit)
                .Include(job => job.ApplicationUser)
                .ToListAsync();

            // Check each PendingEdit job and add its original approved job if found
            foreach (var pendingJob in pendingEditJobs)
            {
                var originalJob = await _context.Jobs
                    .Include(j => j.ApplicationUser)
                    .FirstOrDefaultAsync(j => j.ApplicationUserId == pendingJob.ApplicationUserId
                                               && j.Status == ApprovalStatus.Approved
                                               && j.JobTitle == pendingJob.JobTitle);

                if (originalJob != null)
                {
                    jobViewModels.Add(new UserJobViewModel
                    {
                        Id = originalJob.Id,
                        JobTitle = originalJob.JobTitle,
                        DistrictName = GetDistrictName(originalJob.DistrictId),
                        WardName = GetWardName(originalJob.WardId),
                        SalaryMin = originalJob.SalaryMin,
                        SalaryMax = originalJob.SalaryMax,
                        CreationDate = originalJob.CreationDate,
                        CompanyName = originalJob.ApplicationUser.Name,
                        EmployerId = originalJob.ApplicationUser.Id,
                        CompanyLogo = originalJob.ApplicationUser.Photo
                    });
                }
            }

            // Pagination Logic
            var totalJobs = jobViewModels.Count;
            var totalPages = (int)Math.Ceiling(totalJobs / (double)pageSize);
            var paginatedJobs = jobViewModels.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            // Pass pagination information and job count to ViewBag
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;
            ViewBag.ApprovedJobCount = approvedJobCount;

            return View(paginatedJobs);
        }

        


        [Area("User")]
        [HttpGet]
        public async Task<IActionResult> FilterJobsByDistrict(string district)
        {
            try
            {
                if (string.IsNullOrEmpty(district))
                {
                    // Return all jobs if no district is provided, with the status filter applied
                    var allJobs = await _context.Jobs
                        .Include(j => j.District)
                        .Include(j => j.ApplicationUser)
                        .Where(job => job.Status == ApprovalStatus.Approved || job.Status == ApprovalStatus.ApprovedEdit || job.Status == ApprovalStatus.Working) // Filter by status
                        .Select(job => new
                        {
                            id = job.Id,
                            jobTitle = job.JobTitle,
                            companyName = job.ApplicationUser.Name,
                            districtName = job.District.Name,
                            companyLogo = job.ApplicationUser.Photo != null
                                ? $"data:image/png;base64,{Convert.ToBase64String(job.ApplicationUser.Photo)}"
                                : "/img/logo/DefaultLogo.png",
                            salaryMin = job.SalaryMin,
                            salaryMax = job.SalaryMax,
                            creationDateRelativeTime = job.CreationDate.ToRelativeTime(),
                            postedBy = job.ApplicationUser.Name,
                            wardName = job.Ward.Name,
                        })
                        .ToListAsync();

                    return Json(allJobs);
                }

                // Filter jobs by district and status
                var filteredJobs = await _context.Jobs
                    .Include(j => j.District)
                    .Include(j => j.ApplicationUser)
                    .Where(j => j.District.Name == district &&
                                (j.Status == ApprovalStatus.Approved || j.Status == ApprovalStatus.ApprovedEdit || j.Status == ApprovalStatus.Working)) // Filter by district and status
                    .Select(job => new
                    {
                        id = job.Id,
                        jobTitle = job.JobTitle,
                        companyName = job.ApplicationUser.Name,
                        districtName = job.District.Name,
                        companyLogo = job.ApplicationUser.Photo != null
                            ? $"data:image/png;base64,{Convert.ToBase64String(job.ApplicationUser.Photo)}"
                            : "/img/logo/DefaultLogo.png",
                        salaryMin = job.SalaryMin,
                        salaryMax = job.SalaryMax,
                        creationDateRelativeTime = job.CreationDate.ToRelativeTime(),
                        postedBy = job.ApplicationUser.Name,
                        wardName = job.Ward.Name,
                    })
                    .ToListAsync();

                return Json(filteredJobs);
            }
            catch (Exception ex)
            {
                // Log the error and return a failure response
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error.", details = ex.Message });
            }
        }



        [HttpGet]
        public async Task<IActionResult> FilterJobsByCategory(string category)
        {
            try
            {
                if (string.IsNullOrEmpty(category))
                {
                    // Return all jobs that have 'Approved' or 'ApprovedEdit' status if no category is provided
                    var allJobs = await _context.Jobs
                        .Include(j => j.JobCategory)
                        .Include(j => j.ApplicationUser)
                        .Where(job => job.Status == ApprovalStatus.Approved || job.Status == ApprovalStatus.ApprovedEdit || job.Status == ApprovalStatus.Working)  // Filtering by status
                        .Select(job => new
                        {
                            id = job.Id,
                            jobTitle = job.JobTitle,
                            companyName = job.ApplicationUser.Name,
                            categoryName = job.JobCategory.CategoryName,
                            companyLogo = job.ApplicationUser.Photo != null
                                ? $"data:image/png;base64,{Convert.ToBase64String(job.ApplicationUser.Photo)}"
                                : "/img/logo/DefaultLogo.png",
                            salaryMin = job.SalaryMin,
                            salaryMax = job.SalaryMax,
                            creationDateRelativeTime = job.CreationDate.ToRelativeTime()
                        })
                        .ToListAsync();

                    return Json(allJobs);
                }

                // Filter by category and status
                var filteredJobs = await _context.Jobs
                    .Include(j => j.JobCategory)
                    .Include(j => j.ApplicationUser)
                    .Where(j => j.JobCategory.CategoryName == category &&
                                (j.Status == ApprovalStatus.Approved || j.Status == ApprovalStatus.ApprovedEdit || j.Status == ApprovalStatus.Working))  // Filter by status and category
                    .Select(job => new
                    {
                        id = job.Id,
                        jobTitle = job.JobTitle,
                        companyName = job.ApplicationUser.Name,
                        categoryName = job.JobCategory.CategoryName,
                        companyLogo = job.ApplicationUser.Photo != null
                            ? $"data:image/png;base64,{Convert.ToBase64String(job.ApplicationUser.Photo)}"
                            : "/img/logo/DefaultLogo.png",
                        salaryMin = job.SalaryMin,
                        salaryMax = job.SalaryMax,
                        creationDateRelativeTime = job.CreationDate.ToRelativeTime()
                    })
                    .ToListAsync();

                return Json(filteredJobs);
            }
            catch (Exception ex)
            {
                // Log the error and return a failure response
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error.", details = ex.Message });
            }
        }



        [HttpGet]
        public async Task<IActionResult> FilterJobsByWorkingTime(string workingTime)
        {
            try
            {
                if (string.IsNullOrEmpty(workingTime) || workingTime.Trim().ToLower() == "tất cả ca")
                {
                    var allJobs = await _context.Jobs
                        .Include(j => j.ApplicationUser)
                        .Where(job => job.Status == ApprovalStatus.Approved || job.Status == ApprovalStatus.ApprovedEdit || job.Status == ApprovalStatus.Working) // Filter by Approved or ApprovedEdit status
                        .Select(job => new
                        {
                            id = job.Id,
                            jobTitle = job.JobTitle,
                            companyName = job.ApplicationUser.Name,
                            workingTime = job.WorkingTime,
                            companyLogo = job.ApplicationUser.Photo != null
                                ? $"data:image/png;base64,{Convert.ToBase64String(job.ApplicationUser.Photo)}"
                                : "/img/logo/DefaultLogo.png",
                            creationDateRelativeTime = job.CreationDate.ToRelativeTime(),
                            salaryMin = job.SalaryMin,
                            salaryMax = job.SalaryMax,
                        })
                        .ToListAsync();

                    return Json(allJobs);
                }

                // Extract the working time range
                var timeRange = ExtractTimeRange(workingTime);
                if (string.IsNullOrEmpty(timeRange))
                {
                    return Json(new { error = "Invalid time range format." });
                }

                // Filter jobs by working time and approval status
                var filteredJobs = await _context.Jobs
                    .Include(j => j.ApplicationUser)
                    .Where(job => (job.Status == ApprovalStatus.Approved || job.Status == ApprovalStatus.ApprovedEdit || job.Status == ApprovalStatus.Working) && job.WorkingTime.Contains(timeRange)) // Filter by status and working time
                    .Select(job => new
                    {
                        id = job.Id,
                        jobTitle = job.JobTitle,
                        companyName = job.ApplicationUser.Name,
                        workingTime = job.WorkingTime,
                        companyLogo = job.ApplicationUser.Photo != null
                            ? $"data:image/png;base64,{Convert.ToBase64String(job.ApplicationUser.Photo)}"
                            : "/img/logo/DefaultLogo.png",
                        creationDateRelativeTime = job.CreationDate.ToRelativeTime(),
                        salaryMin = job.SalaryMin,
                        salaryMax = job.SalaryMax,
                    })
                    .ToListAsync();

                return Json(filteredJobs);
            }
            catch (Exception ex)
            {
                // Log or return the error
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error.", details = ex.Message });
            }
        }


        private string ExtractTimeRange(string workingTime)
        {
            var match = System.Text.RegularExpressions.Regex.Match(workingTime, @"\d{1,2}:\d{2}\s*-\s*\d{1,2}:\d{2}");
            return match.Success ? match.Value.Trim() : null;
        }


        [HttpGet]
        public async Task<IActionResult> FilterJobsByPostedWithin(string days)
        {
            try
            {
                var jobQuery = _context.Jobs
                    .Include(j => j.ApplicationUser)
                    .Where(job => job.Status == ApprovalStatus.Approved || job.Status == ApprovalStatus.ApprovedEdit || job.Status == ApprovalStatus.Working);

                // Xử lý theo giá trị của days
                if (!string.IsNullOrEmpty(days) && days != "any")
                {
                    if (days == "1") 
                    {
                        var todayStart = DateTime.Now.Date; 
                        var todayEnd = DateTime.Now.Date.AddDays(1); 
                        jobQuery = jobQuery.Where(job => job.CreationDate >= todayStart && job.CreationDate < todayEnd);
                    }
                    else if (int.TryParse(days, out int daysValue) && daysValue > 1)
                    {
                        var fromDate = DateTime.Now.Date.AddDays(-daysValue); // Ngày bắt đầu
                        var toDate = DateTime.Now.Date; // 00:00 hôm nay (không bao gồm hôm nay)
                        jobQuery = jobQuery.Where(job => job.CreationDate >= fromDate && job.CreationDate < toDate);
                    }
                }

                var filteredJobs = await jobQuery
                    .Select(job => new
                    {
                        id = job.Id,
                        jobTitle = job.JobTitle,
                        companyName = job.ApplicationUser.Name,
                        workingTime = job.WorkingTime,
                        companyLogo = job.ApplicationUser.Photo != null
                            ? $"data:image/png;base64,{Convert.ToBase64String(job.ApplicationUser.Photo)}"
                            : "/img/logo/DefaultLogo.png",
                        creationDateRelativeTime = job.CreationDate.ToRelativeTime(),
                        salaryMin = job.SalaryMin,
                        salaryMax = job.SalaryMax,
                        districtName = job.District.Name
                    })
                    .ToListAsync();

                return Json(filteredJobs);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error.", details = ex.Message });
            }
        }
        [HttpGet]
        public async Task<IActionResult> FilterJobsBySalary(int minSalary, int maxSalary)
        {
            try
            {
                var jobs = await _context.Jobs
                    .Include(j => j.ApplicationUser)
                    .Where(job => job.SalaryMin >= minSalary && job.SalaryMax <= maxSalary)
                    .Where(job => job.Status == ApprovalStatus.Approved || job.Status == ApprovalStatus.ApprovedEdit || job.Status == ApprovalStatus.Working)  // Correct enum comparison
                    .Select(job => new
                    {
                        id = job.Id,
                        jobTitle = job.JobTitle,
                        companyName = job.ApplicationUser.Name,
                        workingTime = job.WorkingTime,
                        companyLogo = job.ApplicationUser.Photo != null
                            ? $"data:image/png;base64,{Convert.ToBase64String(job.ApplicationUser.Photo)}"
                            : "/img/logo/DefaultLogo.png",
                        creationDateRelativeTime = job.CreationDate.ToRelativeTime(),
                        salaryMin = job.SalaryMin,
                        salaryMax = job.SalaryMax,
                        districtName = job.District.Name,
                        wardName = job.Ward.Name,
                        categoryName = job.JobCategory.CategoryName
                    })
                    .ToListAsync();

                return Json(jobs);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error.", details = ex.Message });
            }
        }





        [HttpGet]
        public async Task<IActionResult> SearchJobsByName(string jobName)
        {
            if (string.IsNullOrWhiteSpace(jobName))
            {
                // Trả về tất cả công việc nếu không có từ khóa tìm kiếm
                var allJobs = await _context.Jobs
                    .Include(j => j.ApplicationUser)
                    .Where(j => j.Status == ApprovalStatus.Approved || j.Status == ApprovalStatus.ApprovedEdit || j.Status == ApprovalStatus.Working)
                    .Select(job => new
                    {
                        id = job.Id,
                        jobTitle = job.JobTitle,
                        companyName = job.ApplicationUser.Name,
                        salaryMin = job.SalaryMin,
                        salaryMax = job.SalaryMax,
                        districtName = job.District.Name,
                        categoryName = job.JobCategory.CategoryName,
                        wardName = job.Ward.Name,
                        creationDateRelativeTime = job.CreationDate.ToRelativeTime(),
                        companyLogo = job.ApplicationUser.Photo != null
                            ? $"data:image/png;base64,{Convert.ToBase64String(job.ApplicationUser.Photo)}"
                            : "/img/logo/DefaultLogo.png"
                    })
                    .ToListAsync();

                return Json(allJobs);
            }

            // Tìm kiếm công việc theo tên
            var filteredJobs = await _context.Jobs
                .Include(j => j.ApplicationUser)
                .Where(j => (j.Status == ApprovalStatus.Approved || j.Status == ApprovalStatus.ApprovedEdit || j.Status == ApprovalStatus.Working)
                            && j.JobTitle.Contains(jobName))
                .Select(job => new
                {
                    id = job.Id,
                    jobTitle = job.JobTitle,
                    companyName = job.ApplicationUser.Name,
                    salaryMin = job.SalaryMin,
                    salaryMax = job.SalaryMax,
                    districtName = job.District.Name,
                    categoryName = job.JobCategory.CategoryName,
                    wardName = job.Ward.Name,
                    creationDateRelativeTime = job.CreationDate.ToRelativeTime(),
                    companyLogo = job.ApplicationUser.Photo != null
                        ? $"data:image/png;base64,{Convert.ToBase64String(job.ApplicationUser.Photo)}"
                        : "/img/logo/DefaultLogo.png"
                })
                .ToListAsync();

            return Json(filteredJobs);
        }





        [HttpGet]
        public async Task<IActionResult> GetEmployerInfo(string employerId)
        {
            if (string.IsNullOrEmpty(employerId))
            {
                return Json(new { error = "Invalid employer ID." });
            }

            var employer = await _context.Employers
                .Include(e => e.Jobs) // Bao gồm các công việc
                .FirstOrDefaultAsync(e => e.ApplicationUserId == employerId);

            if (employer == null)
            {
                return Json(new { error = "Employer not found." });
            }

            // Lọc các công việc đã được duyệt
            var approvedJobs = employer.Jobs
                .Where(job => job.Status == ApprovalStatus.Approved || job.Status == ApprovalStatus.ApprovedEdit || job.Status == ApprovalStatus.Working)
                .Select(job => new
                {
                    id = job.Id,
                    jobTitle = job.JobTitle,
                    salaryMin = job.SalaryMin,
                    salaryMax = job.SalaryMax,
                    workingTime = job.WorkingTime,
                    address = job.Address
                })
                .ToList();

            return Json(new
            {
                name = employer.Name,
                description = employer.Description,
                phone = employer.Phone,
                email = employer.Email,
                address = employer.Address,
                photo = employer.Photo != null ? Convert.ToBase64String(employer.Photo) : null,
                facebookLink = employer.FacebookLink,
                twitterLink = employer.TwitterLink,
                linkedInLink = employer.LinkedInLink,
                instagramLink = employer.InstagramLink,
                jobs = approvedJobs
            });
        }























        //private string GetCompanyName(int? companyId)
        //{
        //    if (!companyId.HasValue) return string.Empty; // Handle null case
        //    var company = _context.Employers.FirstOrDefault(c => c.Id == companyId.Value);
        //    return company?.Name ?? string.Empty;
        //}



        public async Task<IActionResult> JobDetail(int id)
        {
            // Lấy thông tin công việc từ cơ sở dữ liệu
            var job = await _context.Jobs
                .Include(j => j.District)
                .Include(j => j.Ward)
                .Include(j => j.ApplicationUser)
                .ThenInclude(u => u.Employer)
                .Include(j => j.JobCategory)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (job == null)
            {
                return NotFound();
            }

            // Lấy thông tin người dùng hiện tại
            var user = await _userManager.GetUserAsync(User);

            // Khai báo các biến kiểm tra trạng thái
            var hasApplied = false;
            var hasCancelled = false;
            bool hasFavorite = false;

            if (user != null)
            {
                var application = await _context.JobApplications
                    .FirstOrDefaultAsync(app => app.JobId == id && app.ApplicationUserId == user.Id);

                if (application != null)
                {
                    hasApplied = application.Status != ApplicationStatus.Cancelled;
                    hasCancelled = application.Status == ApplicationStatus.Cancelled;
                }

                hasFavorite = await _context.FavoriteJobs
                    .AnyAsync(f => f.JobId == id && f.ApplicationUserId == user.Id);
            }

            var employerDescription = job.ApplicationUser?.Employer?.Description ?? "No description available";

            var relatedJobs = await _context.Jobs
                .Where(j => j.ApplicationUserId == job.ApplicationUserId && j.Id != id)
                .Take(5)
                .Select(j => new JobDetailViewModel
                {
                    JobTitle = j.JobTitle,
                    JobDescription = j.JobDescription,
                    SalaryMin = j.SalaryMin,
                    SalaryMax = j.SalaryMax,
                }).ToListAsync();

            // Tạo JobDetailViewModel
            var jobDetailViewModel = new JobDetailViewModel
            {
                Id = job.Id,
                JobTitle = job.JobTitle,
                JobRequirements = job.JobRequirements,
                JobDescription = job.JobDescription,
                Education = job.Education,
                IsActive = job.IsActive ?? false,
                SalaryMin = job.SalaryMin,
                SalaryMax = job.SalaryMax,
                District = job.District?.Name,
                Ward = job.Ward?.Name,
                CreationDate = job.CreationDate,
                Status = job.Status,
                CompanyName = job.ApplicationUser?.Name ?? "Unknown Company",
                UserName = user?.Name ?? "Unknown User",
                CompanyDescription = employerDescription,
                CompanyEmail = job.ApplicationUser?.Email ?? "No email available",
                CompanyLogo = job.ApplicationUser?.Photo,
                UserLogo = user?.Photo,
                CategoryName = job.JobCategory?.CategoryName,
                CategoryDescription = job.JobCategory?.Description,
                RelatedJobs = relatedJobs,
                CurrentUserId = user?.Id,
                EmployerId = job.ApplicationUser?.Id,
                ApplicationUserId = job.ApplicationUser?.Id,

                Benefits = job.Benefits,
                Address = job.Address,
                WorkingTime = job.WorkingTime,
                HiringQuantity = job.HiringQuantity,
                Gender = job.Gender
            };

            // Truyền các trạng thái vào ViewBag
            ViewBag.HasApplied = hasApplied;
            ViewBag.HasCancelled = hasCancelled;
            ViewBag.HasFavorite = hasFavorite;

            return View(jobDetailViewModel);
        }










        private string GetDistrictName(int? districtId)
        {
            if (!districtId.HasValue) return string.Empty; 
                                                           
            var district = _context.Districts.FirstOrDefault(d => d.Id == districtId.Value);
            return district?.Name ?? string.Empty;
        }

        private string GetWardName(int? wardId)
        {
            if (!wardId.HasValue) return string.Empty; 
            var ward = _context.Wards.FirstOrDefault(w => w.Id == wardId.Value);
            return ward?.Name ?? string.Empty;
        }

        public async Task<IActionResult> FavoriteJobs()
        {
            var user = await _userManager.GetUserAsync(User); 

            if (user == null || !User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            var favoriteJobs = await _context.FavoriteJobs
                .Where(f => f.ApplicationUserId == user.Id) 
                .Include(f => f.Job)
                .ThenInclude(j => j.Employer) 
                .ToListAsync();

            var favoriteJobViewModels = favoriteJobs.Select(f => new FavoriteJobViewModel
            {
                JobId = f.Job.Id,
                JobTitle = f.Job.JobTitle,
                CompanyName = f.Job.Employer?.Name ?? "Unknown Company",
                SalaryMin = f.Job.SalaryMin,
                SalaryMax = f.Job.SalaryMax,
                CreationDate = f.Job.CreationDate
            }).ToList();

            return View(favoriteJobViewModels); 
        }


        [HttpPost]
        public async Task<JsonResult> SaveToFavorites(int jobId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null || !User.Identity.IsAuthenticated)
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập để thực hiện chức năng này." });
            }

            var favoriteJob = await _context.FavoriteJobs
                .FirstOrDefaultAsync(f => f.JobId == jobId && f.ApplicationUserId == user.Id);

            if (favoriteJob == null)
            {
                // Thêm vào danh sách yêu thích
                favoriteJob = new FavoriteJob
                {
                    JobId = jobId,
                    ApplicationUserId = user.Id,
                    AddedDate = DateTime.Now
                };

                _context.FavoriteJobs.Add(favoriteJob);
                await _context.SaveChangesAsync();

                return Json(new { success = true, added = true, message = "Đã thêm vào danh sách yêu thích." });
            }
            else
            {
                // Xóa khỏi danh sách yêu thích
                _context.FavoriteJobs.Remove(favoriteJob);
                await _context.SaveChangesAsync();

                return Json(new { success = true, added = false, message = "Đã xóa khỏi danh sách yêu thích." });
            }
        }



        [HttpPost]
        public async Task<IActionResult> RemoveFromFavorites(int jobId)
        {
            var user = await _userManager.GetUserAsync(User); 

            if (user == null || !User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            var favoriteJob = await _context.FavoriteJobs
                .FirstOrDefaultAsync(f => f.JobId == jobId && f.ApplicationUserId == user.Id);

            if (favoriteJob != null)
            {
                _context.FavoriteJobs.Remove(favoriteJob);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("FavoriteJobs");
        }

        public async Task<IActionResult> Apply(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
            var existingApplication = await _context.JobApplications
                .FirstOrDefaultAsync(app => app.JobId == id && app.ApplicationUserId == user.Id);

            if (existingApplication != null)
            {
                if (existingApplication.Status == ApplicationStatus.Cancelled)
                {
                    existingApplication.Status = ApplicationStatus.Pending;
                    existingApplication.ApplicationDate = DateTime.Now;  
                    _context.JobApplications.Update(existingApplication);
                    await _context.SaveChangesAsync();

                    TempData["ApplySuccessMessage"] = "Bạn đã nộp lại đơn ứng tuyển thành công!";
                    return RedirectToAction("JobDetail", new { id });
                }
                else
                {
                    TempData["ErrorMessage"] = "Bạn đã ứng tuyển công việc này rồi.";
                    return RedirectToAction("JobDetail", new { id });
                }
            }

            // If no existing application, proceed with new application flow
            var job = await _context.Jobs.FindAsync(id);
            if (job == null)
            {
                return NotFound();
            }

            var model = new ApplyJobViewModel
            {
                JobId = job.Id,
                JobTitle = job.JobTitle
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Apply(int jobId, IFormFile pdfFile, ApplyJobViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Check if the user has previously applied for the job and the application was cancelled
            var existingApplication = await _context.JobApplications
                .FirstOrDefaultAsync(app => app.JobId == jobId && app.ApplicationUserId == user.Id);

            if (existingApplication != null)
            {
                // If the previous application was cancelled, we update it
                if (existingApplication.Status == ApplicationStatus.Cancelled)
                {
                    existingApplication.Status = ApplicationStatus.Pending;  // Reapply by updating status to Pending
                    existingApplication.ApplicationDate = DateTime.Now;  // Optionally update the application date
                    _context.JobApplications.Update(existingApplication);
                    await _context.SaveChangesAsync();

                    // Send notification for reapplication to the employer
                    var jobForReapply = await _context.Jobs
                        .Include(j => j.ApplicationUser)
                        .FirstOrDefaultAsync(j => j.Id == jobId);

                    if (jobForReapply != null)
                    {
                        // Notification for the employer that the applicant has reapplied
                        var reapplyNotificationForOwner = new Notification
                        {
                            UserId = jobForReapply.ApplicationUser.Id,
                            Message = $"Ứng viên đã nộp lại đơn ứng tuyển cho công việc '{jobForReapply.JobTitle}'!",
                            CreatedAt = DateTime.Now,
                            IsRead = false,
                            Type = "JobApplicationReapply"
                        };
                        _context.Notifications.Add(reapplyNotificationForOwner);

                        await _notificationHubContext.Clients.User(jobForReapply.ApplicationUser.Id).SendAsync("ReceiveNotification", reapplyNotificationForOwner.Message);
                    }

                    var reapplyNotificationForApplicant = new Notification
                    {
                        UserId = user.Id,
                        Message = $"Bạn đã nộp lại đơn ứng tuyển cho công việc '{jobForReapply.JobTitle}' và đang chờ duyệt.",
                        CreatedAt = DateTime.Now,
                        IsRead = false,
                        Type = "ApplicationStatusReapply"
                    };
                    _context.Notifications.Add(reapplyNotificationForApplicant);

                    await _notificationHubContext.Clients.User(user.Id).SendAsync("ReceiveNotification", reapplyNotificationForApplicant.Message);

                    await _context.SaveChangesAsync();

                    TempData["ApplySuccessMessage"] = "Bạn đã nộp lại đơn ứng tuyển thành công!";
                    return RedirectToAction("JobDetail", new { id = jobId });
                }
                else
                {
                    TempData["ErrorMessage"] = "Bạn đã ứng tuyển công việc này rồi.";
                    return RedirectToAction("JobDetail", new { id = jobId });
                }
            }
            if (pdfFile == null || pdfFile.Length == 0)
            {
                ModelState.AddModelError("pdfFile", "Vui lòng tải lên file PDF.");
                model.JobId = jobId;
                return View(model);
            }

            byte[] pdfData;
            using (var memoryStream = new MemoryStream())
            {
                await pdfFile.CopyToAsync(memoryStream);
                pdfData = memoryStream.ToArray();
            }
            var application = new JobApplication
            {
                ApplicationUserId = user.Id,
                JobId = jobId,
                Status = ApplicationStatus.Pending,
                ApplicationDate = DateTime.Now,
                CV = pdfData
            };
            _context.JobApplications.Add(application);
            await _context.SaveChangesAsync();

            var jobForNewApplication = await _context.Jobs
                .Include(j => j.ApplicationUser)
                .FirstOrDefaultAsync(j => j.Id == jobId);

            if (jobForNewApplication != null)
            {
                var notificationForOwner = new Notification
                {
                    UserId = jobForNewApplication.ApplicationUser.Id,
                    Message = $"Bạn có một đơn ứng tuyển mới cho công việc '{jobForNewApplication.JobTitle}'!",
                    CreatedAt = DateTime.Now,
                    IsRead = false,
                    Type = "JobApplication"
                };
                _context.Notifications.Add(notificationForOwner);

                await _notificationHubContext.Clients.User(jobForNewApplication.ApplicationUser.Id).SendAsync("ReceiveNotification", notificationForOwner.Message);
            }

            var notificationForApplicant = new Notification
            {
                UserId = user.Id,
                Message = $"Bạn đã ứng tuyển công việc '{jobForNewApplication.JobTitle}' thành công và đang chờ duyệt.",
                CreatedAt = DateTime.Now,
                IsRead = false,
                Type = "ApplicationStatus"
            };
            _context.Notifications.Add(notificationForApplicant);

            await _notificationHubContext.Clients.User(user.Id).SendAsync("ReceiveNotification", notificationForApplicant.Message);

            await _context.SaveChangesAsync();

            TempData["ApplySuccessMessage"] = "Bạn đã nộp đơn ứng tuyển thành công!";

            return RedirectToAction("JobDetail", new { id = jobId });
        }
    }
}
