using System;
using System.Collections.Generic;

namespace DNTU_JOBS.Models;

public partial class UserTable
{
    public int Id { get; set; }

    public string? ApplicationUserId { get; set; }

    public int? JobCategoryId { get; set; }

    public string? Name { get; set; }

    public string? Phone {  get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? Cnic { get; set; }

    public string? Nationality { get; set; }

    public string? Email { get; set; }

    public Gender? Gender { get; set; }

    public byte[]? Photo { get; set; }

    public string? Qualification { get; set; }

    public string? Address { get; set; }

    public byte[]? AttachCv { get; set; }

    public bool? IsJobless { get; set; }

    public bool? IsActive { get; set; }

    public string? Description { get; set; }


    public string? FacebookLink { get; set; } 
    public string? TwitterLink { get; set; }
    public string? LinkedInLink { get; set; } 
    public string? InstagramLink { get; set; } 


    public virtual JobCategoryTable? JobCategory { get; set; }

    public virtual ApplicationUser? ApplicationUser { get; set; }


    public virtual ICollection<JobApplication> Applications { get; set; } = new List<JobApplication>();
    public virtual ICollection<UserJobCategory> UserJobCategories { get; set; } = new List<UserJobCategory>();
}
public enum Gender
{
    Male,
    Female,
    Other
}
