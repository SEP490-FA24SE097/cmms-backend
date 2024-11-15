using AutoMapper;
using CMMS.API.Constant;
using CMMS.API.Helpers;
using CMMS.API.Services;
using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Enums;
using CMMS.Infrastructure.Services;
using Firebase.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Data;
using System.Net.Http;
using static CMMS.Core.Models.ShipperVM;

namespace CMMS.API.Controllers
{
    [Route("api/customers")]
    [ApiController]
    [AllowAnonymous]
    public class CustomerController : ControllerBase
    {
        private IUserService _userService;
        private IInvoiceService _invoiceService;
        private IInvoiceDetailService _invoiceDetailService;
        private ITransactionService _transactionService;
        private HttpClient _httpClient;
        private ICurrentUserService _currentUserService;
        private IMapper _mapper;
        private IStoreService _storeService;

        public CustomerController(IUserService userService, IInvoiceService invoiceSerivce,
            IInvoiceDetailService invoiceDetailService, ITransactionService transactionService, HttpClient httpClient,
            ICurrentUserService currentUserService, IMapper mapper, IStoreService storeService)
        {
            _userService = userService;
            _invoiceService = invoiceSerivce;
            _invoiceDetailService = invoiceDetailService;
            _transactionService = transactionService;
            _httpClient = httpClient;
            _currentUserService = currentUserService;
            _mapper = mapper;
            _storeService = storeService;
        }

        #region CRUD Customer

        [HttpGet]
        public async Task<ActionResult> GetAllCustomerInStoreAsync([FromQuery] CustomerFilterModel filterModel)
        {
            decimal? currentDebtTotal = 0;
            decimal? totalSale = 0;

            var listCustomer =  _userService.Get(_ => _.Id != null, _ => _.Invoices);
            var filterUserList = new List<string>();
            foreach (var customer in listCustomer)
            {
                var user = _mapper.Map<ApplicationUser>(customer);
                var roles = await _userService.GetRolesAsync(user);
                if (roles.IsNullOrEmpty() || roles.Contains(Role.Customer.ToString()))
                {
                    filterUserList.Add(customer.Id);
                    currentDebtTotal += user.CurrentDebt;
                    var customerInvoices = user.Invoices;
                    if (customerInvoices != null)
                    {
                        foreach (var invoice in customerInvoices)
                        {
                            totalSale += invoice.TotalAmount;
                        }
                    }
                }
            }

            var fitlerList = _userService
            .Get(_ => filterUserList.Contains(_.Id) &&
            (_.CreatedById != null || _.Invoices != null) &&
            (string.IsNullOrEmpty(filterModel.CustomerTrackingCode) || _.Id.Equals(filterModel.CustomerTrackingCode)) &&
            (string.IsNullOrEmpty(filterModel.Email) || _.Email.Equals(filterModel.Email)) &&
            (string.IsNullOrEmpty(filterModel.PhoneNumber) || _.PhoneNumber.Equals(filterModel.PhoneNumber)) &&
            (string.IsNullOrEmpty(filterModel.Status) || _.Status.Equals(Int32.Parse(filterModel.Status)))
            , _ => _.Invoices);
    

          
            var total = fitlerList.Count();
            var filterListPaged = fitlerList.ToPageList(filterModel.defaultSearch.currentPage, filterModel.defaultSearch.perPage)
                .Sort(filterModel.defaultSearch.sortBy, filterModel.defaultSearch.isAscending);
            var result = _mapper.Map<List<UserStoreVM>>(filterListPaged);
            foreach (var item in result)
            {
                item.StoreCreateName = _storeService.Get(_ => _.Id.Equals(item.StoreId)).Select(_ => _.Name).FirstOrDefault();
                item.CreateByName = _userService.Get(_ => _.Id.Equals(item.CreatedById)).Select(_ => _.FullName).FirstOrDefault();
            }
            return Ok(new
            {
                data = new
                {
                    currentDebtTotal,
                    totalSale,
                    result
                },
                pagination = new
                {
                    total,
                    perPage = filterModel.defaultSearch.perPage,
                    currentPage = filterModel.defaultSearch.currentPage,
                }
            });
        }
        [HttpGet("get-in-store")]
        public async Task<ActionResult> GetAllCustomerInStoreByStoreManagerAsync([FromQuery] CustomerFilterModel filterModel)
        {
            var currentUser = await _currentUserService.GetCurrentUser();
            var storeId = currentUser.StoreId;

            decimal? currentDebtTotal = 0;
            decimal? totalSale = 0;


            var listCustomer = _userService.Get(_ => _.Id != null, _ => _.Invoices);
            var filterUserList = new List<string>();
            foreach (var customer in listCustomer)
            {
                var user = _mapper.Map<ApplicationUser>(customer);
                var roles = await _userService.GetRolesAsync(user);
                if (roles.IsNullOrEmpty() || roles.Contains(Role.Customer.ToString()))
                {
                    filterUserList.Add(customer.Id);
                    currentDebtTotal += user.CurrentDebt;
                    var customerInvoices = user.Invoices;
                    if (customerInvoices != null)
                    {
                        foreach (var invoice in customerInvoices)
                        {
                            totalSale += invoice.TotalAmount;
                        }
                    }
                }
            }

            var fitlerList = _userService
            .Get(_ => filterUserList.Contains(_.Id) &&
            (_.StoreId.Equals(storeId) && (_.CreatedById != null) || _.Invoices.Any(iv => iv.StoreId.Equals(storeId))) &&
            (string.IsNullOrEmpty(filterModel.CustomerTrackingCode) || _.Id.Equals(filterModel.CustomerTrackingCode)) &&
            (string.IsNullOrEmpty(filterModel.Email) || _.Email.Equals(filterModel.Email)) &&
            (string.IsNullOrEmpty(filterModel.PhoneNumber) || _.PhoneNumber.Equals(filterModel.PhoneNumber)) &&
            (string.IsNullOrEmpty(filterModel.Status) || _.Status.Equals(Int32.Parse(filterModel.Status)))
            , _ => _.Invoices);

           
            var total = fitlerList.Count();
            var filterListPaged = fitlerList.ToPageList(filterModel.defaultSearch.currentPage, filterModel.defaultSearch.perPage)
                .Sort(filterModel.defaultSearch.sortBy, filterModel.defaultSearch.isAscending);
            var result = _mapper.Map<List<UserStoreVM>>(filterListPaged);
            foreach (var item in result)
            {
                item.StoreCreateName = _storeService.Get(_ => _.Id.Equals(item.StoreId)).Select(_ => _.Name).FirstOrDefault();
                item.CreateByName = _userService.Get(_ => _.Id.Equals(item.CreatedById)).Select(_ => _.FullName).FirstOrDefault();
            }
            return Ok(new
            {
                data = new
                {
                    currentDebtTotal,
                    totalSale,
                    result
                },
                pagination = new
                {
                    total,
                    perPage = filterModel.defaultSearch.perPage,
                    currentPage = filterModel.defaultSearch.currentPage,
                }
            });
        }
        [HttpGet("{id}")]
        public ActionResult GetCustomerInfoById(string id)
        {
            var user = _userService.Get(_ => _.Id.Equals(id)).FirstOrDefault();
            var result = _mapper.Map<UserStoreVM>(user);
            return Ok(new
            {
                data = user
            });
        }
        [HttpPut("update-customer")]
        public async Task<ActionResult> UpdateCustomerInfoInStoreAsync(UserDTO model)
        {
            var user = _mapper.Map<ApplicationUser>(model);
             _userService.Update(user);
            var result = await _userService.SaveChangeAsync();
            return Ok(new
            {
                data = result
            });
        }
        [HttpPost("disable-customer/{id}")]
        public async Task<ActionResult> DisableCustomerAsync(string id)
        {
            var user = await _userService.FindAsync(id);
            user.Status = (int)CustomerStatus.Disable;
            _userService.Update(user);
            var result = await _userService.SaveChangeAsync();
            return Ok(new
            {
                data = result
            });
        }
        [HttpPost]
        public async Task<ActionResult> AddCustomerInStoreAsync(UserDTO model)
        {
            var currentUser = await _currentUserService.GetCurrentUser();
            model.CreatedById = currentUser.Id;
            model.StoreId = currentUser.StoreId;

            string userName = _userService.GenerateCustomerCode();
            model.UserName = userName;
            if (model.TaxCode != null)
            {
                var taxCode = model.TaxCode;
                var apiUrl = "https://api.vietqr.io/v2/business/{taxCode}";
                var response = await _httpClient.GetAsync(apiUrl);

                if (!response.IsSuccessStatusCode)
                {
                    return BadRequest("Failed to fetch taxCode api checking");
                }
                var responseContent = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonConvert.DeserializeObject<TaxCodeCheckApiResponse>(responseContent);

                if (apiResponse.Code == "00")
                {
                    model.Type = (int)CustomerType.Agency;
                    var resultCreate = await _userService.CustomerSignUpAsync(model);
                    if (resultCreate.Succeeded)
                        return Ok(resultCreate.Succeeded);
                }
                else
                {
                    return BadRequest(apiResponse.Desc);
                }
            }
            model.Type = (int)CustomerType.Customer;
            var result = await _userService.CustomerSignUpAsync(model);
            return Ok(result);
        }
        #endregion

        #region Customer Order History
        [HttpGet("history-order/{id}")]
        public ActionResult GetHistoryOrderCustomer(string id)
        {
            return Ok();
        }

        [HttpGet("invoice-detail")]
        public ActionResult ViewInvoiceDetail()
        {
            return Ok();
        }

        // query invoice detail nguoi ban, thong tin giao hang.
        [HttpGet("open-invoice-detail")]
        public ActionResult OpenInvoiceDetail()
        {
            return Ok();
        }

        [HttpGet("generate-invoicePDF")]
        public ActionResult GenerateInvoicePDF()
        {
            return Ok();
        }




        #endregion


        #region Customer Transaction
        [HttpGet("customer-debt/{id}")]
        public ActionResult GetDebtCustomer(string id)
        {
            return Ok();
        }

        // tao thanh toan tra no.
        // cho nay se co 2 option chon tra theo hoa don va tra so tien 
        [HttpPost("purchase-debt")]
        public ActionResult PurchaseCustomerDebt()
        {
            return Ok();
        }

        // dieu chinh cong no neu co sai sot thu
        [HttpGet("update-customer-debt")]
        public ActionResult UpdateCustomerDebt()
        {
            return Ok();
        }

        #endregion
    }
}
