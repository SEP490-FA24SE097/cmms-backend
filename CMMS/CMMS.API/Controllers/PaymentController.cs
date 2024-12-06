using AutoMapper;
using CMMS.API.Constant;
using CMMS.API.Helpers;
using CMMS.API.Services;
using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Constant;
using CMMS.Infrastructure.Data;
using CMMS.Infrastructure.Enums;
using CMMS.Infrastructure.Services;
using CMMS.Infrastructure.Services.Payment;
using CMMS.Infrastructure.Services.Payment.Vnpay.Request;
using CMMS.Infrastructure.Services.Payment.Vnpay.Response;
using CMMS.Infrastructure.Services.Shipping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;

namespace CMMS.API.Controllers
{
    [Route("api/payment")]
    [ApiController]
    [AllowAnonymous]
    public class PaymentController : ControllerBase
    {
        private ICurrentUserService _currentUserService;
        private IPaymentService _paymentService;
        private readonly IVariantService _variantService;
        private readonly IMaterialService _materialService;
        private readonly ICustomerBalanceService _customerBalanceService;
        private readonly IMapper _mapper;
        private IStoreService _storeService;
        private IMaterialVariantAttributeService _materialVariantAttributeService;
        private IStoreInventoryService _storeInventoryService;
        private IShippingService _shippingService;
        private IUserService _userService;
        private readonly ITransaction _efTransaction;

        public PaymentController(IPaymentService paymentService,
            ICurrentUserService currentUserService,
            IVariantService variantService,
            IMaterialService materialService,
            ICustomerBalanceService customerBalanceService,
            IMapper mapper, IStoreService storeService, IMaterialVariantAttributeService materialVariantAttributeService,
            IStoreInventoryService storeInventoryService, IShippingService shippingService, IUserService userService)
        {
            _currentUserService = currentUserService;
            _paymentService = paymentService;
            _variantService = variantService;
            _materialService = materialService;
            _customerBalanceService = customerBalanceService;
            _mapper = mapper;
            _storeService = storeService;
            _materialVariantAttributeService = materialVariantAttributeService;
            _storeInventoryService = storeInventoryService;
            _shippingService = shippingService;
            _userService = userService;
        }
        [HttpPost]
        public async Task<IActionResult> CreatePayment([FromBody] InvoiceData invoiceInfo)
        {
            var customerId = _currentUserService.GetUserId();

            decimal totalCartAmount = 0;
            invoiceInfo.CustomerId = customerId;

            bool result = false;
            CustomerBalanceVM customerBalance = null;
            CustomerBalance customerBalanceEntity = null;
            decimal customerBalanceAvailable = 0;

            switch (invoiceInfo.PaymentType)
            {

                case PaymentType.OnlinePayment:
                //var paymentRequestData = new PaymentRequestData
                //{
                //    CustomerId = customerId,
                //    Note = invoiceInfo.Note,
                //    OrderInfo = $"Purchase for invoice pirce: {totalCartAmount} VND",
                //    Address = invoiceInfo.Address,
                //    CartItems = invoiceInfo.CartItems,
                //};
                //var paymentUrl = _paymentService.VnpayCreatePayPaymentRequestAsync(paymentRequestData);
                //return Ok(paymentUrl);

                #region payment debt invoice finxing
                case PaymentType.DebtInvoice:
                    result = await _paymentService.PaymentDebtInvoiceAsync(invoiceInfo, customerBalanceEntity);
                    if (result)
                        return Ok(new { success = true, message = "Tạo đơn hàng thành công" });
                    return Ok(new { success = false, message = "Bạn đăng kí tài khoản có thể sử dụng hóa đơn trả sau" });
                case PaymentType.DebtPurchase:
                    customerBalance = _customerBalanceService.GetCustomerBalanceById(customerId);
                    customerBalanceEntity = _mapper.Map<CustomerBalance>(customerBalance);
                    result = await _paymentService.PurchaseDebtInvoiceAsync(invoiceInfo, customerBalanceEntity);
                    if (result)
                        return Ok(new { success = true, message = "Tạo đơn hàng thành công" });
                    break;
                #endregion
                case PaymentType.PurchaseFirst:
                case PaymentType.PurchaseAfter:
                    var paymentResult = await _paymentService.PaymentInvoiceAsync(invoiceInfo);
                    if (paymentResult)
                        return Ok(new { success = true, message = "Tạo đơn hàng thành công" });
                    return BadRequest("Tạo đơn hàng không thành công vui lòng thử lại");
            }
            return BadRequest("Faild create payment");
        }

        [HttpGet("vnpay-return")]
        public async Task<IActionResult> VnpayPaymentResponse([FromQuery] VnpayPayResponse vnpayPayResponse)
        {
            var resultData = await _paymentService.VnpayReturnUrl(vnpayPayResponse);
            return Ok(resultData);
        }

        [HttpPost("pre-checkout")]
        public async Task<IActionResult> CheckoutResponseData([FromBody] CartItemRequest cartItems)
        {
            var user = await _currentUserService.GetCurrentUser();
            if (user.Address == null)
                return BadRequest("Cần cung cấp địa chỉ nhận hàng của user");

            var deliveryAddress = await _currentUserService.GetUserAddress();
            var stores = _storeService.GetAll().ToList();
            var listStoreByDistance = await _shippingService.GetListStoreOrderbyDeliveryDistance(deliveryAddress, stores);

            var preCheckOutModels = await _storeInventoryService.DistributeItemsToStores(cartItems, listStoreByDistance);

            foreach (var result in preCheckOutModels)
            {
                var listStoreItem = result.StoreItems;
                float totalWeight = 0;
                foreach (var item in listStoreItem)
                {
                    var weight = await _materialService.GetWeight(Guid.Parse(item.MaterialId), Guid.Parse(item.VariantId));
                    totalWeight += (float)weight;
                }
                // change m to km
                var storeDistance = result.ShippingDistance / 1000;
                var shippingFee = _shippingService.CalculateShippingFee((decimal)storeDistance, (decimal)totalWeight);
                result.ShippngFree = shippingFee;
                result.FinalPrice = shippingFee + result.TotalStoreAmount;
            }

            // handle final price

            var totalAmount = preCheckOutModels.Sum(x => x.FinalPrice);
            var discountPrice = await _userService.GetCustomerDiscountPercentAsync((decimal)totalAmount, user.Id);
            var preCheckOutModel = new PreCheckOutModel
            {
                Items = preCheckOutModels,
                TotalAmount = totalAmount,
                Discount = discountPrice,
                SalePrice = totalAmount - discountPrice,
            };

            return Ok(new { data = preCheckOutModel });

        }
    }
}
