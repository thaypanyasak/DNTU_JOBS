namespace DNTU_JOBS.Models
{
    public class Ward
    {
        public int Id { get; set; } // Primary Key
        public string Name { get; set; } // Tên phường/xã
        
        public int DistrictId { get; set; } // Foreign Key
        public District District { get; set; }
    }
}