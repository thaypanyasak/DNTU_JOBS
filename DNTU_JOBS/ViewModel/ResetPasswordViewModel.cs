using System.ComponentModel.DataAnnotations;

namespace DNTU_JOBS.ViewModel
{
    public class ResetPasswordViewModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }  // Thêm thuộc tính này nếu cần
        [Required]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        public string Role { get; set; }
    }

}
