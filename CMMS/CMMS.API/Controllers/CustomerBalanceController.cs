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
        public ActionResult GetCustomerBalanceAsync([FromQuery] DefaultSearch defaultSearch)
        {
            var customerBalance = _customerBalanceSerivce.Get(_ => _.Id != null, _ => _.Customer);
            var total = customerBalance.Count();
            var customerBalancePaged = customerBalance.ToPageList(defaultSearch.currentPage, defaultSearch.perPage);
            var result = _mapper.Map<List<CustomerBalanceVM>>(customerBalancePaged);
            return Ok(new
            {
                data = result,
                pagination = new
                {
                    total,
                    perPage = defaultSearch.perPage,
                    currentPage = defaultSearch.currentPage,
                }
            });
        }
        [HttpGet("{id}")]
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

                _customerBalanceSerivce.Update(customerBalance);
                var result = await _customerBalanceSerivce.SaveChangeAsync();
                if (result)
                    return Ok(new { success = true, message = "Cập nhật công nợ user thành công" });
            }
            return Ok(new { success = false, message = "Cập nhật công nợ user thất bại" });
        }
    }
}
