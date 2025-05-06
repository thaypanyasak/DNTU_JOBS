using DNTU_JOBS.Data;
using DNTU_JOBS.Hubs;
using DNTU_JOBS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace DNTU_JOBS.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ChatApiController> _logger;
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatApiController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<ChatApiController> logger, IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _hubContext = hubContext;
        }

        [HttpPost("SendMessage")]
        public async Task<IActionResult> SendMessage([FromForm] ChatMessageRequest request, [FromForm] List<IFormFile>? files)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var sender = await _userManager.GetUserAsync(User);
                if (sender == null)
                {
                    return Unauthorized();
                }

                var receiver = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.ReceiverId);
                if (receiver == null)
                {
                    return BadRequest("Người nhận không tồn tại.");
                }

                var message = new ChatMessage
                {
                    SenderId = sender.Id,
                    ReceiverId = receiver.Id,
                    Content = request.Message,
                    SentAt = DateTime.Now
                };

                _context.ChatMessages.Add(message);
                await _context.SaveChangesAsync();

                // Broadcast message to the receiver via SignalR
                await _hubContext.Clients.User(receiver.Id).SendAsync("ReceiveMessage", sender.Id, request.Message);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi tin nhắn.");
                return StatusCode(500, "Lỗi nội bộ server.");
            }
        }



        [HttpPost("SendMessageToEmployer")]
        public async Task<IActionResult> SendMessageToEmployer([FromBody] ChatMessageRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var sender = await _userManager.GetUserAsync(User);
                if (sender == null)
                {
                    return Unauthorized();
                }

                var receiver = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.ReceiverId);
                if (receiver == null)
                {
                    return BadRequest("The employer does not exist.");
                }

                var message = new ChatMessage
                {
                    SenderId = sender.Id,
                    ReceiverId = receiver.Id,
                    Content = request.Message,
                    SentAt = DateTime.Now
                };

                _context.ChatMessages.Add(message);
                await _context.SaveChangesAsync();

                await _hubContext.Clients.User(receiver.Id).SendAsync("ReceiveMessage", sender.Id, request.Message);

                return Ok("Message sent successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to employer.");
                return StatusCode(500, "Internal server error.");
            }
        }

        // Other methods here...



        [HttpGet("GetMessageDetail/{receiverId}")]
        public async Task<IActionResult> GetMessageDetail(string receiverId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            // Tìm các tin nhắn giữa người gửi (user.Id) và người nhận (receiverId)
            var messages = await _context.ChatMessages
                .Where(m => (m.SenderId == user.Id && m.ReceiverId == receiverId) || (m.SenderId == receiverId && m.ReceiverId == user.Id))
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            return Ok(messages);
        }





        [HttpGet("GetMessages/{receiverId}")]
        public async Task<IActionResult> GetMessages(string receiverId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var messages = await _context.ChatMessages
                .Where(m => (m.SenderId == user.Id && m.ReceiverId == receiverId) ||
                            (m.SenderId == receiverId && m.ReceiverId == user.Id))
                .OrderBy(m => m.SentAt)
                .Select(m => new
                {
                    m.Content,
                    m.SentAt,
                    SenderId = m.SenderId,
                    ReceiverId = m.ReceiverId,
                    Avatar = GetUserAvatar(m.SenderId) // Lấy avatar người gửi
                })
                .ToListAsync();

            return Ok(messages);
        }



        private string GetUserAvatar(string userId)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user?.Photo != null)
            {
                return $"data:image/jpeg;base64,{Convert.ToBase64String(user.Photo)}";
            }
            return "/img/logo/UserLogo.png";
        }




        [HttpGet("GetUserMessages/{employerId}")]
        public async Task<IActionResult> GetUserMessages(string employerId)
        {
            // Get a list of users who have sent or received messages with the employer
            var users = await _context.ChatMessages
                .Where(m => m.ReceiverId == employerId || m.SenderId == employerId)  // Include both senders and receivers
                .Select(m => new
                {
                    Id = m.SenderId == employerId ? m.ReceiverId : m.SenderId, // Get the other user's ID
                    Name = m.SenderId == employerId
                        ? _context.Users.FirstOrDefault(u => u.Id == m.ReceiverId).Name
                        : _context.Users.FirstOrDefault(u => u.Id == m.SenderId).Name,
                    Photo = m.SenderId == employerId
                        ? _context.Users.FirstOrDefault(u => u.Id == m.ReceiverId).Photo
                        : _context.Users.FirstOrDefault(u => u.Id == m.SenderId).Photo,
                    UnreadMessages = _context.ChatMessages
                        .Count(x => x.SenderId == (m.SenderId == employerId ? m.ReceiverId : m.SenderId) && x.ReceiverId == employerId && !x.IsRead)
                })
                .Distinct()
                .ToListAsync();

            // Process user photos to convert them to base64 strings for display
            var userDetails = users.Select(u => new
            {
                u.Id,
                u.Name,
                Avatar = u.Photo != null
                    ? ConvertToBase64(u.Photo)  // Convert the photo to base64
                    : "/img/logo/UserLogo.png",  // Default avatar if no photo
                u.UnreadMessages
            });

            return Ok(userDetails);
        }

        // Chuyển đổi byte[] sang base64 để hiển thị hình ảnh trên giao diện
        private static string? ConvertToBase64(byte[]? photo)
        {
            if (photo == null) return null;
            return $"data:image/jpeg;base64,{Convert.ToBase64String(photo)}";  // Chuyển đổi ảnh thành base64
        }


        [HttpPost("MarkMessagesAsRead/{userId}")]
        public async Task<IActionResult> MarkMessagesAsRead(string userId)
        {
            var employer = await _userManager.GetUserAsync(User);
            if (employer == null)
            {
                return Unauthorized();
            }

            // Tìm tất cả các tin nhắn chưa đọc được gửi từ userId cho employer
            var unreadMessages = await _context.ChatMessages
                .Where(m => m.SenderId == userId && m.ReceiverId == employer.Id && !m.IsRead)
                .ToListAsync();

            if (unreadMessages.Any())
            {
                // Đánh dấu tất cả các tin nhắn là đã đọc
                unreadMessages.ForEach(m => m.IsRead = true);
                await _context.SaveChangesAsync();
            }

            return Ok();
        }
        [HttpGet("GetMessageForEmployer/{receiverId}")]
        public async Task<IActionResult> GetMessageForEmployer(string receiverId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var messages = await _context.ChatMessages
                .Where(m => (m.SenderId == user.Id && m.ReceiverId == receiverId) || (m.SenderId == receiverId && m.ReceiverId == user.Id))
                .OrderBy(m => m.SentAt)
                .Include(m => m.Sender)
                .Include(m => m.Images) // Bao gồm danh sách hình ảnh liên quan đến tin nhắn
                .Include(m => m.Files)  // Bao gồm danh sách file đính kèm
                .Select(m => new
                {
                    m.Content,
                    m.SentAt,
                    m.SenderId,
                    m.ReceiverId,
                    Avatar = m.Sender.Photo != null
                        ? $"data:image/jpeg;base64,{Convert.ToBase64String(m.Sender.Photo)}"
                        : "/img/logo/UserLogo.png",
                    Images = m.Images.Select(i => new
                    {
                        i.Id,
                        ImageUrl = Url.Action("GetImage", new { id = i.Id }) // Tạo URL cho hình ảnh
                    }),
                    Files = m.Files.Select(f => new
                    {
                        f.Id,
                        f.FileName,
                        f.FileType,
                        FileUrl = Url.Action("GetFile", new { id = f.Id }) // Tạo URL cho file
                    })
                })
                .ToListAsync();

            return Ok(messages);
        }

        [HttpGet("GetMessageForUser/{employerId}")]
        public async Task<IActionResult> GetMessageForUser(string employerId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var messages = await _context.ChatMessages
                .Where(m => (m.SenderId == user.Id && m.ReceiverId == employerId) ||
                            (m.SenderId == employerId && m.ReceiverId == user.Id))
                .OrderBy(m => m.SentAt)
                .Include(m => m.Sender)
                .Include(m => m.Images)
                .Include(m => m.Files)
                .Select(m => new
                {
                    m.Id,               // Bổ sung trường Id của tin nhắn
                    m.Content,
                    m.SentAt,
                    m.IsRecalled,       // Thêm thuộc tính IsRecalled vào
                    SenderId = m.SenderId,
                    ReceiverId = m.ReceiverId,
                    Avatar = m.Sender.Photo != null
                        ? $"data:image/jpeg;base64,{Convert.ToBase64String(m.Sender.Photo)}"
                        : "/img/logo/default-avatar.png",
                    Images = m.Images.Select(i => new
                    {
                        i.Id,
                        ImageUrl = Url.Action("GetImage", new { id = i.Id }), // Tạo URL cho hình ảnh
                        i.IsRecalled
                    }),
                    Files = m.Files.Select(f => new
                    {
                        f.Id,
                        f.FileName,
                        f.FileType,
                        f.IsRecalled,     // Bổ sung trường IsRecalled của file
                        FileUrl = Url.Action("GetFile", new { id = f.Id }) // Tạo URL cho file
                    })
                })
                .ToListAsync();

            return Ok(messages);
        }





        [HttpGet("GetEmployersForUser")]
        public async Task<IActionResult> GetEmployersForUser()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var roles = await _userManager.GetRolesAsync(user);
            bool isEmployerOrUser = roles.Contains("Employer") || roles.Contains("User");

            // Tìm danh sách nhà tuyển dụng đã nhắn tin với user nhưng không bị xóa
            var employerIds = await _context.ChatMessages
                .Where(m =>
                    (m.SenderId == user.Id && !m.IsDeletedBySender) ||  // Tin nhắn từ user chưa bị xóa
                    (m.ReceiverId == user.Id && !m.IsDeletedByReceiver)) // Tin nhắn đến user chưa bị xóa
                .Select(m => m.SenderId == user.Id ? m.ReceiverId : m.SenderId)  // Lấy EmployerId
                .Distinct()
                .ToListAsync();

            // Truy vấn các thông tin cần thiết của nhà tuyển dụng và tin nhắn mới nhất
            var employers = await _context.Users
                .Where(u => employerIds.Contains(u.Id))
                .Select(e => new
                {
                    e.Id,
                    e.Name,
                    Photo = e.Photo != null ? $"data:image/jpeg;base64,{Convert.ToBase64String(e.Photo)}" : "/img/logo/UserLogo.png",
                    UnreadMessages = _context.ChatMessages.Count(x =>
                        x.SenderId == e.Id &&
                        x.ReceiverId == user.Id &&
                        !x.IsRead &&
                        !x.IsDeletedByReceiver),  // Đếm tin nhắn chưa đọc và chưa bị xóa bởi user
                    LastMessageTime = _context.ChatMessages
                        .Where(x =>
                            (x.SenderId == e.Id || x.ReceiverId == e.Id) &&
                            (x.SenderId == user.Id || x.ReceiverId == user.Id) &&
                            !x.IsDeletedBySender && !x.IsDeletedByReceiver)  // Chỉ lấy tin nhắn chưa bị xóa bởi cả hai bên
                        .OrderByDescending(x => x.SentAt)
                        .Select(x => x.SentAt)
                        .FirstOrDefault()
                })
                .OrderByDescending(e => e.LastMessageTime) // Sắp xếp theo thời gian tin nhắn mới nhất
                .ToListAsync();

            // Nếu người dùng có vai trò Employer hoặc User, thêm AdminChat vào danh sách
            if (isEmployerOrUser)
            {
                var adminChat = await _userManager.FindByEmailAsync("adminchat@dntujobs.com");
                if (adminChat != null)
                {
                    // Xóa danh sách cũ nếu có
                    employers.RemoveAll(e => e.Id == adminChat.Id);

                    employers.Insert(0, new
                    {
                        Id = adminChat.Id,
                        Name = adminChat.Name,
                        Photo = adminChat.Photo != null ? $"data:image/jpeg;base64,{Convert.ToBase64String(adminChat.Photo)}" : "/img/logo/AdminLogo.png",
                        UnreadMessages = 0, // Giả định không có tin nhắn chưa đọc từ AdminChat
                        LastMessageTime = DateTime.MinValue // Admin chat không có thời gian tin nhắn
                    });
                }
            }

            return Ok(employers);
        }


        [HttpPost("RecallMessage")]
        public async Task<IActionResult> RecallMessage([FromBody] RecallMessageRequest request)
        {
            var message = await _context.ChatMessages
                .Include(m => m.Images) // Bao gồm hình ảnh
                .Include(m => m.Files)  // Bao gồm tệp đính kèm
                .FirstOrDefaultAsync(m => m.Id == request.MessageId);

            if (message == null)
            {
                return NotFound(new { error = "Message not found" });
            }

            // Cập nhật trạng thái thu hồi tin nhắn
            message.IsRecalled = true;
            message.Content = "Tin nhắn đã được thu hồi"; // Thay đổi nội dung tin nhắn để hiển thị trạng thái thu hồi

            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("MessageRecalled", message.Id);
            return Ok(new
            {
                messageId = message.Id,
                newContent = message.Content,
                isRecalled = message.IsRecalled
            });
        }


        [HttpPost("RecallFile")]
        public async Task<IActionResult> RecallFile([FromBody] RecallFileRequest request)
        {
            var file = await _context.Files.FirstOrDefaultAsync(f => f.Id == request.FileId);

            if (file == null)
            {
                return NotFound(new { error = "File not found" });
            }

            // Đánh dấu trạng thái thu hồi
            file.IsRecalled = true;

            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("FileRecalled", file.Id);

            return Ok(new
            {
                fileId = file.Id,
                isRecalled = file.IsRecalled,
                newContent = "Tin nhắn đã được thu hồi" // Nội dung tin nhắn thu hồi
            });
        }

        [HttpPost("RecallImage")]
        public async Task<IActionResult> RecallImage([FromBody] RecallImageRequest request)
        {
            var image = await _context.ChatImages.FirstOrDefaultAsync(i => i.Id == request.ImageId);

            if (image == null)
            {
                return NotFound(new { error = "Image not found" });
            }

            // Đánh dấu trạng thái thu hồi
            image.IsRecalled = true;

            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("ImageRecalled", image.Id);
            return Ok(new
            {
                imageId = image.Id,
                isRecalled = image.IsRecalled,
                newContent = "Ảnh đã được thu hồi" 
            });
        }

        public class RecallImageRequest
        {
            public int ImageId { get; set; }
        }




        public class RecallFileRequest
        {
            public int FileId { get; set; }
        }

        public class RecallMessageRequest
        {
            public int MessageId { get; set; }
        }




        [HttpPost("UploadImages")]
        public async Task<IActionResult> UploadImages([FromForm] string receiverId, [FromForm] string? message, [FromForm] List<IFormFile> files)
        {
            if (string.IsNullOrEmpty(receiverId))
            {
                return BadRequest(new { error = "Receiver ID is required." });
            }

            var sender = await _userManager.GetUserAsync(User);
            if (sender == null)
            {
                return Unauthorized();
            }

            var receiver = await _context.Users.FirstOrDefaultAsync(u => u.Id == receiverId);
            if (receiver == null)
            {
                return BadRequest(new { error = "The receiver does not exist." });
            }

            var chatMessage = new ChatMessage
            {
                SenderId = sender.Id,
                ReceiverId = receiver.Id,
                Content = message ?? string.Empty,
                SentAt = DateTime.Now,
                Images = new List<ChatImage>(),
                Files = new List<FileTable>()
            };

            // Xử lý tệp tin và hình ảnh
            if (files != null && files.Count > 0)
            {
                foreach (var file in files)
                {
                    if (file.Length > 0)
                    {
                        using (var ms = new MemoryStream())
                        {
                            await file.CopyToAsync(ms);
                            if (IsImage(file.ContentType))
                            {
                                chatMessage.Images.Add(new ChatImage
                                {
                                    ImageData = ms.ToArray(),
                                    ImageName = file.FileName,
                                });
                            }
                            else
                            {
                                chatMessage.Files.Add(new FileTable
                                {
                                    FileData = ms.ToArray(),
                                    FileName = file.FileName,
                                    FileType = file.ContentType,
                                    FileSize = file.Length
                                });
                            }
                        }
                    }
                }
            }

            sender.LastActivity = DateTime.UtcNow;
            receiver.LastActivity = DateTime.UtcNow;
            _context.Users.Update(sender);
            _context.Users.Update(receiver);

            _context.ChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync();

            // Tạo URL cho hình ảnh và tệp
            var imageUrls = chatMessage.Images.Select(i => Url.Action("GetImage", new { id = i.Id })).ToList();

            var fileUrls = chatMessage.Files.Select(f => new
            {
                FileName = f.FileName,         // Đảm bảo tên tệp được gửi đúng
                FileType = f.FileType,         // Đảm bảo kiểu tệp được gửi đúng
                Url = Url.Action("GetFile", new { id = f.Id }) // Đường dẫn tải tệp
            }).ToList();

            // Gửi qua SignalR
            // SignalR notification
            await _hubContext.Clients.User(receiver.Id).SendAsync("ReceiveMessage", new
            {
                SenderId = sender.Id,
                Avatar = GetUserAvatar(sender.Id),
                Message = chatMessage.Content,
                ImageUrls = imageUrls, // URLs của hình ảnh
                FileUrls = chatMessage.Files.Select(f => new
                {
                    FileName = f.FileName,
                    FileType = f.FileType,
                    Url = Url.Action("GetFile", new { id = f.Id }) // Đường dẫn tải file
                }),
                SentAt = chatMessage.SentAt
            });


            return Ok(new
            {
                messageId = chatMessage.Id, // Trả về ID của tin nhắn
                message = chatMessage.Content, // Nội dung tin nhắn
                imageUrls = imageUrls, // URLs của hình ảnh
                imageIds = chatMessage.Images.Select(i => i.Id).ToList(),
                fileUrls = chatMessage.Files.Select(f => new
                {
                    fileId = f.Id,           // Trả về ID của tệp
                    fileName = f.FileName,   // Tên tệp
                    fileType = f.FileType,   // Kiểu tệp
                    url = Url.Action("GetFile", new { id = f.Id }) // Đường dẫn tải tệp
                }).ToList()
            });

        }




        [HttpDelete("DeleteChatHistory")]
        public async Task<IActionResult> DeleteChatHistory(string employerId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            // Lấy tất cả tin nhắn giữa user và employer (cả hai chiều)
            var messages = await _context.ChatMessages
                .Where(m =>
                    (m.SenderId == user.Id && m.ReceiverId == employerId) ||
                    (m.SenderId == employerId && m.ReceiverId == user.Id))
                .ToListAsync();

            if (messages.Any())
            {
                foreach (var message in messages)
                {
                    // Đánh dấu trạng thái xóa theo phía user
                    if (message.SenderId == user.Id)
                    {
                        message.IsDeletedBySender = true;
                    }
                    if (message.ReceiverId == user.Id)
                    {
                        message.IsDeletedByReceiver = true;
                    }
                }

                await _context.SaveChangesAsync(); // Lưu thay đổi vào database
                return Ok();
            }

            return NotFound("No messages found to delete.");
        }











        [HttpGet("GetUserStatus/{userId}")]
        public async Task<IActionResult> GetUserStatus(string userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return NotFound(new { error = "User not found." });
            }

            var nowUtc = DateTime.UtcNow;  // Sử dụng UTC để tránh vấn đề múi giờ
            var lastActivity = user.LastActivity;

            if (lastActivity.HasValue)
            {
                // Tính số phút từ lastActivity đến thời gian hiện tại
                var minutesAgo = (int)Math.Floor((nowUtc - lastActivity.Value).TotalMinutes);

                // Chuyển từ phút sang giờ nếu vượt quá 60 phút
                string timeAgo = minutesAgo >= 60
                    ? $"{minutesAgo / 60} hour{(minutesAgo / 60 > 1 ? "s" : "")} ago"
                    : $"{minutesAgo} minute{(minutesAgo > 1 ? "s" : "")} ago";

                var isOnline = (nowUtc - lastActivity.Value).TotalMinutes <= 5;

                return Ok(new
                {
                    IsOnline = isOnline,
                    LastActivity = lastActivity,
                    TimeAgo = timeAgo // Trả về thời gian tính toán
                });
            }
            return NotFound(new { error = "Last activity not found." });
        }





        [HttpPost("NotifyUserStatus")]
        public async Task<IActionResult> NotifyUserStatus([FromBody] string userId)
        {
            // Lấy thông tin người dùng từ DB
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return NotFound(new { error = "User not found." });
            }

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
                LastActivity = user.LastActivity?.ToString("o"), // Chuyển sang định dạng ISO 8601
                MinutesAgo = minutesAgo
            });

            return Ok(new
            {
                UserId = user.Id,
                IsOnline = isOnline,
                LastActivity = user.LastActivity?.ToString("o"),
                MinutesAgo = minutesAgo
            });
        }










        // Phương thức kiểm tra xem tệp có phải hình ảnh không
        private bool IsImage(string contentType)
        {
            return contentType.StartsWith("image/");
        }

        [HttpGet("GetFile/{id}")]
        public async Task<IActionResult> GetFile(int id)
        {
            var file = await _context.Set<FileTable>().FindAsync(id);
            if (file == null || file.FileData == null)
            {
                _logger.LogWarning($"File with ID {id} not found.");
                return NotFound();
            }

            // Log thông tin file
            _logger.LogInformation($"File {file.FileName} (type: {file.FileType}) retrieved successfully.");

            return File(file.FileData, file.FileType, file.FileName);
        }


        [HttpGet("GetImage/{id}")]
        public async Task<IActionResult> GetImage(int id)
        {
            var image = await _context.Set<ChatImage>().FindAsync(id);
            if (image == null || image.ImageData == null)
            {
                return NotFound();
            }

            // Return the image data as a JPEG file
            return File(image.ImageData, "image/jpeg");
        }

        public class ChatMessageRequest
        {
            public string ReceiverId { get; set; }
            public string Message { get; set; }
        }


    }
}

