using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Pharmacy.Models;
using Pharmacy.Repositories;
using Pharmacy.ViewModels;

namespace Pharmacy.Areas.Admin.Controllers
{
    public class ProductBatchController : Controller
    {
        private readonly IRepository<ProductBatch> _productbatchrepository;
        private readonly IRepository<Product> _productRepository;

        public ProductBatchController(
            IRepository<ProductBatch> productbatchRepository,
            IRepository<Product> productRepository)
        {
            _productbatchrepository = productbatchRepository;
            _productRepository = productRepository;
        }

        // GET: Admin/ProductBatch
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var batches = await _productbatchrepository.GetAllAsync(
                includes: [b => b.Product]
            );

            var result = batches.Select(b => new ProductBatchVM
            {
                Id = b.Id,
                ProductId = b.ProductId,
                BatchNumber = b.BatchNumber,
                ExpiryDate = b.ExpiryDate,
                CostPrice = b.CostPrice,
                QuantityOnHand = b.QuantityOnHand
            });

            return View(result);
        }

        // GET: Admin/ProductBatch/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new ProductBatchVM();

            await LoadProducts(model);

            return View(model);
        }

        // POST: Admin/ProductBatch/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductBatchVM model)
        {
            if (!ModelState.IsValid)
            {
                await LoadProducts(model);
                return View(model);
            }

            var product = await _productRepository.GetOneAsync(
                filter: p => p.Id == model.ProductId
            );

            if (product == null)
            {
                ModelState.AddModelError(
                    nameof(model.ProductId),
                    "Selected product does not exist."
                );

                await LoadProducts(model);
                return View(model);
            }

            var batch = new ProductBatch
            {
                ProductId = model.ProductId,
                BatchNumber = model.BatchNumber,
                ExpiryDate = model.ExpiryDate,
                CostPrice = model.CostPrice,
                QuantityOnHand = model.QuantityOnHand
            };

            await _productbatchrepository.CreateAsync(batch);
            await _productbatchrepository.CommitAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/ProductBatch/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var batch = await _productbatchrepository.GetOneAsync(
                filter: b => b.Id == id
            );

            if (batch == null)
            {
                return NotFound();
            }

            var model = new ProductBatchVM
            {
                Id = batch.Id,
                ProductId = batch.ProductId,
                BatchNumber = batch.BatchNumber,
                ExpiryDate = batch.ExpiryDate,
                CostPrice = batch.CostPrice,
                QuantityOnHand = batch.QuantityOnHand
            };

            await LoadProducts(model);

            return View(model);
        }

        // POST: Admin/ProductBatch/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProductBatchVM model)
        {
            if (!ModelState.IsValid)
            {
                await LoadProducts(model);
                return View(model);
            }

            var batch = await _productbatchrepository.GetOneAsync(
                filter: b => b.Id == model.Id
            );

            if (batch == null)
            {
                return NotFound();
            }

            var product = await _productRepository.GetOneAsync(
                filter: p => p.Id == model.ProductId
            );

            if (product == null)
            {
                ModelState.AddModelError(
                    nameof(model.ProductId),
                    "Selected product does not exist."
                );

                await LoadProducts(model);
                return View(model);
            }

            batch.ProductId = model.ProductId;
            batch.BatchNumber = model.BatchNumber;
            batch.ExpiryDate = model.ExpiryDate;
            batch.CostPrice = model.CostPrice;
            batch.QuantityOnHand = model.QuantityOnHand;

            _productbatchrepository.Update(batch);

            await _productbatchrepository.CommitAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/ProductBatch/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var batch = await _productbatchrepository.GetOneAsync(
                filter: b => b.Id == id,
                includes: [b => b.Product]
            );

            if (batch == null)
            {
                return NotFound();
            }

            var model = new ProductBatchVM
            {
                Id = batch.Id,
                ProductId = batch.ProductId,
                BatchNumber = batch.BatchNumber,
                ExpiryDate = batch.ExpiryDate,
                CostPrice = batch.CostPrice,
                QuantityOnHand = batch.QuantityOnHand
            };

            return View(model);
        }

        // POST: Admin/ProductBatch/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var batch = await _productbatchrepository.GetOneAsync(
                filter: b => b.Id == id
            );

            if (batch == null)
            {
                return NotFound();
            }

            _productbatchrepository.Delete(batch);

            await _productbatchrepository.CommitAsync();

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadProducts(ProductBatchVM model)
        {
            var products = await _productRepository.GetAllAsync();

            model.Products = products.Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = p.Name,
                Selected = p.Id == model.ProductId
            });
        }
    }
}