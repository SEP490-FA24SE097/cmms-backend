using CMMS.Core.Entities;
using CMMS.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Infrastructure.Repositories
{
    public interface IInvoiceDetailRepository : IBaseRepository<InvoiceDetail, string>
    {
    }
    public class InvoiceDetailRepository : BaseRepository<InvoiceDetail, string>, IInvoiceDetailRepository
    {
        public InvoiceDetailRepository(ApplicationDbContext applicationDbContext) : base(applicationDbContext)
        {
        }
    }
}
