using CMMS.Core.Entities;
using CMMS.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Infrastructure.Repositories
{
    public interface ISupplierRepository : IBaseRepository<Supplier, Guid>
    {

    }
    public class SupplierRepository : BaseRepository<Supplier, Guid>, ISupplierRepository
    {
        public SupplierRepository(ApplicationDbContext applicationDbContext) : base(applicationDbContext)
        {

        }
    }
}