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
    public interface IAttributeRepository : IBaseRepository<Attribute, Guid>
    {

    }
    public class AttributeRepository : BaseRepository<Attribute, Guid>, IAttributeRepository
    {
        public AttributeRepository(ApplicationDbContext applicationDbContext) : base(applicationDbContext)
        {

        }
    }
}