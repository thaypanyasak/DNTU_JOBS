namespace DNTU_JOBS.Models
{
    public class FileTable
    {
        public int Id { get; set; }
        public byte[] FileData { get; set; }
        public string FileName { get; set; }
        public string FileType { get; set; } // Ví dụ: "image/png", "application/pdf", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        public long FileSize { get; set; } // Kích thước file tính bằng byte
        public int ChatMessageId { get; set; }
        public bool IsRecalled { get; set; }
        public virtual ChatMessage ChatMessage { get; set; }
    }
}
