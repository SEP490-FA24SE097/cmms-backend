using CMMS.Core.Entities;
using CMMS.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Infrastructure.Repositories
{
    public interface IShippingDetailRepository : IBaseRepository<ShippingDetail, string>
    {
    }
    public class ShippingDetailRepository : BaseRepository<ShippingDetail, string>, IShippingDetailRepository
    {
        public ShippingDetailRepository(ApplicationDbContext applicationDbContext) : base(applicationDbContext)
        {
        }
    }
}
