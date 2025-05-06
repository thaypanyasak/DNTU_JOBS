using DNTU_JOBS.Areas.Identity.Pages.Account;
using DNTU_JOBS.Data;
using DNTU_JOBS.Models;
using DNTU_JOBS.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;


namespace DNTU_JOBS.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "CanManageAccountPolicy")]
    public class UserManagerController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<RegisterModel> _logger;

        public UserManagerController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ILogger<RegisterModel> logger)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var roleCounts = new Dictionary<string, int>();

            var roles = new[] { "Admin", "Employer", "User" };
            foreach (var role in roles)
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(role);
                roleCounts[role] = usersInRole.Count;
            }

            return View(roleCounts);
        }




        public async Task<IActionResult> ListUsersByRole(string role)
        {
            if (string.IsNullOrEmpty(role))
            {
                return NotFound("Role is not specified.");
            }

            var usersInRole = await _userManager.GetUsersInRoleAsync(role);
            var userViewModels = new List<UserViewModel>();

            foreach (var user in usersInRole)
            {
                var isLockedOut = await _userManager.IsLockedOutAsync(user);
                userViewModels.Add(new UserViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    Name = user.Name,
                    Address = user.Address,
                    IsLockedOut = isLockedOut,
                    Roles = new List<string> { role }
                });
            }

            ViewData["Role"] = role;
            return View(userViewModels);
        }



        public IActionResult Create(string role = null)
        {
            var userViewModel = new UserViewModel
            {
                // Set the role if provided
                Roles = !string.IsNullOrEmpty(role) ? new List<string> { role } : new List<string>(),

                RolesList = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Admin", Text = "Admin" },
                    new SelectListItem { Value = "Employer", Text = "Employer" },
                    new SelectListItem { Value = "User", Text = "User" }
                }
            };

            // Pass information to hide the dropdown in the view if a role is preselected
            ViewData["IsRolePreselected"] = !string.IsNullOrEmpty(role);

            return View(userViewModel);
        }





        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserViewModel model)
        {
            if (model.Roles == null || !model.Roles.Any())
            {
                ModelState.AddModelError(string.Empty, "Role is not specified.");
            }

            if (!ModelState.IsValid)
            {
                model.RolesList = _roleManager.Roles
                                  .Select(role => new SelectListItem
                                  {
                                      Text = role.Name,
                                      Value = role.Name
                                  })
                                  .ToList();
                return View(model);
            }

            // Kiểm tra xem email đã tồn tại trong hệ thống chưa
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Email đã tồn tại. Vui lòng chọn email khác.");
                model.RolesList = _roleManager.Roles
                                  .Select(role => new SelectListItem
                                  {
                                      Text = role.Name,
                                      Value = role.Name
                                  })
                                  .ToList();
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.UserName,
                Email = model.Email,
                Name = model.Name,
                Address = model.Address,
                IsActive = true, 
                EmailConfirmed = true 
            };

            string defaultImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/logo/UserLogo.png");
            if (model.Roles.Contains("Employer"))
            {
                defaultImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/logo/EmployerLogo.png");
            }

            if (model.Roles.Contains("Admin"))
            {
                defaultImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/logo/AdminLogo.png");
            }

            if (System.IO.File.Exists(defaultImagePath))
            {
                user.Photo = await System.IO.File.ReadAllBytesAsync(defaultImagePath);
            }

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                foreach (var role in model.Roles)
                {
                    await _userManager.AddToRoleAsync(user, role);
                }

                if (model.Roles.Contains("Admin"))
                {
                    var adminEntry = new AdminTable
                    {
                        ApplicationUserId = user.Id,
                        Name = model.Name,
                        Email = model.Email,
                        Phone = model.Phone,
                        Photo = user.Photo,
                        CanManageAccount = model.CanManageAccount,
                        CanManageJobs = model.CanManageJobs,
                        CanManageChats = model.CanManageChats
                    };
                    _db.Admins.Add(adminEntry);
                }
                else if (model.Roles.Contains("Employer"))
                {
                    var employerEntry = new EmployerTable
                    {
                        ApplicationUserId = user.Id,
                        Name = model.Name,
                        Email = model.Email,
                        Phone = model.Phone,
                        Photo = user.Photo
                    };
                    _db.Employers.Add(employerEntry);
                }
                else if (model.Roles.Contains("User"))
                {
                    var userEntry = new UserTable
                    {
                        ApplicationUserId = user.Id,
                        Name = model.Name,
                        Email = model.Email,
                        Phone = model.Phone,
                        Nationality = model.Nationality,
                        DateOfBirth = model.DateOfBirth,
                        Gender = model.Gender,
                        Photo = user.Photo
                    };
                    _db.UserJobs.Add(userEntry);
                }

                await _db.SaveChangesAsync();

                TempData["CreateSuccessMessage"] = "Bạn đã tạo tài khoản thành công!";

                return RedirectToAction("ListUsersByRole", new { role = model.Roles.FirstOrDefault() });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            model.RolesList = _roleManager.Roles
                            .Select(role => new SelectListItem
                            {
                                Text = role.Name,
                                Value = role.Name
                            })
                            .ToList();

            return View(model);
        }




        public async Task<IActionResult> Edit(string id)
        {
            // Retrieve the user by ID
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);

            var model = new UserViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Name = user.Name,
                Address = user.Address,
                IsLockedOut = await _userManager.IsLockedOutAsync(user),
                Roles = userRoles.ToList(),
                RolesList = _roleManager.Roles
                             .Select(role => new SelectListItem
                             {
                                 Text = role.Name,
                                 Value = role.Name,
                                 Selected = userRoles.Contains(role.Name) 
                             }).ToList()
            };

            var employerRecord = await _db.Employers.SingleOrDefaultAsync(e => e.ApplicationUserId == id);
            if (employerRecord != null)
            {
                model.Phone = employerRecord.Phone;
            }

            var userRecord = await _db.UserJobs.SingleOrDefaultAsync(u => u.ApplicationUserId == id);
            if (userRecord != null)
            {
                model.Phone = userRecord.Phone;
                model.Nationality = userRecord.Nationality;
                model.DateOfBirth = userRecord.DateOfBirth;
                model.Gender = userRecord.Gender;
                // Populate other fields as necessary from UserTable
            }

            return View(model);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, UserViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Update user details
            user.UserName = model.UserName;
            user.Email = model.Email;
            user.Name = model.Name;
            user.Address = model.Address;

            user.IsActive = true; // Đặt IsActive là true
            user.EmailConfirmed = true; // Đặt EmailConfirmed là true

            // Update user's roles
            var currentRoles = await _userManager.GetRolesAsync(user);
            var rolesToRemove = currentRoles.Except(model.Roles).ToList();
            var rolesToAdd = model.Roles.Except(currentRoles).ToList();

            if (rolesToRemove.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            }
            if (rolesToAdd.Any())
            {
                await _userManager.AddToRolesAsync(user, rolesToAdd);
            }

            if (rolesToAdd.Contains("Employer"))
            {
                var employerEntry = await _db.Employers.SingleOrDefaultAsync(e => e.ApplicationUserId == id);
                if (employerEntry == null)
                {
                    employerEntry = new EmployerTable
                    {
                        ApplicationUserId = user.Id,
                        Name = model.Name,
                        Email = model.Email,
                        Phone = model.Phone,
                        Address = model.Address,
                    };
                    _db.Employers.Add(employerEntry);
                }
                else
                {
                    employerEntry.Name = model.Name;
                    employerEntry.Email = model.Email;
                    employerEntry.Phone = model.Phone;
                    employerEntry.Address = model.Address;
                }
            }
            else if (rolesToRemove.Contains("Employer"))
            {
                var employerEntry = await _db.Employers.SingleOrDefaultAsync(e => e.ApplicationUserId == id);
                if (employerEntry != null)
                {
                    _db.Employers.Remove(employerEntry);
                }
            }

            if (rolesToAdd.Contains("User"))
            {
                var userEntry = await _db.UserJobs.SingleOrDefaultAsync(u => u.ApplicationUserId == id);
                if (userEntry == null)
                {
                    userEntry = new UserTable
                    {
                        ApplicationUserId = user.Id,
                        Name = model.Name,
                        Email = model.Email,
                        Phone = model.Phone,
                        Nationality = model.Nationality,
                        DateOfBirth = model.DateOfBirth,
                        Gender = model.Gender
                    };
                    _db.UserJobs.Add(userEntry);
                }
                else
                {
                    userEntry.Name = model.Name;
                    userEntry.Email = model.Email;
                    userEntry.Phone = model.Phone;
                    userEntry.Nationality = model.Nationality;
                    userEntry.DateOfBirth = model.DateOfBirth;
                    userEntry.Gender = model.Gender;
                }
            }
            else if (rolesToRemove.Contains("User"))
            {
                var userEntry = await _db.UserJobs.SingleOrDefaultAsync(u => u.ApplicationUserId == id);
                if (userEntry != null)
                {
                    _db.UserJobs.Remove(userEntry);
                }
            }

            if (model.IsLockedOut)
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
            }

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                await _db.SaveChangesAsync();

                TempData["EditSuccessMessage"] = "Bạn đã sửa thông tin tài khoản thành công!";

                var currentRole = rolesToAdd.FirstOrDefault() ?? currentRoles.FirstOrDefault();
                return RedirectToAction("ListUsersByRole", new { role = currentRole });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            var roles = _roleManager.Roles.ToList();
            model.RolesList = roles.Select(role => new SelectListItem
            {
                Text = role.Name,
                Value = role.Name
            }).ToList();

            return View(model);
        }





        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);

            var model = new UserViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Name = user.Name,
                Address = user.Address,
                Roles = userRoles
            };

            return View(model);
        }

        private async Task<IActionResult> HandleUserDeleteAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng." });
            }

            var jobApplications = await _db.JobApplications.Where(a => a.ApplicationUserId == id).ToListAsync();
            if (jobApplications.Any())
            {
                _db.JobApplications.RemoveRange(jobApplications);
                await _db.SaveChangesAsync();
            }

            var favorites = await _db.FavoriteJobs.Where(f => f.ApplicationUserId == id).ToListAsync();
            if (favorites.Any())
            {
                _db.FavoriteJobs.RemoveRange(favorites);
                await _db.SaveChangesAsync();
            }

            var chatMessagesAsReceiver = await _db.ChatMessages.Where(c => c.ReceiverId == id).ToListAsync();
            if (chatMessagesAsReceiver.Any())
            {
                _db.ChatMessages.RemoveRange(chatMessagesAsReceiver);
                await _db.SaveChangesAsync();
            }

            var chatMessagesAsSender = await _db.ChatMessages.Where(c => c.SenderId == id).ToListAsync();
            if (chatMessagesAsSender.Any())
            {
                _db.ChatMessages.RemoveRange(chatMessagesAsSender);
                await _db.SaveChangesAsync();
            }

            var userJobCategories = await _db.UserJobs.Where(u => u.ApplicationUserId == id && u.JobCategoryId != null).ToListAsync();
            if (userJobCategories.Any())
            {
                return Json(new { success = false, message = "Không thể xóa tài khoản vì người dùng có liên kết với danh mục công việc." });
            }

            var adminRecord = await _db.Admins.SingleOrDefaultAsync(a => a.ApplicationUserId == id);
            if (adminRecord != null)
            {
                _db.Admins.Remove(adminRecord);
            }

            var employerRecord = await _db.Employers.SingleOrDefaultAsync(e => e.ApplicationUserId == id);
            if (employerRecord != null)
            {
                _db.Employers.Remove(employerRecord);
            }

            var userRecord = await _db.UserJobs.SingleOrDefaultAsync(u => u.ApplicationUserId == id);
            if (userRecord != null)
            {
                _db.UserJobs.Remove(userRecord);
            }

            await _db.SaveChangesAsync();

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                return Json(new { success = true });
            }

            return Json(new { success = false, errors = result.Errors.Select(e => e.Description) });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAjax(string id)
        {
            var result = await HandleUserDeleteAsync(id);
            return result;
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var result = await HandleUserDeleteAsync(id);

            if (result is JsonResult jsonResult)
            {
                var response = jsonResult.Value as dynamic; 
                if (response?.success == true) 
                {
                    TempData["DeleteSuccessMessage"] = "Tài khoản đã được xóa thành công!";
                    return RedirectToAction("Index");
                }

                TempData["DeleteErrorMessage"] = response?.message ?? "Có lỗi khi xóa tài khoản.";
            }

            return RedirectToAction("Index");
        }







        [HttpPost]
        public async Task<IActionResult> LockAccount(string id, string role)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
            return RedirectToAction("ListUsersByRole", new { role });
        }

        [HttpPost]
        public async Task<IActionResult> UnlockAccount(string id, string role)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            await _userManager.SetLockoutEndDateAsync(user, null);
            return RedirectToAction("ListUsersByRole", new { role });
        }
        [HttpGet]
        public async Task<IActionResult> AdminResetPassword(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var model = new ResetPasswordViewModel
            {
                UserId = userId,
                Role = role,
                UserName = user.UserName
            };

            return View(model);
        }



        [HttpPost]
        public async Task<IActionResult> AdminResetPassword([FromBody] ResetPasswordViewModel model)
        {
            if (string.IsNullOrEmpty(model.NewPassword))
            {
                return Json(new { success = false, message = "Password cannot be empty" });
            }

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            var removePasswordResult = await _userManager.RemovePasswordAsync(user);
            if (!removePasswordResult.Succeeded)
            {
                return Json(new { success = false, message = "Failed to remove old password" });
            }

            var addPasswordResult = await _userManager.AddPasswordAsync(user, model.NewPassword);
            if (addPasswordResult.Succeeded)
            {
                return Json(new { success = true, message = "Password reset successfully" });
            }

            return Json(new { success = false, message = "Failed to add new password" });
        }




    }
}
