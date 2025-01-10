using CMMS.Core.Entities.Configurations;
using CMMS.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Infrastructure.Repositories
{


    public interface IConfigCustomerDiscountRepository : IBaseRepository<ConfigCustomerDiscount, string>
    {

    }
    public class ConfigCustomerDiscountRepository : BaseRepository<ConfigCustomerDiscount, string>, IConfigCustomerDiscountRepository
    {
        public ConfigCustomerDiscountRepository(ApplicationDbContext applicationDbContext) : base(applicationDbContext)
        {

        }
    }
}
