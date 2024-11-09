using CMMS.API.Constant;
using CMMS.API.Services;
using CMMS.Core.Entities;
using CMMS.Core.Models;
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
        private readonly IVariantService _variantService;
        private readonly IMaterialService _materialService;

        public PaymentController(IPaymentService paymentService,
            ICurrentUserService currentUserService,
            ICartService cartService,
            IVariantService variantService,
            IMaterialService materialService)
        {
            _currentUserService = currentUserService;
            _paymentService = paymentService;
            _cartService = cartService;
            _variantService = variantService;
            _materialService = materialService;

        }
        [HttpPost]
        public async Task<IActionResult> CreatePayment(InvoiceData invoiceInfo)
        {
            var customerId = _currentUserService.GetUserId();
            // get total cart
            var totalCartAmount = 0;
            foreach (var cartItem in invoiceInfo.CartItems)
            {
                var totalItemPrice = 0;
                var material = await _materialService.FindAsync(Guid.Parse(cartItem.MaterialId));
                totalItemPrice = ((int)(material.SalePrice * cartItem.Quantity));
                if (cartItem.VariantId != null)
                {
                    var variant = _variantService.Get(_ => _.Id.Equals(Guid.Parse(cartItem.VariantId))).FirstOrDefault();
                    totalItemPrice = ((int)(variant.Price * cartItem.Quantity));
                }
                totalCartAmount += totalItemPrice; 
            }

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
