using AutoMapper;
using CMMS.API.Services;
using CMMS.Core.Models;
using CMMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

        public ProfileController(IUserService userSerivce, IInvoiceService invoiceService,
            ITransactionService transactionService, ICurrentUserService currentUserService,
            IMapper mapper)
        {
            _userService = userSerivce;
            _invoiceService = invoiceService;
            _transactionService = transactionService;
            _currentUserService = currentUserService;
            _mapper = mapper;
        }
        [HttpGet]
        public async Task<IActionResult> GetAsync()
        {
            var userId =  _currentUserService.GetUserId();
            var user = await _userService.FindAsync(userId);
            var userVM = _mapper.Map<UserVM>(user);
            return Ok();
        }
    }
}
