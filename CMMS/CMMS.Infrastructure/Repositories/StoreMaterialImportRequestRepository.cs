using CMMS.Core.Entities;
using CMMS.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMMS.Infrastructure.Repositories
{
    public interface IStoreMaterialImportRequestRepository : IBaseRepository<StoreMaterialImportRequest, Guid>
    {
        // Add any custom methods for this repository, if needed
    }

    public class StoreMaterialImportRequestRepository : BaseRepository<StoreMaterialImportRequest, Guid>, IStoreMaterialImportRequestRepository
    {
        public StoreMaterialImportRequestRepository(ApplicationDbContext applicationDbContext) : base(applicationDbContext)
        {
        }
    }
}