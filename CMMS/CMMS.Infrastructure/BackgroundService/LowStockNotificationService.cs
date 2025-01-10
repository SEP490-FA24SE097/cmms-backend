using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CMMS.Core.Entities;
using CMMS.Infrastructure.Services;
using CMMS.Infrastructure.SignalRHub;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CMMS.API.Helpers;
using Google.Api.Gax.ResourceNames;
namespace CMMS.Infrastructure.BackgroundService;

public class LowStockNotificationService : Microsoft.Extensions.Hosting.BackgroundService
{
    // private readonly IWarehouseService _warehouseService;
    //  private readonly IStoreInventoryService _storeInventoryService;
    //  private readonly IStoreMaterialImportRequestService _storeMaterialImportRequestService;
    private readonly IHubContext<StoreNotificationHub> _storeHubContext;
    private readonly IHubContext<WarehouseNotificationHub> _WarehouseHubContext;
    private readonly IServiceScopeFactory _scopeFactory;
    private const int ReNotificationPeriodHours = 24;

    private readonly Dictionary<Guid, DateTime> _notifiedProducts = new Dictionary<Guid, DateTime>();
    private readonly Dictionary<Guid, DateTime> _notifiedWarehouseProducts = new Dictionary<Guid, DateTime>();

    public LowStockNotificationService(IHubContext<StoreNotificationHub> storeHubContext, IHubContext<WarehouseNotificationHub> warehouseHubContext, IServiceScopeFactory scopeFactory)
    {
        // _storeInventoryService = storeInventoryService;
        // _warehouseService = warehouseService;
        // _storeMaterialImportRequestService = storeMaterialImportRequestService;
        _storeHubContext = storeHubContext;
        _WarehouseHubContext = warehouseHubContext;
        _scopeFactory = scopeFactory;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var warehouseService = scope.ServiceProvider.GetRequiredService<IWarehouseService>();
            var storeInventoryService = scope.ServiceProvider.GetRequiredService<IStoreInventoryService>();
            var storeMaterialImportRequestService = scope.ServiceProvider.GetRequiredService<IStoreMaterialImportRequestService>();
            var storeIds = await storeInventoryService.GetAll().Select(x => x.StoreId).Distinct().ToListAsync(stoppingToken);
            var lowStockWarehouseProducts = await warehouseService.GetAll().Include(x => x.Material).Include(x => x.Variant)
                .Where(p => p.TotalQuantity - (p.InRequestQuantity ?? 0) <= p.Material.MinStock)
                .ToListAsync(stoppingToken);
            foreach (var warehouseProduct in lowStockWarehouseProducts)
            {
                if (!_notifiedProducts.ContainsKey(warehouseProduct.Id) ||
                    (DateTime.UtcNow - _notifiedProducts[warehouseProduct.Id]).TotalHours >= ReNotificationPeriodHours)
                {
                    // Update the last notified time for this product
                    _notifiedProducts[warehouseProduct.Id] = DateTime.UtcNow;
                    string? name = warehouseProduct.Material.Name;
                    if (warehouseProduct.VariantId != null)
                    {
                        name = warehouseProduct.Variant.SKU;
                    }
                    // Send notification
                    await _WarehouseHubContext.Clients.All.SendAsync(
                    "ReceiveLowQuantityAlert",
                        $"Số lượng sản phẩm {name} trong kho đang ở mức thấp ({warehouseProduct.TotalQuantity} sản phẩm)"
                        ,
                        stoppingToken
                    );

                }
            }
            var warehouseProductsWithSufficientStock = await warehouseService.GetAll().Include(x => x.Material)
                .Where(p => p.TotalQuantity > p.Material.MinStock)
                .Select(p => p.Id)
                .ToListAsync(stoppingToken);
            foreach (var productId in warehouseProductsWithSufficientStock)
            {
                _notifiedWarehouseProducts.Remove(productId); // Remove if product has sufficient stock
            }
            foreach (var storeId in storeIds)
            {
                var lowStockStoreProducts = await storeInventoryService.GetAll().Include(x => x.Material).Include(x=>x.Variant)
                    .Where(p => p.TotalQuantity - (p.InOrderQuantity ?? 0) <= p.MinStock && p.StoreId == storeId)
                    .ToListAsync(stoppingToken);
                foreach (var product in lowStockStoreProducts)
                {
                    // Check if the product is due for a re-notification
                    if (!_notifiedProducts.ContainsKey(product.Id) || (DateTime.UtcNow - _notifiedProducts[product.Id]).TotalHours >= ReNotificationPeriodHours)
                    {
                        // Update the last notified time for this product
                        _notifiedProducts[product.Id] = DateTime.UtcNow;
                        // Send notification
                        string? name = product.Material.Name;
                        if (product.VariantId != null)
                        {
                            name = product.Variant.SKU;
                        }
                        if (StoreNotificationHub.GetConnectionId(storeId) is { } connectionId)
                        {
                            await _storeHubContext.Clients.Client(connectionId).SendAsync("ReceiveLowQuantityAlert", $"Số lượng sản phẩm {name} trong kho đang ở mức thấp ({product.TotalQuantity} sản phẩm) yêu cầu nhập hàng đã được gửi tự động", stoppingToken);
                        };
                        var checkRequest = await storeMaterialImportRequestService.CheckExist(x =>
                            x.MaterialId == product.MaterialId && x.VariantId == product.VariantId && x.StoreId == storeId && x.Status == "Processing");
                        if (!checkRequest)
                        {
                            if (product.ImportQuantity != null && product.ImportQuantity > 0)
                            {
                                await storeMaterialImportRequestService.AddAsync(new StoreMaterialImportRequest()
                                {
                                    Id = Guid.NewGuid(),
                                    MaterialId = product.MaterialId,
                                    VariantId = product.VariantId,
                                    Quantity = product.ImportQuantity == null ? 0 : (decimal)product.ImportQuantity,
                                    Status = "Processing",
                                    StoreId = storeId,
                                    LastUpdateTime = Helpers.TimeConverter.GetVietNamTime()
                                });
                                await storeMaterialImportRequestService.SaveChangeAsync();
                            }
                        }
                    }
                }
            }
            // Clean up: remove entries for products with sufficient stock
            var productsWithSufficientStock = await storeInventoryService.GetAll()
                .Where(p => p.TotalQuantity > p.MinStock)
                .Select(p => p.Id)
                .ToListAsync(stoppingToken);
            foreach (var productId in productsWithSufficientStock)
            {
                _notifiedProducts.Remove(productId); // Remove if product has sufficient stock
            }
            await Task.Delay(60000, stoppingToken);
        }

    }
}



