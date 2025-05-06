namespace DNTU_JOBS.Models
{
    public class District
    {
        public int Id { get; set; } // Primary Key
        public string Name { get; set; } // Tên quận/huyện

        // Liên kết một-tới-nhiều với Ward
        public ICollection<Ward> Wards { get; set; } = new List<Ward>();
    }
}