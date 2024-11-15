﻿using AutoMapper;
using CMMS.API.Helpers;
using CMMS.API.OptionsSetup;
using CMMS.API.Services;
using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Constant;
using CMMS.Infrastructure.Data;
using CMMS.Infrastructure.Enums;
using CMMS.Infrastructure.Services;
using CMMS.Infrastructure.Services.Payment;
using CMMS.Infrastructure.Services.Payment.Vnpay.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CMMS.API.Controllers
{
    [Route("api/invoices")]
    [ApiController]
    [AllowAnonymous]
    public class InvoiceController : ControllerBase
    {
        private IInvoiceService _invoiceService;
        private IInvoiceDetailService _invoiceDetailService;
        private IMapper _mapper;
        private IShippingDetailService _shippingDetailService;
        private ICartService _cartService;
        private readonly IMaterialVariantAttributeService _materialVariantAttributeService;
        private readonly IVariantService _variantService;
        private readonly IMaterialService _materialService;
        private readonly IUserService _userService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IPaymentService _paymentService;
        private readonly ITransactionService _transactionService;
        private readonly IStoreInventoryService _storeInventoryService;
        private readonly ITransaction _efTransaction;

        public InvoiceController(IInvoiceService invoiceService,
            IInvoiceDetailService invoiceDetailService, IMapper mapper,
            IShippingDetailService shippingDetailService,
            ICartService cartService,
            IVariantService variantService,
            IMaterialService materialService,
            IMaterialVariantAttributeService materialVariantAttributeService,
            IUserService userService, ICurrentUserService currentUserService,
            IPaymentService paymentService, ITransactionService transactionService,
            IStoreInventoryService storeInventoryService, ITransaction transaction)
        {
            _invoiceService = invoiceService;
            _invoiceDetailService = invoiceDetailService;
            _mapper = mapper;
            _shippingDetailService = shippingDetailService;
            _cartService = cartService;
            _materialVariantAttributeService = materialVariantAttributeService;
            _variantService = variantService;
            _materialService = materialService;
            _userService = userService;
            _currentUserService = currentUserService;
            _paymentService = paymentService;
            _transactionService = transactionService;
            _storeInventoryService = storeInventoryService;
            _efTransaction = transaction;
        }

        [HttpGet]
        public async Task<IActionResult> GetInvoicesAsync([FromQuery] InvoiceFitlerModel filterModel)
        {
            var fitlerList = _invoiceService
            .Get(_ =>
            (!filterModel.FromDate.HasValue || _.InvoiceDate >= filterModel.FromDate) &&
            (!filterModel.ToDate.HasValue || _.InvoiceDate <= filterModel.ToDate) &&
            (string.IsNullOrEmpty(filterModel.Id) || _.Id.Equals(filterModel.Id)) &&
            (string.IsNullOrEmpty(filterModel.StoreId) || _.StoreId.Equals(filterModel.StoreId)) &&
            (string.IsNullOrEmpty(filterModel.CustomerName) || _.Customer.FullName.Equals(filterModel.CustomerName)) &&
            (string.IsNullOrEmpty(filterModel.CustomerId) || _.Customer.Id.Equals(filterModel.Id)) &&
            (filterModel.InvoiceType == null || _.InvoiceType.Equals(filterModel.InvoiceType)) &&
            (filterModel.InvoiceStatus == null || _.InvoiceStatus.Equals(filterModel.InvoiceStatus))
            , _ => _.Customer);
            var total = fitlerList.Count();
            var filterListPaged = fitlerList.ToPageList(filterModel.defaultSearch.currentPage, filterModel.defaultSearch.perPage)
                .Sort(filterModel.defaultSearch.sortBy, filterModel.defaultSearch.isAscending);
            var result = _mapper.Map<List<InvoiceVM>>(filterListPaged);

            foreach (var invoice in result)
            {
                var invoiceDetailList = _invoiceDetailService.Get(_ => _.InvoiceId.Equals(invoice.Id));
                var shippingDetail = _shippingDetailService.Get(_ => _.InvoiceId.Equals(invoice.Id), _ => _.Shipper).FirstOrDefault();
                invoice.InvoiceDetails = _mapper.Map<List<InvoiceDetailVM>>(invoiceDetailList.ToList());

                // load data in invoice Detail 
                foreach (var invoiceDetail in invoice.InvoiceDetails)
                {
                    var itemInStoreModel = _mapper.Map<AddItemModel>(invoiceDetail);
                    itemInStoreModel.StoreId = invoice.StoreId;
                    var item = await _cartService.GetItemInStoreAsync(itemInStoreModel);
                    if (item != null)
                    {
                        var material = await _materialService.FindAsync(Guid.Parse(invoiceDetail.MaterialId));
                        invoiceDetail.ItemName = material.Name;
                        invoiceDetail.SalePrice = material.SalePrice;
                        invoiceDetail.ImageUrl = material.ImageUrl;
                        invoiceDetail.ItemTotalPrice = material.SalePrice * invoiceDetail.Quantity;
                        if (invoiceDetail.VariantId != null)
                        {
                            var variant = _variantService.Get(_ => _.Id.Equals(Guid.Parse(invoiceDetail.VariantId))).FirstOrDefault();
                            var variantAttribute = _materialVariantAttributeService.Get(_ => _.VariantId.Equals(variant.Id)).FirstOrDefault();
                            invoiceDetail.ItemName += $" | {variantAttribute.Value}";
                            invoiceDetail.SalePrice = variant.Price;
                            invoiceDetail.ImageUrl = variant.VariantImageUrl;
                            invoiceDetail.ItemTotalPrice = variant.Price * invoiceDetail.Quantity;
                        }
                    }
                }

                invoice.shippingDetailVM = _mapper.Map<ShippingDetaiInvoicelVM>(shippingDetail);
            }

            return Ok(new
            {
                data = result,
                pagination = new
                {
                    total,
                    perPage = filterModel.defaultSearch.perPage,
                    currentPage = filterModel.defaultSearch.currentPage,
                }
            });
        }

        [HttpGet("invoice-detail")]
        public async Task<IActionResult> GetInvoicesDetailAsync([FromQuery] InvoiceDetailFitlerModel filterModel)
        {
            var invoiceDetails = _invoiceDetailService
            .Get(_ => _.InvoiceId.Equals(filterModel.InvoiceId));
            var total = invoiceDetails.Count();
            var invoiceDetailsPaged = invoiceDetails.ToPageList(filterModel.defaultSearch.currentPage, filterModel.defaultSearch.perPage)
                .Sort(filterModel.defaultSearch.sortBy, filterModel.defaultSearch.isAscending);
            var result = _mapper.Map<List<InvoiceDetailVM>>(invoiceDetailsPaged);

            foreach (var invoiceItem in result)
            {
                var itemInStoreModel = _mapper.Map<AddItemModel>(invoiceItem);
                var item = await _cartService.GetItemInStoreAsync(itemInStoreModel);
                if (item != null)
                {
                    var material = await _materialService.FindAsync(Guid.Parse(invoiceItem.MaterialId));
                    invoiceItem.ItemName = material.Name;
                    invoiceItem.SalePrice = material.SalePrice;
                    invoiceItem.ImageUrl = material.ImageUrl;
                    invoiceItem.ItemTotalPrice = material.SalePrice * invoiceItem.Quantity;
                    if (invoiceItem.VariantId != null)
                    {
                        var variant = _variantService.Get(_ => _.Id.Equals(Guid.Parse(invoiceItem.VariantId))).FirstOrDefault();
                        var variantAttribute = _materialVariantAttributeService.Get(_ => _.VariantId.Equals(variant.Id)).FirstOrDefault();
                        invoiceItem.ItemName += $" | {variantAttribute.Value}";
                        invoiceItem.SalePrice = variant.Price;
                        invoiceItem.ImageUrl = variant.VariantImageUrl;
                        invoiceItem.ItemTotalPrice = variant.Price * invoiceItem.Quantity;
                    }
                }
            }

            var invoice = await _invoiceService.FindAsync(filterModel.InvoiceId);
            var invoiceVM = _mapper.Map<InvoiceVM>(invoice);
            return Ok(new
            {
                data = new
                {
                    totalAmounmt = invoiceVM.TotalAmount,
                    result,
                },
                pagination = new
                {
                    total,
                    perPage = filterModel.defaultSearch.perPage,
                    currentPage = filterModel.defaultSearch.currentPage,
                }
            });
        }


        [HttpPost("create-invoice")]
        public async Task<IActionResult> CreatePayment([FromBody] InvoiceStoreData invoiceInfo)
        {

            var totalAmount = invoiceInfo.TotalAmount;
            var discount = invoiceInfo.Discount;
            var salePrices = invoiceInfo.SalePrice;
            var customerPaid = invoiceInfo.CustomerPaid;
            var customerId = invoiceInfo.CustomerId;
            var shipperId = invoiceInfo.ShipperId;
            var phoneRecevied = invoiceInfo.PhoneReceive;
            var note = invoiceInfo.Note;

            var storeManager = await _currentUserService.GetCurrentUser();
            try
            {
                // hoa don do cua hang tu tao
                if (invoiceInfo.InvoiceId == null)
                {
                    var responseMessage = "Không đủ số lượng tồn kho cho sản phẩm ";
                    var isValidQuantity = true;

                    // validate quantity in stock again
                    foreach (var item in invoiceInfo.StoreItems)
                    {
                        var addItemModel = _mapper.Map<AddItemModel>(item);
                        var storeItem = await _cartService.GetItemInStoreAsync(addItemModel);
                        var material = await _materialService.FindAsync(Guid.Parse(item.MaterialId));
                        var storeItemName = material.Name;

                        if (item.VariantId != null)
                        {
                            var variant = _variantService.Get(_ => _.Id.Equals(Guid.Parse(item.VariantId))).FirstOrDefault();
                            var variantAttribute = _materialVariantAttributeService.Get(_ => _.VariantId.Equals(variant.Id)).FirstOrDefault();
                            storeItemName += $" | {variantAttribute.Value}";
                        }
                        if (item.Quantity >= storeItem.TotalQuantity)
                        {
                            responseMessage += $", {storeItemName}";
                            isValidQuantity = false;
                        }
                    }
                    if (!isValidQuantity) return Ok(new { success = false, message = responseMessage });
                    // generate invoice
                    var invoiceCode = _invoiceService.GenerateInvoiceCode();
                    // insert invoice
                    var invoice = new Invoice
                    {
                        Id = invoiceCode,
                        CustomerId = customerId,
                        InvoiceDate = DateTime.Now,
                        InvoiceStatus = (int)InvoiceStatus.Shipping,
                        InvoiceType = (int)InvoiceType.Normal,
                        Note = note,
                        StoreId = storeManager.StoreId,
                        StaffId = storeManager.Id,
                        // get total cart 
                        TotalAmount = (decimal)totalAmount,
                        SalePrice = invoiceInfo.SalePrice,
                        SellPlace = (int)SellPlace.InStore,
                    };

                    await _invoiceService.AddAsync(invoice);
                    await _invoiceService.SaveChangeAsync();

                    Transaction transaction = null;

                    transaction = new Transaction();
                    transaction.Id = "DH" + invoiceCode;
                    transaction.TransactionType = (int)TransactionType.SaleItem;
                    transaction.TransactionDate = DateTime.Now;
                    transaction.CustomerId = customerId;
                    transaction.InvoiceId = invoice.Id;
                    transaction.TransactionPaymentType = 1;
                    transaction.Amount = (decimal)salePrices;
                    await _transactionService.AddAsync(transaction);

                    if (invoiceInfo.CustomerPaid > 0)
                    {
                        // tao them 1 transaction nua la thanh toan cho hoa don do.
                        invoice.CustomerPaid = invoiceInfo.CustomerPaid;

                        transaction = new Transaction();
                        transaction.Id = "TT" + invoiceCode;
                        transaction.TransactionType = (int)TransactionType.PurchaseDebtInvoice;
                        transaction.TransactionDate = DateTime.Now;
                        transaction.CustomerId = customerId;
                        transaction.InvoiceId = invoice.Id;
                        transaction.Amount = (decimal)customerPaid;
                        transaction.TransactionPaymentType = 1;
                        await _transactionService.AddAsync(transaction);
                    }
                    await _invoiceService.SaveChangeAsync();
                    // generate invoice detail
                    foreach (var item in invoiceInfo.StoreItems)
                    {
                        var material = await _materialService.FindAsync(Guid.Parse(item.MaterialId));
                        var totalItemPrice = material.SalePrice * item.Quantity;
                        if (item.VariantId != null)
                        {
                            var variant = _variantService.Get(_ => _.Id.Equals(Guid.Parse(item.VariantId))).FirstOrDefault();
                            totalItemPrice = variant.Price * item.Quantity;
                        }
                        // insert invoice Details
                        var invoiceDetail = new InvoiceDetail
                        {
                            Id = Guid.NewGuid().ToString(),
                            LineTotal = totalItemPrice,
                            MaterialId = Guid.Parse(item.MaterialId),
                            VariantId = item.VariantId != null ? Guid.Parse(item.VariantId) : null,
                            Quantity = item.Quantity,
                            InvoiceId = invoiceCode,
                        };
                        // update store quantity
                        var updateQuantityStatus = await _paymentService.UpdateStoreInventoryAsync(item);
                        if (updateQuantityStatus == 0)
                            // chỗ này phải lock process của luồng này lại k cho chạy đồng thời.
                            return Ok(new { success = false, message = "Số lượng hàng hóa có biến động kiểm tra lại" });
                        await _invoiceDetailService.AddAsync(invoiceDetail);
                    }
                    // generate shipping detail
                    var shippingDetail = new ShippingDetail();
                    shippingDetail.Id = "GH" + invoiceCode;
                    shippingDetail.Invoice = invoice;
                    shippingDetail.PhoneReceive = invoiceInfo.PhoneReceive;
                    shippingDetail.EstimatedArrival = DateTime.Now.AddDays(3);
                    shippingDetail.Address = invoiceInfo.Address;
                    shippingDetail.ShipperId = shipperId;
                    await _shippingDetailService.AddAsync(shippingDetail);
                    var result = await _shippingDetailService.SaveChangeAsync();
                    await _efTransaction.CommitAsync();
                    if (result) return Ok(new { success = true, message = "Tạo đơn hàng thành công" });
                }
                // hóa đơn từ khách hàng
                else
                {
                    var invoice = await _invoiceService.FindAsync(invoiceInfo.InvoiceId);
                    string invoiceCode = invoice.Id;
                    invoice.StoreId = storeManager.StoreId;
                    invoice.InvoiceStatus = (int)InvoiceStatus.Shipping;
                    invoice.InvoiceType = (int)InvoiceType.Normal;
                    invoice.Note = note;
                    invoice.StaffId = storeManager.Id;
                    invoice.TotalAmount = (decimal)totalAmount;
                    invoice.SalePrice = salePrices;
                    invoice.Discount = discount;
                    invoice.CustomerPaid = customerPaid;

                    _invoiceService.Update(invoice);

                    // dieu chinh don hang cua khach hang.
                    if (invoiceInfo.StoreItems != null)
                    {

                    }

                    // var invoiceDetails
                    var invoiceDetails = _invoiceDetailService.Get(_ => _.InvoiceId.Equals(invoiceCode));
                    foreach (var invoiceDetail in invoiceDetails)
                    {

                    }

                    Transaction transaction = null;

                    transaction = new Transaction();
                    transaction.Id = "DH" + invoiceCode;
                    transaction.TransactionType = (int)TransactionType.SaleItem;
                    transaction.TransactionDate = DateTime.Now;
                    transaction.CustomerId = customerId;
                    transaction.InvoiceId = invoice.Id;
                    transaction.TransactionPaymentType = 1;
                    transaction.Amount = (decimal)salePrices;
                    await _transactionService.AddAsync(transaction);

                    if (invoiceInfo.CustomerPaid > 0)
                    {
                        // tao them 1 transaction nua la thanh toan cho hoa don do.
                        invoice.CustomerPaid = invoiceInfo.CustomerPaid;

                        transaction = new Transaction();
                        transaction.Id = "TT" + invoiceCode;
                        transaction.TransactionType = (int)TransactionType.PurchaseDebtInvoice;
                        transaction.TransactionDate = DateTime.Now;
                        transaction.CustomerId = customerId;
                        transaction.InvoiceId = invoice.Id;
                        transaction.Amount = (decimal)customerPaid;
                        transaction.TransactionPaymentType = 1;
                        await _transactionService.AddAsync(transaction);
                    }
                    await _invoiceService.SaveChangeAsync();

                    // generate shipping detail
                    var shippingDetail = new ShippingDetail();
                    shippingDetail.Id = "GH" + invoice.Id;
                    shippingDetail.Invoice = invoice;
                    shippingDetail.PhoneReceive = invoiceInfo.PhoneReceive;
                    shippingDetail.EstimatedArrival = DateTime.Now.AddDays(3);
                    shippingDetail.Address = invoiceInfo.Address;
                    shippingDetail.ShipperId = shipperId;
                    await _shippingDetailService.AddAsync(shippingDetail);
                    var result = await _shippingDetailService.SaveChangeAsync();
                    await _efTransaction.CommitAsync();
                    if (result) return Ok(new { success = true, message = "Tạo đơn hàng thành công" });
                }
            }
            catch (Exception)
            {
                await _efTransaction.RollbackAsync();
                throw;
            }
            return Ok();
        }
    }
}
