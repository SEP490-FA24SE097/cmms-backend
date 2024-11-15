﻿using AutoMapper;
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
        private ICartService _cartService;
        private readonly IVariantService _variantService;
        private readonly IMaterialService _materialService;
        private readonly ICustomerBalanceService _customerBalanceService;
        private readonly IMapper _mapper;
        private readonly IInvoiceService _invoiceService;
        private readonly IInvoiceDetailService _invoiceDetailService;
        private readonly ITransactionService _transactionService;
        private readonly ITransaction _efTransaction;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IShippingDetailService _shippingDetailService;

        public PaymentController(IPaymentService paymentService,
            ICurrentUserService currentUserService,
            ICartService cartService,
            IVariantService variantService,
            IMaterialService materialService,
            ICustomerBalanceService customerBalanceService,
            IMapper mapper)
        {
            _currentUserService = currentUserService;
            _paymentService = paymentService;
            _cartService = cartService;
            _variantService = variantService;
            _materialService = materialService;
            _customerBalanceService = customerBalanceService;
            _mapper = mapper;

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
                    customerBalance = _customerBalanceService.GetCustomerBalanceById(customerId);
                    if (customerBalance != null)
                    {
                        var currentDebt = customerBalance.TotalDebt;
                        customerBalanceAvailable = customerBalance.Balance - (totalCartAmount + currentDebt);
                        if ((decimal)customerBalance.Balance >= customerBalanceAvailable)
                        {
                            invoiceInfo.Amount = totalCartAmount;
                            customerBalanceEntity = _mapper.Map<CustomerBalance>(customerBalance);
                            result = await _paymentService.PaymentDebtInvoiceAsync(invoiceInfo, customerBalanceEntity);
                            if (result)
                                return Ok(new { success = true, message = "Tạo đơn hàng thành công" });
                        }
                        else
                        {
                            return Ok(new { success = false, message = "Số hóa tiền trong điều kiện hóa đơn trả sau của bạn không đủ" });
                        }
                    }
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
                    return Ok(new { success = false, message = "Thất bại" });
            }
            return BadRequest("Faild create payment");
        }

        [HttpGet("vnpay-return")]
        public async Task<IActionResult> VnpayPaymentResponse([FromQuery] VnpayPayResponse vnpayPayResponse)
        {
            var resultData = await _paymentService.VnpayReturnUrl(vnpayPayResponse);
            return Ok(resultData);
        }
    }
}
