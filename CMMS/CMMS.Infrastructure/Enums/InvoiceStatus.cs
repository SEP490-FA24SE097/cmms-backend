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
		Cancel = 4,
	}
	public enum InvoiceType
	{
		Debt = 0,
		Normal = 1,
	}
}
