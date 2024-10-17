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
		PaymentSucces = 1,
		Done = 2,
		Debt = 3,
		Cancel = 4,
	}
}
