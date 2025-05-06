namespace DNTU_JOBS.Models
{
    public class UserJobCategory
    {
        public int UserTableId { get; set; }
        public UserTable UserTable { get; set; } // Quan hệ tới UserTable

        public int JobCategoryId { get; set; }
        public JobCategoryTable JobCategory { get; set; }
    }
}
