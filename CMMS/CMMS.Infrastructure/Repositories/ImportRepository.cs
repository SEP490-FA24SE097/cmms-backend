using CMMS.Core.Entities;
using CMMS.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Infrastructure.Repositories
{
    public interface IImportRepository : IBaseRepository<Import, Guid>
    {

    }
    public class ImportRepository : BaseRepository<Import, Guid>, IImportRepository
    {
        public ImportRepository(ApplicationDbContext applicationDbContext) : base(applicationDbContext)
        {

        }
    }
}