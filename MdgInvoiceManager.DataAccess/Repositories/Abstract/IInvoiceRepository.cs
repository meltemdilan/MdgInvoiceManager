using System.Collections.Generic;
using System.Threading.Tasks;
using MdgInvoiceManager.Core.Entities;

namespace MdgInvoiceManager.DataAccess.Repositories.Abstract
{
    public interface IInvoiceRepository
    {
        Task<List<Invoice>> GetAllAsync();
        Task<Invoice?> GetByIdAsync(int id);
        Task AddAsync(Invoice invoice);
        void Update(Invoice invoice);
        void Delete(Invoice invoice);
        Task SaveChangesAsync();
    }
}