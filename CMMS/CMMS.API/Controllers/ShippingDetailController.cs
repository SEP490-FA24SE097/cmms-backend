using AutoMapper;
using CMMS.API.Constant;
using CMMS.API.Helpers;
using CMMS.API.Services;
using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Data;
using CMMS.Infrastructure.Enums;
using CMMS.Infrastructure.Services;
using CMMS.Infrastructure.Services.Shipping;
using Firebase.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Net.Http;
using System.Security.Policy;

namespace CMMS.API.Controllers
{
    [ApiController]
    [Route("api/shippingDetails")]
    [AllowAnonymous]
    public class ShippingDetailController : ControllerBase
    {
        private IMapper _mapper;
        private IShippingDetailService _shippingDetailService;
        private IInvoiceService _invoiceService;
        private IUserService _userService;
        private IStoreService _storeService;
        private ITransaction _efTransaction;
        private IMailService _mailService;
        private IShippingService _shippingService;
        private ICurrentUserService _currentUserService;
        private readonly IStoreInventoryService _storeInventoryService;
        private readonly IMaterialVariantAttributeService _materialVariantAttributeService;
        private readonly IVariantService _variantService;
        private readonly IMaterialService _materialService;
        private readonly ITransactionService _transactionService;
        private readonly IInvoiceDetailService _invoiceDetailService;

        public ShippingDetailController(IShippingDetailService shippingDetailService, IMapper mapper,
            IInvoiceService invoiceService, IUserService userService, IStoreService storeService,
            IStoreInventoryService storeInventoryService,
            ICurrentUserService currentUserService,
            IVariantService variantService,
            IMaterialService materialService,
            IMaterialVariantAttributeService materialVariantAttributeService,
            ITransactionService transactionService,
            IInvoiceDetailService invoiceDetailService,
            ITransaction efTransaction,
            IMailService mailService, IShippingService shippingService)
        {
            _mapper = mapper;
            _shippingDetailService = shippingDetailService;
            _invoiceService = invoiceService;
            _userService = userService;
            _storeService = storeService;
            _storeInventoryService = storeInventoryService;
            _materialVariantAttributeService = materialVariantAttributeService;
            _variantService = variantService;
            _materialService = materialService;
            _transactionService = transactionService;
            _invoiceDetailService = invoiceDetailService;
            _efTransaction = efTransaction;
            _mailService = mailService;
            _shippingService = shippingService;
            _currentUserService = currentUserService;

        }
        [HttpGet("getShippingDetails")]
        public async Task<IActionResult> GetListShippingDetailAsync([FromQuery] ShippingDetailFilterModel filterModel)
        {
            var fitlerList = _shippingDetailService
                .Get(_ =>
                (!filterModel.FromDate.HasValue || _.ShippingDate >= filterModel.FromDate) &&
                (!filterModel.ToDate.HasValue || _.ShippingDate <= filterModel.ToDate) &&
                (string.IsNullOrEmpty(filterModel.InvoiceId) || _.InvoiceId.Equals(filterModel.InvoiceId)) &&
                (string.IsNullOrEmpty(filterModel.ShippingDetailCode) || _.Id.Equals(filterModel.ShippingDetailCode)) &&
                (filterModel.InvoiceStatus == null || _.Invoice.InvoiceStatus.Equals(filterModel.InvoiceStatus)) &&
                (string.IsNullOrEmpty(filterModel.ShipperId) || _.ShipperId.Equals(filterModel.ShipperId))
                , _ => _.Invoice, _ => _.Invoice.InvoiceDetails, _ => _.Shipper, _ => _.Shipper.Store);
            var total = fitlerList.Count();
            var filterListPaged = fitlerList.ToPageList(filterModel.defaultSearch.currentPage, filterModel.defaultSearch.perPage)
                .Sort(filterModel.defaultSearch.sortBy, filterModel.defaultSearch.isAscending);
            var result = _mapper.Map<List<ShippingDetailVM>>(filterListPaged);

            foreach (var item in result)
            {
                var invoice = _invoiceService.Get(_ => _.Id.Equals(item.Invoice.Id), _ => _.Customer).FirstOrDefault();
                var staff = _userService.Get(_ => _.Id.Equals(invoice.StaffId)).FirstOrDefault();
                var store = _storeService.Get(_ => _.Id.Equals(invoice.StoreId)).FirstOrDefault();
                item.Invoice.UserVM = _mapper.Map<UserVM>(invoice.Customer);
                item.Invoice.StaffId = staff != null ? staff.Id : null;
                item.Invoice.StaffName = staff != null ? staff.FullName : null;
                item.Invoice.NeedToPay = _shippingDetailService.Get(_ => _.InvoiceId.Equals(invoice.Id)).FirstOrDefault().NeedToPay;
                item.Invoice.StoreName = store.Name;
                item.Invoice.StoreId = store.Id;
                foreach (var invoiceDetails in item.Invoice.InvoiceDetails)
                {
                    invoiceDetails.StoreId = item.Invoice.StoreId;
                    var addItemModel = _mapper.Map<AddItemModel>(invoiceDetails);
                    var storeItem = await _storeInventoryService.GetItemInStoreAsync(addItemModel);
                    if (storeItem != null)
                    {
                        var material = await _materialService.FindAsync(storeItem.MaterialId);
                        invoiceDetails.ItemName = material.Name;
                        invoiceDetails.SalePrice = material.SalePrice;
                        invoiceDetails.ImageUrl = material.ImageUrl;
                        invoiceDetails.ItemTotalPrice = invoiceDetails.ItemTotalPrice;
                        if (storeItem.VariantId != null)
                        {
                            var variant = _variantService.Get(_ => _.Id.Equals(storeItem.VariantId)).FirstOrDefault();
                            var variantAttribute = _materialVariantAttributeService.Get(_ => _.VariantId.Equals(variant.Id)).FirstOrDefault();
                            invoiceDetails.ItemName += $" | {variantAttribute.Value}";
                            invoiceDetails.SalePrice = variant.Price;
                            invoiceDetails.ImageUrl = variant.VariantImageUrl;
                            invoiceDetails.ItemTotalPrice = invoiceDetails.ItemTotalPrice;
                        }
                    }
                }
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
        [HttpPost("update-shippingDetail-status")]
        public async Task<IActionResult> UpdateShippingDetailStatus(ShippingDetailDTO model)
        {
            try
            {
                var shippingDetail = await _shippingDetailService.Get(_ => _.Id.Equals(model.Id), _ => _.Invoice).FirstOrDefaultAsync();
                if (shippingDetail != null)
                {
                    // update invoice status
                    var invoice = await _invoiceService.FindAsync(shippingDetail.InvoiceId);
                    invoice.InvoiceStatus = (int)InvoiceStatus.Done;
                    _invoiceService.Update(invoice);
                    shippingDetail.ShippingDate = model.ShippingDate;
                    shippingDetail.TransactionPaymentType = model.TransactionPaymentType;
                    shippingDetail.Address = model.Address;
                    shippingDetail.EstimatedArrival = (DateTime)model.EstimatedArrival;
                    _shippingDetailService.Update(shippingDetail);

                    // tao transaction mới là thu tiền thành công.
                    var transaction = new Transaction();
                    transaction.Id = "TTGH" + invoice.Id;
                    transaction.TransactionType = (int)TransactionType.PurchaseDebtInvoice;
                    transaction.TransactionDate = DateTime.Now;
                    transaction.CustomerId = shippingDetail.Invoice.CustomerId;
                    transaction.InvoiceId = invoice.Id;
                    transaction.Amount = (decimal)shippingDetail.NeedToPay;
                    transaction.TransactionPaymentType = 1;
                    await _transactionService.AddAsync(transaction);

                    // update so lunog trong kho. 
                    var invoiceDetail = _invoiceDetailService.Get(_ => _.InvoiceId.Equals(invoice.Id));
                    foreach (var item in invoiceDetail)
                    {
                        var cartItem = _mapper.Map<CartItem>(item);
                        cartItem.StoreId = invoice.StoreId;
                        // update store quantity
                        var updateQuantityStatus = await _storeInventoryService.UpdateStoreInventoryAsync(cartItem, (int)InvoiceStatus.Done);
                    }
                    var result = await _transactionService.SaveChangeAsync();
                    if (result)
                    {
                        await _efTransaction.CommitAsync();
                        return Ok(new { success = true, message = "Cập nhật tình trạng giao hàng thành công" });
                    }
                }
                return BadRequest("Không tìm thấy shipping detail");
            }
            catch (Exception)
            {
                await _efTransaction.RollbackAsync();
                throw;
            }

        }

        [HttpPut("update-shippingDetail")]
        public async Task<IActionResult> UpdateShippingDetailAsync(ShippingDetailDTO model)
        {
            var shippingDetail = await _shippingDetailService.FindAsync(model.Id);
            if (shippingDetail != null)
            {
                shippingDetail.ShippingDate = model.ShippingDate;
                shippingDetail.ShipperId = model.ShipperId;
                shippingDetail.TransactionPaymentType = model.TransactionPaymentType;
                shippingDetail.Address = model.Address;
                shippingDetail.EstimatedArrival = (DateTime)model.EstimatedArrival;
                _shippingDetailService.Update(shippingDetail);
                var result = await _shippingDetailService.SaveChangeAsync();
                if (result) return Ok(new { success = true, message = "Cập nhật thông tin giao hàng thành công" });
            }
            return Ok(new { success = false, message = "Không tìm thấy shipping detail" });
        }

        [HttpPost("add-shipper")]
        public async Task<IActionResult> AddNewShipper(UserDTO model)
        {
            var emailExist = await _userService.FindbyEmail(model.Email);
            var userNameExist = await _userService.FindByUserName(model.UserName);
            if (emailExist != null)
            {
                return BadRequest("Email đã được sử dụng");
            }
            else if (userNameExist != null)
            {
                return BadRequest("User đã được sử dụng");
            }
            var result = await _userService.ShipperSignUpAsync(model);

            if (result.Succeeded)
                await _mailService.SendEmailAsync(model.Email, "Tài khoản shipper cho hệ thống CMMS", null);

            await _userService.SaveChangeAsync();
            return Ok(new
            {
                data = result.Succeeded,
                pagination = new
                {
                    total = 0,
                    perPage = 0,
                    currentPage = 0,
                },
            });
        }

        [HttpGet("get-shipper")]
        public async Task<IActionResult> GetShipper([FromQuery] ShipperFilterModel filterModel)
        {

            var listCustomer = await _userService.GetAll();
            var filterUserList = new List<string>();
            foreach (var customer in listCustomer)
            {
                var user = _mapper.Map<ApplicationUser>(customer);
                var roles = await _userService.GetRolesAsync(user);
                if (roles.Contains(Role.Shipper_Store.ToString()))
                {
                    filterUserList.Add(user.Id);
                }
            }

            var fitlerList = _userService
                .Get(_ => filterUserList.Contains(_.Id) &&
              (string.IsNullOrEmpty(filterModel.InvoiceId) || _.Invoices.Any(_ => _.Id.Equals(filterModel.InvoiceId))) &&
              (string.IsNullOrEmpty(filterModel.StoreId) || _.StoreId.Equals(filterModel.StoreId)) &&
              (string.IsNullOrEmpty(filterModel.ShipperId) || _.Id.Equals(filterModel.ShipperId))
              , _ => _.Invoices, _ => _.Store);


            var total = fitlerList.Count();
            var filterListPaged = fitlerList.ToPageList(filterModel.defaultSearch.currentPage, filterModel.defaultSearch.perPage)
                .Sort(filterModel.defaultSearch.sortBy, filterModel.defaultSearch.isAscending);



            var result = _mapper.Map<List<ShipperVM>>(filterListPaged);
            foreach (var shipperVM in result)
            {
                var storeInfo = await _storeService.FindAsync(shipperVM.StoreId);
                shipperVM.StoreName = storeInfo.Name;
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


        [HttpPost("get-shipping-fee")]
        public async Task<IActionResult> GetShippingPrices(ShippingFeeModel model)
        {
            var user = await _currentUserService.GetCurrentUser();
            var store = _storeService.Get(_ => _.Id.Equals(user.StoreId)).FirstOrDefault();
            if (store != null)
            {
                var deliveryAddress = model.DeliveryAddress;
                var storeDistance = await _shippingService.GeStoreOrderbyDeliveryDistance(deliveryAddress, store);


                float totalWeight = 0;
                foreach (var item in model.storeItems)
                {
                    var weight = await _materialService.GetWeight(item.MaterialId, item.VariantId);
                    totalWeight += (float)weight;
                } 
                var shippingFee = _shippingService.CalculateShippingFee((decimal)storeDistance.Distance / 1000, (decimal)totalWeight);
                // handle final price
                return Ok(new
                {
                    data = new
                    {
                        shippingFee = shippingFee,
                        totalWeight = totalWeight,
                        shippingDistance = storeDistance.Distance
                    }
                });
            }
            return BadRequest("Không tìm thấy cửa hàng");

        }
    }
}
