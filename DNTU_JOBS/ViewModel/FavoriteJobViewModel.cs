namespace DNTU_JOBS.ViewModel
{
    public class FavoriteJobViewModel
    {
        public int JobId { get; set; }
        public string JobTitle { get; set; }
        public string CompanyName { get; set; }
        public decimal? SalaryMin { get; set; }
        public decimal? SalaryMax { get; set; }
        public DateTime CreationDate { get; set; }
    }
}
