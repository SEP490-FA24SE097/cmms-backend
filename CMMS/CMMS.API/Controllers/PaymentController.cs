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
        private readonly ITransaction _efTransaction;

        public PaymentController(IPaymentService paymentService,
            ICurrentUserService currentUserService,
            IVariantService variantService,
            IMaterialService materialService,
            ICustomerBalanceService customerBalanceService,
            IMapper mapper, IStoreService storeService, IMaterialVariantAttributeService materialVariantAttributeService,
            IStoreInventoryService storeInventoryService, IShippingService shippingService)
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

        }
        [HttpPost]
        public async Task<IActionResult> CreatePayment([FromBody] InvoiceData invoiceInfo)
        {
            var customerId = _currentUserService.GetUserId();

            decimal totalCartAmount = 0;
            invoiceInfo.CustomerId = customerId;

            if (invoiceInfo.Amount == null)
            {
                foreach (var cartItem in invoiceInfo.CartItems)
                {
                    var material = await _materialService.FindAsync(Guid.Parse(cartItem.MaterialId));
                    var totalItemPrice = material.SalePrice * cartItem.Quantity;
                    if (cartItem.VariantId != null)
                    {
                        var variant = _variantService.Get(_ => _.Id.Equals(Guid.Parse(cartItem.VariantId))).FirstOrDefault();
                        totalItemPrice = variant.Price * cartItem.Quantity;
                    }
                    totalCartAmount += totalItemPrice;
                }
                invoiceInfo.Amount = totalCartAmount;
            }

            bool result = false;
            CustomerBalanceVM customerBalance = null;
            CustomerBalance customerBalanceEntity = null;
            decimal customerBalanceAvailable = 0;

            switch (invoiceInfo.PaymentType)
            {

                // FIXXXXXXXXXXX
                case PaymentType.OnlinePayment:
                    var paymentRequestData = new PaymentRequestData
                    {
                        CustomerId = customerId,
                        Note = invoiceInfo.Note,
                        OrderInfo = $"Purchase for invoice pirce: {totalCartAmount} VND",
                        Address = invoiceInfo.Address,
                        CartItems = invoiceInfo.CartItems,
                    };

                    // vnpay process 
                    var paymentUrl = _paymentService.VnpayCreatePayPaymentRequestAsync(paymentRequestData);
                    return Ok(paymentUrl);

                // FIXXXXXX
                case PaymentType.DebtInvoice:
                    //customerBalance = _customerBalanceService.GetCustomerBalanceById(customerId);
                    //if (customerBalance != null)
                    //{
                    result = await _paymentService.PaymentDebtInvoiceAsync(invoiceInfo, customerBalanceEntity);
                    if (result)
                        return Ok(new { success = true, message = "Tạo đơn hàng thành công" });
                    //else
                    //{
                    //    return Ok(new { success = false, message = "Số hóa tiền trong điều kiện hóa đơn trả sau của bạn không đủ" });
                    //}
                    //}
                    return Ok(new { success = false, message = "Bạn đăng kí tài khoản có thể sử dụng hóa đơn trả sau" });

                // FIXXXXXX
                case PaymentType.DebtPurchase:
                    customerBalance = _customerBalanceService.GetCustomerBalanceById(customerId);
                    customerBalanceEntity = _mapper.Map<CustomerBalance>(customerBalance);
                    result = await _paymentService.PurchaseDebtInvoiceAsync(invoiceInfo, customerBalanceEntity);
                    if (result)
                        return Ok(new { success = true, message = "Tạo đơn hàng thành công" });
                    break;
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

            var totalAmount = preCheckOutModels.Sum(x => x.TotalStoreAmount);
            var preCheckOutModel = new PreCheckOutModel
            {
                Items = preCheckOutModels,
                TotalAmount = totalAmount,
                Discount = 0,
                SalePrice = totalAmount
            };

            return Ok(new { data = preCheckOutModel });

        }
        //public async Task<IActionResult> CheckoutResponseData([FromBody] CartItemRequest cartItems)
        //{
        //    var user = await _currentUserService.GetCurrentUser();
        //    if (user.Address == null)
        //        return BadRequest("Cần cung cấp địa chỉ nhận hàng của user");
        //    var deliveryAddress = await _currentUserService.GetUserAddress();

        //    // khong group theo theo store id nữa.
        //    PreCheckOutModel preCheckOutModel = new PreCheckOutModel();
        //    decimal totalAmount = 0;


        //    var stores = _storeService.GetAll().ToList();
        //    var listStoreByDistance = await _shippingService.GetListStoreOrderbyDeliveryDistance(deliveryAddress, stores);
        //    List<PreCheckOutItemCartModel> ListStoreVM = new List<PreCheckOutItemCartModel>();

        //    var isContainItem = false;
        //    foreach (var store in listStoreByDistance)
        //    {
        //        decimal totalStoreItemAmout = 0;
        //        PreCheckOutItemCartModel preCheckOutItemCartModel = new PreCheckOutItemCartModel();
        //        List<CartItemVM> listStoresItemVM = new List<CartItemVM>();

        //        if (isContainItem) continue;

        //        foreach (var item in cartItems.CartItems)
        //        {
        //            isContainItem = false;
        //            CartItemVM cartItemVM = _mapper.Map<CartItemVM>(item);
        //            cartItemVM.StoreId = store.Store.Id;
        //            var cartItem = _mapper.Map<CartItem>(cartItemVM);
        //            var storeQuantity = await _storeInventoryService.GetAvailableQuantityInStore(cartItem);
        //            // neu cua hang khong co san pham thi bo qua cua hang do
        //            if (storeQuantity == 0)
        //            {
        //                isContainItem = true;
        //                continue;
        //            }

        //            var material = await _materialService.FindAsync(Guid.Parse(item.MaterialId));

        //            if (cartItemVM.Quantity <= storeQuantity)
        //            {

        //                cartItemVM.ItemName = material.Name;
        //                cartItemVM.SalePrice = material.SalePrice;
        //                cartItemVM.ImageUrl = material.ImageUrl;
        //                cartItemVM.Quantity = cartItemVM.Quantity;
        //                cartItemVM.ItemTotalPrice = material.SalePrice * cartItemVM.Quantity;
        //                if (item.VariantId != null)
        //                {
        //                    var variant = _variantService.Get(_ => _.Id.Equals(Guid.Parse(item.VariantId))).FirstOrDefault();
        //                    var variantAttribute = _materialVariantAttributeService.Get(_ => _.VariantId.Equals(variant.Id)).FirstOrDefault();
        //                    cartItemVM.ItemName += $" | {variantAttribute.Value}";
        //                    cartItemVM.SalePrice = variant.Price;
        //                    cartItemVM.ImageUrl = variant.VariantImageUrl;
        //                    cartItemVM.ItemTotalPrice = variant.Price * cartItemVM.Quantity;
        //                }
        //                totalStoreItemAmout += cartItemVM.ItemTotalPrice;
        //                listStoresItemVM.Add(cartItemVM);

        //                preCheckOutItemCartModel.StoreItems = listStoresItemVM;

        //            }
        //            else
        //            {

        //                cartItemVM.ItemName = material.Name;
        //                cartItemVM.SalePrice = material.SalePrice;
        //                cartItemVM.ImageUrl = material.ImageUrl;
        //                cartItemVM.ItemTotalPrice = material.SalePrice * storeQuantity;

        //                if (item.VariantId != null)
        //                {
        //                    var variant = _variantService.Get(_ => _.Id.Equals(Guid.Parse(item.VariantId))).FirstOrDefault();
        //                    var variantAttribute = _materialVariantAttributeService.Get(_ => _.VariantId.Equals(variant.Id)).FirstOrDefault();
        //                    cartItemVM.ItemName += $" | {variantAttribute.Value}";
        //                    cartItemVM.SalePrice = variant.Price;
        //                    cartItemVM.ImageUrl = variant.VariantImageUrl;
        //                    cartItemVM.ItemTotalPrice = variant.Price * storeQuantity;
        //                }

        //                totalStoreItemAmout += cartItemVM.ItemTotalPrice;

        //                CartItemVM cartItemStore = cartItemVM.Clone();
        //                cartItemStore.Quantity = storeQuantity;
        //                listStoresItemVM.Add(cartItemStore);

        //                cartItemVM.Quantity = cartItem.Quantity - storeQuantity;

        //                // loop again 
        //                var currentTrackingStore = store;
        //                int currentTrackingStoreIndex = listStoreByDistance.IndexOf(currentTrackingStore);
        //                // doan skip nay bi sai 
        //                var listTrackingStoreRemains = listStoreByDistance.Skip(currentTrackingStoreIndex).ToList();

        //                // debug chay k lap vo tan ma respose thi k tra ra cai gi
        //                foreach (var trackingStoreRemain in listTrackingStoreRemains)
        //                {
        //                    var storeReMainsQuantity = await _storeInventoryService.GetAvailableQuantityInStore(cartItem);
        //                    if (storeReMainsQuantity == 0) break;
        //                    while(cartItemVM.Quantity > storeReMainsQuantity)
        //                    {
        //                        cartItemVM.ItemName = material.Name;
        //                        cartItemVM.SalePrice = material.SalePrice;
        //                        cartItemVM.ImageUrl = material.ImageUrl;
        //                        cartItemVM.ItemTotalPrice = material.SalePrice * storeQuantity;

        //                        if (item.VariantId != null)
        //                        {
        //                            var variant = _variantService.Get(_ => _.Id.Equals(Guid.Parse(item.VariantId))).FirstOrDefault();
        //                            var variantAttribute = _materialVariantAttributeService.Get(_ => _.VariantId.Equals(variant.Id)).FirstOrDefault();
        //                            cartItemVM.ItemName += $" | {variantAttribute.Value}";
        //                            cartItemVM.SalePrice = variant.Price;
        //                            cartItemVM.ImageUrl = variant.VariantImageUrl;
        //                            cartItemVM.ItemTotalPrice = variant.Price * storeQuantity;
        //                        }

        //                        totalStoreItemAmout += cartItemVM.ItemTotalPrice;

        //                        cartItemStore = cartItemVM.Clone();
        //                        cartItemStore.Quantity = storeQuantity;
        //                        listStoresItemVM.Add(cartItemStore);

        //                        cartItemVM.Quantity = cartItem.Quantity - storeQuantity;
        //                    }
        //                }

        //                preCheckOutItemCartModel.StoreItems = listStoresItemVM;

        //            }
        //        }

        //        totalAmount += totalStoreItemAmout;
        //        preCheckOutItemCartModel.StoreId = store.Store.Id;
        //        preCheckOutItemCartModel.StoreName = store.Store.Name;
        //        preCheckOutItemCartModel.TotalStoreAmount = totalStoreItemAmout;
        //        preCheckOutItemCartModel.ShippngFree = 999;
        //        preCheckOutItemCartModel.FinalPrice = 999;
        //        ListStoreVM.Add(preCheckOutItemCartModel);

        //    }
        //    preCheckOutModel.Items = ListStoreVM;
        //    preCheckOutModel.TotalAmount = totalAmount;
        //    // handle discount value
        //    preCheckOutModel.Discount = 0;
        //    preCheckOutModel.SalePrice = totalAmount - preCheckOutModel.Discount;
        //    return Ok(new
        //    {
        //        data = preCheckOutModel
        //    });
        //}
    }
}
