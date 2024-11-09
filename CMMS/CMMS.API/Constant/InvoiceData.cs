using CMMS.Core.Models;

namespace CMMS.API.Constant
{
	public class InvoiceData
	{
		public string? Note { get; set; }
		public string Address { get; set; }
		public PaymentType PaymentType { get; set; }
		public List<CartItem> CartItems { get; set; }
	}
	public enum PaymentType
	{
		DebtInvoice = 0,
		DebtPurchase = 1,
		COD = 3,
		OnlinePayment = 4,
	}
}
