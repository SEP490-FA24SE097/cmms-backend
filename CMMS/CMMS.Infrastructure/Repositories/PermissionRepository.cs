using CMMS.Core.Entities;
using CMMS.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Infrastructure.Repositories
{
    public interface IPermissionRepository : IBaseRepository<Permission, string>
    {

    }
    public class PermissionRepository : BaseRepository<Permission, string>, IPermissionRepository
    {
        public PermissionRepository(ApplicationDbContext applicationDbContext) : base(applicationDbContext)
        {
        }
    }
}
