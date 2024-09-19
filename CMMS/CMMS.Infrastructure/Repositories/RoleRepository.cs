using CMMS.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Infrastructure.Repositories
{
    public interface IRoleRepository : IBaseRepository<IdentityRole, String>
    {

    }
    public class RoleRepository : BaseRepository<IdentityRole, String>, IRoleRepository
    {
        public RoleRepository(ApplicationDbContext applicationDbContext) : base(applicationDbContext)
        {
        }
    }
}
