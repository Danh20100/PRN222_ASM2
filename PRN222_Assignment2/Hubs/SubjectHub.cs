using Microsoft.AspNetCore.SignalR;

namespace PRN222_Assignment2.Hubs;

public class SubjectHub : Hub
{
    // Hub này đóng vai trò là kênh giao tiếp. 
    // Các phương thức Broadcast sẽ được gọi từ phía Server (IHubContext)
}
