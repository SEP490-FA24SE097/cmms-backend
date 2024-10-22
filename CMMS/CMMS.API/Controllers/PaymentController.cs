using CMMS.API.Constant;
using CMMS.API.Services;
using CMMS.Core.Entities;
using CMMS.Infrastructure.Services;
using CMMS.Infrastructure.Services.Payment;
using CMMS.Infrastructure.Services.Payment.Vnpay.Request;
using CMMS.Infrastructure.Services.Payment.Vnpay.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.API.Controllers
{
	[Route("api/payment")]
	[ApiController]
	[AllowAnonymous]
	public class PaymentController : ControllerBase
	{
		private ICurrentUserService _currentUserService;
		private IPaymentService _paymentService;
		private ICartService _cartService;

		public PaymentController(IPaymentService paymentService,
			ICurrentUserService currentUserService, ICartService cartService)
		{
			_currentUserService = currentUserService;
			_paymentService = paymentService;
			_cartService = cartService;

		}
		[HttpPost]
		public async Task<IActionResult> CreatePayment(InvoiceData invoiceInfo)
		{
			var customerId = _currentUserService.GetUserId();
			var totalCartAmount = await _cartService.GetTotalAmountCart(customerId);
			var paymentRequestData = new PaymentRequestData
			{
				Amount = totalCartAmount,
				CustomerId = customerId,
				Note = invoiceInfo.Note,
				OrderInfo = $"Purchase for invoice pirce: {totalCartAmount} VND",
				Address = invoiceInfo.Address,
			};
			var paymentUrl = _paymentService.VnpayCreatePayPaymentRequest(paymentRequestData);
			return Ok(paymentUrl);
		}
		[HttpGet("vnpay-return")]
		public async Task<IActionResult> VnpayPaymentResponse([FromQuery] VnpayPayResponse vnpayPayResponse)
		{
			var resultData = await _paymentService.VnpayReturnUrl(vnpayPayResponse);
			return Ok(resultData);
		}
	}
}
