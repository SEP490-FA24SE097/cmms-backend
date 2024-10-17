


using CMMS.Infrastructure.Services.Payment;

namespace CMMS.API.Services.BackgroundJob
{
	public class PaymentBackgroundService : BackgroundService
	{
		private IPaymentService _paymentService;

		public PaymentBackgroundService(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }
        public Task StartAsync(CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}
		protected override Task ExecuteAsync(CancellationToken stoppingToken)
		{
			throw new NotImplementedException();
		}
	}
}
