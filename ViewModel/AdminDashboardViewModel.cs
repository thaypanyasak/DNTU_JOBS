using DNTU_JOBS.Models;

namespace DNTU_JOBS.ViewModel
{
    public class AdminDashboardViewModel
    {
        public string SelectedMenuItem { get; set; }
        public List<ApplicationUser> Users { get; set; }
        public string? AdminName { get; set; }
        public byte[]? AdminPhoto { get; set; }

        public bool CanManageAccount { get; set; }
        public bool CanManageJobs { get; set; }
        public bool CanManageChats { get; set; }
    }
}
