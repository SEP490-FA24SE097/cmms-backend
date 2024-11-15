using AutoMapper;
using CMMS.API.Constant;
using CMMS.API.Helpers;
using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Enums;
using CMMS.Infrastructure.Services;
using Firebase.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Net.Http;

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

        public ShippingDetailController(IShippingDetailService shippingDetailService, IMapper mapper,
            IInvoiceService invoiceService, IUserService userService, IStoreService storeService)
        {
            _mapper = mapper;
            _shippingDetailService = shippingDetailService;
            _invoiceService = invoiceService;
            _userService = userService;
            _storeService = storeService;
        }
        [HttpGet("getShippingDetails")]
        public IActionResult GetListShippingDetail([FromQuery] ShippingDetailFilterModel filterModel)
        {
            var fitlerList = _shippingDetailService
                .Get(_ =>
                (!filterModel.FromDate.HasValue || _.ShippingDate >= filterModel.FromDate) &&
                (!filterModel.ToDate.HasValue || _.ShippingDate <= filterModel.ToDate) &&
                (string.IsNullOrEmpty(filterModel.InvoiceId) || _.InvoiceId.Equals(filterModel.InvoiceId)) &&
                (string.IsNullOrEmpty(filterModel.ShippingDetailCode) || _.Id.Equals(filterModel.ShippingDetailCode)) &&
                (string.IsNullOrEmpty(filterModel.ShipperId) || _.ShipperId.Equals(filterModel.ShipperId))
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
                shippingDetail.TransactionPaymentType = model.TransactionPaymentType;
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

    }
}
