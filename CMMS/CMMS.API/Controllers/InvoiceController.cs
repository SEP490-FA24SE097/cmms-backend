using AutoMapper;
using CMMS.API.Helpers;
using CMMS.API.OptionsSetup;
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

        public InvoiceController(IInvoiceService invoiceService,
            IInvoiceDetailService invoiceDetailService, IMapper mapper,
            IShippingDetailService shippingDetailService)
        {
            _invoiceService = invoiceService;
            _invoiceDetailService = invoiceDetailService;
            _mapper = mapper;
            _shippingDetailService = shippingDetailService;
        }

        [HttpGet]
        public IActionResult GetInvoices([FromQuery] InvoiceFitlerModel filterModel)
        {
            var fitlerList = _invoiceService
            .Get(_ =>
            (!filterModel.FromDate.HasValue || _.InvoiceDate >= filterModel.FromDate) &&
            (!filterModel.ToDate.HasValue || _.InvoiceDate <= filterModel.ToDate) &&
            (string.IsNullOrEmpty(filterModel.Id) || _.Id.Equals(filterModel.Id)) &&
            (string.IsNullOrEmpty(filterModel.CustomerName) || _.Customer.FullName.Equals(filterModel.CustomerName))
            , _ => _.Customer);
            var total = fitlerList.Count();
            var filterListPaged = fitlerList.ToPageList(filterModel.defaultSearch.currentPage, filterModel.defaultSearch.perPage)
                .Sort(filterModel.defaultSearch.sortBy, filterModel.defaultSearch.isAscending);
                var result = _mapper.Map<List<InvoiceVM>>(filterListPaged);

            foreach (var invoice in result) {
                var invoiceDetailList = _invoiceDetailService.Get(_ => _.InvoiceId.Equals(invoice.Id));
                var shippingDetail = _shippingDetailService.Get(_ => _.InvoiceId.Equals(invoice.Id)).FirstOrDefault();
                invoice.InvoiceDetails = _mapper.Map<List<InvoiceDetailVM>>(invoiceDetailList.ToList());
                invoice.shippingDetailVM = _mapper.Map<ShippingDetaiInvoicelVM>(shippingDetail);
            }

            return Ok(new
            {
                //data =  JsonSerializer.Serialize(result, JsonOptionsSetup.DefaultOptions           
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
