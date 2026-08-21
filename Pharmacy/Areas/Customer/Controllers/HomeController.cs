using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pharmacy.Models;
using Pharmacy.Repositories;
using Pharmacy.Utils;

namespace Pharmacy.Areas.Customer.Controllers
{
    [Area(CD.CUSTOMER_AREA)]
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IRepository<Product> _repository;

        public HomeController(IRepository<Product> repository)
        {
            _repository = repository;
        }

        // GET: Customer/Home
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var products = await _repository.GetAllAsync(
                includes: [p => p.Category],
                IsTracking: false
            );

            var featuredProducts = products
                .Take(8)
                .ToList();

            return View(featuredProducts);
        }
    }
}