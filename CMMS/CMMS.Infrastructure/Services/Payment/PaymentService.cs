using CMMS.Core.Entities;
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
        string VnpayCreatePayPaymentRequest(PaymentRequestData paymentRequestData);
        Task<VnpayResponseData> VnpayReturnUrl(VnpayPayResponse vnpayPayResponse);
    }
    public class PaymentService : IPaymentService
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IPaymentRepository _paymentRepsitory;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PaymentService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public PaymentService(IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor, IPaymentRepository paymentRepository,
            IUnitOfWork unitOfWork, ILogger<PaymentService> logger, IServiceScopeFactory serviceScopeFactory)
        {
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _paymentRepsitory = paymentRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public string VnpayCreatePayPaymentRequest(PaymentRequestData paymentRequestData)
        {
            var paymentId = Guid.NewGuid().ToString();
            var customerId = paymentRequestData.CustomerId;
            var orderInfo = paymentRequestData.OrderInfo;
            var note = paymentRequestData.Note != null ? paymentRequestData.Note : "";
            var totalAmount = paymentRequestData.Amount;
            var shippingAddress = paymentRequestData.Address;
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
                            // get customer cart
                            //var customerCart = _cartRepositoryScope.Get(_ => _.CustomerId.Equals(customerId));
                            //foreach (var cartItem in customerCart)
                            //{
                            //    var invoiceDetail = new InvoiceDetail
                            //    {
                            //        Id = Guid.NewGuid().ToString(),
                            //        LineTotal = cartItem.TotalAmount,
                            //        MaterialId = cartItem.MaterialId,
                            //        VariantId = cartItem.VariantId,
                            //        Quantity = cartItem.Quantity,
                            //        InvoiceId = invoice.Id,
                            //    };
                            //    await _invoiceDetailRepositoryScope.AddAsync(invoiceDetail);
                            //    // remove cart row immediately after add invoiceDetail
                            //    _cartRepositoryScope.Remove(cartItem);
                            //}

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
                            TransactionType = TransactionType.PaymentOnline,
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
