using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using MassTransit;
using MdgInvoiceManager.Business.Abstract;
using MdgInvoiceManager.Core; // Event modelimiz burada
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
        private readonly IDistributedCache _cache;
        private readonly IPublishEndpoint _publishEndpoint;

        public InvoiceManager(
            IInvoiceRepository invoiceRepository,
            IHttpContextAccessor httpContextAccessor,
            IDistributedCache cache,
            IPublishEndpoint publishEndpoint)
        {
            _invoiceRepository = invoiceRepository;
            _httpContextAccessor = httpContextAccessor;
            _cache = cache;
            _publishEndpoint = publishEndpoint;
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

            string roleOrUserKey = isAdmin ? "admin" : $"user:{currentUserId}";
            string cacheKey = $"invoices:{roleOrUserKey}:p:{pageNumber}:s:{pageSize}";

            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonSerializer.Deserialize<List<Invoice>>(cachedData)!;
            }

            var invoices = await _invoiceRepository.GetPagedInvoicesAsync(currentUserId, isAdmin, pageNumber, pageSize);

            if (invoices != null && invoices.Count > 0)
            {
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
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

            Invoice? invoice = null;
            var cachedData = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedData))
            {
                invoice = JsonSerializer.Deserialize<Invoice>(cachedData);
            }
            else
            {
                invoice = await _invoiceRepository.GetByIdAsync(id);

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

            if (IsAdmin()) return invoice;

            string currentUserId = GetCurrentUserId();
            if (invoice.UserId != currentUserId)
            {
                return null;
            }

            return invoice;
        }

        // ==========================================
        // 3. CREATE INVOICE (KUYRUK ENTEGRASYONU)
        // ==========================================
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

            // 1. Fatura veritabanına kaydedilir
            await _invoiceRepository.AddAsync(invoice);

            // 2. RabbitMQ kuyruğuna mesaj fırlatılır
            await _publishEndpoint.Publish(new InvoiceCreatedEvent
            {
                InvoiceId = invoice.Id,
                CustomerName = invoice.CustomerName,
                TotalAmount = invoice.TotalAmount
            });

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

            await _cache.RemoveAsync($"invoice:{id}");

            return true;
        }
    }
}