using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MdgInvoiceManager.Core.Entities;

namespace MdgInvoiceManager.DataAccess.Data
{
    public class MdgInvoiceDbContext : IdentityDbContext<IdentityUser>
    {
        public MdgInvoiceDbContext(DbContextOptions<MdgInvoiceDbContext> options) : base(options)
        {
        }

        public DbSet<Invoice> Invoices { get; set; }
   
    }
}