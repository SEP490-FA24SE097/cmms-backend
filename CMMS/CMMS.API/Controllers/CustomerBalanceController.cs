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
    [ApiController]
    [Route("api/customerBalances")]
    [AllowAnonymous]
    public class CustomerBalanceController : ControllerBase
    {
        private ICustomerBalanceService _customerBalanceSerivce;
        private IUserService _userService;
        private IMapper _mapper;

        public CustomerBalanceController(ICustomerBalanceService customerBalanceService,
            IUserService userService,
            IMapper mapper)
        {
            _customerBalanceSerivce = customerBalanceService;
            _userService = userService;
            _mapper = mapper;
        }
        [HttpGet]
        public ActionResult GetCustomerBalanceAsync([FromQuery] CustomerBalanceFitlerModel model)
        {
            var customerBalance = _customerBalanceSerivce.Get(_ => _.Id != null, _ => _.Customer);
            if(!model.CustomerName.IsNullOrEmpty())
                customerBalance = _customerBalanceSerivce.Get(_ => _.Customer.FullName.Contains(model.CustomerName) 
                , _ => _.Customer);   
            var total = customerBalance.Count();
            var customerBalancePaged = customerBalance.ToPageList(model.defaultSearch.currentPage, model.defaultSearch.perPage)
                .Sort(model.defaultSearch.sortBy, model.defaultSearch.isAscending);
            var result = _mapper.Map<List<CustomerBalanceVM>>(customerBalancePaged);
            return Ok(new
            {
                data = result,
                pagination = new
                {
                    total,
                    perPage = model.defaultSearch.perPage,
                    currentPage = model.defaultSearch.currentPage,
                }
            });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetCustomerBalanceByIdAsync(string id)
        {
            var result = await _customerBalanceSerivce.FindAsync(id);
            return Ok(new
            {
                data = result
            });
        }

        [HttpGet("{customerId}")]
        public ActionResult GetCustomerBalanceById(string customerId)
        {
            var result = _customerBalanceSerivce.GetCustomerBalanceById(customerId);
            return Ok(new
            {
                data = result
            });
        }
        [HttpPost]
        public async Task<IActionResult> AddCustomerBalanceAsync([FromBody] CustomerBalanceDTO model)
        {
            var user = await _userService.FindAsync(model.CustomerId);
            if (user != null)
            {
                var customerBalance = _mapper.Map<CustomerBalance>(model);
                customerBalance.Customer = user;
                customerBalance.Id = Guid.NewGuid().ToString();
                customerBalance.CreatedAt = DateTime.Now;
                await _customerBalanceSerivce.AddAsync(customerBalance);
                var result = await _customerBalanceSerivce.SaveChangeAsync();
                if (result)
                    return Ok(new { success = true, message = "Thêm công nợ user thành công" });
            }
            return Ok(new { success = false, message = "Thêm công nợ user thất bại" });
        }
        [HttpPut("update-customer-balance")]
        public async Task<IActionResult> UpdateCustomerBalanceAsync(CustomerBalanceUpdateModel updateModel)
        {
            var customerBalance = await _customerBalanceSerivce.FindAsync(updateModel.Id);
            if (customerBalance != null)
            {
                customerBalance.Balance = updateModel.Balance;
                customerBalance.TotalPaid = updateModel.TotalPaid;
                customerBalance.TotalDebt = updateModel.TotalDebt;
                customerBalance.UpdatedAt = DateTime.Now;

                _customerBalanceSerivce.Update(customerBalance);
                var result = await _customerBalanceSerivce.SaveChangeAsync();
                if (result)
                    return Ok(new { success = true, message = "Cập nhật công nợ user thành công" });
            }
            return Ok(new { success = false, message = "Cập nhật công nợ user thất bại" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomerBalanceAsync(string id) {
              await _customerBalanceSerivce.Remove(id);
            var result = await _customerBalanceSerivce.SaveChangeAsync();
            if(result)
                return Ok(new { success = true, message = "Xóa công nợ của user thành công" });
            return Ok(new { success = false, message = "Xóa công nợ của user thành công" });
        }
    }
}
