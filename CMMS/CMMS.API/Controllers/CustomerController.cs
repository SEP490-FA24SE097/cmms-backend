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
using Microsoft.AspNetCore.Mvc.ApplicationModels;
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
        private IStoreInventoryService _storeInventoryService;
        private IMaterialService _materialService;
        private IVariantService _variantService;
        private IMaterialVariantAttributeService _materialVariantAttributeService;
        private IShippingDetailService _shippingDetailService;

        public CustomerController(IUserService userService, IInvoiceService invoiceSerivce,
            IInvoiceDetailService invoiceDetailService, ITransactionService transactionService, HttpClient httpClient,
            ICurrentUserService currentUserService, IMapper mapper, IStoreService storeService,
            IStoreInventoryService storeInventoryService, IMaterialService materialService,
            IVariantService variantService, IMaterialVariantAttributeService materialVariantAttributeService, IShippingDetailService shippingDetailService)
        {
            _userService = userService;
            _invoiceService = invoiceSerivce;
            _invoiceDetailService = invoiceDetailService;
            _transactionService = transactionService;
            _httpClient = httpClient;
            _currentUserService = currentUserService;
            _mapper = mapper;
            _storeService = storeService;
            _storeInventoryService = storeInventoryService;
            _materialService = materialService;
            _variantService = variantService;
            _materialVariantAttributeService = materialVariantAttributeService;
            _shippingDetailService = shippingDetailService;
        }

        #region CRUD Customer

        [HttpGet]
        public async Task<ActionResult> GetAllCustomerInStoreAsync([FromQuery] CustomerFilterModel filterModel)
        {
            var listCustomer = _userService.Get(_ => _.Id != null, _ => _.Invoices);
            var filterUserList = new List<string>();
            foreach (var customer in listCustomer)
            {
                var user = _mapper.Map<ApplicationUser>(customer);
                var roles = await _userService.GetRolesAsync(user);
                if (roles.IsNullOrEmpty() || roles.Contains(Role.Customer.ToString()))
                {
                    filterUserList.Add(customer.Id);
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

            decimal? currentDebtTotal = _userService.GetAllCustomerCurrentDebt();
            decimal? totalSale = _userService.GetAllCustomerTotalSale();
            decimal totalSaleAfterRefund = _userService.GetAllCustomerTotalSaleAfterRefund();


            var total = fitlerList.Count();
            var filterListPaged = fitlerList.ToPageList(filterModel.defaultSearch.currentPage, filterModel.defaultSearch.perPage)
                .Sort(filterModel.defaultSearch.sortBy, filterModel.defaultSearch.isAscending);
            var result = _mapper.Map<List<UserStoreVM>>(filterListPaged);
            foreach (var item in result)
            {
                item.StoreCreateName = _storeService.Get(_ => _.Id.Equals(item.StoreId)).Select(_ => _.Name).FirstOrDefault();
                item.CreateByName = _userService.Get(_ => _.Id.Equals(item.CreatedById)).Select(_ => _.FullName).FirstOrDefault();

                item.CurrentDebt = _userService.GetCustomerCurrentDebt(item.Id);
                item.TotalSale = _userService.GetCustomerTotalSale(item.Id);
                item.TotalSaleAfterRefund = _userService.GetCustomerTotalSaleAfterRefund(item.Id);
            }
            return Ok(new
            {
                data = new
                {
                    currentDebtTotal,
                    totalSale,
                    totalSaleAfterRefund,
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

            var listCustomer = _userService.Get(_ => _.Id != null, _ => _.Invoices);
            var filterUserList = new List<string>();
            foreach (var customer in listCustomer)
            {
                var user = _mapper.Map<ApplicationUser>(customer);
                var roles = await _userService.GetRolesAsync(user);
                if (roles.IsNullOrEmpty() || roles.Contains(Role.Customer.ToString()))
                {
                    filterUserList.Add(customer.Id);
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


            decimal? currentDebtTotal = _userService.GetAllCustomerCurrentDebt();
            decimal? totalSale = _userService.GetAllCustomerTotalSale();
            decimal totalSaleAfterRefund = _userService.GetAllCustomerTotalSaleAfterRefund();

            var total = fitlerList.Count();
            var filterListPaged = fitlerList.ToPageList(filterModel.defaultSearch.currentPage, filterModel.defaultSearch.perPage)
                .Sort(filterModel.defaultSearch.sortBy, filterModel.defaultSearch.isAscending);
            var result = _mapper.Map<List<UserStoreVM>>(filterListPaged);
            foreach (var item in result)
            {
                item.StoreCreateName = _storeService.Get(_ => _.Id.Equals(item.StoreId)).Select(_ => _.Name).FirstOrDefault();
                item.CreateByName = _userService.Get(_ => _.Id.Equals(item.CreatedById)).Select(_ => _.FullName).FirstOrDefault();

                item.CurrentDebt = _userService.GetCustomerCurrentDebt(item.Id);
                item.TotalSale = _userService.GetCustomerTotalSale(item.Id);
                item.TotalSaleAfterRefund = _userService.GetCustomerTotalSaleAfterRefund(item.Id);
            }
            return Ok(new
            {
                data = new
                {
                    currentDebtTotal,
                    totalSale,
                    totalSaleAfterRefund,
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

        [HttpGet("get-customer-data-in-store")]
        public async Task<ActionResult> GetAllCustomerInStoreByStoreAsync([FromQuery] CustomerFilterModel filterModel)
        {
            var currentUser = await _currentUserService.GetCurrentUser();
            var storeId = currentUser.StoreId;

            var listCustomer = await _userService.GetAll();
            var filterUserList = new List<string>();
            foreach (var customer in listCustomer)
            {
                var user = _mapper.Map<ApplicationUser>(customer);
                var roles = await _userService.GetRolesAsync(user);
                if (roles.IsNullOrEmpty() || roles.Contains(Role.Customer.ToString()))
                {
                    filterUserList.Add(customer.Id);
                }
            }
            var fitlerList = _userService
                 .Get(_ => filterUserList.Contains(_.Id) &&
                 (string.IsNullOrEmpty(filterModel.CustomerTrackingCode) || _.Id.Equals(filterModel.CustomerTrackingCode)) &&
                 (string.IsNullOrEmpty(filterModel.Email) || _.Email.Equals(filterModel.Email)) &&
                 (string.IsNullOrEmpty(filterModel.PhoneNumber) || _.PhoneNumber.Equals(filterModel.PhoneNumber)) &&
                 (string.IsNullOrEmpty(filterModel.Status) || _.Status.Equals(Int32.Parse(filterModel.Status)))
                 , _ => _.Invoices);


            decimal? currentDebtTotal = _userService.GetAllCustomerCurrentDebt();
            decimal? totalSale = _userService.GetAllCustomerTotalSale();
            decimal totalSaleAfterRefund = _userService.GetAllCustomerTotalSaleAfterRefund();

            var total = fitlerList.Count();
            var filterListPaged = fitlerList.ToPageList(filterModel.defaultSearch.currentPage, filterModel.defaultSearch.perPage)
                .Sort(filterModel.defaultSearch.sortBy, filterModel.defaultSearch.isAscending);
            var result = _mapper.Map<List<UserDataStoreVM>>(filterListPaged);
            foreach (var item in result)
            {
                //item.StoreCreateName = _storeService.Get(_ => _.Id.Equals(item.StoreId)).Select(_ => _.Name).FirstOrDefault();
                //item.CreateByName = _userService.Get(_ => _.Id.Equals(item.CreatedById)).Select(_ => _.FullName).FirstOrDefault();

                item.CurrentDebt = _userService.GetCustomerCurrentDebt(item.Id);
                //item.TotalSale = _userService.GetCustomerTotalSale(item.Id);
                //item.TotalSaleAfterRefund = _userService.GetCustomerTotalSaleAfterRefund(item.Id);
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
        [HttpGet("history-order")]
        public async Task<ActionResult> GetHistoryOrderCustomerAsync([FromQuery] InvoiceilterModel filterModel)
        {
            var fitlerList = _invoiceService
            .Get(_ => _.CustomerId.Equals(filterModel.CustomerId) &&
            filterModel.InvoiceStatus == null || _.InvoiceStatus.Equals(filterModel.InvoiceStatus));
            var total = fitlerList.Count();
            var filterListPaged = fitlerList.ToPageList(filterModel.defaultSearch.currentPage, filterModel.defaultSearch.perPage)
                .Sort(filterModel.defaultSearch.sortBy, filterModel.defaultSearch.isAscending);
            var result = _mapper.Map<List<InvoiceVM>>(filterListPaged);

            foreach (var invoice in result)
            {
                var invoiceDetailList = _invoiceDetailService.Get(_ => _.InvoiceId.Equals(invoice.Id));
                var shippingDetail = _shippingDetailService.Get(_ => _.InvoiceId.Equals(invoice.Id), _ => _.Shipper).FirstOrDefault();
                invoice.InvoiceDetails = _mapper.Map<List<InvoiceDetailVM>>(invoiceDetailList.ToList());
                // load data in invoice Detail 
                foreach (var invoiceDetail in invoice.InvoiceDetails)
                {

                    var staff = _userService.Get(_ => _.Id.Equals(invoice.StaffId)).FirstOrDefault();
                    var store = _storeService.Get(_ => _.Id.Equals(invoice.StoreId)).FirstOrDefault();
                    invoice.StaffName = staff != null ? staff.FullName : store.Name;
                    invoice.StoreName = store != null ? store.Name : "";

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
                            var variant = _variantService.Get(_ => _.Id.Equals(Guid.Parse(invoiceDetail.VariantId))).FirstOrDefault();
                            var variantAttribute = _materialVariantAttributeService.Get(_ => _.VariantId.Equals(variant.Id)).FirstOrDefault();
                            invoiceDetail.ItemName += $" | {variantAttribute.Value}";
                            invoiceDetail.SalePrice = variant.Price;
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

        // query invoice detail nguoi ban, thong tin giao hang.
        [HttpGet("open-invoice-detail")]
        public ActionResult OpenInvoiceDetail()
        {
            return Ok();
        }

        #endregion

        #region Customer Transaction
        [HttpGet("customer-debt")]
        public async Task<ActionResult> GetDebtCustomerAsync([FromQuery] TransactionFilterModel filterModel)
        {
            var filterList = _transactionService.Get(_ =>
            (string.IsNullOrEmpty(filterModel.InvoiceId) || _.InvoiceId.Equals(filterModel.InvoiceId)) &&
            (string.IsNullOrEmpty(filterModel.TransactionId) || _.Id.Equals(filterModel.TransactionId)) &&
            (string.IsNullOrEmpty(filterModel.TransactionType) || _.TransactionType.Equals(Int32.Parse(filterModel.TransactionType))) &&
            (string.IsNullOrEmpty(filterModel.CustomerName) || _.Customer.FullName.Contains(filterModel.CustomerName)) &&
            (string.IsNullOrEmpty(filterModel.CustomerId) || _.Customer.Id.Equals(filterModel.CustomerId)),
            _ => _.Invoice);

            var total = filterList.Count();
            var filterListPaged = filterList.ToPageList(filterModel.defaultSearch.currentPage, filterModel.defaultSearch.perPage)
                .Sort("TransactionDate", false);
            var result = _mapper.Map<List<TransactionVM>>(filterListPaged);

            foreach (var transaction in result)
            {
                if (transaction.InvoiceId != null)
                {
                    var invoice = _invoiceService.Get(_ => _.Id.Equals(transaction.InvoiceId), _ => _.InvoiceDetails).FirstOrDefault();
                    var invoiceDetailVM = _mapper.Map<List<InvoiceDetailVM>>(invoice.InvoiceDetails.ToList());
                    transaction.InvoiceVM = _mapper.Map<InvoiceTransactionVM>(invoice);
                    var staff = _userService.Get(_ => _.Id.Equals(invoice.StaffId)).FirstOrDefault();
                    var store = _storeService.Get(_ => _.Id.Equals(invoice.StoreId)).FirstOrDefault();
                    transaction.InvoiceVM.StaffName = staff != null ? staff.FullName : store.Name;
                    transaction.InvoiceVM.StoreName = store != null ? store.Name : "";

                    transaction.CustomerCurrentDebt = _userService.GetCustomerCurrentDeftAtTheLastTransaction(transaction.Id, transaction.CustomerId);
                    var userVM = _userService.Get(_ => _.Id.Equals(transaction.CustomerId)).FirstOrDefault();
                    transaction.InvoiceVM.UserVM = _mapper.Map<UserVM>(userVM);

                    foreach (var invoiceDetail in invoiceDetailVM)
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
                                var variant = _variantService.Get(_ => _.Id.Equals(Guid.Parse(invoiceDetail.VariantId))).FirstOrDefault();
                                var variantAttribute = _materialVariantAttributeService.Get(_ => _.VariantId.Equals(variant.Id)).FirstOrDefault();
                                invoiceDetail.ItemName += $" | {variantAttribute.Value}";
                                invoiceDetail.SalePrice = variant.Price;
                                invoiceDetail.ImageUrl = variant.VariantImageUrl;
                                invoiceDetail.ItemTotalPrice = variant.Price * invoiceDetail.Quantity;
                            }
                        }
                    }
                    transaction.InvoiceVM.InvoiceDetails = invoiceDetailVM;
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

        // dieu chinh cong no neu co sai sot thu
        [HttpPost("delivery-address")]
        public async Task<ActionResult> CustomerDeliveryAddressAsync(CustomerAddressModel model)
        {
            var currrentUser = await _currentUserService.GetCurrentUser();
            if (currrentUser != null) {
                currrentUser.Address = model.Address;
                currrentUser.Ward = model.Ward;
                currrentUser.District = model.District;
                currrentUser.Province = model.Province;

                _userService.Update(currrentUser);
                var result =  await _userService.SaveChangeAsync();
                if (result) return Ok("Cập nhật địa chỉ giao hàng thành công");
            }
            return BadRequest("Không tìm thấy user");
        }



        // dieu chinh cong no neu co sai sot thu
        [HttpPost("current-user")]
        public async Task<ActionResult> GetCurrentUser()
        {
            var currrentUser = await _currentUserService.GetCurrentUser();
            var result = _mapper.Map<UserVM>(currrentUser);
            return Ok(new
            {
                data = result
            });
        }

        #endregion
    }
}
