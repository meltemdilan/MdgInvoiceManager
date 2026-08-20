using MassTransit;
using MdgInvoiceManager.Core;
using Microsoft.Extensions.Logging;

namespace MdgInvoiceManager.Business.Concrete
{
    public class InvoiceCreatedConsumer : IConsumer<InvoiceCreatedEvent>
    {
        private readonly ILogger<InvoiceCreatedConsumer> _logger;

        public InvoiceCreatedConsumer(ILogger<InvoiceCreatedConsumer> logger)
        {
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<InvoiceCreatedEvent> context)
        {
            var data = context.Message;

            _logger.LogInformation($"[KUYRUK TETIKLENDI] Fatura ID: {data.InvoiceId} - Müşteri: {data.CustomerName} ({data.TotalAmount} TL) için arka plan işlemleri başladı.");

            await Task.Delay(2000);

            _logger.LogInformation($"[İŞLEM TAMAMLANDI] Fatura ID: {data.InvoiceId} başarıyla işlendi.");
        }
    }
}