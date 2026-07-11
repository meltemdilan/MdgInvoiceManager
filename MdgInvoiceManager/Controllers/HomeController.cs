using Microsoft.AspNetCore.Mvc;
using MdgInvoiceManager.Models;
using System.Diagnostics;

namespace MdgInvoiceManager.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        
        public IActionResult Create()
        {
            return RedirectToAction("Create", "Invoice");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
