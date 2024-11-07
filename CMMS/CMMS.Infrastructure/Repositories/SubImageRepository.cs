using CMMS.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CMMS.Core.Entities;
using Attribute = System.Attribute;

namespace CMMS.Infrastructure.Repositories
{
    public interface ISubImageRepository : IBaseRepository<SubImage, Guid>
    {

    }
    public class SubImageRepository : BaseRepository<SubImage, Guid>, ISubImageRepository
    {
        public SubImageRepository(ApplicationDbContext applicationDbContext) : base(applicationDbContext)
        {

        }
    }
}
