using DNTU_JOBS.Models;

namespace DNTU_JOBS.ViewModel
{
    public class EmployerChatViewModel
    {
        public string ApplicationUserId { get; set; }
        public string EmployerPhoto { get; set; }  // Ảnh đại diện của Employer
        public List<UserChatInfo> Users { get; set; }
    }

    public class UserChatInfo
    {
        public string UserId { get; set; }
        public string Name { get; set; }
        public string Photo { get; set; }
        public int UnreadMessages { get; set; }  // Số lượng tin nhắn chưa đọc
    }
}
