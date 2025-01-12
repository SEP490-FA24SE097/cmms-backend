using AutoMapper;
using CMMS.API.Constant;
using CMMS.API.Helpers;
using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CMMS.API.Controllers
{
    [Route("api/transactions")]
    [ApiController]
    [AllowAnonymous]
    public class TransactionController : ControllerBase
    {
        private ITransactionService _transactionService;
        private IMapper _mapper;
        private IInvoiceService _invoiceService;
        private IUserService _userService;
        private IInvoiceDetailService _invoiceDetailService;
        private IShippingDetailService _shippingDetailService;
        private IStoreService _storeService;
        private IStoreInventoryService _storeInventoryService;
        private IMaterialService _materialService;
        private IVariantService _variantService;
        private IMaterialVariantAttributeService _materialVariantAttributeService;

        public TransactionController(ITransactionService transactionService, IMapper mapper,
            IInvoiceService invoiceService, IUserService userSerivce, IInvoiceDetailService invoiceDetailService,
            IShippingDetailService shippingDetailService, IStoreService storeSerivce, IStoreInventoryService storeInventoryService,
             IVariantService variantService,
            IMaterialService materialService,
            IMaterialVariantAttributeService materialVariantAttributeService)
        {
            _transactionService = transactionService;
            _mapper = mapper;
            _invoiceService = invoiceService;
            _userService = userSerivce;
            _invoiceDetailService = invoiceDetailService;
            _shippingDetailService = shippingDetailService;
            _storeService = storeSerivce;
            _storeInventoryService = storeInventoryService;
            _materialService = materialService;
            _variantService = variantService;
            _materialVariantAttributeService = materialVariantAttributeService;

        }

        [HttpGet]
        public async Task<IActionResult> GetTransactionAsync([FromQuery] TransactionFilterModel filterModel)
        {
            if (filterModel.CustomerId != null)
            {
                var filterCustomerList = _transactionService.Get(_ =>
                    (string.IsNullOrEmpty(filterModel.CustomerId) || _.Customer.Id.Equals(filterModel.CustomerId))
                , _ => _.Customer).OrderByDescending(_ => _.TransactionDate);
                var totalCustomerTransaction = filterCustomerList.Count();
                decimal currentDebt = 0;
                var customerTransactionResult = _mapper.Map<List<TransactionVM>>(filterCustomerList);

                foreach (var transaction in customerTransactionResult)
                {
                    if (transaction.InvoiceId != null)
                    {
                        var invoiceEntity = _invoiceService.Get(_ => _.Id.Equals(transaction.InvoiceId), _ => _.InvoiceDetails).FirstOrDefault();
                        currentDebt += _transactionService.GetAmountDebtLeftFromInvoice(invoiceEntity.Id);
                        transaction.CustomerCurrentDebt = currentDebt;

                        var invoice = _mapper.Map<InvoiceVM>(invoiceEntity);
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

                        transaction.InvoiceVM = invoice;
                    }
                }

                var filterCustomerListPaged = customerTransactionResult.OrderByDescending(_ => _.TransactionDate).ToPageList(filterModel.defaultSearch.currentPage, filterModel.defaultSearch.perPage);
                return Ok(new
                {
                    data = filterCustomerListPaged,
                    pagination = new
                    {
                        total = totalCustomerTransaction,
                        perPage = filterModel.defaultSearch.perPage,
                        currentPage = filterModel.defaultSearch.currentPage,
                    }
                });
            }
            return BadRequest("Cần phải truyền customerId vào để tracking");

        }
    }
}
