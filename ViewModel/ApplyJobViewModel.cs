using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace DNTU_JOBS.ViewModel
{
    public class ApplyJobViewModel
    {
        public int JobId { get; set; }
        public string JobTitle { get; set; }

        [Required(ErrorMessage = "Tên đầy đủ là bắt buộc.")]
        [Display(Name = "Tên đầy đủ")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Tải lên CV là bắt buộc.")]
        [Display(Name = "Tải lên CV (PDF)")]
        public IFormFile CV { get; set; }
    }
}
