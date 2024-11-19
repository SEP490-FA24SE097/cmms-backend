using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace CMMS.Infrastructure.SignalRHub
{
    public class WarehouseNotificationHub:Hub
    {
        public async Task SendLowQuantityAlert(string productName, int quantity)
        {
            await Clients.All.SendAsync("ReceiveLowQuantityAlert", $"Số lượng sản phẩm {productName} trong kho đang ở mức thấp ({quantity} sản phẩm)");
        }
    }
}
