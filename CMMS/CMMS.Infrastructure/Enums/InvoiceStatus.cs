using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMMS.Infrastructure.Enums
{
	public enum InvoiceStatus
	{
		Pending = 0,
		Approve = 1,
		Shipping = 2,
        Done = 3,
		Cancel = 4,
		Refund = 5,
		DoneInStore = 6,
		NotReceived = 7,
    }
	public enum InvoiceType
	{
		Debt = 0,
		Normal = 1,
	}
}

