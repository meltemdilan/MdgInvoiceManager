using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MdgInvoiceManager.Models
{
    [Table("Invoice")]
    public class Invoice
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(250)]
        public string CustomerName { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public string InvoiceType { get; set; } = "Genel";

        [Required]
        public DateTime InvoiceDate { get; set; } = DateTime.Now;

        
        public string? FilePath { get; set; }

        
        public string? Scenario { get; set; }

        
        public string? Currency { get; set; }

      
        public string? VknTckn { get; set; }

       
        public string? TaxOffice { get; set; }

        
        public string? City { get; set; }

        
        public decimal TaxAmount { get; set; }


        public decimal TotalAmount { get; set; }
    }
}