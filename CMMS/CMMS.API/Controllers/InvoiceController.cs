using CMMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.API.Controllers
{
    [Route("api/invoices")]
    [ApiController]
    [AllowAnonymous]
    public class InvoiceController : ControllerBase
    {
        private IInvoiceService _invoiceService;
        private IInvoiceDetailService _invoiceDetailService;

        public InvoiceController(IInvoiceService invoiceService, 
            IInvoiceDetailService invoiceDetailService)
        {
            _invoiceService = invoiceService;
            _invoiceDetailService = invoiceDetailService;
        }
        
    }
}
