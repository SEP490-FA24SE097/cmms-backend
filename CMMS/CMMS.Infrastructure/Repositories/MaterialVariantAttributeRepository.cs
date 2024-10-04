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
    public interface IMaterialVariantAttributeRepository : IBaseRepository<MaterialVariantAttribute, Guid>
    {

    }
    public class MaterialVariantAttributeRepository : BaseRepository<MaterialVariantAttribute, Guid>, IMaterialVariantAttributeRepository
    {
        public MaterialVariantAttributeRepository(ApplicationDbContext applicationDbContext) : base(applicationDbContext)
        {

        }
    }
}