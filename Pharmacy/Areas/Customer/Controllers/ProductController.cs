using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pharmacy.Utils;

namespace Pharmacy.Areas.Customer.Controllers
{
    [Area(CD.CUSTOMER_AREA)]
    [Authorize]
    public class ProductController : Controller
    {
        private readonly IRepository<Product> _repository;
        public ProductController(IRepository<Product> repository)
        {
            _repository = repository;
        }
        // GET: Customer/Product
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var products = await _repository.GetAllAsync(
                includes: [p => p.Category],
                IsTracking: false
            );
            return View(products);
        }
        // GET: Customer/Product/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var product = await _repository.GetOneAsync(
                filter: p => p.Id == id,
                includes: [p => p.Category],
                IsTracking: false
                );
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }
    }
}
