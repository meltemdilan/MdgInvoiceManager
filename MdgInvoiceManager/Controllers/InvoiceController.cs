using Microsoft.AspNetCore.Mvc;
using MdgInvoiceManager.Models;
using MdgInvoiceManager.Data;
using System;
using System.Linq;

namespace MdgInvoiceManager.Controllers
{
    public class InvoiceController : Controller
    {
        private readonly MdgInvoiceDbContext _context;

        public InvoiceController(MdgInvoiceDbContext context)
        {
            _context = context;
        }

        
        public IActionResult Index()
        {
            var invoices = _context.Invoices.ToList();
            return View(invoices);
        }

        
        public IActionResult Create()
        {
            return View();
        }

        
        [HttpPost]
        public IActionResult Create(Invoice invoice)
        {
            invoice.InvoiceType = "SATIŞ";
            invoice.InvoiceDate = DateTime.Now;

            if (ModelState.IsValid)
            {
                _context.Invoices.Add(invoice);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(invoice);
        }

        
        public IActionResult Edit(int id)
        {
            var invoice = _context.Invoices.Find(id);
            if (invoice == null)
            {
                return NotFound();
            }
            return View(invoice);
        }

        
        [HttpPost]
        public IActionResult Edit(Invoice invoice)
        {
            if (ModelState.IsValid)
            {
                _context.Invoices.Update(invoice);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(invoice);
        }

       
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var invoice = _context.Invoices.Find(id);
            if (invoice != null)
            {
                _context.Invoices.Remove(invoice);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}

