using DNTU_JOBS.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;


namespace DNTU_JOBS.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context; // Khai báo _context
        private readonly IHubContext<ChatHub> _hubContext;

        // Constructor nhận ApplicationDbContext qua Dependency Injection
        public ChatHub(ApplicationDbContext context, IHubContext<ChatHub> hubContext)
        {
            _context = context; // Gán giá trị _context
            _hubContext = hubContext;
        }

        private string GetUserAvatar(string userId)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user?.Photo != null)
            {
                return $"data:image/jpeg;base64,{Convert.ToBase64String(user.Photo)}";
            }
            return "/img/logo/UserLogo.png"; // Ảnh mặc định nếu không có avatar
        }

        public async Task SendMessage(string receiverId, string message, List<string> imageUrls = null, List<dynamic> fileUrls = null)
        {
            if (string.IsNullOrEmpty(receiverId))
            {
                throw new ArgumentException("Receiver ID cannot be null or empty.");
            }

            var avatar = GetUserAvatar(Context.UserIdentifier);

            await Clients.User(receiverId).SendAsync("ReceiveMessage", new
            {
                MessageId = Guid.NewGuid().ToString(), // Tạo MessageId duy nhất
                SenderId = Context.UserIdentifier,
                ReceiverId = receiverId,
                Avatar = avatar,
                Message = string.IsNullOrWhiteSpace(message) ? string.Empty : message.Trim(),
                ImageUrls = imageUrls?.Where(url => !string.IsNullOrWhiteSpace(url)).Distinct().ToList() ?? new List<string>(),
                FileUrls = fileUrls?.Select(f => (dynamic)new
                {
                    f.FileName,
                    f.FileType,
                    f.Url
                }).ToList() ?? new List<dynamic>(),
                SentAt = DateTime.UtcNow
            });


            await UpdateUnreadCount(receiverId);
            await Clients.User(receiverId).SendAsync("UpdateUnreadCount", receiverId);
        }


        //recall
        public async Task RecallMessage(int messageId)
        {
            // Gửi thông báo đến tất cả các client trong nhóm chat
            await Clients.All.SendAsync("MessageRecalled", messageId);
        }

        public async Task RecallImage(int imageId)
        {
            await Clients.All.SendAsync("ImageRecalled", imageId);
        }

        public async Task RecallFile(int fileId)
        {
            await Clients.All.SendAsync("FileRecalled", fileId);
        }


        private async Task UpdateUnreadCount(string receiverId)
        {
            await Clients.User(receiverId).SendAsync("UpdateUnreadCount", receiverId);
        }


        public async Task NotifyUserStatus(string userId)
        {
            // Lấy thông tin người dùng từ DB
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null)
            {
                var nowUtc = DateTime.UtcNow;

                // Kiểm tra và tính trạng thái online/offline
                bool isOnline = user.LastActivity.HasValue &&
                                (nowUtc - user.LastActivity.Value).TotalMinutes <= 5;

                // Tính số phút đã trôi qua (nếu có LastActivity)
                int? minutesAgo = user.LastActivity.HasValue
                    ? (int)Math.Floor((nowUtc - user.LastActivity.Value).TotalMinutes)
                    : (int?)null;

                // Gửi trạng thái đến SignalR clients
                await _hubContext.Clients.All.SendAsync("UpdateUserStatus", new
                {
                    UserId = user.Id,
                    IsOnline = isOnline,
                    LastActivity = user.LastActivity?.ToString("o"), // Định dạng ISO 8601
                    MinutesAgo = minutesAgo
                });
            }
        }






        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            await base.OnConnectedAsync();
            Console.WriteLine($"User connected: {userId}");
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            await base.OnDisconnectedAsync(exception);
            Console.WriteLine($"User disconnected: {userId}");
        }
    }
}
