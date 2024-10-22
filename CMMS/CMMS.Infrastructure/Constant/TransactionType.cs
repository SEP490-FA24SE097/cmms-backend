using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Infrastructure.Constant
{
    public class TransactionType
    {
        public const string PaymentOnline = "Online payment";
        public const string PaymentStore = "Purchase in store";
        public const string DebtInvoice = "Debt Invoice";
        public const string PaymentDebtInvoice = "Payment debt invoice";
    }
}
