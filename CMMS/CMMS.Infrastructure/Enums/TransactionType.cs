using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Infrastructure.Enums
{
    public enum TransactionType
    {
        DebtInvoice = 0,
        DebtPurchase = 1,
        Cash = 2,
        OnlinePayment = 3,
    }
}
