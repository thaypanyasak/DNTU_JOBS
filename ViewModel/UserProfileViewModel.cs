using DNTU_JOBS.Models;

namespace DNTU_JOBS.ViewModel
{
    public class UserProfileViewModel
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string? Cnic { get; set; }
        public string? Nationality { get; set; }
        public string? Email { get; set; }
        public Gender? Gender { get; set; }
        public byte[]? Photo { get; set; }
        public string? Qualification { get; set; }
        public string? Address { get; set; }
        public byte[]? AttachCv { get; set; }
        public string? Description { get; set; }

        public string? FacebookLink { get; set; }
        public string? TwitterLink { get; set; }
        public string? LinkedInLink { get; set; }
        public string? InstagramLink { get; set; }

        public string? RejectionReason { get; set; }

        public int? JobCategoryId { get; set; }

        public string? JobCategoryName { get; set; }

        public bool IsTwoFactorEnabled { get; set; }


        public UserTable User { get; set; }

        public List<FavoriteJob> FavoriteJobs { get; set; }

        public List<JobApplication> Applications { get; set; }

        public List<int> SelectedJobCategoryIds { get; set; } = new List<int>();
        public List<string> JobCategoryNames { get; set; } = new List<string>();
        public List<UserJobCategory> UserJobCategories { get; set; } = new List<UserJobCategory>();

        public IEnumerable<JobCategoryTable>? JobCategories { get; set; }
    }
}
