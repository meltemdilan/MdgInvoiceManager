using System.Collections.Generic;
using System.Threading.Tasks;
using MdgInvoiceManager.Core.Entities;

namespace MdgInvoiceManager.DataAccess.Repositories.Abstract
{
    public interface IInvoiceRepository
    {
        Task<List<Invoice>> GetPagedInvoicesAsync(
            string? userId,
            bool isAdmin,
            int pageNumber,
            int pageSize);

        Task<List<Invoice>> GetAllAsync();
        Task<Invoice?> GetByIdAsync(int id);
        Task AddAsync(Invoice invoice);
        Task UpdateAsync(Invoice invoice);
        Task DeleteAsync(Invoice invoice);
    }
}