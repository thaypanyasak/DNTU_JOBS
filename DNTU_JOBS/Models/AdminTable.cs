namespace DNTU_JOBS.Models
{
    public class AdminTable
    {
        public int Id { get; set; }

        public string? ApplicationUserId { get; set; }

        // Thông tin admin
        public string Name { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }

        public byte[]? Photo { get; set; }

        // Các quyền của admin
        public bool CanManageAccount { get; set; }
        public bool CanManageJobs { get; set; }
        public bool CanManageChats { get; set; }

        // Quan hệ với ApplicationUser (Admin là người dùng trong Identity)
        public virtual ApplicationUser ApplicationUser { get; set; }
    }
}
