using Microsoft.AspNetCore.SignalR;

namespace AttendanceApi.Hubs;

public class AttendanceHub : Hub
{
    // Hub quản lý kết nối WebSocket/SSE từ Frontend Dashboard
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}