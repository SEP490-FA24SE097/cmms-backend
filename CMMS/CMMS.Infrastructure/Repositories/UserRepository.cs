using CMMS.Core.Entities;
using CMMS.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Infrastructure.Repositories
{
    public interface IUserRepository : IBaseRepository<ApplicationUser, Guid>
    {

    }
    public class UserRepository : BaseRepository<ApplicationUser, Guid>, IUserRepository
    {
        public UserRepository(ApplicationDbContext applicationDbContext) : base(applicationDbContext)
        {

        }
    }
}
