using DNTU_JOBS.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;

namespace DNTU_JOBS.Areas.Identity.Pages.Account
{
    public class UpdateRoleModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UpdateRoleModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ErrorMessage { get; set; }

        public class InputModel
        {
            public string Email { get; set; }
            public string Name { get; set; }
            public string Phone { get; set; }
            public string Role { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // Lấy thông tin người dùng từ Claims
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                ErrorMessage = "Không tìm thấy người dùng.";
                return RedirectToPage("./Login");
            }

            Input = new InputModel
            {
                Email = user.Email,
                Name = user.Name,
                Phone = user.PhoneNumber
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                ErrorMessage = "Không tìm thấy người dùng.";
                return RedirectToPage("./Login");
            }

            // Cập nhật vai trò cho người dùng
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Any())
            {
                ErrorMessage = "Người dùng đã có vai trò.";
                return Page();
            }

            var result = await _userManager.AddToRoleAsync(user, Input.Role);
            if (result.Succeeded)
            {
                return RedirectToPage("/Index"); // Chuyển hướng về trang chính sau khi cập nhật
            }
            else
            {
                ErrorMessage = "Có lỗi khi cập nhật vai trò.";
                return Page();
            }
        }
    }
}
