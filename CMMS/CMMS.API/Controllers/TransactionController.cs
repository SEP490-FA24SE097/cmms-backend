using AutoMapper;
using CMMS.API.Constant;
using CMMS.API.Helpers;
using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

        public TransactionController(ITransactionService transactionService, IMapper mapper,
            IInvoiceService invoiceService, IUserService userSerivce)
        {
            _transactionService = transactionService;
            _mapper = mapper;
            _invoiceService = invoiceService;
            _userService = userSerivce;
        }
        [HttpGet]
        public IActionResult GetTransaction([FromQuery] TransactionFilterModel filterModel) 
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
                if(transaction.InvoiceId != null)
                {
                    var invoice = _invoiceService.Get(_ => _.Id.Equals(transaction.InvoiceId), _ => _.InvoiceDetails).FirstOrDefault();
                    transaction.InvoiceVM = _mapper.Map<InvoiceTransactionVM>(invoice);
                }
                var userVM = _userService.Get(_ => _.Id.Equals(transaction.CustomerId)).FirstOrDefault();
                transaction.UserVM = _mapper.Map<UserVM>(userVM);
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
