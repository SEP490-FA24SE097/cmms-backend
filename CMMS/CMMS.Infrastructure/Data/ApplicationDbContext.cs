using CMMS.Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Attribute = CMMS.Core.Entities.Attribute;

namespace CMMS.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<CustomerBalance> CustomerBalances { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceDetail> InvoiceDetails { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<ShippingDetail> ShippingDetails { get; set; }

        public DbSet<Material> Materials { get; set; }
        public DbSet<Unit> Units { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Store> Stores { get; set; }


        public DbSet<UserPermission> UserPermissions { get; set; }

        public DbSet<Brand> Brands { get; set; }
        public DbSet<Attribute> Attributes { get; set; }
        public DbSet<Variant> Variants { get; set; }
        public DbSet<MaterialVariantAttribute> MaterialVariantAttributes { get; set; }
        public DbSet<Import> Imports { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<StoreInventory> StoreInventories { get; set; }
        public DbSet<GoodsDeliveryNote> GoodsDeliveryNotes { get; set; }
        public DbSet<GoodsDeliveryNoteDetail> GoodsDeliveryNoteDetails { get; set; }
        public DbSet<SubImage> SubImages { get; set; }
        public DbSet<StoreMaterialImportRequest> StoreMaterialImportRequests { get; set; }
        public DbSet<ConversionUnit> ConversionUnits { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                var tableName = entityType.GetTableName();
                if (tableName.StartsWith("AspNet"))
                {
                    entityType.SetTableName(tableName.Substring(6));
                }
            }
        }
    }
}
