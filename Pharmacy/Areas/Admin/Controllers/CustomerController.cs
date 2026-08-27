using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pharmacy.Models;
using Pharmacy.Repositories;
using Pharmacy.Utils;
using Pharmacy.ViewModels;

namespace Pharmacy.Areas.Admin.Controllers
{
    [Authorize]
    [Area(CD.ADMIN_AREA)]
    public class CustomerController : Controller
    {
        private readonly IRepository<Models.Customer> _customerRepository;
        private readonly IRepository<SalesInvoice> _salesInvoiceRepository;

        public CustomerController(
            IRepository<Models.Customer> customerRepository,
            IRepository<SalesInvoice> salesInvoiceRepository)
        {
            _customerRepository = customerRepository;
            _salesInvoiceRepository = salesInvoiceRepository;
        }

        // =========================================================
        // INDEX
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var customers = await _customerRepository.GetAllAsync();

            var result = customers.Select(c => new CustomerVM
            {
                Id = c.Id,
                Name = c.Name,
                Phone = c.Phone,
                CurrentBalance = c.CurrentBalance
            }).ToList();

            return View(result);
        }

        // =========================================================
        // CREATE - GET
        // =========================================================

        [HttpGet]
        public IActionResult Create()
        {
            return View(new CustomerVM());
        }

        // =========================================================
        // CREATE - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var customer = new Models.Customer
            {
                Name = model.Name,
                Phone = model.Phone,
                CurrentBalance = model.CurrentBalance
            };

            await _customerRepository.CreateAsync(customer);

            await _customerRepository.CommitAsync();

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // EDIT - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var customer = await _customerRepository.GetOneAsync(
                filter: c => c.Id == id
            );

            if (customer == null)
            {
                return NotFound();
            }

            var model = new CustomerVM
            {
                Id = customer.Id,
                Name = customer.Name,
                Phone = customer.Phone,
                CurrentBalance = customer.CurrentBalance
            };

            return View(model);
        }

        // =========================================================
        // EDIT - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CustomerVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var customer = await _customerRepository.GetOneAsync(
                filter: c => c.Id == model.Id
            );

            if (customer == null)
            {
                return NotFound();
            }

            customer.Name = model.Name;
            customer.Phone = model.Phone;

            _customerRepository.Update(customer);

            await _customerRepository.CommitAsync();

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // DELETE - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var customer = await _customerRepository.GetOneAsync(
                filter: c => c.Id == id
            );

            if (customer == null)
            {
                return NotFound();
            }

            var model = new CustomerVM
            {
                Id = customer.Id,
                Name = customer.Name,
                Phone = customer.Phone,
                CurrentBalance = customer.CurrentBalance
            };

            return View(model);
        }

        // =========================================================
        // DELETE - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var customer = await _customerRepository.GetOneAsync(
                filter: c => c.Id == id
            );

            if (customer == null)
            {
                return NotFound();
            }

            // =====================================================
            // Check if customer has sales invoices
            // =====================================================

            var invoices = await _salesInvoiceRepository.GetAllAsync(
                filter: i => i.CustomerId == id
            );

            if (invoices.Any())
            {
                TempData["Error"] =
                    "This customer cannot be deleted because they have sales invoices.";

                return RedirectToAction(nameof(Index));
            }

            // =====================================================
            // Delete Customer
            // =====================================================

            _customerRepository.Delete(customer);

            await _customerRepository.CommitAsync();

            TempData["Success"] =
                "Customer deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}