using AutoMapper;
using Azure;
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
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Net.Http;
using System.Net.WebSockets;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
        private readonly HttpClient _httpClient;

        public PaymentController(IPaymentService paymentService,
            ICurrentUserService currentUserService,
            IVariantService variantService,
            IMaterialService materialService,
            ICustomerBalanceService customerBalanceService,
            IMapper mapper, IStoreService storeService, IMaterialVariantAttributeService materialVariantAttributeService,
            IStoreInventoryService storeInventoryService, IShippingService shippingService, IUserService userService, HttpClient httpClient)
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
            _httpClient = httpClient;
        }
        [HttpPost]
        public async Task<IActionResult> CreatePayment([FromBody] InvoiceData invoiceInfo)
        {
            var customerId = _currentUserService.GetUserId();

            invoiceInfo.CustomerId = customerId;

            bool result = false;
            CustomerBalanceVM customerBalance = null;
            CustomerBalance customerBalanceEntity = null;

            switch (invoiceInfo.PaymentType)
            {
                case PaymentType.OnlinePayment:
                    var paymentRequestData = new PaymentRequestData
                    {
                        CustomerId = customerId,
                        Note = invoiceInfo.Note,
                        OrderInfo = $"Purchase for invoice pirce: {invoiceInfo.SalePrice} VND",
                        Address = invoiceInfo.Address,
                        PreCheckOutItemCartModel = invoiceInfo.PreCheckOutItemCartModel,
                        TotalAmount = invoiceInfo.SalePrice,
                    };
                    var paymentUrl = _paymentService.VnpayCreatePayPaymentRequestAsync(paymentRequestData);
                    return Ok(paymentUrl);

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
            if(resultData.PaymentStatus == "00")
            {
                return Redirect(resultData.RedirectUrl);
            } else
            {
                return Redirect(resultData.RedirectUrl);
            }
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
                    var weight = await _materialService.GetWeight(item.MaterialId, item.VariantId);
                    totalWeight += (float)(weight * (float)item.Quantity);
                }
                // change m to km
                var storeDistance = result.ShippingDistance / 1000;

                if (storeDistance >= 200)
                {
                    result.IsOver200km = true;
                }
                else
                {
                    var shippingFee = _shippingService.CalculateShippingFee((decimal)storeDistance, (decimal)totalWeight);
                  
                    decimal roundedAmountShippingFee = (int)Math.Round((double)shippingFee / 1000.0) * 1000; ;
                    result.ShippngFree = roundedAmountShippingFee;
                    result.FinalPrice = roundedAmountShippingFee + result.TotalStoreAmount;
                }
            }
            // handle final price
            var totalAmount = preCheckOutModels.Sum(x => x.FinalPrice);
            var totalShippingFee = preCheckOutModels.Sum(x => x.ShippngFree);
            var discountPrice = await _userService.GetCustomerDiscountPercentAsync((decimal)totalAmount, user.Id);
            var preCheckOutModel = new PreCheckOutModel
            {
                Items = preCheckOutModels,
                TotalAmount = totalAmount - totalShippingFee,
                Discount = discountPrice,
                SalePrice = totalAmount - discountPrice,
                ShippingFee = totalShippingFee
            };
            return Ok(new { data = preCheckOutModel });

        }

        [HttpPost("update-pre-checkout")]
        public async Task<IActionResult> UpdateCheckoutResponseData([FromBody] List<PreCheckOutItemCartModel> preCheckOutModels)
        {
            var user = await _currentUserService.GetCurrentUser();
            if (user.Address == null)
                return BadRequest("Cần cung cấp địa chỉ nhận hàng của user");

            foreach (var result in preCheckOutModels)
            {
                var listStoreItem = result.StoreItems;
                float totalWeight = 0;
                var storeId = result.StoreId;
                List<CartItemVM> cartItemVMs = new List<CartItemVM>();
                foreach (var item in listStoreItem)
                {

                    var material = await _materialService.FindAsync(Guid.Parse(item.MaterialId));
                    var itemTotalPrice = material.SalePrice * item.Quantity;

                    var cartItemVM = new CartItemVM
                    {
                        MaterialId = item.MaterialId,
                        VariantId = item.VariantId,
                        Quantity = item.Quantity,
                        ItemName = material.Name,
                        SalePrice = material.SalePrice,
                        ItemTotalPrice = itemTotalPrice,
                        ImageUrl = material.ImageUrl,
                    };

                    // Xử lý biến thể (variant) nếu có
                    if (!string.IsNullOrEmpty(item.VariantId))
                    {
                        var variant = _variantService.Get(_ => _.Id.Equals(Guid.Parse(item.VariantId))).Include(x => x.MaterialVariantAttributes).FirstOrDefault();
                        if (variant != null)
                        {
                            if (!variant.MaterialVariantAttributes.IsNullOrEmpty())
                            {
                                var variantAttributes = _materialVariantAttributeService.Get(_ => _.VariantId.Equals(variant.Id)).Include(x => x.Attribute).ToList();
                                var attributesString = string.Join('-', variantAttributes.Select(x => $"{x.Attribute.Name} :{x.Value} "));
                                cartItemVM.ItemName = $"{variant.SKU} {attributesString}";
                            }
                            else
                            {
                                cartItemVM.ItemName = $"{variant.SKU}";
                            }
                            cartItemVM.SalePrice = variant.Price;
                            cartItemVM.ImageUrl = variant.VariantImageUrl;
                            cartItemVM.ItemTotalPrice = variant.Price * cartItemVM.Quantity;
                        }
                    }

                    var weight = await _materialService.GetWeight(item.MaterialId, item.VariantId);
                    totalWeight += (float)(weight * (float)item.Quantity);
                    var storeInventoryItem = _mapper.Map<CartItem>(cartItemVM);
                    storeInventoryItem.StoreId = storeId;
                    cartItemVM.InStock = await _storeInventoryService.GetAvailableQuantityInStore(storeInventoryItem);

                    cartItemVMs.Add(cartItemVM);
                }
                result.StoreItems = cartItemVMs;
                // change m to km
                var storeDistance = result.ShippingDistance / 1000;

                if (storeDistance >= 200)
                {
                    result.IsOver200km = true;
                }
                else
                {
                    var shippingFee = _shippingService.CalculateShippingFee((decimal)storeDistance, (decimal)totalWeight);
                    decimal roundedAmount = Math.Floor(shippingFee);
                    result.ShippngFree = roundedAmount;
                    result.FinalPrice = shippingFee + result.TotalStoreAmount;
                }
          
            }
            // handle final price
            var totalAmount = preCheckOutModels.Sum(x => x.FinalPrice);
            var totalShippingFee = preCheckOutModels.Sum(x => x.ShippngFree);
            var discountPrice = await _userService.GetCustomerDiscountPercentAsync((decimal)totalAmount, user.Id);
            var preCheckOutModel = new PreCheckOutModel
            {
                Items = preCheckOutModels,
                TotalAmount = totalAmount - totalShippingFee,
                Discount = discountPrice,
                SalePrice = totalAmount - discountPrice,
                ShippingFee = totalShippingFee
            };
            return Ok(new { data = preCheckOutModel });

        }

    }
}
