using System;
using System.Collections.Generic;

namespace DNTU_JOBS.Models;

public enum ApprovalStatus
{
    Pending,        // Chờ duyệt mới
    Approved,       // Đã duyệt
    Rejected,       // Bị từ chối
    PendingEdit,    // Đang chờ duyệt sửa
    ApprovedEdit,    // Đã duyệt bài sửa
    RejectedEdit,   // Bị từ chối bài sửa
    Filled,          // Đủ số lượng cần tuyển 
    Break,          // Tạm ngừng
    Working            // Hoạt động bình thường
}

public partial class JobTable
{
    public int Id { get; set; }

    public string? JobTitle { get; set; }

    public string? JobRequirements { get; set; }

    public string? JobDescription { get; set; }

    public string? Education { get; set; }

    public bool? IsActive { get; set; }

    public decimal? SalaryMin { get; set; }
    
    public decimal? SalaryMax { get; set; }

    public string? Benefits { get; set; } 
    
    public string? Address { get; set; } 
    
    public string? WorkingTime { get; set; } // Time làm việc
    
    public int HiringQuantity { get; set; } // Số lượng tuyển
    
    public string? Gender { get; set; } // Giới tính

    public string? JobEditRequestDetails { get; set; }

    public string? CategoryName { get; set; }

    public string? RejectionReason { get; set; }


    public int? JobCategoryId { get; set; }

    public virtual JobCategoryTable JobCategory { get; set; }

    public int? DistrictId { get; set; }
    public int? WardId { get; set; }

    public virtual District District { get; set; }
    public virtual Ward Ward { get; set; }


    public int? EmployerId { get; set; }

    public virtual EmployerTable Employer { get; set; }


    public int? UserId { get; set; }

    public virtual UserTable User { get; set; }

    public string ApplicationUserId { get; set; }

    public virtual ApplicationUser ApplicationUser { get; set; }

    public DateTime CreationDate { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; } = null;

    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

    public virtual ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();

    public virtual ICollection<FavoriteJob> FavoriteJobs { get; set; }
}

