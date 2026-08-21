using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Pharmacy.Enums;
using Pharmacy.Models;
using Pharmacy.Repositories;
using Pharmacy.Services;
using Pharmacy.Utils;
using Pharmacy.ViewModels;

namespace Pharmacy.Areas.Admin.Controllers
{
    [Authorize]
    [Area(CD.ADMIN_AREA)]
    public class OrderController : Controller
    {
        private readonly IRepository<Order> _orderrepository;
        private readonly IRepository<OrderItem> _itemRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrderController(
            IRepository<Order> repository,
            IRepository<OrderItem> itemRepository,
            IRepository<Product> productRepository,
            UserManager<ApplicationUser> userManager,
            INotificationService notificationService)
        {
            _orderrepository = repository;
            _itemRepository = itemRepository;
            _productRepository = productRepository;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        // GET: Admin/Order
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var orders = await _orderrepository.GetAllAsync(
                includes:
                [
                    o => o.ApplicationUser
                ]
            );

            var result = orders.Select(o => new OrderVM
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                OrderDate = o.OrderDate,
                Status = o.Status,
                TotalAmount = o.TotalAmount,
                Discount = o.Discount,
                DeliveryFees = o.DeliveryFees,
                NetAmount = o.NetAmount,
                PaymentMethod = o.PaymentMethod,
                DeliveryAddress = o.DeliveryAddress,
                Notes = o.Notes,
                ApplicationUserId = o.ApplicationUserId
            });

            return View(result);
        }

        // GET: Admin/Order/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var order = await _orderrepository.GetOneAsync(
                filter: o => o.Id == id,
                includes:
                [
                    o => o.ApplicationUser,
                    o => o.OrderItems
                ]
            );

            if (order == null)
            {
                return NotFound();
            }

            var model = new OrderVM
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                OrderDate = order.OrderDate,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                Discount = order.Discount,
                DeliveryFees = order.DeliveryFees,
                NetAmount = order.NetAmount,
                PaymentMethod = order.PaymentMethod,
                DeliveryAddress = order.DeliveryAddress,
                Notes = order.Notes,
                ApplicationUserId = order.ApplicationUserId,

                OrderItems = order.OrderItems.Select(item =>
                    new OrderItemVM
                    {
                        Id = item.Id,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.TotalPrice
                    }).ToList()
            };

            return View(model);
        }

        // GET: Admin/Order/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new OrderVM();

            await LoadUsers(model);

            return View(model);
        }

        // POST: Admin/Order/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderVM model)
        {
            if (!ModelState.IsValid)
            {
                await LoadUsers(model);
                return View(model);
            }

            if (model.OrderItems == null || !model.OrderItems.Any())
            {
                ModelState.AddModelError(
                    "",
                    "Order must contain at least one item."
                );

                await LoadUsers(model);
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(
                model.ApplicationUserId
            );

            if (user == null)
            {
                ModelState.AddModelError(
                    nameof(model.ApplicationUserId),
                    "Selected user does not exist."
                );

                await LoadUsers(model);
                return View(model);
            }

            var order = new Order
            {
                OrderNumber = model.OrderNumber,
                OrderDate = model.OrderDate,
                Status = model.Status,
                Discount = model.Discount,
                DeliveryFees = model.DeliveryFees,
                PaymentMethod = model.PaymentMethod,
                DeliveryAddress = model.DeliveryAddress,
                Notes = model.Notes,
                ApplicationUserId = model.ApplicationUserId
            };

            decimal total = 0;

            foreach (var itemVM in model.OrderItems)
            {
                if (itemVM.Quantity <= 0)
                {
                    ModelState.AddModelError(
                        "",
                        "Quantity must be greater than zero."
                    );

                    await LoadUsers(model);
                    return View(model);
                }

                var product = await _productRepository.GetOneAsync(
                    filter: p => p.Id == itemVM.ProductId
                );

                if (product == null)
                {
                    ModelState.AddModelError(
                        "",
                        $"Product #{itemVM.ProductId} was not found."
                    );

                    await LoadUsers(model);
                    return View(model);
                }

                var item = new OrderItem
                {
                    ProductId = itemVM.ProductId,
                    Quantity = itemVM.Quantity,
                    UnitPrice = itemVM.UnitPrice,
                    TotalPrice = itemVM.Quantity * itemVM.UnitPrice
                };

                total += item.TotalPrice;

                order.OrderItems.Add(item);
            }

            order.TotalAmount = total;

            order.NetAmount =
                order.TotalAmount
                - order.Discount
                + order.DeliveryFees;

            await _orderrepository.CreateAsync(order);
            await _orderrepository.CommitAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Order/Edit/5
        [HttpGet]
        // GET: Admin/Order/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var order = await _orderrepository.GetOneAsync(
                filter: o => o.Id == id,
                includes:
                [
                    o => o.OrderItems
                ]
            );

            if (order == null)
            {
                return NotFound();
            }

            var model = new OrderVM
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                OrderDate = order.OrderDate,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                Discount = order.Discount,
                DeliveryFees = order.DeliveryFees,
                NetAmount = order.NetAmount,
                PaymentMethod = order.PaymentMethod,
                DeliveryAddress = order.DeliveryAddress,
                Notes = order.Notes,
                ApplicationUserId = order.ApplicationUserId,

                OrderItems = order.OrderItems.Select(item =>
                    new OrderItemVM
                    {
                        Id = item.Id,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.TotalPrice
                    }).ToList()
            };

            await LoadUsers(model);
            await LoadProducts(model);

            return View(model);
        }

        // POST: Admin/Order/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(OrderVM model)
        {
            if (!ModelState.IsValid)
            {
                await LoadUsers(model);
                await LoadProducts(model);

                return View(model);
            }

            var order = await _orderrepository.GetOneAsync(
                filter: o => o.Id == model.Id,
                includes:
                [
                    o => o.OrderItems
                ]
            );

            if (order == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(
                model.ApplicationUserId
            );

            if (user == null)
            {
                ModelState.AddModelError(
                    nameof(model.ApplicationUserId),
                    "Selected user does not exist."
                );

                await LoadUsers(model);
                await LoadProducts(model);

                return View(model);
            }


            // =========================
            // Update Order
            // =========================

            order.OrderNumber = model.OrderNumber;
            order.OrderDate = model.OrderDate;
            order.Status = model.Status;
            order.Discount = model.Discount;
            order.DeliveryFees = model.DeliveryFees;
            order.PaymentMethod = model.PaymentMethod;
            order.DeliveryAddress = model.DeliveryAddress;
            order.Notes = model.Notes;
            order.ApplicationUserId = model.ApplicationUserId;


            // =========================
            // Delete Old Items
            // =========================

            foreach (var oldItem in order.OrderItems.ToList())
            {
                _itemRepository.Delete(oldItem);
            }

            order.OrderItems.Clear();


            // =========================
            // Add New Items
            // =========================

            decimal total = 0;

            foreach (var itemVM in model.OrderItems)
            {
                if (itemVM.Quantity <= 0)
                {
                    ModelState.AddModelError(
                        "",
                        "Quantity must be greater than zero."
                    );

                    await LoadUsers(model);
                    await LoadProducts(model);

                    return View(model);
                }


                var product = await _productRepository.GetOneAsync(
                    filter: p => p.Id == itemVM.ProductId
                );

                if (product == null)
                {
                    ModelState.AddModelError(
                        "",
                        $"Product #{itemVM.ProductId} was not found."
                    );

                    await LoadUsers(model);
                    await LoadProducts(model);

                    return View(model);
                }


                // Get price from database
                var item = new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = product.Id,
                    Quantity = itemVM.Quantity,
                    UnitPrice = product.Price,
                    TotalPrice = itemVM.Quantity * product.Price
                };


                total += item.TotalPrice;

                order.OrderItems.Add(item);
            }


            // =========================
            // Calculate Totals
            // =========================

            order.TotalAmount = total;

            order.NetAmount =
                order.TotalAmount
                - order.Discount
                + order.DeliveryFees;


            // =========================
            // Save
            // =========================

            _orderrepository.Update(order);

            await _orderrepository.CommitAsync();
            string message = model.Status switch
            {
                OrderStatus.Processing =>
                    $"Your order #{order.OrderNumber} is now being processed.",

                OrderStatus.Completed =>
                    $"Your order #{order.OrderNumber} has been completed.",

                OrderStatus.Cancelled =>
                    $"Your order #{order.OrderNumber} has been canceled.",

                _ =>
                    $"Your order #{order.OrderNumber} status has been changed to {order.Status}."
            };

            await _notificationService.CreateAsync(
                order.ApplicationUserId,
                message,
                "OrderStatus",
                order.Id
            );


            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Order/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var order = await _orderrepository.GetOneAsync(
                filter: o => o.Id == id,
                includes:
                [
                    o => o.ApplicationUser,
                    o => o.OrderItems
                ]
            );

            if (order == null)
            {
                return NotFound();
            }

            var model = new OrderVM
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                OrderDate = order.OrderDate,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                Discount = order.Discount,
                DeliveryFees = order.DeliveryFees,
                NetAmount = order.NetAmount,
                PaymentMethod = order.PaymentMethod,
                DeliveryAddress = order.DeliveryAddress,
                Notes = order.Notes,
                ApplicationUserId = order.ApplicationUserId,

                OrderItems = order.OrderItems.Select(item =>
                    new OrderItemVM
                    {
                        Id = item.Id,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.TotalPrice
                    }).ToList()
            };

            return View(model);
        }

        // POST: Admin/Order/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _orderrepository.GetOneAsync(
                filter: o => o.Id == id,
                includes: [o => o.OrderItems]
            );

            if (order == null)
            {
                return NotFound();
            }

            foreach (var item in order.OrderItems.ToList())
            {
                _itemRepository.Delete(item);
            }

            _orderrepository.Delete(order);

            await _orderrepository.CommitAsync();

            return RedirectToAction(nameof(Index));
        }
        private async Task LoadUsers(OrderVM model)
        {
            var users = _userManager.Users.ToList();

            model.Users = users.Select(u => new SelectListItem
            {
                Value = u.Id,
                Text = $"{u.FirstName} {u.LastName} - {u.Email}",
                Selected = u.Id == model.ApplicationUserId
            });
        }
        private async Task LoadProducts(OrderVM model)
        {
            var products = await _productRepository.GetAllAsync();

            model.Products = products.Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = $"{p.Name} - {p.Price:0.00} EGP"
            }).ToList();
        }

    }
}