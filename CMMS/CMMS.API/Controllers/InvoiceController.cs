using AutoMapper;
using CMMS.API.Helpers;
using CMMS.API.Services;
using CMMS.API.TimeConverter;
using CMMS.Core.Entities;
using CMMS.Core.Enums;
using CMMS.Core.Models;
using CMMS.Infrastructure.Constant;
using CMMS.Infrastructure.Data;
using CMMS.Infrastructure.Enums;
using CMMS.Infrastructure.Handlers;
using CMMS.Infrastructure.InvoicePdf;
using CMMS.Infrastructure.Services;
using CMMS.Infrastructure.Services.Payment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

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
        private readonly IMaterialVariantAttributeService _materialVariantAttributeService;
        private readonly IVariantService _variantService;
        private readonly IMaterialService _materialService;
        private readonly IUserService _userService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IPaymentService _paymentService;
        private readonly ITransactionService _transactionService;
        private readonly IStoreInventoryService _storeInventoryService;
        private readonly ITransaction _efTransaction;
        private readonly IStoreService _storeService;
        private readonly IGenerateInvoicePdf _generateInvoicePdf;

        public InvoiceController(IInvoiceService invoiceService,
            IInvoiceDetailService invoiceDetailService, IMapper mapper,
            IShippingDetailService shippingDetailService,
            IVariantService variantService,
            IMaterialService materialService,
            IMaterialVariantAttributeService materialVariantAttributeService,
            IUserService userService, ICurrentUserService currentUserService,
            IPaymentService paymentService, ITransactionService transactionService,
            IStoreInventoryService storeInventoryService, ITransaction transaction, IStoreService storeService,
            IGenerateInvoicePdf generateInvoicePdf)
        {
            _invoiceService = invoiceService;
            _invoiceDetailService = invoiceDetailService;
            _mapper = mapper;
            _shippingDetailService = shippingDetailService;
            _materialVariantAttributeService = materialVariantAttributeService;
            _variantService = variantService;
            _materialService = materialService;
            _userService = userService;
            _currentUserService = currentUserService;
            _paymentService = paymentService;
            _transactionService = transactionService;
            _storeInventoryService = storeInventoryService;
            _efTransaction = transaction;
            _storeService = storeService;
            _generateInvoicePdf = generateInvoicePdf;
        }

        [HttpGet]
        [HasPermission(PermissionName.AddNewCustomer)]

        public async Task<IActionResult> GetInvoicesAsync([FromQuery] InvoiceFitlerModel filterModel)
        {
            var fitlerList = _invoiceService
              .Get(_ =>
              (!filterModel.FromDate.HasValue || _.InvoiceDate >= filterModel.FromDate) &&
              (!filterModel.ToDate.HasValue || _.InvoiceDate <= filterModel.ToDate) &&
              (string.IsNullOrEmpty(filterModel.Id) || _.Id.Equals(filterModel.Id)) &&
              (string.IsNullOrEmpty(filterModel.StoreId) || _.StoreId.Equals(filterModel.StoreId)) &&
              (string.IsNullOrEmpty(filterModel.CustomerName) || _.Customer.FullName.Contains(filterModel.CustomerName)) &&
              (string.IsNullOrEmpty(filterModel.CustomerId) || _.Customer.Id.Equals(filterModel.CustomerId)) &&
              (string.IsNullOrEmpty(filterModel.StaffId) || _.StaffId.Equals(filterModel.StaffId)) &&
              (filterModel.InvoiceType == null || _.InvoiceType.Equals(filterModel.InvoiceType)) &&
              (filterModel.InvoiceId == null || _.Id.Equals(filterModel.InvoiceId)) &&
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
                var staff = _userService.Get(_ => _.Id.Equals(invoice.StaffId)).FirstOrDefault();
                var store = _storeService.Get(_ => _.Id.Equals(invoice.StoreId)).FirstOrDefault();
                invoice.StaffName = staff != null ? staff.FullName : store.Name;
                invoice.StoreName = store != null ? store.Name : "";

                // load data in invoice Detail 
                foreach (var invoiceDetail in invoice.InvoiceDetails)
                {
                    var itemInStoreModel = _mapper.Map<AddItemModel>(invoiceDetail);
                    itemInStoreModel.StoreId = invoice.StoreId;
                    var item = await _storeInventoryService.GetItemInStoreAsync(itemInStoreModel);
                    if (item != null)
                    {
                        var material = await _materialService.FindAsync(Guid.Parse(invoiceDetail.MaterialId));
                        invoiceDetail.ItemName = material.Name;
                        invoiceDetail.SalePrice = material.SalePrice;
                        invoiceDetail.ImageUrl = material.ImageUrl;
                        invoiceDetail.ItemTotalPrice = material.SalePrice * invoiceDetail.Quantity;
                        if (invoiceDetail.VariantId != null)
                        {
                            var variant = _variantService.Get(_ => _.Id.Equals(Guid.Parse(invoiceDetail.VariantId))).Include(x => x.MaterialVariantAttributes).FirstOrDefault();
                            //var variantAttribute = _materialVariantAttributeService.Get(_ => _.VariantId.Equals(variant.Id)).FirstOrDefault();
                            //invoiceDetail.ItemName += $" | {variantAttribute.Value}";
                            if (!variant.MaterialVariantAttributes.IsNullOrEmpty())
                            {
                                var variantAttributes = _materialVariantAttributeService.Get(_ => _.VariantId.Equals(variant.Id)).Include(x => x.Attribute).ToList();
                                var attributesString = string.Join('-', variantAttributes.Select(x => $"{x.Attribute.Name} :{x.Value} "));
                                invoiceDetail.ItemName = $"{variant.SKU} {attributesString}";
                            }
                            else
                            {
                                invoiceDetail.ItemName = $"{variant.SKU}";
                            }

                            invoiceDetail.SalePrice = variant.Price;
                            invoiceDetail.ImageUrl = variant.VariantImageUrl;
                            invoiceDetail.ItemTotalPrice = variant.Price * invoiceDetail.Quantity;
                            invoiceDetail.InOrder = item.InOrderQuantity;
                            invoiceDetail.InStock = item.TotalQuantity;

                        }
                    }
                }
                invoice.shippingDetailVM = _mapper.Map<ShippingDetaiInvoiceResponseVM>(shippingDetail);
            }
            var reuslt = result.OrderByDescending(_ => _.InvoiceDate);
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

        [HttpGet("customer")]
        [HasPermission(PermissionName.InvoicePermissions)]
        public async Task<IActionResult> GetCustomerInvoicesAsync([FromQuery] InvoiceFitlerModel filterModel)
        {
            var userId = _currentUserService.GetUserId();
            var filteredList = _invoiceService
           .Get(_ =>
           (!filterModel.FromDate.HasValue || _.InvoiceDate >= filterModel.FromDate) &&
           (!filterModel.ToDate.HasValue || _.InvoiceDate <= filterModel.ToDate) &&
           (string.IsNullOrEmpty(filterModel.Id) || _.Id.Equals(filterModel.Id)) &&
           (string.IsNullOrEmpty(filterModel.CustomerId) || _.CustomerId.Equals(filterModel.CustomerId)) &&
           (string.IsNullOrEmpty(filterModel.StoreId) || _.StoreId.Equals(filterModel.StoreId)) &&
           (string.IsNullOrEmpty(filterModel.CustomerName) || _.Customer.FullName.Contains(filterModel.CustomerName)) &&
           (filterModel.InvoiceType == null || _.InvoiceType.Equals(filterModel.InvoiceType)) &&
           (filterModel.InvoiceStatus == null || _.InvoiceStatus.Equals(filterModel.InvoiceStatus))
           , _ => _.Customer).OrderByDescending(_ => _.InvoiceDate);

            // 2. Group theo GroupId
            var groupedInvoices = filteredList
                .GroupBy(_ => _.GroupId)
                .ToList(); // Tạm lưu kết quả Group vào List
            var total = groupedInvoices.Count();
            // 3. Phân trang trên danh sách GroupId
            //var pagedGroups = groupedInvoices.ToPageList(filterModel.defaultSearch.currentPage, filterModel.defaultSearch.perPage);

            var result = new List<GroupInvoiceVM>();
            foreach (var groupInvoice in groupedInvoices)
            {
                var groupInvoiceVM = new GroupInvoiceVM();
                var groupId = groupInvoice.Key;
                var listInvoices = _mapper.Map<List<InvoiceVM>>(groupInvoice.ToList());
                foreach (var invoice in listInvoices)
                {
                    var invoiceDetailList = _invoiceDetailService.Get(_ => _.InvoiceId.Equals(invoice.Id));
                    var shippingDetail = _shippingDetailService.Get(_ => _.InvoiceId.Equals(invoice.Id), _ => _.Shipper).FirstOrDefault();
                    invoice.InvoiceDetails = _mapper.Map<List<InvoiceDetailVM>>(invoiceDetailList.ToList());

                    var staff = _userService.Get(_ => _.Id.Equals(invoice.StaffId)).FirstOrDefault();
                    var store = _storeService.Get(_ => _.Id.Equals(invoice.StoreId)).FirstOrDefault();
                    invoice.StaffName = staff != null ? staff.FullName : store.Name;
                    invoice.StoreName = store != null ? store.Name : "";

                    // load data in invoice Detail 
                    foreach (var invoiceDetail in invoice.InvoiceDetails)
                    {
                        var itemInStoreModel = _mapper.Map<AddItemModel>(invoiceDetail);
                        itemInStoreModel.StoreId = invoice.StoreId;
                        var item = await _storeInventoryService.GetItemInStoreAsync(itemInStoreModel);
                        if (item != null)
                        {
                            var material = await _materialService.FindAsync(Guid.Parse(invoiceDetail.MaterialId));
                            invoiceDetail.ItemName = material.Name;
                            invoiceDetail.SalePrice = material.SalePrice;
                            invoiceDetail.ImageUrl = material.ImageUrl;
                            invoiceDetail.ItemTotalPrice = material.SalePrice * invoiceDetail.Quantity;
                            if (invoiceDetail.VariantId != null)
                            {
                                var variant = _variantService.Get(_ => _.Id.Equals(Guid.Parse(invoiceDetail.VariantId))).Include(x => x.MaterialVariantAttributes).FirstOrDefault();
                                //var variantAttribute = _materialVariantAttributeService.Get(_ => _.VariantId.Equals(variant.Id)).FirstOrDefault();
                                //invoiceDetail.ItemName += $" | {variantAttribute.Value}";
                                if (!variant.MaterialVariantAttributes.IsNullOrEmpty())
                                {
                                    var variantAttributes = _materialVariantAttributeService.Get(_ => _.VariantId.Equals(variant.Id)).Include(x => x.Attribute).ToList();
                                    var attributesString = string.Join('-', variantAttributes.Select(x => $"{x.Attribute.Name} :{x.Value} "));
                                    invoiceDetail.ItemName = $"{variant.SKU} {attributesString}";
                                }
                                else
                                {
                                    invoiceDetail.ItemName = $"{variant.SKU}";
                                }
                                invoiceDetail.SalePrice = variant.Price;
                                invoiceDetail.ImageUrl = variant.VariantImageUrl;
                                invoiceDetail.ItemTotalPrice = variant.Price * invoiceDetail.Quantity;
                            }
                        }
                    }

                    invoice.shippingDetailVM = _mapper.Map<ShippingDetaiInvoiceResponseVM>(shippingDetail);
                }

                groupInvoiceVM.TotalAmount = (double)listInvoices.Sum(_ => _.SalePrice);
                groupInvoiceVM.InvoiceDate = listInvoices.Last().InvoiceDate;
                groupInvoiceVM.Invoices = listInvoices;
                result.Add(groupInvoiceVM);
            }

        
            return Ok(new
            {
                data = result.OrderByDescending(_ => _.InvoiceDate).ToPageList(filterModel.defaultSearch.currentPage, filterModel.defaultSearch.perPage),
                pagination = new
                {
                    total,
                    perPage = filterModel.defaultSearch.perPage,
                    currentPage = filterModel.defaultSearch.currentPage,
                }
            });
        }

        [HttpGet("invoice-detail")]
        [HasPermission(PermissionName.InvoicePermissions)]
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
                var item = await _storeInventoryService.GetItemInStoreAsync(itemInStoreModel);
                if (item != null)
                {
                    var material = await _materialService.FindAsync(Guid.Parse(invoiceItem.MaterialId));
                    invoiceItem.ItemName = material.Name;
                    invoiceItem.SalePrice = material.SalePrice;
                    invoiceItem.ImageUrl = material.ImageUrl;
                    invoiceItem.ItemTotalPrice = material.SalePrice * invoiceItem.Quantity;
                    if (invoiceItem.VariantId != null)
                    {
                        var variant = _variantService.Get(_ => _.Id.Equals(Guid.Parse(invoiceItem.VariantId))).Include(x => x.MaterialVariantAttributes).FirstOrDefault();
                        //var variantAttribute = _materialVariantAttributeService.Get(_ => _.VariantId.Equals(variant.Id)).FirstOrDefault();
                        //invoiceDetail.ItemName += $" | {variantAttribute.Value}";
                        if (!variant.MaterialVariantAttributes.IsNullOrEmpty())
                        {
                            var variantAttributes = _materialVariantAttributeService.Get(_ => _.VariantId.Equals(variant.Id)).Include(x => x.Attribute).ToList();
                            var attributesString = string.Join('-', variantAttributes.Select(x => $"{x.Attribute.Name} :{x.Value} "));
                            invoiceItem.ItemName = $"{variant.SKU} {attributesString}";
                        }
                        else
                        {
                            invoiceItem.ItemName = $"{variant.SKU}";
                        }
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
        [HasPermission(PermissionName.InvoicePermissions)]
        public async Task<IActionResult> CreatePayment([FromBody] InvoiceStoreData invoiceInfo)
        {

            var totalAmount = invoiceInfo.TotalAmount;
            var discount = invoiceInfo.Discount != null ? invoiceInfo.Discount : 0;
            var salePrices = invoiceInfo.SalePrice;
            var customerPaid = invoiceInfo.CustomerPaid != null ? invoiceInfo.CustomerPaid : 0;
            var customerId = invoiceInfo.CustomerId;
            var shipperId = invoiceInfo.ShipperId != null ? invoiceInfo.ShipperId : null;
            var phoneRecevied = invoiceInfo.PhoneReceive != null ? invoiceInfo.PhoneReceive : null;
            var note = invoiceInfo.Note;
            

            var storeManager = await _currentUserService.GetCurrentUser();
            var storeId = storeManager.StoreId;
            try
            {
                // quick sale => sale in store
                if (invoiceInfo.InvoiceType == (int)InvoiceStoreType.QuickSale)
                {
                    string invoiceCode = _invoiceService.GenerateInvoiceCode();

                    Invoice invoice = new Invoice();
                    invoice.Id = invoiceCode;
                    invoice.InvoiceDate = TimeConverter.TimeConverter.GetVietNamTime();
                    invoice.StoreId = storeId;
                    invoice.InvoiceStatus = (int)InvoiceStatus.Done;
                    invoice.InvoiceType = (int)InvoiceType.Normal;
                    invoice.Note = note;
                    invoice.StaffId = storeManager.Id;
                    invoice.TotalAmount = (decimal)totalAmount;
                    invoice.SalePrice = salePrices;
                    invoice.Discount = discount;
                    invoice.CustomerId = customerId;
                    invoice.GroupId = Guid.NewGuid().ToString();
                    await _invoiceService.AddAsync(invoice);
                    foreach (var item in invoiceInfo.StoreItems)
                    {

                        var material = await _materialService.FindAsync(Guid.Parse(item.MaterialId));
                        var totalItemPrice = material.SalePrice * item.Quantity;
                        if (item.VariantId != null)
                        {
                            var variant = _variantService.Get(_ => _.Id.Equals(Guid.Parse(item.VariantId))).FirstOrDefault();
                            totalItemPrice = variant.Price * item.Quantity;
                        }
                        var invoiceDetail = new InvoiceDetail
                        {
                            Id = Guid.NewGuid().ToString(),
                            LineTotal = totalItemPrice,
                            MaterialId = Guid.Parse(item.MaterialId),
                            VariantId = item.VariantId != null ? Guid.Parse(item.VariantId) : null,
                            Quantity = item.Quantity,
                            InvoiceId = invoiceCode,
                        };
                        var cartItem = _mapper.Map<CartItem>(item);
                        cartItem.StoreId = storeId;
                        await _storeInventoryService.UpdateStoreInventoryAsync(cartItem, (int)InvoiceStatus.DoneInStore);
                        await _invoiceDetailService.AddAsync(invoiceDetail);
                    }
                    Transaction transaction = new Transaction();

                    transaction.Id = "TT" + invoiceCode;
                    transaction.TransactionType = (int)TransactionType.QuickSale;
                    transaction.TransactionDate = TimeConverter.TimeConverter.GetVietNamTime();
                    transaction.CustomerId = customerId;
                    transaction.InvoiceId = invoice.Id;
                    transaction.Amount = (decimal)salePrices;
                    transaction.TransactionPaymentType = 1;
                    await _transactionService.AddAsync(transaction);

                    var result = await _invoiceService.SaveChangeAsync();

                    await _efTransaction.CommitAsync();
                    if (result) return Ok(new { success = true, message = "Tạo đơn hàng bán nhanh thành công" });

                }
                else if (invoiceInfo.InvoiceType == (int)InvoiceStoreType.DeliverySale)
                {
                    // hoa don do cua hang tu tao

                    // cập nhật xong => nhập địa chri xong => Nhấn nút check giá ship => Sau đó mới truyển data từ phía trên xuống.
                    if (invoiceInfo.InvoiceId == null)
                    {
                        var responseMessage = "Không đủ số lượng tồn kho cho sản phẩm ";
                        var isValidQuantity = true;
                        var groupInvoiceId = Guid.NewGuid().ToString();
                        // validate quantity in stock again
                        foreach (var item in invoiceInfo.StoreItems)
                        {
                            var addItemModel = _mapper.Map<AddItemModel>(item);
                            var storeItem = await _storeInventoryService.GetItemInStoreAsync(addItemModel);
                            var material = await _materialService.FindAsync(Guid.Parse(item.MaterialId));
                            var storeItemName = material.Name;

                            if (item.VariantId != null)
                            {
                                //  var variant = _variantService.Get(_ => _.Id.Equals(Guid.Parse(item.VariantId))).FirstOrDefault();
                                //  var variantAttribute = _materialVariantAttributeService.Get(_ => _.VariantId.Equals(variant.Id)).FirstOrDefault();
                                //storeItemName += $" | {variantAttribute.Value}";
                                var variant = _variantService.Get(_ => _.Id.Equals(Guid.Parse(item.VariantId))).Include(x => x.MaterialVariantAttributes).FirstOrDefault();
                                if (!variant.MaterialVariantAttributes.IsNullOrEmpty())
                                {
                                    var variantAttributes = _materialVariantAttributeService.Get(_ => _.VariantId.Equals(variant.Id)).Include(x => x.Attribute).ToList();
                                    var attributesString = string.Join('-', variantAttributes.Select(x => $"{x.Attribute.Name} :{x.Value} "));
                                    storeItemName += $" | {variant.SKU} {attributesString}";
                                }
                                else
                                {
                                    storeItemName += $" | {variant.SKU}";
                                }
                            }
                            var cartItem = _mapper.Map<CartItem>(item);
                            cartItem.StoreId = storeId;
                            var canPurchase = await _storeInventoryService.CanPurchase(cartItem);
                            if (!canPurchase)
                            {
                                responseMessage += $", {storeItemName}";
                                isValidQuantity = false;
                            }
                        }
                        if (!isValidQuantity) return BadRequest(responseMessage);
                        // generate invoice
                        var invoiceCode = _invoiceService.GenerateInvoiceCode();
                        // insert invoice
                        var invoice = new Invoice
                        {
                            Id = invoiceCode,
                            CustomerId = customerId,
                            InvoiceDate = TimeConverter.TimeConverter.GetVietNamTime(),
                            InvoiceStatus = (int)InvoiceStatus.Shipping,
                            InvoiceType = (int)InvoiceType.Normal,
                            Note = note,
                            StoreId = storeManager.StoreId,
                            StaffId = storeManager.Id,
                            // get total cart 
                            TotalAmount = (decimal)totalAmount,
                            SalePrice = invoiceInfo.SalePrice,
                            SellPlace = (int)Infrastructure.Enums.SellPlace.InStore,
                            Discount = discount,
                            GroupId = groupInvoiceId,
                        };

                        var needToPay = salePrices;

                        Transaction transaction = null;

                        transaction = new Transaction();
                        transaction.Id = "DH" + invoiceCode;
                        transaction.TransactionType = (int)TransactionType.SaleItem;
                        transaction.TransactionDate = TimeConverter.TimeConverter.GetVietNamTime();
                        transaction.CustomerId = customerId;
                        transaction.InvoiceId = invoice.Id;
                        transaction.TransactionPaymentType = 1;
                        transaction.Amount = (decimal)salePrices;
                        await _transactionService.AddAsync(transaction);

                        if (invoiceInfo.CustomerPaid > 0)
                        {
                            needToPay -= invoiceInfo.CustomerPaid;
                            // tao them 1 transaction nua la thanh toan cho hoa don do.
                            invoice.CustomerPaid = invoiceInfo.CustomerPaid;

                            transaction = new Transaction();
                            transaction.Id = "TT" + invoiceCode;
                            transaction.TransactionType = (int)TransactionType.PurchaseDebtInvoice;
                            transaction.TransactionDate = TimeConverter.TimeConverter.GetVietNamTime();
                            transaction.CustomerId = customerId;
                            transaction.InvoiceId = invoice.Id;
                            transaction.Amount = (decimal)customerPaid;
                            transaction.TransactionPaymentType = 1;
                            await _transactionService.AddAsync(transaction);
                        }
                        await _invoiceService.AddAsync(invoice);
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

                            var cartItem = _mapper.Map<CartItem>(item);
                            cartItem.StoreId = storeId;
                            // update store quantity
                            var updateQuantityStatus = await _storeInventoryService.UpdateStoreInventoryAsync(cartItem, (int)InvoiceStatus.Pending);
                            //if (updateQuantityStatus)
                            //    // chỗ này phải lock process của luồng này lại k cho chạy đồng thời.
                            //    return Ok(new { success = false, message = "Số lượng hàng hóa có biến động kiểm tra lại" });
                            await _invoiceDetailService.AddAsync(invoiceDetail);
                        }
                        // generate shipping detail
                        var shippingDetailId = "GH" + invoiceCode;
                        var shippingDetail = new ShippingDetail();
                        //var shippingDetail = await _shippingDetailService.FindAsync(shippingDetailId);
                        shippingDetail.Id = shippingDetailId;
                        shippingDetail.Invoice = invoice;
                        shippingDetail.PhoneReceive = invoiceInfo.PhoneReceive;
                        shippingDetail.EstimatedArrival = TimeConverter.TimeConverter.GetVietNamTime().AddDays(3);
                        shippingDetail.Address = invoiceInfo.Address;
                        shippingDetail.NeedToPay = needToPay;
                        shippingDetail.ShippingFee = invoiceInfo.ShippingFee;
                        shippingDetail.ShipperId = shipperId;
                        shippingDetail.ShippingDetailStatus = (int)ShippingDetailStatus.Pending;
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
                        invoice.StoreId = invoice.StoreId;
                        invoice.InvoiceStatus = (int)InvoiceStatus.Shipping;
                        invoice.InvoiceType = (int)InvoiceType.Normal;
                        invoice.Note = note;
                        invoice.StaffId = storeManager.Id;
                        invoice.TotalAmount = (decimal)totalAmount;
                        invoice.SalePrice = salePrices;
                        invoice.Discount = discount;
                        invoice.CustomerId = invoice.CustomerId;
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
                            var item = _mapper.Map<CartItem>(invoiceDetail);
                            item.StoreId = invoice.StoreId;
                            var updateQuantityStatus = await _storeInventoryService.UpdateStoreInventoryAsync(item, (int)InvoiceStatus.Pending);
                        }
                        var needToPay = salePrices - discount + invoiceInfo.ShippingFee;


                        if (invoiceInfo.CustomerPaid > 0)
                        {
                            // tao them 1 transaction nua la thanh toan cho hoa don do.
                            Transaction transaction = null;
                            invoice.CustomerPaid = invoiceInfo.CustomerPaid;
                            needToPay -= invoiceInfo.CustomerPaid;

                            transaction = new Transaction();
                            transaction.Id = "TT" + invoiceCode;
                            transaction.TransactionType = (int)TransactionType.PurchaseDebtInvoice;
                            transaction.TransactionDate = TimeConverter.TimeConverter.GetVietNamTime();
                            transaction.CustomerId = customerId;
                            transaction.InvoiceId = invoice.Id;
                            transaction.Amount = (decimal)customerPaid;
                            transaction.TransactionPaymentType = 1;
                            await _transactionService.AddAsync(transaction);
                        }
                        await _invoiceService.SaveChangeAsync();

                        var shippingDetailId = "GH" + invoiceCode;
                        var shippingDetail = await _shippingDetailService.FindAsync(shippingDetailId);
                        shippingDetail.Invoice = invoice;
                        shippingDetail.PhoneReceive = invoiceInfo.PhoneReceive;
                        shippingDetail.EstimatedArrival = TimeConverter.TimeConverter.GetVietNamTime().AddDays(3);
                        shippingDetail.Address = invoiceInfo.Address;
                        shippingDetail.NeedToPay = needToPay;
                        shippingDetail.ShipperId = shipperId;
                        shippingDetail.ShippingDetailStatus = (int)ShippingDetailStatus.Pending;
                        _shippingDetailService.Update(shippingDetail);
                        var result = await _shippingDetailService.SaveChangeAsync();
                        await _efTransaction.CommitAsync();
                        if (result) return Ok(new { success = true, message = "Tạo đơn bán giao hàng thành công" });
                    }
                }
            }
            catch (Exception)
            {
                await _efTransaction.RollbackAsync();
                throw;
            }
            return BadRequest("Failed");
        }

        [HasPermission(PermissionName.InvoicePermissions)]
        [HttpPost("update-invoice")]
        public async Task<IActionResult> UpdateInvoice(InvoiceDataUpdateStatus model)
        {
            try
            {
                var shippingDetail = _shippingDetailService.Get(_ => _.Id.Equals(model.ShippingDetailId), _ => _.Invoice).FirstOrDefault();
                if (model.UpdateType == 0)
                {
                    if (shippingDetail != null)
                    {
                        shippingDetail.ShippingDate = model.ShippingDate;
                        shippingDetail.Note = model.Reason;
                        shippingDetail.ShippingDetailStatus = (int)ShippingDetailStatus.NotRecived;
                        _shippingDetailService.Update(shippingDetail);

                        var invoice = shippingDetail.Invoice;
                        invoice.InvoiceStatus = (int)InvoiceStatus.NotReceived;
                        _invoiceService.Update(invoice);

                        // hoàn đơn hàng vào kho.
                        //var invoiceDetails = _invoiceDetailService.Get(_ => _.InvoiceId.Equals(invoice.Id));
                        //foreach (var invoiceDetail in invoiceDetails)
                        //{
                        //    var item = _mapper.Map<CartItem>(invoiceDetail);
                        //    item.StoreId = invoice.StoreId;
                        //    // update store quantity
                        //    await _storeInventoryService.UpdateStoreInventoryAsync(item, (int)InvoiceStatus.Refund);
                        //}
                        var result = await _shippingDetailService.SaveChangeAsync();
                        await _efTransaction.CommitAsync();
                        if (result) return Ok(new { success = true, message = "Cập nhật thông tin đơn hàng thành công" });
                    }
                }
                // refund invoice
                else
                {
                    // create refund invoice
                    var staffManager = await _currentUserService.GetCurrentUser();
                    var invoice = await _invoiceService.FindAsync(model.InvoiceId);
                    string invoiceCode = _invoiceService.GenerateInvoiceCode();
                    decimal totalRefundAmount = 0;
                    var invoiceDetails = _invoiceDetailService.Get(_ => _.InvoiceId.Equals(invoice.Id));

                    var refundItems = (from refundItem in model.RefundItems
                                       join invoiceDetail in invoiceDetails
                                       on new { refundItem.MaterialId, refundItem.VariantId } equals new
                                       { MaterialId = invoiceDetail.MaterialId.ToString(), VariantId = invoiceDetail.VariantId?.ToString() }
                                       select new
                                       {
                                           MaterialId = invoiceDetail.MaterialId,
                                           VariantId = invoiceDetail.VariantId,
                                           // quantity of refund item
                                           RefundQuantity = refundItem.Quantity,
                                           PricePerQuantity = invoiceDetail.LineTotal / invoiceDetail.Quantity,
                                           InvoiceId = invoiceDetail.InvoiceId
                                       }).ToList();

                    var groupInvoiceId = Guid.NewGuid().ToString();
                    foreach (var item in refundItems)
                    {
                        if (item.RefundQuantity == 0)
                        {
                            continue;
                        }
                        var lineTotalRefund = item.RefundQuantity * item.PricePerQuantity;
                        var material = await _materialService.FindAsync(item.MaterialId);

                        // insert invoice Details
                        var invoiceDetail = new InvoiceDetail
                        {
                            Id = Guid.NewGuid().ToString(),
                            LineTotal = lineTotalRefund,
                            MaterialId = item.MaterialId,
                            VariantId = item.VariantId != null ? item.VariantId : null,
                            Quantity = item.RefundQuantity,
                            InvoiceId = invoiceCode,
                        };
                        totalRefundAmount += lineTotalRefund;
                        // update store quantity

                        var refundItem = new CartItem
                        {
                            MaterialId = item.MaterialId.ToString(),
                            Quantity = item.RefundQuantity,
                            VariantId = item.VariantId != null ? item.VariantId.ToString() : null,
                            StoreId = invoice.StoreId,
                        };
                        var updateQuantityStatus = await _storeInventoryService.UpdateStoreInventoryAsync(refundItem, (int)InvoiceStatus.Refund);
                        await _invoiceDetailService.AddAsync(invoiceDetail);
                    }

                    var refundInvoice = new Invoice
                    {
                        Id = invoiceCode,
                        CustomerId = invoice.CustomerId,
                        InvoiceDate = TimeConverter.TimeConverter.GetVietNamTime(),
                        InvoiceStatus = (int)InvoiceStatus.Refund,
                        InvoiceType = (int)InvoiceType.Normal,
                        //Note = shippingDetail.Note,
                        StoreId = invoice.StoreId,
                        // get total cart 
                        StaffId = staffManager.Id,
                        SalePrice = totalRefundAmount,
                        TotalAmount = (decimal)totalRefundAmount,
                        Discount = 0,
                        SellPlace = (int)Core.Enums.SellPlace.InStore,
                        GroupId = groupInvoiceId,
                    };
                    await _invoiceService.AddAsync(refundInvoice);

                    var transaction = new Transaction();
                    transaction.Id = "TH" + invoiceCode;
                    transaction.TransactionType = (int)TransactionType.RefundInvoice;
                    transaction.TransactionDate = TimeConverter.TimeConverter.GetVietNamTime();
                    transaction.CustomerId = invoice.CustomerId;
                    transaction.InvoiceId = invoice.Id;
                    transaction.Amount = (decimal)totalRefundAmount;
                    transaction.TransactionPaymentType = 1;
                    await _transactionService.AddAsync(transaction);

                    var result = await _transactionService.SaveChangeAsync();
                    await _efTransaction.CommitAsync();
                    if (result)
                        return Ok(new { success = true, message = "Tạo hóa đơn trả hàng thành công" });
                }
            }
            catch (Exception)
            {
                await _efTransaction.RollbackAsync();
                throw;
            }

            return Ok(new { success = false, message = "Không tìm thấy shipping detail" });
        }

        [HttpGet("{invoiceId}")]
        [HasPermission(PermissionName.InvoicePermissions)]
        public async Task<IActionResult> GetInvoicePdf(string invoiceId)
        {
            var fileName = $"{invoiceId}.pdf";
            var htmlContent = await _generateInvoicePdf.GenerateHtmlFromInvoiceAsync(invoiceId);
            //var pdfBytes = await _generateInvoicePdf.GeneratePdf(htmlContent);
            var pdfBytes = _generateInvoicePdf.GeneratePdf(htmlContent, fileName);
            return File(pdfBytes, "application/pdf", fileName);
        }


    }
}
