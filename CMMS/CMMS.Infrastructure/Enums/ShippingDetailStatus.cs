using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Infrastructure.Enums
{
    public enum ShippingDetailStatus
    {
        Pending = 0,
        RequestToChange = 1,
        Approved = 2,
        Rejected = 3,
        NotRecived = 4,
        Done = 5,
    }
}
