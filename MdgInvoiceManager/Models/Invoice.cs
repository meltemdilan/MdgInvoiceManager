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

        [Required(ErrorMessage = "Müşteri Adı / Unvanı boş bırakılamaz.")]
        [StringLength(250, ErrorMessage = "Müşteri adı en fazla 250 karakter olabilir.")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tutar alanı boş bırakılamaz.")]
        [Range(0.01, 10000000.00, ErrorMessage = "Fatura tutarı 0'dan büyük ve makul bir değer olmalıdır.")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Şehir alanı boş bırakılamaz.")]
        [City] // 👈 Eklediğimiz 81 İl Kontrolü
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Fatura türü boş bırakılamaz.")]
        public string InvoiceType { get; set; } = "Genel";

        [Required(ErrorMessage = "Fatura tarihi seçilmelidir.")]
        public DateTime InvoiceDate { get; set; } = DateTime.Now;

        public string? FilePath { get; set; }

        public string? Scenario { get; set; }

        public string? Currency { get; set; }

        // VKN/TCKN Alanı (Sadece 10 veya 11 haneli rakamlar)
        [RegularExpression(@"^[0-9]{10,11}$", ErrorMessage = "VKN/TCKN alanı sadece 10 haneli (VKN) veya 11 haneli (TCKN) rakamlardan oluşmalıdır.")]
        public string? VknTckn { get; set; }

        // Vergi Dairesi
        public string? TaxOffice { get; set; }

        // Otomatik hesaplanan alanlar
        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }
    }
}