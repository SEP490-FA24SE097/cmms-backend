using CMMS.Infrastructure.Data;
using CMMS.Infrastructure.Repositories;
using CMMS.Infrastructure.Services;
using CMMS.Infrastructure.Services.Payment;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CMMS.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(connectionString)
                .EnableSensitiveDataLogging()
                .LogTo(Console.WriteLine);   // Log queries and errors to console
            });
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<ITransaction, EfTransaction>();

			services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<ICategoryService, CategoryService>();

            services.AddScoped<IUnitRepository, UnitRepository>();
            services.AddScoped<IUnitService, UnitService>();

            services.AddScoped<ISupplierRepository, SupplierRepository>();
            services.AddScoped<ISupplierService, SupplierService>();

            services.AddScoped<IMaterialRepository, MaterialRepository>();
            services.AddScoped<IMaterialService, MaterialService>();

            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUserService, UserService>();

            services.AddScoped<IRolePermissionRepository, RolePermisisonRepository>();

            services.AddScoped<IUserPermisisonRepository, UserPermisisonRepository>();

            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IRoleService, RoleService>();

            services.AddScoped<IPermissionRepository, PermissionRepository>();
            services.AddScoped<IPermissionSerivce, PermissionService>();

            services.AddScoped<IStoreRepository, StoreRepository>();
            services.AddScoped<IStoreService, StoreService>();

            services.AddScoped<IStoreInventoryRepository, StoreInventoryRepository>();
            services.AddScoped<IStoreInventoryService, StoreInventoryService>();

            services.AddScoped<IAttributeRepository, AttributeRepository>();
            services.AddScoped<IAttributeService, AttributeService>();

            services.AddScoped<IVariantRepository, VariantRepository>();
            services.AddScoped<IVariantService, VariantService>();

            services.AddScoped<IMaterialVariantAttributeRepository, MaterialVariantAttributeRepository>();
            services.AddScoped<IMaterialVariantAttributeService, MaterialVariantAttributeService>();

            services.AddScoped<IBrandRepository, BrandRepository>();
            services.AddScoped<IBrandService, BrandService>();

            services.AddScoped<IImportRepository, ImportRepository>();
            services.AddScoped<IImportService, ImportService>();

            services.AddScoped<IWarehouseRepository, WarehouseRepository>();
            services.AddScoped<IWarehouseService, WarehouseService>();

            services.AddScoped<ICartService, CartService>();

			services.AddScoped<IPaymentRepository, PaymentRepository>();
			services.AddScoped<IPaymentService, PaymentService>();

			services.AddScoped<IInvoiceRepository, InvoiceRepository>();
			services.AddScoped<IInvoiceService, InvoiceService>();

            services.AddScoped<IInvoiceDetailRepository, InvoiceDetailRepository>();
            services.AddScoped<IInvoiceDetailService, InvoiceDetailService>();

            services.AddScoped<IShippingDetailRepository, ShippingDetailRepository>();
            services.AddScoped<IShippingDetailService, ShippingDetailService>();

            services.AddScoped<ITransactionRepository, TransactionRepository>();
            services.AddScoped<ITransactionService, TransactionService>();

            services.AddScoped<ICustomerBalanceRepository, CustomerBalanceRepository>();
            services.AddScoped<ICustomerBalanceService, CustomerBalanceService>();

            services.AddScoped<IGoodsDeliveryNoteRepository, GoodsDeliveryNoteRepository>();
            services.AddScoped<IGoodsDeliveryNoteService, GoodsDeliveryNoteService>();

            services.AddScoped<IGoodsDeliveryNoteDetailRepository, GoodsDeliveryNoteDetailRepository>();
            services.AddScoped<IGoodsDeliveryNoteDetailService, GoodsDeliveryNoteDetailService>();

            services.AddScoped<IStoreMaterialImportRequestRepository, StoreMaterialImportRequestRepository>();
            services.AddScoped<IStoreMaterialImportRequestService, StoreMaterialImportRequestService>();
            return services;
        }
    }
}
