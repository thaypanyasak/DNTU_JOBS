namespace DNTU_JOBS.ViewModel
{
    public class UserJobViewModel
    {
        public int Id { get; set; } // Job ID
        public string JobTitle { get; set; } // Job Title
        public string DistrictName { get; set; } // District Name
        public string WardName { get; set; } // Ward Name
        public decimal? SalaryMin { get; set; } // Minimum Salary
        public decimal? SalaryMax { get; set; } // Maximum Salary
        public DateTime CreationDate { get; set; } // Creation Date
        public string CompanyName { get; set; }
        public string EmployerId { get; set; }
        public byte[] CompanyLogo { get; set; }

    }
}
