using DNTU_JOBS.Models;

namespace DNTU_JOBS.ViewModel
{
    public class ChatViewModel
    {
        public int EmployerId { get; set; }
        public int UserId { get; set; }
        public List<ChatMessage> Messages { get; set; }
    }
}
