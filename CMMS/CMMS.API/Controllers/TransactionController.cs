using CMMS.API.Constant;
using CMMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.API.Controllers
{
    [Route("api/shippingDetails")]
    [ApiController]
    [AllowAnonymous]
    public class TransactionController : ControllerBase
    {
        private ITransactionService _transactionService;

        public TransactionController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }
        [HttpGet]
        public IActionResult Get(DefaultSearch defaultSearch)
        {
            return Ok();
        }

    }
}
