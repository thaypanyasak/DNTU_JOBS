using System;
using DNTU_JOBS.Models;

namespace DNTU_JOBS.ViewModel
{
    public class JobDetailViewModel
    {
        public int Id { get; set; }
        public string JobTitle { get; set; }
        public string JobRequirements { get; set; }
        public string JobDescription { get; set; }
        public string Education { get; set; }
        public bool IsActive { get; set; }
        public decimal? SalaryMin { get; set; }
        public decimal? SalaryMax { get; set; }
        public string JobCategory { get; set; }
        public string District { get; set; }
        public string Ward { get; set; }
        public DateTime CreationDate { get; set; }
        public ApprovalStatus Status { get; set; }


        public string DistrictName { get; set; } // District Name
        public string WardName { get; set; } // Ward Name
        public int CompanyId { get; set; } // Thêm thuộc tính này
        public string CompanyName { get; set;}
        public string CompanyEmail { get; set; }
        public string? EmployerId { get; set; }
        public byte[] CompanyLogo { get; set; }
        public byte[] UserLogo { get; set; }

        public string UserName { get; set; }

        public string? Benefits { get; set; } // Quyền lợi

        public string? Address { get; set; } // Địa điểm

        public string? WorkingTime { get; set; } // Time làm việc

        public int HiringQuantity { get; set; } // Số lượng tuyển

        public string? Gender { get; set; } // Giới tính

        public string RejectionReason { get; set; }


        public string CompanyDescription { get; set; }

        public string CategoryName { get; set; }

        public string CategoryDescription { get; set; }

        public string CurrentUserId { get; set; }

        public List<JobDetailViewModel> RelatedJobs { get; set; }

        public string ApplicationUserId { get; set; }

    }
}
