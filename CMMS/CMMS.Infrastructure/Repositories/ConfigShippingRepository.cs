using CMMS.Core.Entities;
using CMMS.Core.Entities.Configurations;
using CMMS.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Infrastructure.Repositories
{

    public interface IConfigShippingRepository : IBaseRepository<ConfigShipping, string>
    {

    }
    public class ConfigShippingRepository : BaseRepository<ConfigShipping, string>, IConfigShippingRepository
    {
        public ConfigShippingRepository(ApplicationDbContext applicationDbContext) : base(applicationDbContext)
        {

        }
    }
}
