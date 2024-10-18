using CMMS.Core.Entities;
using CMMS.Infrastructure.Data;
using System;

namespace CMMS.Infrastructure.Repositories
{
    public interface IStoreInventoryRepository : IBaseRepository<StoreInventory, Guid>
    {
    }

    public class StoreInventoryRepository : BaseRepository<StoreInventory, Guid>, IStoreInventoryRepository
    {
        public StoreInventoryRepository(ApplicationDbContext applicationDbContext) : base(applicationDbContext)
        {
        }
    }
}
