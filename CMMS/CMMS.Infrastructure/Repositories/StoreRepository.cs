using CMMS.Core.Entities;
using CMMS.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Infrastructure.Repositories
{
    public interface IStoreRepository : IBaseRepository<Store, string> 
    {
    }

    public class StoreRepository : BaseRepository<Store, string>, IStoreRepository
    {
        public StoreRepository(ApplicationDbContext applicationDbContext) : base(applicationDbContext)
        {
        }
    }
}
