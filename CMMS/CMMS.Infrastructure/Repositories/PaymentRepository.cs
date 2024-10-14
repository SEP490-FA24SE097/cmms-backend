using CMMS.Core.Entities;
using CMMS.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Infrastructure.Repositories
{
    public interface IPaymentRepository : IBaseRepository<Payment, string>
    {
    }
    public class PaymentRepository : BaseRepository<Payment, string>, IPaymentRepository
    {
        public PaymentRepository(ApplicationDbContext applicationDbContext) : base(applicationDbContext)
        {
        }
    }
}
