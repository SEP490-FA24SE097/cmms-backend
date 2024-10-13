using CMMS.Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Attribute = CMMS.Core.Entities.Attribute;

namespace CMMS.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
        public DbSet<Permission> Permissions { get;set; }
        public DbSet<RolePermission> RolePermissions { get;set; }

        public DbSet<Material>  Materials { get;set; }
        public DbSet<Unit> Units  { get;set; }
        public DbSet<Category> Categories  { get;set; }
        public DbSet<Supplier> Suppliers { get;set; }
        

        public DbSet<UserPermission> UserPermissions { get;set; }

        public DbSet<Brand> Brands { get;set; }
        public DbSet<Attribute> Attributes { get; set; }
        public DbSet<Variant> Variants { get;set; }
        public DbSet<MaterialVariantAttribute> MaterialVariantAttributes { get;set; }
        public DbSet<Import> Imports { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
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
