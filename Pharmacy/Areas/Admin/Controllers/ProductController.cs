using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Pharmacy.Models;
using Pharmacy.Repositories;
using Pharmacy.ViewModels;

namespace Pharmacy.Areas.Admin.Controllers
{
    public class ProductController : Controller
    {
        private readonly Repository<Product> _repository;
        private readonly Repository<Category> _categoryRepository;

        public ProductController(Repository<Product> repository, Repository<Category> categoryRepository)
        {
            _repository = repository;
            _categoryRepository = categoryRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Index(ProductFilterVM productFilterVM, int page=1)
        {
            var products = await _repository.GetAllAsync(includes:[p=>p.Category]);
            //filter
            if (productFilterVM.Name!=null)
            {
                products = products.Where(p => p.Name == productFilterVM.Name);
            }
            if (productFilterVM.Id!=null)
            {
                products = products.Where(p => p.Id == productFilterVM.Id);
            }
            if (productFilterVM.Category!=null)
            {
                products = products.Where(p => p.Category?.Name == productFilterVM.Category);
            }
            if (productFilterVM.Maxprice!=null)
            {
                products = products.Where(p => p.Price <= productFilterVM.Maxprice);
            }
            if (productFilterVM.Minprice!=null)
            {
                products = products.Where(p => p.Price >= productFilterVM.Minprice);
            }
            int totalPages = (int)Math.Ceiling(products.Count() / 5.0);
            page = Math.Max(1, Math.Min(page, totalPages > 0 ? totalPages : 1));
            products = products.Skip((page - 1) * 5).Take(5);

            return View(new ProductFilterVM()
            {
                products = products.AsEnumerable(),
                TotalPages = totalPages,
                Page = page
            });

        }
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            
            var categories = await _categoryRepository.GetAllAsync();

            var viewModel = new ProductVM
            {
                Categories = categories.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
            };

            return View(viewModel);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductVM productVM, IFormFile ImageFile)
        {
            if (!ModelState.IsValid)
            {
                var categories = await _categoryRepository.GetAllAsync();
                productVM.Categories = categories.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                });

                
                return View(productVM);
            }



            var product = new Product
            {
                Name = productVM.Name,
                GenericName = productVM.GenericName,
                Price = productVM.Price,

                MinStockLevel = productVM.MinStockLevel,
                RequiresPrescription = productVM.RequiresPrescription,
                CategoryId = productVM.CategoryId
            };
            if (ImageFile is not null && ImageFile.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + "-" + ImageFile.FileName;
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\images\\", fileName);
                using (var stream = System.IO.File.Create(filePath))
                {
                    ImageFile.CopyTo(stream);
                }
                product.ProductImg = fileName;


            }
                await _repository.CreateAsync(product);
            await _repository.CommitAsync();

                return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _repository.GetOneAsync(filter: p => p.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            var categories = await _categoryRepository.GetAllAsync();

            var productVM = new ProductVM
            {
                Id = product.Id,
                Name = product.Name,
                GenericName = product.GenericName,
                Price = product.Price,
                MinStockLevel = product.MinStockLevel,
                RequiresPrescription = product.RequiresPrescription,
                CategoryId = product.CategoryId,
                ProductImg = product.ProductImg, 
                Categories = categories.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name,
                    Selected = c.Id == product.CategoryId
                })
            };

            return View(productVM);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProductVM productVM, IFormFile? ImageFile)
        {
            if (!ModelState.IsValid)
            {
                var categories = await _categoryRepository.GetAllAsync();
                productVM.Categories = categories.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                });

                return View(productVM);
            }

           
             var product = await _repository.GetOneAsync(filter: p => p.Id == productVM.Id);
            if (product == null)
            {
                return NotFound();
            }

           
            product.Name = productVM.Name;
            product.GenericName = productVM.GenericName;
            product.Price = productVM.Price;
            product.MinStockLevel = productVM.MinStockLevel;
            product.RequiresPrescription = productVM.RequiresPrescription;
            product.CategoryId = productVM.CategoryId;

           
            if (ImageFile is not null && ImageFile.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + "-" + ImageFile.FileName;
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\images\\", fileName);

                using (var stream = System.IO.File.Create(filePath))
                {
                    ImageFile.CopyTo(stream);
                }

               
                product.ProductImg = fileName;
            }
            
             _repository.Update(product); 

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Product/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            // Fetch product along with its Category to show full details on the confirmation page
            var product = await _repository.GetOneAsync(
                filter: p => p.Id == id,
                includes: [p => p.Category]
            );

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        
        [HttpPost, ActionName("Delete")]
        
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _repository.GetOneAsync(filter: p => p.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            
            if (!string.IsNullOrEmpty(product.ProductImg))
            {
                var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\images\\", product.ProductImg);
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            
            _repository.Delete(product);
            await _repository.CommitAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
