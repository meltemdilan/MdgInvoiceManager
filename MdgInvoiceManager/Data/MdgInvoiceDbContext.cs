using Microsoft.EntityFrameworkCore;
using MdgInvoiceManager.Models; 

namespace MdgInvoiceManager.Data
{
    public class MdgInvoiceDbContext : DbContext
    {
        public MdgInvoiceDbContext(DbContextOptions<MdgInvoiceDbContext> options) : base(options)
        {
        }

        
        public DbSet<Invoice> Invoices { get; set; }
    }
}
