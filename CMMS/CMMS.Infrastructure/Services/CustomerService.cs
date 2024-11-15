using CMMS.Infrastructure.Migrations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Infrastructure.Services
{
    public interface ICustomerService
    {
        Task<bool> AddCustomer();

        
    }
    public class CustomerService
    {
    }
}
