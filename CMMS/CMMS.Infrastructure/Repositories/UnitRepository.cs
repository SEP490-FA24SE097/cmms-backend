using CMMS.Core.Entities;
using CMMS.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Infrastructure.Repositories
{
    public interface IUnitRepository : IBaseRepository<Unit, Guid>
    {

    }
    public class UnitRepository : BaseRepository<Unit, Guid>, IUnitRepository
    {
        public UnitRepository(ApplicationDbContext applicationDbContext) : base(applicationDbContext)
        {

        }
    }
}