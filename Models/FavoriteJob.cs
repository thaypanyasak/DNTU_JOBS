namespace DNTU_JOBS.Models
{
    public class FavoriteJob
    {
        public int Id { get; set; }

        public int JobId { get; set; }
        public virtual JobTable Job { get; set; }

        public string ApplicationUserId { get; set; }
        public virtual ApplicationUser ApplicationUser { get; set; }

        public DateTime AddedDate { get; set; } = DateTime.Now;
    
    }
}
