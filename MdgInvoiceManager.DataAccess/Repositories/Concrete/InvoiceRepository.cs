using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MdgInvoiceManager.DataAccess.Data;
using MdgInvoiceManager.DataAccess.Repositories.Abstract;
using Microsoft.EntityFrameworkCore;
using Invoice = MdgInvoiceManager.Core.Entities.Invoice;

namespace MdgInvoiceManager.DataAccess.Repositories.Concrete
{
    public class InvoiceRepository : IInvoiceRepository
    {
        private readonly MdgInvoiceDbContext _context;

        public InvoiceRepository(MdgInvoiceDbContext context)
        {
            _context = context;
        }

        public async Task<Invoice?> GetByIdAsync(int id)
        {
            return await _context.Invoices
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<Invoice>> GetAllAsync()
        {
            return await _context.Invoices
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<Invoice>> GetPagedInvoicesAsync(string? userId, bool isAdmin, int pageNumber, int pageSize)
        {
            var query = _context.Invoices.AsNoTracking();

            if (!isAdmin && !string.IsNullOrEmpty(userId))
            {
                query = query.Where(y => y.UserId == userId);
            }

            return await query
                .OrderByDescending(y => y.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task AddAsync(Invoice invoice)
        {
            await _context.Invoices.AddAsync(invoice);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Invoice invoice)
        {
            _context.Invoices.Update(invoice);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Invoice invoice)
        {
            _context.Invoices.Remove(invoice);
            await _context.SaveChangesAsync();
        }
    }
}