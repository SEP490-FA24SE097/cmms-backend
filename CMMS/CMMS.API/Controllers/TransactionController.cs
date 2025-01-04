using AutoMapper;
using CMMS.API.Constant;
using CMMS.API.Helpers;
using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
            var filterList = _transactionService.Get(_ =>
            (!filterModel.FromDate.HasValue || _.TransactionDate >= filterModel.FromDate) &&
            (!filterModel.ToDate.HasValue || _.TransactionDate <= filterModel.ToDate) &&
            (string.IsNullOrEmpty(filterModel.InvoiceId) || _.InvoiceId.Equals(filterModel.InvoiceId)) &&
            (string.IsNullOrEmpty(filterModel.TransactionId) || _.Id.Equals(filterModel.TransactionId)) &&
            (string.IsNullOrEmpty(filterModel.TransactionType) || _.TransactionType.Equals(Int32.Parse(filterModel.TransactionType))) &&
            (string.IsNullOrEmpty(filterModel.CustomerName) || _.Customer.FullName.Equals(filterModel.CustomerName)) &&
            (string.IsNullOrEmpty(filterModel.CustomerId) || _.Customer.Id.Equals(filterModel.CustomerId))
            , _ => _.Customer);

            var total = filterList.Count();
            var filterListPaged = filterList.ToPageList(filterModel.defaultSearch.currentPage, filterModel.defaultSearch.perPage)
                .Sort(filterModel.defaultSearch.sortBy, filterModel.defaultSearch.isAscending);

            var result = _mapper.Map<List<TransactionVM>>(filterListPaged);

            foreach (var transaction in result)
            {
                if (transaction.InvoiceId != null)
                {
                    var invoiceEntity = _invoiceService.Get(_ => _.Id.Equals(transaction.InvoiceId), _ => _.InvoiceDetails).FirstOrDefault();
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
                                if (variant.MaterialVariantAttributes.Count > 0)
                                {
                                    var variantAttributes = _materialVariantAttributeService.Get(_ => _.VariantId.Equals(variant.Id)).Include(x => x.Attribute).ToList();
                                    var attributesString = string.Join('-', variantAttributes.Select(x => $"{x.Attribute.Name} :{x.Value} "));
                                    invoiceDetail.ItemName += $" | {variant.SKU} {attributesString}";
                                }
                                else
                                {
                                    invoiceDetail.ItemName += $" | {variant.SKU}";
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
                //var userVM = _userService.Get(_ => _.Id.Equals(transaction.CustomerId)).FirstOrDefault();
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

    }
}
