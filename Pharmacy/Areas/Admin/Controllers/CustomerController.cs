using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pharmacy.Utils;


namespace Pharmacy.Areas.Admin.Controllers
{
    [Authorize]
    [Area(CD.ADMIN_AREA)]
    public class CustomerController : Controller
    {
        private readonly IRepository<Pharmacy.Models.Customer> _customerrepository;

        public CustomerController(IRepository<Pharmacy.Models.Customer> repository)
        {
            _customerrepository = repository;
        }

        // GET: Admin/Customer
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var customers = await _customerrepository.GetAllAsync();

            var result = customers.Select(c => new CustomerVM
            {
                Id = c.Id,
                Name = c.Name,
                Phone = c.Phone,
                CurrentBalance = c.CurrentBalance
            });

            return View(result);
        }

        // GET: Admin/Customer/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View(new CustomerVM());
        }

        // POST: Admin/Customer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var customer = new Pharmacy.Models.Customer
            {
                Name = model.Name,
                Phone = model.Phone,
                CurrentBalance = model.CurrentBalance
            };

            await _customerrepository.CreateAsync(customer);
            await _customerrepository.CommitAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Customer/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var customer = await _customerrepository.GetOneAsync(
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

        // POST: Admin/Customer/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CustomerVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var customer = await _customerrepository.GetOneAsync(
                filter: c => c.Id == model.Id
            );

            if (customer == null)
            {
                return NotFound();
            }

            customer.Name = model.Name;
            customer.Phone = model.Phone;
            customer.CurrentBalance = model.CurrentBalance;

            _customerrepository.Update(customer);

            await _customerrepository.CommitAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Customer/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var customer = await _customerrepository.GetOneAsync(
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

        // POST: Admin/Customer/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var customer = await _customerrepository.GetOneAsync(
                filter: c => c.Id == id
            );

            if (customer == null)
            {
                return NotFound();
            }

            _customerrepository.Delete(customer);

            await _customerrepository.CommitAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}