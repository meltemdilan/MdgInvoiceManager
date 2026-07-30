


namespace MdgInvoiceManager.Core.Dtos
{
    public class ResponseModel
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Token { get; set; }
}
}