using AutoMapper;
using CMMS.Core.Entities;
using CMMS.Core.Models;
using CMMS.Infrastructure.Constant;
using CMMS.Infrastructure.Data;
using CMMS.Infrastructure.Enums;
using CMMS.Infrastructure.Helpers;
using CMMS.Infrastructure.Repositories;
using CMMS.Infrastructure.Services.Payment.Vnpay.Request;
using CMMS.Infrastructure.Services.Payment.Vnpay.Response;
using CMMS.Infrastructure.Services.Shipping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;


namespace CMMS.Infrastructure.Services.Payment
{
    public interface IPaymentService
    {
        string VnpayCreatePayPaymentRequestAsync(PaymentRequestData paymentRequestData);
        Task<VnpayResponseData> VnpayReturnUrl(VnpayPayResponse vnpayPayResponse);
        Task<bool> PaymentInvoiceAsync(InvoiceData invoiceInfo);
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
        private readonly IStoreInventoryService _storeInventoryService;
        private readonly IMapper _mapper;
        private readonly IShippingService _shippingService;

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
            ITransaction transaction, IUserService userService,
            IStoreInventoryService storeInventoryService,
             IMapper mapper, IShippingService shippingService)
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
            _storeInventoryService = storeInventoryService;
            _mapper = mapper;
            _shippingService = shippingService;
        }

        public async Task<bool> PaymentDebtInvoiceAsync(InvoiceData invoiceInfo, CustomerBalance customerBalance)
        {
            try
            {
                // insert invoice
                var invoice = new Invoice
                {
                    Id = _invoiceService.GenerateInvoiceCode(),
                    CustomerId = customerBalance.Customer.Id,
                    InvoiceDate = TimeConverter.GetVietNamTime(),
                    InvoiceStatus = (int)InvoiceStatus.Pending,
                    InvoiceType = (int)InvoiceType.Debt,
                    Note = invoiceInfo.Note,
                    TotalAmount = (decimal)invoiceInfo.TotalAmount,
                };
                await _invoiceService.AddAsync(invoice);
                await _invoiceService.SaveChangeAsync();

                // insert transaction
                var transaction = new Transaction();

                transaction = new Transaction();
                transaction.Id = "DH" + invoice.Id;
                transaction.TransactionType = (int)TransactionType.SaleItem;
                transaction.TransactionDate = TimeConverter.GetVietNamTime();
                transaction.CustomerId = invoice.CustomerId;
                transaction.InvoiceId = invoice.Id;
                transaction.TransactionPaymentType = 1;
                transaction.Amount = (decimal)invoiceInfo.TotalAmount;
                await _transactionService.AddAsync(transaction);

                // insert invoice detail
        
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

        public async Task<bool> PaymentInvoiceAsync(InvoiceData invoiceInfo)
        {
            try
            {
         
                var customer = _userService.Get(_ => _.Id.Equals(invoiceInfo.CustomerId)).FirstOrDefault();
                if(customer == null) return false;
                var customerAddress = $"{customer.Address}, {customer.Ward}, {customer.District}, {customer.Province}";

                //  chỗ này đem qua bên phần xử lý hóa đơn.
                //if (invoiceInfo.Address != null)
                //{
                //    var customerDeliveryPostition = (await _shippingService.ResponseLatitueLongtitueValue(customerAddress)).Split(",");
                //    var newAddress = $"{invoiceInfo.Address}, {invoiceInfo.Ward}, {invoiceInfo.District}, {invoiceInfo.Province}";
                //    var newDeliveryPostition = (await _shippingService.ResponseLatitueLongtitueValue(newAddress)).Split(",");

                //    var distanceBetween = _shippingService.CalculateDistanceBetweenPostionLatLon(double.Parse(customerDeliveryPostition[0]), double.Parse(customerDeliveryPostition[1]),
                //        double.Parse(newDeliveryPostition[0]), double.Parse(newDeliveryPostition[1]));
                //    // nếu khoảng cách lớn hơn 1 thì tính thêm tiền ship
                //    if (distanceBetween > 1)
                //    {

                //    }
                //}
                var storeInvoices = invoiceInfo.PreCheckOutItemCartModel;
                var groupInvoiceId = Guid.NewGuid().ToString();
                foreach (var storeInvoice in storeInvoices)
                {
                    var storeId = storeInvoice.StoreId;
                    var invoiceCode = _invoiceService.GenerateInvoiceCode();

                    // insert invoice

                    // sua cho invoice nay lai lay sai data.
                    var invoice = new Invoice
                    {
                        Id = invoiceCode,
                        CustomerId = invoiceInfo.CustomerId,
                        InvoiceDate = TimeConverter.GetVietNamTime(),
                        InvoiceStatus = invoiceInfo.PaymentType.Equals(PaymentType.PurchaseAfter) ? (int)InvoiceStatus.Pending : (int)InvoiceStatus.Done,
                        InvoiceType = (int)InvoiceType.Normal,
                        Note = invoiceInfo.Note,
                        StoreId = storeId,
                        // get total cart 
                        SalePrice = (decimal)storeInvoice.FinalPrice,
                        TotalAmount = (decimal)storeInvoice.TotalStoreAmount,
                        Discount = invoiceInfo.Discount != null ? invoiceInfo.Discount : 0,
                        SellPlace = (int)SellPlace.Website,
                        // create group invoice
                        GroupId = groupInvoiceId

                    };
                    await _invoiceService.AddAsync(invoice);
                    var InvoiceResult = await _invoiceService.SaveChangeAsync();

                    foreach (var storeItem in storeInvoice.StoreItems)
                    {
                        // insert invoice Details
                        var invoiceDetail = new InvoiceDetail
                        {
                            Id = Guid.NewGuid().ToString(),
                            LineTotal = storeItem.ItemTotalPrice,
                            MaterialId = Guid.Parse(storeItem.MaterialId),
                            VariantId = storeItem.VariantId != null ? Guid.Parse(storeItem.VariantId) : null,
                            Quantity = storeItem.Quantity,
                            InvoiceId = invoice.Id,
                        };
                        await _invoiceDetailService.AddAsync(invoiceDetail);

                        var cartItem = new CartItem
                        {
                            MaterialId = storeItem.MaterialId,
                            VariantId = storeItem.VariantId != null ? storeItem.VariantId : null,
                            StoreId = storeId,
                            Quantity = storeItem.Quantity
                        };
                        // update store quantity
                        var updateQuantityStatus = await _storeInventoryService.UpdateStoreInventoryAsync(cartItem, (int)InvoiceStatus.Pending);
                    }
                    if (InvoiceResult)
                    {
                        // insert transaction
                        // tao transaction ban hang.
                        var transaction = new Transaction();
                        transaction.Id = "DH" + invoiceCode;
                        transaction.TransactionType = (int)TransactionType.SaleItem;
                        transaction.TransactionDate = TimeConverter.GetVietNamTime();
                        transaction.CustomerId = invoiceInfo.CustomerId;
                        transaction.InvoiceId = invoice.Id;
                        transaction.Amount = (decimal)storeInvoice.FinalPrice;
                        transaction.TransactionPaymentType = 1;
                        await _transactionService.AddAsync(transaction);

                        //await _invoiceService.SaveChangeAsync();
                        var shippingDetail = new ShippingDetail();
                        shippingDetail.Id = "GH" + invoiceCode;
                        shippingDetail.Invoice = invoice;
                        shippingDetail.PhoneReceive = invoiceInfo.PhoneReceive;
                        shippingDetail.EstimatedArrival = TimeConverter.GetVietNamTime().AddDays(3);
                        shippingDetail.Address = customerAddress;
                        shippingDetail.ShippingFee = storeInvoice.ShippngFree;
                        await _shippingDetailService.AddAsync(shippingDetail);
                    }
                }
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

        public async Task<bool> PurchaseDebtInvoiceAsync(InvoiceData invoiceInfo, CustomerBalance customerBalance)
        {
            try
            {
                decimal? totalPaided = 0;
                // update customerBalance
                customerBalance.CustomerId = customerBalance.Customer.Id;
                totalPaided = invoiceInfo.TotalAmount;
                // insert transaction
                var transaction = new Transaction();
                transaction.Id = Guid.NewGuid().ToString();
                transaction.TransactionType = (int)TransactionType.PurchaseCustomerDebt;
                transaction.TransactionDate = TimeConverter.GetVietNamTime();
                transaction.CustomerId = customerBalance.Customer.Id;
                transaction.Amount = (decimal)invoiceInfo.TotalAmount;

                if (invoiceInfo.InvoiceId != null)
                {
                    string invoiceId = invoiceInfo.InvoiceId;
                    var invoice = await _invoiceService.FindAsync(invoiceId);
                    totalPaided = invoice.TotalAmount;
                    transaction.Amount = (decimal)totalPaided;
                    transaction.InvoiceId = invoiceId;

                    // update invoice 
                    invoice.InvoiceStatus = (int)InvoiceStatus.Done;
                    _invoiceService.Update(invoice);
                }

                customerBalance.TotalPaid += (decimal)totalPaided;
                var customerBalanceLeft = customerBalance.TotalDebt - customerBalance.TotalPaid;

                _customerBalanceService.Update(customerBalance);

                await _transactionService.AddAsync(transaction);
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

        public string VnpayCreatePayPaymentRequestAsync(PaymentRequestData paymentRequestData)
        {
            var paymentId = Guid.NewGuid().ToString();
            var invoiceCode = _invoiceService.GenerateInvoiceCode();
            var orderInfo = paymentRequestData.OrderInfo;
            var note = paymentRequestData.Note != null ? paymentRequestData.Note : "";
            var totalAmount = paymentRequestData.TotalAmount;
            var customer = _userService.Get(_ => _.Id.Equals(paymentRequestData.CustomerId)).FirstOrDefault();
            // get customer address
            var customerAddress = $"{customer.Address} {customer.Ward} {customer.District} {customer.Province}";
            VnpayPayRequest vnpayPaymentRequest = new VnpayPayRequest
            {
                vnp_Version = _configuration["Vnpay:Version"],
                vnp_TmnCode = _configuration["Vnpay:TmnCode"],
                vnp_Command = "pay",
                vnp_Amount = (int)totalAmount * 100,
                vnp_CreateDate = TimeConverter.GetVietNamTime().ToString("yyyyMMddHHmmss"),
                vnp_IpAddr = ClientHelper.GetIpAddress(_httpContextAccessor.HttpContext),
                vnp_Locale = "vn",
                vnp_OrderInfo = orderInfo,
                vnp_OrderType = "other",
                vnp_CurrCode = _configuration["Vnpay:CurrentCode"],
                vnp_ReturnUrl = _configuration["Vnpay:ReturnUrl"],
                vnp_ExpireDate = TimeConverter.GetVietNamTime().AddMinutes(5).ToString("yyyyMMddHHmmss"),
                vnp_TxnRef = invoiceCode,

            };
            var paymentLink = vnpayPaymentRequest.GetLink(_configuration["Vnpay:PaymentUrl"], _configuration["Vnpay:HashSecret"]);
            try
            {
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
                            var _transactionRepostoryScope = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();
                            var _stroInventoryServiceScope = scope.ServiceProvider.GetRequiredService<IStoreInventoryService>();

                            // create database transcation 
                            try
                            {
                                var storeInvoices = paymentRequestData.PreCheckOutItemCartModel;
                                var groupInvoiceId = Guid.NewGuid().ToString();
                                foreach (var storeInvoice in storeInvoices)
                                {
                                    var storeId = storeInvoice.StoreId;

                                    var invoice = new Invoice
                                    {
                                        Id = invoiceCode,
                                        CustomerId = paymentRequestData.CustomerId,
                                        InvoiceDate = TimeConverter.GetVietNamTime(),
                                        InvoiceStatus = (int)InvoiceStatus.Pending,
                                        InvoiceType = (int)InvoiceType.Normal,
                                        Note = paymentRequestData.Note,
                                        StoreId = storeId,
                                        SalePrice = (decimal)storeInvoice.TotalStoreAmount,
                                        TotalAmount = (decimal)storeInvoice.TotalStoreAmount,
                                        Discount = paymentRequestData.Discount != null ? paymentRequestData.Discount : 0,
                                        SellPlace = (int)SellPlace.Website,
                                        GroupId = groupInvoiceId

                                    };
                                    await _invoiceRepositoryScope.AddAsync(invoice);
                                    var InvoiceResult = await _unitOfWorkScope.SaveChangeAsync();

                                    foreach (var storeItem in storeInvoice.StoreItems)
                                    {
                                        var invoiceDetail = new InvoiceDetail
                                        {
                                            Id = Guid.NewGuid().ToString(),
                                            LineTotal = storeItem.ItemTotalPrice,
                                            MaterialId = Guid.Parse(storeItem.MaterialId),
                                            VariantId = storeItem.VariantId != null ? Guid.Parse(storeItem.VariantId) : null,
                                            Quantity = storeItem.Quantity,
                                            InvoiceId = invoice.Id,
                                        };
                                        await _invoiceDetailRepositoryScope.AddAsync(invoiceDetail);

                                        var cartItem = new CartItem
                                        {
                                            MaterialId = storeItem.MaterialId,
                                            VariantId = storeItem.VariantId != null ? storeItem.VariantId : null,
                                            StoreId = storeId,
                                            Quantity = storeItem.Quantity
                                        };
                                        // update store quantity
                                        var updateQuantityStatus = await _stroInventoryServiceScope.UpdateStoreInventoryAsync(cartItem, (int)InvoiceStatus.Pending);
                                    }
                                    if (InvoiceResult)
                                    {

                                        var transaction = new Transaction();
                                        transaction.Id = "DH" + invoiceCode;
                                        transaction.TransactionType = (int)TransactionType.SaleItem;
                                        transaction.TransactionDate = TimeConverter.GetVietNamTime();
                                        transaction.CustomerId = paymentRequestData.CustomerId;
                                        transaction.InvoiceId = invoice.Id;
                                        transaction.Amount = (decimal)storeInvoice.FinalPrice;
                                        transaction.TransactionPaymentType = 1;
                                        await _transactionRepostoryScope.AddAsync(transaction);

                                        var shippingDetail = new ShippingDetail();
                                        shippingDetail.Id = "GH" + invoiceCode;
                                        shippingDetail.Invoice = invoice;
                                        shippingDetail.PhoneReceive = paymentRequestData.PhoneReceive;
                                        shippingDetail.EstimatedArrival = TimeConverter.GetVietNamTime().AddDays(3);
                                        shippingDetail.Address = customerAddress;
                                        shippingDetail.ShippingFee = storeInvoice.ShippngFree;
                                        await _shippingDetailRepositoryScope.AddAsync(shippingDetail);
                                    }

                                    // create payment
                                    var payment = new Core.Entities.Payment
                                    {
                                        Id = Guid.NewGuid().ToString(),
                                        AmountPaid = (decimal)invoice.SalePrice,
                                        PaymentDate = TimeConverter.GetVietNamTime(),
                                        PaymentDescription = orderInfo,
                                        PaymentStatus = 0,
                                        PaymentMethod = "pay",
                                        InvoiceId = invoice.Id,
                                        BankCode = vnpayPaymentRequest.vnp_BankCode,
                                    };
                                    await _paymentRepositoryscoped.AddAsync(payment);
                                }
                                var result = await _unitOfWorkScope.SaveChangeAsync();
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
            }
            catch (Exception)
            {

                throw;
            }
      
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

                    var payment = _paymentRepositoryScope.Get(_ => _.InvoiceId.Equals(vnpayPayResponse.vnp_TxnRef)).FirstOrDefault();
                    if (payment != null)
                    {
                        //update invoice
                        var invoice = await _invoiceRepositoryScope.FindAsync(payment.InvoiceId);
                        // update payment
                        payment.BankCode = vnpayPayResponse.vnp_BankCode;
                        payment.PaymentStatus = Int32.Parse(vnpayPayResponse.vnp_ResponseCode);
                        _paymentRepositoryScope.Update(payment);

                        var transaction = new Transaction();
                        transaction.Id = "TT" + invoice.Id;
                        transaction.TransactionType = (int)TransactionType.PurchaseDebtInvoice;
                        transaction.TransactionDate = TimeConverter.GetVietNamTime();
                        transaction.CustomerId = invoice.CustomerId;
                        transaction.InvoiceId = invoice.Id;
                        transaction.Amount = (decimal)invoice.SalePrice;
                        transaction.TransactionPaymentType = 1;
                        await _transactionRepositoryScope.AddAsync(transaction);
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
