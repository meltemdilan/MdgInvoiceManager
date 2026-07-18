using Microsoft.AspNetCore.Authorization; // Yetkilendirme kütüphanesi
using Microsoft.AspNetCore.Mvc;
using MdgInvoiceManager.Models;
using MdgInvoiceManager.Data;
using System;
using System.Linq;

namespace MdgInvoiceManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvoiceController : ControllerBase
    {
        private readonly MdgInvoiceDbContext _context;

        public InvoiceController(MdgInvoiceDbContext context)
        {
            _context = context;
        }

        // GET: api/Invoice
        // Tüm faturaları listeler (Giriş yapmış her kullanıcı görebilir)
        [HttpGet]
       // [Authorize]
        public IActionResult GetAll()
        {
            var invoices = _context.Invoices.ToList();
            return Ok(invoices); // HTTP 200 OK + JSON Listesi
        }

        // GET: api/Invoice/5
        // Id'ye göre tek bir faturayı getirir (Giriş yapmış her kullanıcı görebilir)
        [HttpGet("{id}")]
       // [Authorize]
        public IActionResult GetById(int id)
        {
            var invoice = _context.Invoices.Find(id);
            if (invoice == null)
            {
                return NotFound(new { message = $"ID değeri {id} olan fatura bulunamadı." }); // HTTP 404
            }
            return Ok(invoice); // HTTP 200 OK + Fatura JSON
        }

        // POST: api/Invoice
        // Yeni bir fatura oluşturur (Giriş yapmış her kullanıcı ekleyebilir)
        [HttpPost]
       // [Authorize]
        public IActionResult Create([FromBody] Invoice invoice)
        {
            // 1. Veri Boş mu Kontrolü
            if (invoice == null)
            {
                return BadRequest(new { message = "Geçersiz veya boş fatura verisi." }); // HTTP 400
            }

            // 2. Model Validasyon Kontrolü
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState); // HTTP 400
            }

            // 3. Varsayılan Değerler ve İş Mantığı (KDV & Toplam Hesaplama)
            invoice.InvoiceType = string.IsNullOrEmpty(invoice.InvoiceType) ? "SATIŞ" : invoice.InvoiceType;
            invoice.Scenario = string.IsNullOrEmpty(invoice.Scenario) ? "TİCARİ FATURA" : invoice.Scenario;
            invoice.InvoiceDate = DateTime.Now;

            // KDV (%20) ve Genel Toplam Otomatik Hesaplama
            invoice.TaxAmount = Math.Round(invoice.Amount * 0.20m, 2);
            invoice.TotalAmount = Math.Round(invoice.Amount + invoice.TaxAmount, 2);

            // 4. Veritabanına Kayıt
            _context.Invoices.Add(invoice);
            _context.SaveChanges();

            return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, invoice);
        }

        // PUT: api/Invoice/5
        // Var olan faturayı günceller (🛑 SADECE ADMİN ROLÜ GÜNCELLEYEBİLİR!)
        [HttpPut("{id}")]
       // [Authorize(Roles = "Admin")]
        public IActionResult Update(int id, [FromBody] Invoice updatedInvoice)
        {
            // 1. Veri Boş mu Kontrolü
            if (updatedInvoice == null)
            {
                return BadRequest(new { message = "Geçersiz veri." }); // HTTP 400
            }

            // 2. Güncelleme İşleminde de Validasyon Kontrolü
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState); // HTTP 400
            }

            // 3. Kayıt Var mı Kontrolü
            var invoice = _context.Invoices.Find(id);
            if (invoice == null)
            {
                return NotFound(new { message = $"{id} ID'li güncellenecek fatura bulunamadı." }); // HTTP 404
            }

            // 4. Güncellenen Tutar Üzerinden KDV ve Toplamı Yeniden Hesapla
            updatedInvoice.Id = id; // ID çakışmasını önle
            updatedInvoice.TaxAmount = Math.Round(updatedInvoice.Amount * 0.20m, 2);
            updatedInvoice.TotalAmount = Math.Round(updatedInvoice.Amount + updatedInvoice.TaxAmount, 2);
            updatedInvoice.InvoiceDate = invoice.InvoiceDate; // Orijinal tarihi koru

            // 5. Değerleri Aktar ve Kaydet
            _context.Entry(invoice).CurrentValues.SetValues(updatedInvoice);
            _context.SaveChanges();

            return Ok(new { message = "Fatura başarıyla güncellendi.", data = invoice }); // HTTP 200 OK
        }

        // DELETE: api/Invoice/5
        // Faturayı siler (🛑 SADECE ADMİN ROLÜ SİLEBİLİR!)
        [HttpDelete("{id}")]
       // [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            var invoice = _context.Invoices.Find(id);
            if (invoice == null)
            {
                return NotFound(new { message = $"{id} ID'li silinecek fatura bulunamadı." }); // HTTP 404
            }

            _context.Invoices.Remove(invoice);
            _context.SaveChanges();

            return Ok(new { message = "Fatura başarıyla silindi." }); // HTTP 200 OK
        }
    }
}