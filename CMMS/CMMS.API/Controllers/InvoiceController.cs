using AutoMapper;
using CMMS.API.Helpers;
using CMMS.API.OptionsSetup;
using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Services;
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

        public InvoiceController(IInvoiceService invoiceService,
            IInvoiceDetailService invoiceDetailService, IMapper mapper,
            IShippingDetailService shippingDetailService,
            ICartService cartService,
            IVariantService variantService,
            IMaterialService materialService,
            IMaterialVariantAttributeService materialVariantAttributeService)
        {
            _invoiceService = invoiceService;
            _invoiceDetailService = invoiceDetailService;
            _mapper = mapper;
            _shippingDetailService = shippingDetailService;
            _cartService = cartService;
            _materialVariantAttributeService = materialVariantAttributeService;
            _variantService = variantService;
            _materialService = materialService;
        }

        [HttpGet]
        public async Task<IActionResult> GetInvoicesAsync([FromQuery] InvoiceFitlerModel filterModel)
        {
            var fitlerList = _invoiceService
            .Get(_ =>
            (!filterModel.FromDate.HasValue || _.InvoiceDate >= filterModel.FromDate) &&
            (!filterModel.ToDate.HasValue || _.InvoiceDate <= filterModel.ToDate) &&
            (string.IsNullOrEmpty(filterModel.Id) || _.Id.Equals(filterModel.Id)) &&
            (string.IsNullOrEmpty(filterModel.CustomerName) || _.Customer.FullName.Equals(filterModel.CustomerName)) &&
            (string.IsNullOrEmpty(filterModel.CustomerId) || _.Customer.Id.Equals(filterModel.Id))
            , _ => _.Customer);
            var total = fitlerList.Count();
            var filterListPaged = fitlerList.ToPageList(filterModel.defaultSearch.currentPage, filterModel.defaultSearch.perPage)
                .Sort(filterModel.defaultSearch.sortBy, filterModel.defaultSearch.isAscending);
            var result = _mapper.Map<List<InvoiceVM>>(filterListPaged);

            foreach (var invoice in result)
            {
                var invoiceDetailList = _invoiceDetailService.Get(_ => _.InvoiceId.Equals(invoice.Id));
                var shippingDetail = _shippingDetailService.Get(_ => _.InvoiceId.Equals(invoice.Id)).FirstOrDefault();
                invoice.InvoiceDetails = _mapper.Map<List<InvoiceDetailVM>>(invoiceDetailList.ToList());

                // load data in invoice Detail 
                foreach (var invoiceDetail in invoice.InvoiceDetails)
                {
                    var itemInStoreModel = _mapper.Map<AddItemModel>(invoiceDetail);
                    var item = await _cartService.GetItemInStoreAsync(itemInStoreModel);
                    if (item != null)
                    {
                        var material = await _materialService.FindAsync(Guid.Parse(invoiceDetail.MaterialId));
                        invoiceDetail.ItemName = material.Name;
                        invoiceDetail.BasePrice = material.SalePrice;
                        invoiceDetail.ImageUrl = material.ImageUrl;
                        invoiceDetail.ItemTotalPrice = material.SalePrice * invoiceDetail.Quantity;
                        if (invoiceDetail.VariantId != null)
                        {
                            var variant = _variantService.Get(_ => _.Id.Equals(Guid.Parse(invoiceDetail.VariantId))).FirstOrDefault();
                            var variantAttribute = _materialVariantAttributeService.Get(_ => _.VariantId.Equals(variant.Id)).FirstOrDefault();
                            invoiceDetail.ItemName += $" | {variantAttribute.Value}";
                            invoiceDetail.BasePrice = variant.Price;
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
                    invoiceItem.BasePrice = material.SalePrice;
                    invoiceItem.ImageUrl = material.ImageUrl;
                    invoiceItem.ItemTotalPrice = material.SalePrice * invoiceItem.Quantity;
                    if (invoiceItem.VariantId != null)
                    {
                        var variant = _variantService.Get(_ => _.Id.Equals(Guid.Parse(invoiceItem.VariantId))).FirstOrDefault();
                        var variantAttribute = _materialVariantAttributeService.Get(_ => _.VariantId.Equals(variant.Id)).FirstOrDefault();
                        invoiceItem.ItemName += $" | {variantAttribute.Value}";
                        invoiceItem.BasePrice = variant.Price;
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
                    invoiceCode = invoiceVM.InvoiceCode,
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
    }
}
