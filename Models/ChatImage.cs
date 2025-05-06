namespace DNTU_JOBS.Models
{
    public class ChatImage
    {
        public int Id { get; set; }
        public byte[] ImageData { get; set; }
        public string ImageName { get; set; }
        public int ChatMessageId { get; set; }

        public bool IsRecalled { get; set; }

        public virtual ChatMessage ChatMessage { get; set; }
    }
}
