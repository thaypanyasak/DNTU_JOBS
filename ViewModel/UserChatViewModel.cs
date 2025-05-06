namespace DNTU_JOBS.ViewModel
{
    public class UserChatViewModel
    {
        public string ApplicationUserId { get; set; }
        public string UserPhoto { get; set; }
        public List<EmployerChatInfo> Employers { get; set; }
    }

    public class EmployerChatInfo
    {
        public string EmployerId { get; set; }
        public string Name { get; set; }
        public string Photo { get; set; }
        public int UnreadMessages { get; set; }
    }
}
