using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Pharmacy.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
    }
}
