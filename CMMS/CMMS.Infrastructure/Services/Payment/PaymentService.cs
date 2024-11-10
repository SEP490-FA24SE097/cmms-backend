using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Constant;
using CMMS.Infrastructure.Data;
using CMMS.Infrastructure.Enums;
using CMMS.Infrastructure.Helpers;
using CMMS.Infrastructure.Repositories;
using CMMS.Infrastructure.Services.Payment.Vnpay.Request;
using CMMS.Infrastructure.Services.Payment.Vnpay.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


namespace CMMS.Infrastructure.Services.Payment
{
    public interface IPaymentService
    {
        string VnpayCreatePayPaymentRequestAsync(PaymentRequestData paymentRequestData);
        Task<VnpayResponseData> VnpayReturnUrl(VnpayPayResponse vnpayPayResponse);
        //Task<bool> PaymentInvoiceAsync(InvoiceData invoiceInfo);
        Task<bool> PaymentDebtInvoiceAsync(InvoiceData invoiceInfo, CustomerBalance customerBalance);
        Task<bool> PurchaseDebtInvoiceAsync(InvoiceData invoiceInfo, CustomerBalance customerBalance);
    }
    public class PaymentService : IPaymentService
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IPaymentRepository _paymentRepsitory;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PaymentService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IInvoiceService _invoiceService;
        private readonly IInvoiceDetailService _invoiceDetailService;
        private readonly IShippingDetailService _shippingDetailService;
        private readonly ITransactionService _transactionService;
        private readonly ICustomerBalanceService _customerBalanceService;
        private readonly IVariantService _variantService;
        private readonly IMaterialService _materialService;
        private readonly ITransaction _efTransaction;
        private readonly IUserService _userService;

        public PaymentService(IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor, IPaymentRepository paymentRepository,
            IUnitOfWork unitOfWork, ILogger<PaymentService> logger, 
            IServiceScopeFactory serviceScopeFactory,
            ICustomerBalanceService customerBalanceService,
            ITransactionService transactionService,
            IMaterialService materialService, 
            IVariantService variantService,
            IInvoiceService invoiceService,
            IInvoiceDetailService invoiceDetailService,
            IShippingDetailService shippingDetailService,
            ITransaction transaction, IUserService userService)
        {
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _paymentRepsitory = paymentRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _invoiceService = invoiceService;
            _invoiceDetailService = invoiceDetailService;
            _shippingDetailService = shippingDetailService;
            _transactionService = transactionService;
            _customerBalanceService = customerBalanceService;
            _variantService = variantService;
            _materialService = materialService;
            _efTransaction = transaction;
            _userService = userService;
        }

        public async Task<bool> PaymentDebtInvoiceAsync(InvoiceData invoiceInfo, CustomerBalance customerBalance)
        {
            try
            {
                // update customerBalance
                var customerBalanceLeft = customerBalance.TotalDebt - (double)invoiceInfo.Amount;
                customerBalance.Balance -= customerBalanceLeft;
                customerBalance.CustomerId = customerBalance.Customer.Id;
                _customerBalanceService.Update(customerBalance);

                // insert transaction
                var transaction = new Transaction();
                transaction.Id = Guid.NewGuid().ToString();
                transaction.TransactionType = ((int)TransactionType.DebtInvoice).ToString();
                transaction.TransactionDate = DateTime.Now;
                transaction.CustomerId = customerBalance.Customer.Id;
                transaction.Amount = (decimal)invoiceInfo.Amount;
                await _transactionService.AddAsync(transaction);

                // insert invoice
                var invoice = new Invoice
                {
                    Id = Guid.NewGuid().ToString(),
                    CustomerId = customerBalance.Customer.Id,
                    InvoiceDate = DateTime.Now,
                    InvoiceStatus = (int)InvoiceStatus.Debt,
                    Note = invoiceInfo.Note,
                    TotalAmount = (decimal)invoiceInfo.Amount,
                };
                await _invoiceService.AddAsync(invoice);
                await _invoiceService.SaveChangeAsync();

                // insert invoice detail
                foreach (var cartItem in invoiceInfo.CartItems)
                {
                    var material = await _materialService.FindAsync(Guid.Parse(cartItem.MaterialId));
                    var lineTotal = material.SalePrice * cartItem.Quantity;
                    if (cartItem.VariantId != null)
                    {
                        var variant = _variantService.Get(_ => _.Id.Equals(Guid.Parse(cartItem.VariantId))).FirstOrDefault();
                        lineTotal = variant.Price * cartItem.Quantity;
                    }
                    var invoiceDetail = new InvoiceDetail
                    {
                        Id = Guid.NewGuid().ToString(),
                        LineTotal = lineTotal,
                        MaterialId = Guid.Parse(cartItem.MaterialId),
                        VariantId = cartItem.VariantId != null ? Guid.Parse(cartItem.VariantId) : null,
                        Quantity = cartItem.Quantity,
                        InvoiceId = invoice.Id,
                    };
                    await _invoiceDetailService.AddAsync(invoiceDetail);
                }
                // insert shipping detail.
                var shippingDetail = new ShippingDetail();
                shippingDetail.Id = Guid.NewGuid().ToString();
                shippingDetail.Invoice = invoice;
                shippingDetail.EstimatedArrival = DateTime.Now.AddDays(3);
                shippingDetail.Address = invoiceInfo.Address;
                await _shippingDetailService.AddAsync(shippingDetail);
                var result = await _unitOfWork.SaveChangeAsync();
                await _efTransaction.CommitAsync();
                if (result) return true;
            }
            catch (Exception)
            {
                await _efTransaction.RollbackAsync();
                throw;
            }
            return false;
        }

        //public Task<bool> PaymentInvoiceAsync(InvoiceData invoiceInfo)
        //{
        //    try
        //    {

        //    }
        //    catch (Exception)
        //    {

        //        throw;
        //    }
        //    return false;
        //}

        public async Task<bool> PurchaseDebtInvoiceAsync(InvoiceData invoiceInfo, CustomerBalance customerBalance)
        {
            try
            {
                decimal? totalPaided = 0;
                // update customerBalance
                customerBalance.CustomerId = customerBalance.Customer.Id;
                totalPaided = invoiceInfo.Amount;
                // insert transaction
                var transaction = new Transaction();
                transaction.Id = Guid.NewGuid().ToString();
                transaction.TransactionType = ((int)TransactionType.DebtPurchase).ToString();
                transaction.TransactionDate = DateTime.Now;
                transaction.CustomerId = customerBalance.Customer.Id;
                transaction.Amount = (decimal)invoiceInfo.Amount;
                

                if (invoiceInfo.InvoiceId != null)
                {
                    string invoiceId = invoiceInfo.InvoiceId;
                    var invoice = await _invoiceService.FindAsync(invoiceId);
                    totalPaided = invoice.TotalAmount;
                    transaction.Amount = (decimal)invoice.TotalAmount;
                    transaction.InvoiceId = invoiceId;
                }
             
                customerBalance.TotalPaid += (double)totalPaided;
                var customerBalanceLeft = customerBalance.TotalDebt - customerBalance.TotalPaid;
                customerBalance.Balance = customerBalanceLeft;

                _customerBalanceService.Update(customerBalance);

                await _transactionService.AddAsync(transaction);
                await _unitOfWork.SaveChangeAsync();
                await _efTransaction.CommitAsync();
            }
            catch (Exception)
            {
                await _efTransaction.RollbackAsync();
                throw;
            }
            return false;
        }

        public  string VnpayCreatePayPaymentRequestAsync(PaymentRequestData paymentRequestData)
        {
            var paymentId = Guid.NewGuid().ToString();
            var customerId = paymentRequestData.CustomerId;
            var orderInfo = paymentRequestData.OrderInfo;
            var note = paymentRequestData.Note != null ? paymentRequestData.Note : "";
            var totalAmount = paymentRequestData.Amount;

            var customer = _userService.Get(_ => _.Id.Equals(customerId)).FirstOrDefault();
            // get customer address
            var customerAddress = $"{customer.Address} {customer.Ward} {customer.District} {customer.Province}";
            var shippingAddress = paymentRequestData.Address != null ? paymentRequestData.Address : customerAddress;
            VnpayPayRequest vnpayPaymentRequest = new VnpayPayRequest
            {
                vnp_Version = _configuration["Vnpay:Version"],
                vnp_TmnCode = _configuration["Vnpay:TmnCode"],
                vnp_Command = "pay",
                vnp_Amount = (int)totalAmount * 100,
                vnp_CreateDate = DateTime.Now.ToString("yyyyMMddHHmmss"),
                vnp_IpAddr = ClientHelper.GetIpAddress(_httpContextAccessor.HttpContext),
                vnp_Locale = "vn",
                vnp_OrderInfo = orderInfo,
                vnp_OrderType = "other",
                vnp_CurrCode = _configuration["Vnpay:CurrentCode"],
                vnp_ReturnUrl = _configuration["Vnpay:ReturnUrl"],
                vnp_ExpireDate = DateTime.Now.AddMinutes(5).ToString("yyyyMMddHHmmss"),
                vnp_TxnRef = paymentId,

            };
            var paymentLink = vnpayPaymentRequest.GetLink(_configuration["Vnpay:PaymentUrl"], _configuration["Vnpay:HashSecret"]);

            Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("Starting payment task for order: {orderDescription}", orderInfo);
                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        var _paymentRepositoryscoped = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();
                        var _unitOfWorkScope = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                        var _efTranscationScope = scope.ServiceProvider.GetRequiredService<ITransaction>();
                        var _invoiceRepositoryScope = scope.ServiceProvider.GetRequiredService<IInvoiceRepository>();
                        var _invoiceDetailRepositoryScope = scope.ServiceProvider.GetRequiredService<IInvoiceDetailRepository>();
                        var _shippingDetailRepositoryScope = scope.ServiceProvider.GetRequiredService<IShippingDetailRepository>();

                        // create database transcation 
                        try
                        {
                            // create invoice 
                            var invoice = new Invoice
                            {
                                Id = Guid.NewGuid().ToString(),
                                CustomerId = customerId,
                                InvoiceDate = DateTime.Now,
                                InvoiceStatus = (int)InvoiceStatus.Pending,
                                Note = note,
                                TotalAmount = totalAmount
                            };
                            await _invoiceRepositoryScope.AddAsync(invoice);
                            await _unitOfWorkScope.SaveChangeAsync();

                            // create invoiceDetail
                            foreach (var cartItem in paymentRequestData.CartItems)
                            {
                                var material = await _materialService.FindAsync(Guid.Parse(cartItem.MaterialId));
                                var invoiceDetail = new InvoiceDetail
                                {
                                    Id = Guid.NewGuid().ToString(),
                                    LineTotal = material.SalePrice * cartItem.Quantity,
                                    MaterialId = Guid.Parse(cartItem.MaterialId),
                                    VariantId = Guid.Parse(cartItem.VariantId),
                                    Quantity = cartItem.Quantity,
                                    InvoiceId = invoice.Id,
                                };
                                if (cartItem.VariantId != null)
                                {
                                    var variant = _variantService.Get(_ => _.Id.Equals(Guid.Parse(cartItem.VariantId))).FirstOrDefault();
                                    invoiceDetail.VariantId = Guid.Parse(cartItem.VariantId);
                                    invoiceDetail.LineTotal = variant.Price *  cartItem.Quantity;
                                }
                       
                                await _invoiceDetailRepositoryScope.AddAsync(invoiceDetail);
                            }

                            // create payment
                            var payment = new Core.Entities.Payment
                            {
                                Id = paymentId,
                                AmountPaid = totalAmount,
                                PaymentDate = DateTime.Now,
                                PaymentDescription = orderInfo,
                                PaymentStatus = 0,
                                PaymentMethod = "pay",
                                InvoiceId = invoice.Id,
                                BankCode = vnpayPaymentRequest.vnp_BankCode,

                            };
                            await _paymentRepositoryscoped.AddAsync(payment);

                            // create shipping detail

                            var shippingDetail = new ShippingDetail
                            {
                                Id = Guid.NewGuid().ToString(),
                                Address = shippingAddress,
                                EstimatedArrival = DateTime.Now.AddDays(3),
                                InvoiceId = invoice.Id,
                            };
                            await _shippingDetailRepositoryScope.AddAsync(shippingDetail);


                            await _unitOfWorkScope.SaveChangeAsync();

                            // Commit transaction sucecssfully
                            await _efTranscationScope.CommitAsync();
                        }
                        catch (Exception)
                        {
                            // rollback 
                            await _efTranscationScope.RollbackAsync();
                            throw;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing payment for order: {orderDescription}", paymentRequestData.OrderInfo);
                }

            });
            return paymentLink;
        }

        public async Task<VnpayResponseData> VnpayReturnUrl(VnpayPayResponse vnpayPayResponse)
        {
            var resultData = new VnpayResponseData();
            try
            {
                var isValidSignature = vnpayPayResponse.IsValidSignature(_configuration["Vnpay:HashSecret"]);
                if (!isValidSignature)
                {
                    resultData.PaymentStatus = "99";
                    resultData.PaymentMessage = "Invalid signature in response";
                }
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var _paymentRepositoryScope = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();
                    var _unitOfWorkScope = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    var _invoiceRepositoryScope = scope.ServiceProvider.GetRequiredService<IInvoiceRepository>();
                    var _invoiceDetailRepositoryScope = scope.ServiceProvider.GetRequiredService<IInvoiceDetailRepository>();
                    var _transactionRepositoryScope = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();

                    var paymentStatus = vnpayPayResponse.vnp_ResponseCode;
                    resultData.PaymentStatus = paymentStatus;
                    switch (paymentStatus)
                    {
                        case "00":
                            resultData.PaymentMessage = "Payment succesfully";
                            break;
                        case "10":
                            resultData.PaymentMessage = "Payment process failed";
                            break;
                    }

                    var payment = _paymentRepositoryScope.Get(_ => _.Id.Equals(vnpayPayResponse.vnp_TxnRef)).FirstOrDefault();
                    if (payment != null)
                    {
                        //update invoice
                        var invoice = await _invoiceRepositoryScope.FindAsync(payment.InvoiceId);
                        invoice.InvoiceStatus = (int)InvoiceStatus.PaymentSucces;
                        _invoiceRepositoryScope.Update(invoice);
                        // update payment
                        payment.BankCode = vnpayPayResponse.vnp_BankCode;
                        payment.PaymentStatus = Int32.Parse(vnpayPayResponse.vnp_ResponseCode);
                        _paymentRepositoryScope.Update(payment);

                        // create transaction
                        var transcation = new Transaction
                        {
                            Id = Guid.NewGuid().ToString(),
                            Amount = (decimal)vnpayPayResponse.vnp_Amount,
                            CustomerId = invoice.CustomerId,
                            InvoiceId = invoice.Id,
                            TransactionDate = DateTime.Now,
                            TransactionType = ((int)TransactionType.OnlinePayment).ToString(),
                        };
                        await _transactionRepositoryScope.AddAsync(transcation);
                        var result = await _unitOfWork.SaveChangeAsync();
                    }
                    return resultData;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
