using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Pharmacy.Models;
using Pharmacy.Repositories;
using Pharmacy.ViewModels;

namespace Pharmacy.Areas.Admin.Controllers
{
    public class SalesInvoiceController : Controller
    {
        private readonly IRepository<SalesInvoice> _salesInvoicerepository;
        private readonly IRepository<SalesInvoiceItem> _itemRepository;
        private readonly IRepository<Pharmacy.Models.Customer> _customerRepository;
        private readonly IRepository<Order> _orderRepository;
        private readonly IRepository<ProductBatch> _batchRepository;

        public SalesInvoiceController(
            IRepository<SalesInvoice> repository,
            IRepository<SalesInvoiceItem> itemRepository,
            IRepository<Pharmacy.Models.Customer> customerRepository,
            IRepository<Order> orderRepository,
            IRepository<ProductBatch> batchRepository)
        {
            _salesInvoicerepository = repository;
            _itemRepository = itemRepository;
            _customerRepository = customerRepository;
            _orderRepository = orderRepository;
            _batchRepository = batchRepository;
        }

        // GET: Admin/SalesInvoice
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var invoices = await _salesInvoicerepository.GetAllAsync(
                includes:
                [
                    i => i.Customer,
                    i => i.Order
                ]
            );

            var result = invoices.Select(i => new SalesInvoiceVM
            {
                Id = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                InvoiceDate = i.InvoiceDate,
                TotalAmount = i.TotalAmount,
                Discount = i.Discount,
                NetAmount = i.NetAmount,
                CustomerId = i.CustomerId,
                OrderId = i.OrderId
            });

            return View(result);
        }

        // GET: Admin/SalesInvoice/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var invoice = await _salesInvoicerepository.GetOneAsync(
                filter: i => i.Id == id,
                includes:
                [
                    i => i.Customer,
                    i => i.Order,
                    i => i.InvoiceItems
                ]
            );

            if (invoice == null)
            {
                return NotFound();
            }

            var model = new SalesInvoiceVM
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                InvoiceDate = invoice.InvoiceDate,
                TotalAmount = invoice.TotalAmount,
                Discount = invoice.Discount,
                NetAmount = invoice.NetAmount,
                CustomerId = invoice.CustomerId,
                OrderId = invoice.OrderId,

                InvoiceItems = invoice.InvoiceItems.Select(item =>
                    new SalesInvoiceItemVM
                    {
                        Id = item.Id,
                        ProductBatchId = item.ProductBatchId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.TotalPrice
                    }).ToList()
            };

            return View(model);
        }

        // GET: Admin/SalesInvoice/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new SalesInvoiceVM();

            await LoadInvoiceData(model);

            return View(model);
        }

        // POST: Admin/SalesInvoice/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SalesInvoiceVM model)
        {
            if (!ModelState.IsValid)
            {
                await LoadInvoiceData(model);
                return View(model);
            }

            if (model.CustomerId == null && model.OrderId == null)
            {
                ModelState.AddModelError(
                    "",
                    "Invoice must belong to a customer or an online order."
                );

                await LoadInvoiceData(model);
                return View(model);
            }

            if (model.CustomerId != null && model.OrderId != null)
            {
                ModelState.AddModelError(
                    "",
                    "Invoice cannot belong to both a customer and an order."
                );

                await LoadInvoiceData(model);
                return View(model);
            }

            if (model.InvoiceItems == null || !model.InvoiceItems.Any())
            {
                ModelState.AddModelError(
                    "",
                    "Invoice must contain at least one item."
                );

                await LoadInvoiceData(model);
                return View(model);
            }

            var invoice = new SalesInvoice
            {
                InvoiceNumber = model.InvoiceNumber,
                InvoiceDate = model.InvoiceDate,
                CustomerId = model.CustomerId,
                OrderId = model.OrderId,
                Discount = model.Discount
            };

            decimal total = 0;

            foreach (var itemVM in model.InvoiceItems)
            {
                if (itemVM.Quantity <= 0)
                {
                    ModelState.AddModelError(
                        "",
                        "Quantity must be greater than zero."
                    );

                    await LoadInvoiceData(model);
                    return View(model);
                }

                var batch = await _batchRepository.GetOneAsync(
                    filter: b => b.Id == itemVM.ProductBatchId,
                    includes: [b => b.Product]
                );

                if (batch == null)
                {
                    ModelState.AddModelError(
                        "",
                        $"Batch #{itemVM.ProductBatchId} was not found."
                    );

                    await LoadInvoiceData(model);
                    return View(model);
                }

                if (itemVM.Quantity > batch.QuantityOnHand)
                {
                    ModelState.AddModelError(
                        "",
                        $"Not enough stock for batch {batch.BatchNumber}."
                    );

                    await LoadInvoiceData(model);
                    return View(model);
                }

                var invoiceItem = new SalesInvoiceItem
                {
                    ProductBatchId = itemVM.ProductBatchId,
                    Quantity = itemVM.Quantity,
                    UnitPrice = itemVM.UnitPrice,
                    TotalPrice = itemVM.Quantity * itemVM.UnitPrice
                };

                total += invoiceItem.TotalPrice;

                batch.QuantityOnHand -= itemVM.Quantity;

                _batchRepository.Update(batch);

                invoice.InvoiceItems.Add(invoiceItem);
            }

            invoice.TotalAmount = total;
            invoice.NetAmount = total - invoice.Discount;

            await _salesInvoicerepository.CreateAsync(invoice);
            await _salesInvoicerepository.CommitAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/SalesInvoice/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var invoice = await _salesInvoicerepository.GetOneAsync(
                filter: i => i.Id == id,
                includes:
                [
                    i => i.Customer,
                    i => i.Order,
                    i => i.InvoiceItems
                ]
            );

            if (invoice == null)
            {
                return NotFound();
            }

            var model = new SalesInvoiceVM
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                InvoiceDate = invoice.InvoiceDate,
                TotalAmount = invoice.TotalAmount,
                Discount = invoice.Discount,
                NetAmount = invoice.NetAmount,
                CustomerId = invoice.CustomerId,
                OrderId = invoice.OrderId,

                InvoiceItems = invoice.InvoiceItems.Select(item =>
                    new SalesInvoiceItemVM
                    {
                        Id = item.Id,
                        ProductBatchId = item.ProductBatchId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.TotalPrice
                    }).ToList()
            };

            return View(model);
        }

        // POST: Admin/SalesInvoice/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var invoice = await _salesInvoicerepository.GetOneAsync(
                filter: i => i.Id == id,
                includes: [i => i.InvoiceItems]
            );

            if (invoice == null)
            {
                return NotFound();
            }

            // Return stock
            foreach (var item in invoice.InvoiceItems)
            {
                var batch = await _batchRepository.GetOneAsync(
                    filter: b => b.Id == item.ProductBatchId
                );

                if (batch != null)
                {
                    batch.QuantityOnHand += item.Quantity;
                    _batchRepository.Update(batch);
                }

                _itemRepository.Delete(item);
            }

            _salesInvoicerepository.Delete(invoice);

            await _salesInvoicerepository.CommitAsync();

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadInvoiceData(SalesInvoiceVM model)
        {
            var customers = await _customerRepository.GetAllAsync();

            model.Customers = customers.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = $"{c.Name} - {c.Phone}",
                Selected = c.Id == model.CustomerId
            });

            var orders = await _orderRepository.GetAllAsync();

            model.Orders = orders.Select(o => new SelectListItem
            {
                Value = o.Id.ToString(),
                Text = o.OrderNumber,
                Selected = o.Id == model.OrderId
            });
        }
    }
}