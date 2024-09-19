using CMMS.Core.Entities;
using CMMS.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Infrastructure.Repositories
{
    public interface IMaterialRepository : IBaseRepository<Material, Guid>
    {

    }
    public class MaterialRepository : BaseRepository<Material, Guid>, IMaterialRepository
    {
        public MaterialRepository(ApplicationDbContext applicationDbContext) : base(applicationDbContext)
        {

        }
    }
}