using CMMS.Core.Entities;
using CMMS.Infrastructure.Data;

namespace CMMS.Infrastructure.Repositories
{
    public interface IRolePermissionRepository :  IBaseRepository<RolePermission, string> { }
    public class RolePermisisonRepository : BaseRepository<RolePermission, string>, IRolePermissionRepository
    {
        public RolePermisisonRepository(ApplicationDbContext applicationDbContext) : base(applicationDbContext)
        {
        }
    }
}
