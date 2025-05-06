namespace DNTU_JOBS.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public string UserId { get; set; } // Id của người nhận (Employer)
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public string Type { get; set; }

        public virtual ApplicationUser User { get; set; }
    }

}
