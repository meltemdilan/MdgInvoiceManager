namespace MdgInvoiceManager.Core
{
    public record InvoiceCreatedEvent
    {
        public int InvoiceId { get; init; }
        public string CustomerName { get; init; } = string.Empty;
        public decimal TotalAmount { get; init; }
    }
}