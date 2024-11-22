using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Infrastructure.Enums
{
    public enum TransactionType
    {
        SaleItem = 0,
        PurchaseDebtInvoice = 1,
        FixDebt = 2,
        UsingCustomerDebt = 3,
        PurchaseCustomerDebt = 4,
        FixCustomerDebt = 5,
        RefundInvoice = 6,
    }

    public enum TransactionPaymentType
    {
        COD = 0,
        OnlinePayment = 1,
    }
}
