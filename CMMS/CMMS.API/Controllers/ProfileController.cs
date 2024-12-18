using AutoMapper;
using CMMS.API.Services;
using CMMS.Core.Models;
using CMMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CMMS.API.Helpers;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System;
using Microsoft.EntityFrameworkCore;

namespace CMMS.API.Controllers
{
    [Route("/api/profiles")]
    [ApiController]
    [AllowAnonymous]
    public class ProfileController : ControllerBase
    {
        private IUserService _userService;
        private IInvoiceService _invoiceService;
        private ITransactionService _transactionService;
        private ICurrentUserService _currentUserService;
        private IMapper _mapper;
        private IMailService _mailService;
        private readonly IMaterialVariantAttributeService _materialVariantAttributeService;
        private readonly IVariantService _variantService;
        private readonly IMaterialService _materialService;
        private IShippingDetailService _shippingDetailService;
        private readonly IInvoiceDetailService _invoiceDetailService;
        private IStoreInventoryService _storeInventoryService;

        public ProfileController(IUserService userSerivce, IInvoiceService invoiceService,
            ITransactionService transactionService, ICurrentUserService currentUserService,
            IMapper mapper, IMailService mailService,
            IVariantService variantService,
            IMaterialService materialService,
            IMaterialVariantAttributeService materialVariantAttributeService,
            IShippingDetailService shippingDetailService, 
            IInvoiceDetailService invoiceDetailService, IStoreInventoryService storeInventoryService)
        {
            _userService = userSerivce;
            _invoiceService = invoiceService;
            _transactionService = transactionService;
            _currentUserService = currentUserService;
            _mapper = mapper;
            _mailService = mailService;
            _materialVariantAttributeService = materialVariantAttributeService;
            _variantService = variantService;
            _materialService = materialService;
            _shippingDetailService = shippingDetailService;
            _invoiceDetailService = invoiceDetailService;
            _storeInventoryService = storeInventoryService;
        }
        [HttpGet]
        public async Task<IActionResult> GetAsync()
        {
            var userId = _currentUserService.GetUserId();
            var user = await _userService.FindAsync(userId);
            var userVM = _mapper.Map<UserVM>(user);
            return Ok(new
            {
                data = userVM,
            });
        }
        [HttpPut]
        public async Task<IActionResult> UpProfileAsync(UserVM model)
        {
            var userId = _currentUserService.GetUserId();
            var user = await _userService.FindAsync(userId);
            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            user.Address = model.Address;
            user.District = model.District;
            user.Ward = model.Ward;
            user.Province = model.Province;
            user.DOB = model.DOB;

            _userService.Update(user);
            var result = await _userService.SaveChangeAsync();
            if (result) return Ok(new { susscess = true, message = "Cập nhật hồ sơ người dùng thành công" });
            return Ok(new { susscess = false, message = "Cập nhật hồ sơ người dùng thất bại" });
        }
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var user = await _userService.FindbyEmail(request.Email);
            if (user == null || !(await _userService.IsEmailConfirmedAsync(user)))
            {
                return BadRequest("Invalid email.");
            }
            var token = await _userService.GeneratePasswordResetTokenAsync(user);

            // redirect sang trang reset password => saui do nhap password moi roi moi nhap ve goi method change password.
            var url = Url.Action(nameof(ResetPassword), nameof(ProfileController).Replace("Controller", ""), null, Request.Scheme);
                url += $"?token={token}&email={request.Email}";
            await _mailService.SendEmailAsync(request.Email, "Reset Password", url);

            return Ok("Reset password link has been sent to your email.");
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var user = await _userService.FindbyEmail(request.Email);
            if (user == null)
            {
                return BadRequest("Invalid email.");
            }
            var result = await _userService.ResetPasswordAsync(user, request.ResetCode, request.NewPassword);
            if (result.Succeeded)
            {
                return Ok("Password has been reset successfully.");
            }

            return BadRequest("Error resetting password.");
        }

        [HttpGet("invoices")]
        public async Task<IActionResult> GetInvoice([FromQuery]InvoiceFitlerModel filterModel)
        {
            var userId = _currentUserService.GetUserId();
            //var userId = "508bfd68-f940-4a55-823d-53e75d6e1194";
            var fitlerList = _invoiceService
            .Get(_ => (_.CustomerId.Equals(userId)) &&  
            (!filterModel.FromDate.HasValue || _.InvoiceDate >= filterModel.FromDate) &&
            (!filterModel.ToDate.HasValue || _.InvoiceDate <= filterModel.ToDate) &&
            (string.IsNullOrEmpty(filterModel.InvoiceId) || _.Id.Equals(filterModel.Id))
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
                           // var variant = _variantService.Get(_ => _.Id.Equals(Guid.Parse(invoiceDetail.VariantId))).FirstOrDefault();
                           // var variantAttribute = _materialVariantAttributeService.Get(_ => _.VariantId.Equals(variant.Id)).FirstOrDefault();
                            var variant = _variantService.Get(_ => _.Id.Equals(Guid.Parse(invoiceDetail.VariantId))).Include(x => x.MaterialVariantAttributes).FirstOrDefault();
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
                            //invoiceDetail.ItemName += $" | {variantAttribute.Value}";
                            invoiceDetail.SalePrice = variant.Price;
                            invoiceDetail.ImageUrl = variant.VariantImageUrl;
                            invoiceDetail.ItemTotalPrice = variant.Price * invoiceDetail.Quantity;
                        }
                    }
                }
                invoice.SalePrice += (shippingDetail.ShippingFee != null ? (decimal)shippingDetail.ShippingFee : 0);
                invoice.shippingDetailVM = _mapper.Map<ShippingDetaiInvoiceResponseVM>(shippingDetail);
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

        [HttpGet("transactions")]
        public IActionResult GetTransaction([FromQuery] TransactionFilterModel filterModel)
        {
            var customerId = _currentUserService.GetUserId();
            var filterList = _transactionService.Get(_ =>
            (_.CustomerId.Equals(customerId)) &&
            (!filterModel.FromDate.HasValue || _.TransactionDate >= filterModel.FromDate) &&
            (!filterModel.ToDate.HasValue || _.TransactionDate <= filterModel.ToDate) &&
            (string.IsNullOrEmpty(filterModel.InvoiceId) || _.InvoiceId.Equals(filterModel.InvoiceId)) &&
            (string.IsNullOrEmpty(filterModel.TransactionId) || _.Id.Equals(filterModel.TransactionId)) &&
            (string.IsNullOrEmpty(filterModel.TransactionType) || _.TransactionType.Equals(Int32.Parse(filterModel.TransactionType))) 
            , _ => _.Customer);

            var total = filterList.Count();
            var filterListPaged = filterList.ToPageList(filterModel.defaultSearch.currentPage, filterModel.defaultSearch.perPage)
                .Sort(filterModel.defaultSearch.sortBy, filterModel.defaultSearch.isAscending);

            var result = _mapper.Map<List<TransactionVM>>(filterListPaged);

            foreach (var transaction in result)
            {
                if (transaction.InvoiceId != null)
                {
                    var invoice = _invoiceService.Get(_ => _.Id.Equals(transaction.InvoiceId), _ => _.InvoiceDetails).FirstOrDefault();
                    transaction.InvoiceVM = _mapper.Map<InvoiceTransactionVM>(invoice);
                }
                var userVM = _userService.Get(_ => _.Id.Equals(transaction.CustomerId)).FirstOrDefault();
                //transaction.UserVM = _mapper.Map<UserVM>(userVM);
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
