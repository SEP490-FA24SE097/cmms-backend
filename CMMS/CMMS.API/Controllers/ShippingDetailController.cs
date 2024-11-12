using AutoMapper;
using CMMS.API.Constant;
using CMMS.API.Helpers;
using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Enums;
using CMMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

        public ShippingDetailController(IShippingDetailService shippingDetailService, IMapper mapper,
            IInvoiceService invoiceService)
        {
            _mapper = mapper;
            _shippingDetailService = shippingDetailService;
            _invoiceService = invoiceService;
        }
        [HttpGet("getShippingDetails")]
        public IActionResult GetListShippingDetail([FromQuery] ShippingDetailFilterModel filterModel)
        {
            var fitlerList = _shippingDetailService
                .Get(_ =>
                (!filterModel.FromDate.HasValue || _.ShippingDate >= filterModel.FromDate) &&
                (!filterModel.ToDate.HasValue || _.ShippingDate <= filterModel.ToDate) &&
                (string.IsNullOrEmpty(filterModel.InvoiceId) || _.InvoiceId.Equals(filterModel.InvoiceId))
                , _ => _.Invoice);
            var total = fitlerList.Count();
            var filterListPaged = fitlerList.ToPageList(filterModel.defaultSearch.currentPage, filterModel.defaultSearch.perPage)
                .Sort(filterModel.defaultSearch.sortBy, filterModel.defaultSearch.isAscending);
            var result = _mapper.Map<List<ShippingDetailVM>>(filterListPaged);
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

        /// <summary>
        /// Update shipping detail status
        /// </summary>
        /// <param name="shippingDate">Shipping date for complete shipped</param>
        /// <param name="id">Shipping Detail Od</param>
        /// <returns>A JSON object containing user information.</returns>
        /// <response code="200">Successfully retrieved user information.</response>
        /// <response code="404">User  not found.</response>
        /// <response code="400">Invalid parameters.</response>
        [HttpPost]
        public async Task<IActionResult> UpdateShippingDetailStatus(ShippingDetailDTO model)
        {
            var shippingDetail = await _shippingDetailService.FindAsync(model.Id);
            if (shippingDetail != null)
            {
                // update invoice status
                var invoice = await _invoiceService.FindAsync(shippingDetail.InvoiceId);
                invoice.InvoiceStatus = (int)InvoiceStatus.Done;
                _invoiceService.Update(invoice);
                shippingDetail.ShippingDate = model.ShippingDate;
                _shippingDetailService.Update(shippingDetail);
                var result = await _shippingDetailService.SaveChangeAsync();
                if (result) return Ok(new { success = true, message = "Cập nhật tình trạng giao hàng thành công" });
            }
            return Ok(new { success = false, message = "Không tìm thấy shipping detail" });
        }

        [HttpPut("update-shippingDetail")]
        public async Task<IActionResult> UpdateShippingDetailAsync(ShippingDetailDTO model)
        {
            var shippingDetail = await _shippingDetailService.FindAsync(model.Id);
            if (shippingDetail != null)
            {
                shippingDetail.ShippingDate = model.ShippingDate;
                shippingDetail.Address = model.Address;
                shippingDetail.EstimatedArrival = (DateTime)model.EstimatedArrival;
                _shippingDetailService.Update(shippingDetail);
                var result = await _shippingDetailService.SaveChangeAsync();
                if (result) return Ok(new { success = true, message = "Cập nhật thông tin giao hàng thành công" });
            }
            return Ok(new { success = false, message = "Không tìm thấy shipping detail" });
        }
    }
}
