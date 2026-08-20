using Microsoft.AspNetCore.Mvc;
using Pharmacy.Models;
using Pharmacy.Repositories;
using Pharmacy.Utils;
using Pharmacy.ViewModels;

namespace Pharmacy.Areas.Customer.Controllers
{
    [Area(CD.CUSTOMER_AREA)]
    public class HomeController : Controller
    {
        private readonly Repository<Product> _repository;
        private readonly Repository<Category> _categoryRepository;

        public HomeController(Repository<Product> repository, Repository<Category> categoryRepository)
        {
            _repository = repository;
            _categoryRepository = categoryRepository;
        }
        public async Task<IActionResult> Index(ProductFilterVM filter, int page = 1)
        {
            
            ViewBag.Categories = await _categoryRepository.GetAllAsync();

            
            var query = await _repository.GetAllAsync(includes: [p => p.Category]);

            
            if (!string.IsNullOrWhiteSpace(filter.Name))
            {
                query = query.Where(p => p.Name.Contains(filter.Name, StringComparison.OrdinalIgnoreCase));
            }
            if (filter.Id.HasValue)
            {
                query = query.Where(p => p.Id == filter.Id);
            }
            if (!string.IsNullOrWhiteSpace(filter.Category))
            {
                query = query.Where(p => p.Category?.Name == filter.Category);
            }
            if (filter.Maxprice.HasValue)
            {
                query = query.Where(p => p.Price <= filter.Maxprice);
            }
            if (filter.Minprice.HasValue)
            {
                query = query.Where(p => p.Price >= filter.Minprice);
            }

            int totalItems = query.Count();
            int pageSize = 5;
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            page = Math.Max(1, Math.Min(page, totalPages > 0 ? totalPages : 1));

            
            filter.Products = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            filter.TotalPages = totalPages;
            filter.Page = page;

            return View(filter);
        }

        
        public async Task<IActionResult> Details(int id)
        {
            var product = await _repository.GetOneAsync(p => p.Id == id, includes: [p => p.Category]);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }
    }
}
