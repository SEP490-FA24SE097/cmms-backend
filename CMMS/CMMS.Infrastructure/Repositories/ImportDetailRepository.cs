using CMMS.Core.Entities;
using CMMS.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Infrastructure.Repositories
{
    public interface IImportDetailRepository : IBaseRepository<ImportDetail, Guid>
    {

    }
    public class ImportDetailRepository : BaseRepository<ImportDetail, Guid>, IImportDetailRepository
    {
        public ImportDetailRepository(ApplicationDbContext applicationDbContext) : base(applicationDbContext)
        {

        }
    }
}