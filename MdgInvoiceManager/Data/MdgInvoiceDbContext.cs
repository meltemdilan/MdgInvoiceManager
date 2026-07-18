using Microsoft.EntityFrameworkCore;
using MdgInvoiceManager.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace MdgInvoiceManager.Data
{
    public class MdgInvoiceDbContext : IdentityDbContext<IdentityUser>
    {
        public MdgInvoiceDbContext(DbContextOptions<MdgInvoiceDbContext> options) : base(options)
        {
        }

        
        public DbSet<Invoice> Invoices { get; set; }
    }
}
