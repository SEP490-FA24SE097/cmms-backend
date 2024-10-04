using CMMS.Core.Entities;
using CMMS.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Infrastructure.Repositories
{
    public interface IBrandRepository : IBaseRepository<Brand, Guid>
    {

    }
    public class BrandRepository : BaseRepository<Brand, Guid>, IBrandRepository
    {
        public BrandRepository(ApplicationDbContext applicationDbContext) : base(applicationDbContext)
        {

        }
    }
}