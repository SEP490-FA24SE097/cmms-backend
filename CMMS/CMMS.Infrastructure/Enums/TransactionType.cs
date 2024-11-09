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
        PurchaseFirst = 2,
        PurchaseAfter = 3,
        OnlinePayment = 4,
    }
}
