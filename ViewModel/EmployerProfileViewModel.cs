using DNTU_JOBS.Models;

namespace DNTU_JOBS.ViewModel
{
    public class EmployerProfileViewModel
    {
        public EmployerTable Employer { get; set; }
        public bool IsTwoFactorEnabled { get; set; }
    }
}
