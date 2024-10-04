using CMMS.Core.Entities;
using CMMS.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Attribute = CMMS.Core.Entities.Attribute;

namespace CMMS.Infrastructure.Repositories
{
    public interface IVariantRepository : IBaseRepository<Variant, Guid>
    {

    }
    public class VariantRepository : BaseRepository<Variant, Guid>, IVariantRepository
    {
        public VariantRepository(ApplicationDbContext applicationDbContext) : base(applicationDbContext)
        {

        }
    }
}