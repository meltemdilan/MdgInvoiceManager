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

        // LINQ sorgularını veritabanı seviyesinde (SQL) çalıştırmak için IQueryable döner
        public IQueryable<Invoice> GetAllQueryable()
        {
            return _context.Invoices.AsNoTracking();
        }

        public async Task<List<Invoice>> GetAllAsync()
        {
            return await _context.Invoices.ToListAsync();
        }

        public async Task<Invoice?> GetByIdAsync(int id)
        {
            return await _context.Invoices.FindAsync(id);
        }

        public async Task AddAsync(Invoice invoice)
        {
            await _context.Invoices.AddAsync(invoice);
        }

        public void Update(Invoice invoice)
        {
            _context.Invoices.Update(invoice);
        }

        public void Delete(Invoice invoice)
        {
            _context.Invoices.Remove(invoice);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}