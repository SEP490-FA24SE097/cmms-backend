using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace CMMS.Infrastructure.SignalRHub
{
    public class StoreNotificationHub : Hub
    {
        private static readonly ConcurrentDictionary<string, string> UserConnections = new();

        public override Task OnConnectedAsync()
        {

            var storeId = Context.User?.Identity?.Name ?? Context.ConnectionId;
            UserConnections[storeId] = Context.ConnectionId;

            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.Identity?.Name ?? Context.ConnectionId;
            UserConnections.TryRemove(userId, out _);

            return base.OnDisconnectedAsync(exception);
        }

        public static string? GetConnectionId(string storeId)
        {
            return UserConnections.TryGetValue(storeId, out var connectionId) ? connectionId : null;
        }
       
    }
}
