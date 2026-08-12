using System.Collections.Generic;
using System.Collections.Generic;
using System.Threading.Tasks;
using MdgInvoiceManager.Core.Entities;

namespace MdgInvoiceManager.Business.Abstract
{
    public interface IInvoiceService
    {
        Task<List<Invoice>> GetAllInvoicesAsync(int pageNumber = 1, int pageSize = 10);
        Task<Invoice?> GetInvoiceByIdAsync(int id);
        Task<Invoice> CreateInvoiceAsync(Invoice invoice);
        Task<bool> UpdateInvoiceAsync(int id, Invoice updatedInvoice);
        Task<bool> DeleteInvoiceAsync(int id);
    }
}