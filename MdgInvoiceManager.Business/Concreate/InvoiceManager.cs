using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MdgInvoiceManager.Business.Abstract;
using MdgInvoiceManager.Core.Entities;
using MdgInvoiceManager.DataAccess.Repositories.Abstract;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace MdgInvoiceManager.Business.Concrete
{
    public class InvoiceManager : IInvoiceService
    {
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public InvoiceManager(IInvoiceRepository invoiceRepository, IHttpContextAccessor httpContextAccessor)
        {
            _invoiceRepository = invoiceRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        // Token'dan o anki kullanıcının ID'sini okur
        private string GetCurrentUserId()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null) return string.Empty;

            return user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user.FindFirst("sub")?.Value
                ?? user.FindFirst("id")?.Value
                ?? user.FindFirst("userId")?.Value
                ?? string.Empty;
        }

        // Token'dan o anki kullanıcının Admin olup olmadığını kontrol eder
        private bool IsAdmin()
        {
            return _httpContextAccessor.HttpContext?.User?.IsInRole("Admin") ?? false;
        }

        public async Task<List<Invoice>> GetAllInvoicesAsync(int pageNumber = 1, int pageSize = 10)
        {
            // 1. Veritabanından veriyi henüz çekmiyoruz, IQueryable sorgusu oluşturuyoruz
            var query = _invoiceRepository.GetAllQueryable();

            // Admin değilse (normal kullanıcı ise) filtresini veritabanı sorgusuna ekliyoruz
            if (!IsAdmin())
            {
                string currentUserId = GetCurrentUserId();

                if (string.IsNullOrEmpty(currentUserId))
                {
                    return new List<Invoice>();
                }

                query = query.Where(x => x.UserId != null && x.UserId == currentUserId);
            }

            // 2. SQL Server tarafında SADECE ilgili 10 kaydı çeken sorguyu çalıştırıyoruz
            return query.OrderByDescending(x => x.Id).Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync().Result;
        }

        public async Task<Invoice?> GetInvoiceByIdAsync(int id)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(id);
            if (invoice == null) return null;

            // Admin her faturayı görebilir
            if (IsAdmin()) return invoice;

            // Normal kullanıcı başkasının faturasını ID yazarak çekmeye çalışırsa engelle
            string currentUserId = GetCurrentUserId();
            if (invoice.UserId?.ToString() != currentUserId)
            {
                return null;
            }

            return invoice;
        }

        public async Task<Invoice> CreateInvoiceAsync(Invoice invoice)
        {
            // Fatura oluşturulurken UserId verilmemişse otomatik token'daki ID'yi atar
            if (string.IsNullOrEmpty(invoice.UserId))
            {
                invoice.UserId = GetCurrentUserId();
            }

            invoice.InvoiceType = string.IsNullOrEmpty(invoice.InvoiceType) ? "SATIŞ" : invoice.InvoiceType;
            invoice.Scenario = string.IsNullOrEmpty(invoice.Scenario) ? "TİCARİ FATURA" : invoice.Scenario;
            invoice.InvoiceDate = DateTime.Now;

            // KDV ve Toplam Tutar hesaplaması
            invoice.TaxAmount = Math.Round(invoice.Amount * 0.20m, 2);
            invoice.TotalAmount = Math.Round(invoice.Amount + invoice.TaxAmount, 2);

            await _invoiceRepository.AddAsync(invoice);
            await _invoiceRepository.SaveChangesAsync();

            return invoice;
        }

        public async Task<bool> UpdateInvoiceAsync(int id, Invoice updatedInvoice)
        {
            // İŞ KURALI: Fatura güncelleme yetkisi SADECE Admin rolüne aittir!
            if (!IsAdmin())
            {
                return false;
            }

            // Veritabanındaki takip edilen (tracked) mevcut faturayı çekiyoruz
            var existingInvoice = await _invoiceRepository.GetByIdAsync(id);
            if (existingInvoice == null) return false;

            // EF Core Tracking çakışmasını önlemek için mevcut nesnenin alanlarını güncelliyoruz
            existingInvoice.CustomerName = updatedInvoice.CustomerName;
            existingInvoice.Amount = updatedInvoice.Amount;
            existingInvoice.City = updatedInvoice.City;
            existingInvoice.InvoiceType = string.IsNullOrEmpty(updatedInvoice.InvoiceType) ? existingInvoice.InvoiceType : updatedInvoice.InvoiceType;
            existingInvoice.FilePath = updatedInvoice.FilePath;
            existingInvoice.Scenario = string.IsNullOrEmpty(updatedInvoice.Scenario) ? existingInvoice.Scenario : updatedInvoice.Scenario;

            // Yeniden hesaplamaları yapıyoruz
            existingInvoice.TaxAmount = Math.Round(updatedInvoice.Amount * 0.20m, 2);
            existingInvoice.TotalAmount = Math.Round(updatedInvoice.Amount + existingInvoice.TaxAmount, 2);

            _invoiceRepository.Update(existingInvoice);
            await _invoiceRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteInvoiceAsync(int id)
        {
            // İŞ KURALI: Fatura silme yetkisi SADECE Admin rolüne aittir!
            if (!IsAdmin())
            {
                return false;
            }

            var invoice = await _invoiceRepository.GetByIdAsync(id);
            if (invoice == null) return false;

            _invoiceRepository.Delete(invoice);
            await _invoiceRepository.SaveChangesAsync();
            return true;
        }
    }
}