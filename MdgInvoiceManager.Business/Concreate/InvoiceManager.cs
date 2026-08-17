using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using MdgInvoiceManager.Business.Abstract;
using MdgInvoiceManager.Core.Entities;
using MdgInvoiceManager.DataAccess.Repositories.Abstract;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;

namespace MdgInvoiceManager.Business.Concrete
{
    public class InvoiceManager : IInvoiceService
    {
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IDistributedCache _cache; // Redis önbellek servisi

        public InvoiceManager(
      IInvoiceRepository invoiceRepository,
      IHttpContextAccessor httpContextAccessor,
      IDistributedCache cache)
        {
            _invoiceRepository = invoiceRepository;
            _httpContextAccessor = httpContextAccessor;
            _cache = cache;
        }

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

        private bool IsAdmin()
        {
            return _httpContextAccessor.HttpContext?.User?.IsInRole("Admin") ?? false;
        }

        // ==========================================
        // 1. GET ALL (REDIS İLE ÖNBELLEKLENMİŞ LİSTE)
        // ==========================================
        public async Task<List<Invoice>> GetAllInvoicesAsync(int pageNumber = 1, int pageSize = 10)
        {
            string currentUserId = GetCurrentUserId();
            bool isAdmin = IsAdmin();

            if (!isAdmin && string.IsNullOrEmpty(currentUserId))
            {
                return new List<Invoice>();
            }

            // Kullanıcıya veya role ve sayfa numarasına özel benzersiz Redis anahtarı
            // Örnek: "invoices:user:abc-123:p:1:s:10" veya "invoices:admin:p:1:s:10"
            string roleOrUserKey = isAdmin ? "admin" : $"user:{currentUserId}";
            string cacheKey = $"invoices:{roleOrUserKey}:p:{pageNumber}:s:{pageSize}";

            // 1. Önce Redis Cache'e bak
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                // RAM'de varsa doğrudan JSON'dan deserialize edip dön (SQL'e hiç gitmez)
                return JsonSerializer.Deserialize<List<Invoice>>(cachedData)!;
            }

            // 2. Redis'te yoksa Veritabanından (SQL) getir
            var invoices = await _invoiceRepository.GetPagedInvoicesAsync(currentUserId, isAdmin, pageNumber, pageSize);

            // 3. Veritabanından gelen listeyi Redis'e 5 dakikalığına kaydet
            if (invoices != null && invoices.Count > 0)
            {
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) // Liste için 5 dakika idealdir
                };

                var serializedData = JsonSerializer.Serialize(invoices);
                await _cache.SetStringAsync(cacheKey, serializedData, cacheOptions);
            }

            return invoices ?? new List<Invoice>();
        }

        // ==========================================
        // 2. GET BY ID (REDIS İLE TEKİL FATURA)
        // ==========================================
        public async Task<Invoice?> GetInvoiceByIdAsync(int id)
        {
            string cacheKey = $"invoice:{id}";

            // 1. Önce Redis Cache'e bakıyoruz
            Invoice? invoice = null;
            var cachedData = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedData))
            {
                // Redis'te varsa doğrudan JSON'dan nesneye çevir
                invoice = JsonSerializer.Deserialize<Invoice>(cachedData);
            }
            else
            {
                // 2. Redis'te yoksa Veritabanından (SQL) getir
                invoice = await _invoiceRepository.GetByIdAsync(id);

                // 3. Veritabanında bulunduysa Redis'e 10 dakikalığına kaydet
                if (invoice != null)
                {
                    var cacheOptions = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                    };

                    var serializedData = JsonSerializer.Serialize(invoice);
                    await _cache.SetStringAsync(cacheKey, serializedData, cacheOptions);
                }
            }

            if (invoice == null) return null;

            // Güvenlik ve Yetki Kontrolü
            if (IsAdmin()) return invoice;

            string currentUserId = GetCurrentUserId();
            if (invoice.UserId != currentUserId)
            {
                return null;
            }

            return invoice;
        }

        public async Task<Invoice> CreateInvoiceAsync(Invoice invoice)
        {
            if (string.IsNullOrEmpty(invoice.UserId))
            {
                invoice.UserId = GetCurrentUserId();
            }

            invoice.InvoiceType = string.IsNullOrEmpty(invoice.InvoiceType) ? "SATIŞ" : invoice.InvoiceType;
            invoice.Scenario = string.IsNullOrEmpty(invoice.Scenario) ? "TİCARİ FATURA" : invoice.Scenario;
            invoice.InvoiceDate = DateTime.Now;

            invoice.TaxAmount = Math.Round(invoice.Amount * 0.20m, 2);
            invoice.TotalAmount = Math.Round(invoice.Amount + invoice.TaxAmount, 2);

            await _invoiceRepository.AddAsync(invoice);

            return invoice;
        }

        public async Task<bool> UpdateInvoiceAsync(int id, Invoice updatedInvoice)
        {
            if (!IsAdmin())
            {
                return false;
            }

            var existingInvoice = await _invoiceRepository.GetByIdAsync(id);
            if (existingInvoice == null) return false;

            existingInvoice.CustomerName = updatedInvoice.CustomerName;
            existingInvoice.Amount = updatedInvoice.Amount;
            existingInvoice.City = updatedInvoice.City;
            existingInvoice.InvoiceType = string.IsNullOrEmpty(updatedInvoice.InvoiceType) ? existingInvoice.InvoiceType : updatedInvoice.InvoiceType;
            existingInvoice.FilePath = updatedInvoice.FilePath;
            existingInvoice.Scenario = string.IsNullOrEmpty(updatedInvoice.Scenario) ? existingInvoice.Scenario : updatedInvoice.Scenario;

            existingInvoice.TaxAmount = Math.Round(updatedInvoice.Amount * 0.20m, 2);
            existingInvoice.TotalAmount = Math.Round(updatedInvoice.Amount + existingInvoice.TaxAmount, 2);

            await _invoiceRepository.UpdateAsync(existingInvoice);

            
            await _cache.RemoveAsync($"invoice:{id}");

            return true;
        }

        public async Task<bool> DeleteInvoiceAsync(int id)
        {
            if (!IsAdmin())
            {
                return false;
            }

            var invoice = await _invoiceRepository.GetByIdAsync(id);
            if (invoice == null) return false;

            await _invoiceRepository.DeleteAsync(invoice);

            // Önemli: Silinen faturanın önbellek kaydını Redis'ten siliyoruz
            await _cache.RemoveAsync($"invoice:{id}");

            return true;
        }
    }
}
