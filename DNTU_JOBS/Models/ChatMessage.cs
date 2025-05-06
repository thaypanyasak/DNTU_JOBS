namespace DNTU_JOBS.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public string SenderId { get; set; }
        public string ReceiverId { get; set; }
        public string Content { get; set; }
        public DateTime SentAt { get; set; }

        public bool IsRecalled { get; set; }


        public bool IsRead { get; set; }

        public bool IsDeletedBySender { get; set; }
        public bool IsDeletedByReceiver { get; set; }

        public ICollection<ChatImage> Images { get; set; } = new List<ChatImage>();
        public ICollection<FileTable> Files { get; set; } = new List<FileTable>();
        public virtual ApplicationUser Sender { get; set; }
        public virtual ApplicationUser Receiver { get; set; }
    }
}
