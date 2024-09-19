using CMMS.Core.Entities;
using CMMS.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Infrastructure.Repositories
{
    public interface ICategoryRepository : IBaseRepository<Category, Guid>
    {

    }
    public class CategoryRepository : BaseRepository<Category, Guid>, ICategoryRepository
    {
        public CategoryRepository(ApplicationDbContext applicationDbContext) : base(applicationDbContext)
        {

        }
    }
}