using CMMS.Core.Entities;
using CMMS.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Infrastructure.Repositories
{
    public interface IUserPermisisonRepository : IBaseRepository<UserPermission, String> { }
    public class UserPermisisonRepository : BaseRepository<UserPermission, string>, IUserPermisisonRepository
    {
        public UserPermisisonRepository(ApplicationDbContext applicationDbContext) : base(applicationDbContext)
        {
        }
    }

}
