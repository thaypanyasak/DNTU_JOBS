using Microsoft.AspNetCore.SignalR;

namespace DNTU_JOBS.Hubs
{
    public class NotificationHub : Hub
    {
        public async Task SendNotification(string userId, string message)
        {

            await Clients.User(userId).SendAsync("ReceiveNotification", message);
        }

        // Nếu muốn gửi thông báo cho tất cả người dùng kết nối
        public async Task SendNotificationToAll(string message)
        {
            await Clients.All.SendAsync("ReceiveNotification", message);
        }
    }
}
