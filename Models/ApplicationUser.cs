using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace DNTU_JOBS.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        public string Name { get; set; }

        // Personal Information Fields
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string? Nationality { get; set; }
        public Gender? Gender { get; set; }
        public string? Qualification { get; set; }
        public string? Description { get; set; }

        // Social Media Links
        public string? FacebookLink { get; set; }
        public string? TwitterLink { get; set; }
        public string? LinkedInLink { get; set; }
        public string? InstagramLink { get; set; }

        // Additional Information
        public byte[]? Photo { get; set; }
        public byte[]? AttachCv { get; set; }
        public bool? IsJobless { get; set; }
        //public bool? IsActive { get; set; }

        public bool IsActive { get; set; } = false;


        public DateTimeOffset? LastActivity { get; set; }

        // Quan hệ một-một với UserTable và EmployerTable
        public virtual UserTable? UserJob { get; set; }
        public virtual EmployerTable? Employer { get; set; }
        public virtual AdminTable? Admin { get; set; }




        // Quan hệ một-nhiều với FavoriteJobs
        public virtual ICollection<FavoriteJob> FavoriteJobs { get; set; }
        public virtual ICollection<JobApplication> Applications { get; set; } = new List<JobApplication>();

    }
}
