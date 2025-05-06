namespace DNTU_JOBS.Models
{
    public class JobApplication
    {
        public int Id { get; set; }
        public string ApplicationUserId { get; set; }
        public int JobId { get; set; }
        public ApplicationStatus Status { get; set; }
        public DateTime ApplicationDate { get; set; }
        public byte[]? CV { get; set; } // Thuộc tính mới

        public int? UserTableId { get; set; } // Foreign key
        public virtual UserTable UserTable { get; set; }

        public virtual ApplicationUser ApplicationUser { get; set; }
        public virtual JobTable Job { get; set; }

        public string? RejectionReason { get; set; }
    }

    public enum ApplicationStatus
    {
        Pending, 
        Accepted, 
        Rejected,
        Cancelled
    }
}
