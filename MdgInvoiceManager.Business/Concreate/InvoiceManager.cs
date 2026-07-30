using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MdgInvoiceManager.Business.Abstract;
using MdgInvoiceManager.Core.Entities;
using MdgInvoiceManager.DataAccess.Repositories.Abstract;

namespace MdgInvoiceManager.Business.Concreate
{
    public class InvoiceManager : IInvoiceService
    {
        private readonly IInvoiceRepository _invoiceRepository;

        public InvoiceManager(IInvoiceRepository invoiceRepository)
        {
            _invoiceRepository = invoiceRepository;
        }

        public async Task<List<Invoice>> GetAllInvoicesAsync()
        {
            return await _invoiceRepository.GetAllAsync();
        }

        public async Task<Invoice?> GetInvoiceByIdAsync(int id)
        {
            return await _invoiceRepository.GetByIdAsync(id);
        }

        public async Task<Invoice> CreateInvoiceAsync(Invoice invoice)
        {
            invoice.InvoiceType = string.IsNullOrEmpty(invoice.InvoiceType) ? "SATIŞ" : invoice.InvoiceType;
            invoice.Scenario = string.IsNullOrEmpty(invoice.Scenario) ? "TİCARİ FATURA" : invoice.Scenario;
            invoice.InvoiceDate = DateTime.Now;

            invoice.TaxAmount = Math.Round(invoice.Amount * 0.20m, 2);
            invoice.TotalAmount = Math.Round(invoice.Amount + invoice.TaxAmount, 2);

            await _invoiceRepository.AddAsync(invoice);
            await _invoiceRepository.SaveChangesAsync();

            return invoice;
        }

        public async Task<bool> UpdateInvoiceAsync(int id, Invoice updatedInvoice)
        {
            var existingInvoice = await _invoiceRepository.GetByIdAsync(id);
            if (existingInvoice == null) return false;

            updatedInvoice.Id = id;
            updatedInvoice.TaxAmount = Math.Round(updatedInvoice.Amount * 0.20m, 2);
            updatedInvoice.TotalAmount = Math.Round(updatedInvoice.Amount + updatedInvoice.TaxAmount, 2);
            updatedInvoice.InvoiceDate = existingInvoice.InvoiceDate;

            _invoiceRepository.Update(updatedInvoice);
            await _invoiceRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteInvoiceAsync(int id)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(id);
            if (invoice == null) return false;

            _invoiceRepository.Delete(invoice);
            await _invoiceRepository.SaveChangesAsync();
            return true;
        }
    }
}