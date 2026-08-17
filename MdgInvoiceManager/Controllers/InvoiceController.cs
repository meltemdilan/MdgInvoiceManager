using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MdgInvoiceManager.Business.Abstract;
using MdgInvoiceManager.Core.Entities;

namespace MdgInvoiceManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvoiceController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;

        public InvoiceController(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        // GET: api/Invoice?pageNumber=1&pageSize=10
        // Admin -> Tüm faturaları alır | User -> Sadece kendi faturalarını alır
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var invoices = await _invoiceService.GetAllInvoicesAsync(pageNumber, pageSize);
            return Ok(invoices);
        }

        // GET: api/Invoice/5
        // Admin -> Her faturayı okur | User -> Sadece kendi faturasını okur
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id)
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
            if (invoice == null)
            {
                return NotFound(new { message = $"ID değeri {id} olan fatura bulunamadı veya bu faturayı görme yetkiniz yok." });
            }
            return Ok(invoice);
        }

        // POST: api/Invoice
        // Giriş yapmış her kullanıcı (User & Admin) kendi adına fatura oluşturabilir
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] Invoice invoice)
        {
            if (invoice == null)
            {
                return BadRequest(new { message = "Geçersiz veya boş fatura verisi." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var createdInvoice = await _invoiceService.CreateInvoiceAsync(invoice);

            return CreatedAtAction(nameof(GetById), new { id = createdInvoice.Id }, createdInvoice);
        }

        // PUT: api/Invoice/5
        // Sadece Admin rolüne sahip kullanıcılar fatura güncelleyebilir!
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] Invoice updatedInvoice)
        {
            if (updatedInvoice == null)
            {
                return BadRequest(new { message = "Geçersiz veri." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _invoiceService.UpdateInvoiceAsync(id, updatedInvoice);
            if (!result)
            {
                return NotFound(new { message = $"{id} ID'li fatura bulunamadı." });
            }

            return Ok(new { message = "Fatura başarıyla güncellendi." });
        }

        // DELETE: api/Invoice/5
        // Sadece Admin rolüne sahip kullanıcılar fatura silebilir!
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _invoiceService.DeleteInvoiceAsync(id);
            if (!result)
            {
                return NotFound(new { message = $"{id} ID'li fatura bulunamadı." });
            }

            return Ok(new { message = "Fatura başarıyla silindi." });
        }
    }
}