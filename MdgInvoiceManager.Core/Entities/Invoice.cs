using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MdgInvoiceManager.Core.Entities
{
    public class Invoice
    {
        public int Id { get; set; }

        // Kullanıcıya özel fatura yetkilendirmesi için gerekli alan
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Müşteri adı boş bırakılamaz.")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tutar alanı boş bırakılamaz.")]
        [Range(0.01, 10000000.00, ErrorMessage = "Fatura tutarı 0'dan büyük ve makul bir değer olmalıdır.")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Şehir alanı boş bırakılamaz.")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Fatura türü boş bırakılamaz.")]
        public string InvoiceType { get; set; } = "Genel";

        [Required(ErrorMessage = "Fatura tarihi seçilmelidir.")]
        public DateTime InvoiceDate { get; set; } = DateTime.Now;

        public string? FilePath { get; set; }

        public string? Scenario { get; set; }

        public string? Currency { get; set; }

        [RegularExpression(@"^[0-9]{10,11}$", ErrorMessage = "VKN/TCKN alanı 10 veya 11 haneli rakamlardan oluşmalıdır.")]
        public string? VknTckn { get; set; }

        public string? TaxOffice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }
    }
}