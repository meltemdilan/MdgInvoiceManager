using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using MdgInvoiceManager.Business.Abstract;
using MdgInvoiceManager.Core.Entities;
using MdgInvoiceManager.DataAccess.Repositories.Abstract;
using Microsoft.AspNetCore.Http;

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

        public async Task<List<Invoice>> GetAllInvoicesAsync(int pageNumber = 1, int pageSize = 10)
        {
            string currentUserId = GetCurrentUserId();
            bool isAdmin = IsAdmin();

            if (!isAdmin && string.IsNullOrEmpty(currentUserId))
            {
                return new List<Invoice>();
            }

            return await _invoiceRepository.GetPagedInvoicesAsync(currentUserId, isAdmin, pageNumber, pageSize);
        }

        public async Task<Invoice?> GetInvoiceByIdAsync(int id)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(id);
            if (invoice == null) return null;

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
            return true;
        }
    }
}