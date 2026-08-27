using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Pharmacy.Models;
using Pharmacy.Repositories;
using Pharmacy.Utils;
using Pharmacy.ViewModels;

namespace Pharmacy.Areas.Admin.Controllers
{
    [Authorize]
    [Area(CD.ADMIN_AREA)]
    public class SalesInvoiceController : Controller
    {
        private readonly IRepository<SalesInvoice> _salesInvoiceRepository;
        private readonly IRepository<SalesInvoiceItem> _itemRepository;
        private readonly IRepository<Models.Customer> _customerRepository;
        private readonly IRepository<Order> _orderRepository;
        private readonly IRepository<ProductBatch> _batchRepository;

        public SalesInvoiceController(
            IRepository<SalesInvoice> salesInvoiceRepository,
            IRepository<SalesInvoiceItem> itemRepository,
            IRepository<Models.Customer> customerRepository,
            IRepository<Order> orderRepository,
            IRepository<ProductBatch> batchRepository)
        {
            _salesInvoiceRepository = salesInvoiceRepository;
            _itemRepository = itemRepository;
            _customerRepository = customerRepository;
            _orderRepository = orderRepository;
            _batchRepository = batchRepository;
        }

        // =========================================================
        // INDEX
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var invoices = await _salesInvoiceRepository.GetAllAsync(
                includes:
                [
                    i => i.Customer,
                    i => i.Order
                ]
            );

            var result = invoices
                .OrderByDescending(i => i.InvoiceDate)
                .Select(i => new SalesInvoiceVM
                {
                    Id = i.Id,
                    InvoiceNumber = i.InvoiceNumber,
                    InvoiceDate = i.InvoiceDate,
                    TotalAmount = i.TotalAmount,
                    Discount = i.Discount,
                    NetAmount = i.NetAmount,
                    CustomerId = i.CustomerId,
                    OrderId = i.OrderId
                })
                .ToList();

            return View(result);
        }

        // =========================================================
        // DETAILS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var invoice = await _salesInvoiceRepository.GetOneAsync(
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

                InvoiceItems = invoice.InvoiceItems
                    .Select(item => new SalesInvoiceItemVM
                    {
                        Id = item.Id,
                        ProductBatchId = item.ProductBatchId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.TotalPrice
                    })
                    .ToList()
            };

            return View(model);
        }

        // =========================================================
        // CREATE GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new SalesInvoiceVM
            {
                InvoiceDate = DateTime.Now,

                InvoiceItems = new List<SalesInvoiceItemVM>
                {
                    new SalesInvoiceItemVM
                    {
                        Quantity = 1
                    }
                }
            };

            await LoadInvoiceData(model);

            return View(model);
        }

        // =========================================================
        // CREATE POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SalesInvoiceVM model)
        {
            // مهم جدًا:
            // TotalAmount و NetAmount مش المفروض ييجوا من الـ View
            // لذلك نشيل validation errors الخاصة بيهم لو موجودة.

            ModelState.Remove(nameof(model.TotalAmount));
            ModelState.Remove(nameof(model.NetAmount));

            if (model.CustomerId == null && model.OrderId == null)
            {
                ModelState.AddModelError(
                    "",
                    "Invoice must belong to a customer or an online order."
                );
            }

            if (model.CustomerId != null && model.OrderId != null)
            {
                ModelState.AddModelError(
                    "",
                    "Invoice cannot belong to both a customer and an order."
                );
            }

            if (model.InvoiceItems == null || !model.InvoiceItems.Any())
            {
                ModelState.AddModelError(
                    "",
                    "Invoice must contain at least one item."
                );
            }

            if (!ModelState.IsValid)
            {
                await LoadInvoiceData(model);
                return View(model);
            }

            // =====================================================
            // Customer
            // =====================================================

            Models.Customer? customer = null;

            if (model.CustomerId.HasValue)
            {
                customer = await _customerRepository.GetOneAsync(
                    filter: c => c.Id == model.CustomerId.Value
                );

                if (customer == null)
                {
                    ModelState.AddModelError(
                        nameof(model.CustomerId),
                        "Selected customer does not exist."
                    );

                    await LoadInvoiceData(model);
                    return View(model);
                }
            }

            // =====================================================
            // Order
            // =====================================================

            if (model.OrderId.HasValue)
            {
                var order = await _orderRepository.GetOneAsync(
                    filter: o => o.Id == model.OrderId.Value
                );

                if (order == null)
                {
                    ModelState.AddModelError(
                        nameof(model.OrderId),
                        "Selected order does not exist."
                    );

                    await LoadInvoiceData(model);
                    return View(model);
                }
            }

            // =====================================================
            // Create Invoice
            // =====================================================

            var invoice = new SalesInvoice
            {
                InvoiceNumber = model.InvoiceNumber,
                InvoiceDate = model.InvoiceDate,
                CustomerId = model.CustomerId,
                OrderId = model.OrderId,
                Discount = model.Discount
            };

            decimal total = 0;

            // =====================================================
            // Items
            // =====================================================

            foreach (var itemVM in model.InvoiceItems)
            {
                if (itemVM.ProductBatchId <= 0)
                {
                    ModelState.AddModelError(
                        "",
                        "Please select a valid product batch."
                    );

                    await LoadInvoiceData(model);
                    return View(model);
                }

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
                    includes:
                    [
                        b => b.Product
                    ]
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

                if (batch.Product == null)
                {
                    ModelState.AddModelError(
                        "",
                        "Product for the selected batch was not found."
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

                // =================================================
                // IMPORTANT
                // Price comes from DB
                // =================================================

                decimal unitPrice = batch.Product.Price;

                var invoiceItem = new SalesInvoiceItem
                {
                    ProductBatchId = batch.Id,
                    Quantity = itemVM.Quantity,
                    UnitPrice = unitPrice,
                    TotalPrice = unitPrice * itemVM.Quantity
                };

                total += invoiceItem.TotalPrice;

                // Decrease stock
                batch.QuantityOnHand -= itemVM.Quantity;

                _batchRepository.Update(batch);

                invoice.InvoiceItems.Add(invoiceItem);
            }

            // =====================================================
            // Calculate
            // =====================================================

            invoice.TotalAmount = total;

            invoice.NetAmount =
                invoice.TotalAmount - invoice.Discount;

            // Prevent negative invoice
            if (invoice.NetAmount < 0)
            {
                ModelState.AddModelError(
                    nameof(model.Discount),
                    "Discount cannot be greater than total amount."
                );

                await LoadInvoiceData(model);
                return View(model);
            }

            // =====================================================
            // Customer Balance
            // =====================================================

            if (customer != null)
            {
                customer.CurrentBalance += invoice.NetAmount;

                _customerRepository.Update(customer);
            }

            // =====================================================
            // Save
            // =====================================================

            await _salesInvoiceRepository.CreateAsync(invoice);

            await _salesInvoiceRepository.CommitAsync();

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // DELETE GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var invoice = await _salesInvoiceRepository.GetOneAsync(
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

                InvoiceItems = invoice.InvoiceItems
                    .Select(item => new SalesInvoiceItemVM
                    {
                        Id = item.Id,
                        ProductBatchId = item.ProductBatchId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.TotalPrice
                    })
                    .ToList()
            };

            return View(model);
        }

        // =========================================================
        // DELETE POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var invoice = await _salesInvoiceRepository.GetOneAsync(
                filter: i => i.Id == id,
                includes:
                [
                    i => i.InvoiceItems,
                    i => i.Customer
                ]
            );

            if (invoice == null)
            {
                return NotFound();
            }

            // =====================================================
            // Return Customer Balance
            // =====================================================

            if (invoice.Customer != null)
            {
                invoice.Customer.CurrentBalance -= invoice.NetAmount;

                if (invoice.Customer.CurrentBalance < 0)
                {
                    invoice.Customer.CurrentBalance = 0;
                }

                _customerRepository.Update(invoice.Customer);
            }

            // =====================================================
            // Return Stock
            // =====================================================

            foreach (var item in invoice.InvoiceItems.ToList())
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

            // =====================================================
            // Delete Invoice
            // =====================================================

            _salesInvoiceRepository.Delete(invoice);

            await _salesInvoiceRepository.CommitAsync();

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // LOAD DATA
        // =========================================================

        private async Task LoadInvoiceData(SalesInvoiceVM model)
        {
            var customers = await _customerRepository.GetAllAsync();

            model.Customers = customers
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = $"{c.Name} - {c.Phone}",
                    Selected = c.Id == model.CustomerId
                })
                .ToList();

            var orders = await _orderRepository.GetAllAsync();

            model.Orders = orders
                .Select(o => new SelectListItem
                {
                    Value = o.Id.ToString(),
                    Text = o.OrderNumber,
                    Selected = o.Id == model.OrderId
                })
                .ToList();

            var batches = await _batchRepository.GetAllAsync(
                includes:
                [
                    b => b.Product
                ]
            );

            ViewBag.Batches = batches
                .Where(b => b.Product != null)
                .Select(b => new SelectListItem
                {
                    Value = b.Id.ToString(),
                    Text =
                        $"{b.Product!.Name} | " +
                        $"Batch: {b.BatchNumber} | " +
                        $"Price: {b.Product.Price:0.00} | " +
                        $"Stock: {b.QuantityOnHand}"
                })
                .ToList();
        }
    }
}