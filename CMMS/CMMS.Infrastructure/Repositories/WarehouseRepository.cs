using CMMS.Core.Entities;
using CMMS.Infrastructure.Data;
using System;

namespace CMMS.Infrastructure.Repositories
{
    public interface IWarehouseRepository : IBaseRepository<Warehouse, Guid>
    {
        
    }

    public class WarehouseRepository : BaseRepository<Warehouse, Guid>, IWarehouseRepository
    {
        public WarehouseRepository(ApplicationDbContext applicationDbContext) : base(applicationDbContext)
        {
        }
    }
}