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
        private readonly IRepository<Order> _orderRepository;
        private readonly IRepository<OrderItem> _itemRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<Notification> _notificationRepository;

        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrderController(
            IRepository<Order> orderRepository,
            IRepository<OrderItem> itemRepository,
            IRepository<Product> productRepository,
            IRepository<Notification> notificationRepository,
            UserManager<ApplicationUser> userManager,
            INotificationService notificationService)
        {
            _orderRepository = orderRepository;
            _itemRepository = itemRepository;
            _productRepository = productRepository;
            _notificationRepository = notificationRepository;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        // =========================================================
        // INDEX
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var orders = await _orderRepository.GetAllAsync(
                includes:
                [
                    o => o.ApplicationUser
                ]
            );

            var result = orders
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new OrderVM
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
            var order = await _orderRepository.GetOneAsync(
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

                OrderItems = order.OrderItems
                    .Select(item => new OrderItemVM
                    {
                        Id = item.Id,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.TotalPrice
                    })
                    .ToList()
            };

            return View(model);
        }

        // =========================================================
        // CREATE - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new OrderVM
            {
                OrderDate = DateTime.Now,
                Status = OrderStatus.Pending
            };

            await LoadUsers(model);
            await LoadProducts(model);

            return View(model);
        }

        // =========================================================
        // CREATE - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderVM model)
        {
            if (!ModelState.IsValid)
            {
                await LoadUsers(model);
                await LoadProducts(model);

                return View(model);
            }

            if (model.OrderItems == null || !model.OrderItems.Any())
            {
                ModelState.AddModelError(
                    "",
                    "Order must contain at least one item."
                );

                await LoadUsers(model);
                await LoadProducts(model);

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
                await LoadProducts(model);

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

                // Price comes from database
                var item = new OrderItem
                {
                    ProductId = product.Id,
                    Quantity = itemVM.Quantity,
                    UnitPrice = product.Price,
                    TotalPrice = itemVM.Quantity * product.Price
                };

                total += item.TotalPrice;

                order.OrderItems.Add(item);
            }

            order.TotalAmount = total;

            order.NetAmount =
                order.TotalAmount
                - order.Discount
                + order.DeliveryFees;

            await _orderRepository.CreateAsync(order);
            await _orderRepository.CommitAsync();

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // EDIT - GET
        // =========================================================
        // =========================================================
        // GET: Admin/Order/Edit/5
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var order = await _orderRepository.GetOneAsync(
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


        // =========================================================
        // POST: Admin/Order/Edit
        // =========================================================

        // =========================================================
        // POST: Admin/Order/Edit
        // =========================================================
        // POST: Admin/Order/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(OrderVM model)
        {
            // TotalAmount and NetAmount are calculated on server
            ModelState.Remove(nameof(model.TotalAmount));
            ModelState.Remove(nameof(model.NetAmount));

            // ==========================================
            // Get existing order with items
            // ==========================================

            var order = await _orderRepository.GetOneAsync(
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

            // ==========================================
            // Validate User
            // ==========================================

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

            // ==========================================
            // IMPORTANT
            //
            // If OrderItems weren't posted,
            // don't touch existing items.
            // ==========================================

            if (model.OrderItems == null ||
                !model.OrderItems.Any())
            {
                // Keep existing items exactly as they are.
            }
            else
            {
                // ==========================================
                // Validate posted items
                // ==========================================

                foreach (var item in model.OrderItems)
                {
                    if (item.ProductId <= 0)
                    {
                        ModelState.AddModelError(
                            "",
                            "Please select a valid product."
                        );

                        await LoadUsers(model);
                        await LoadProducts(model);

                        return View(model);
                    }

                    if (item.Quantity <= 0)
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
                        filter: p => p.Id == item.ProductId
                    );

                    if (product == null)
                    {
                        ModelState.AddModelError(
                            "",
                            $"Product #{item.ProductId} was not found."
                        );

                        await LoadUsers(model);
                        await LoadProducts(model);

                        return View(model);
                    }
                }

                // ==========================================
                // Update existing items
                // or add new items
                // ==========================================

                var existingItems =
                    order.OrderItems.ToList();

                var postedItemIds =
                    model.OrderItems
                        .Where(x => x.Id > 0)
                        .Select(x => x.Id)
                        .ToHashSet();

                // ==========================================
                // Delete items removed from the form
                // ==========================================

                foreach (var existingItem in existingItems)
                {
                    if (!postedItemIds.Contains(existingItem.Id))
                    {
                        _itemRepository.Delete(existingItem);
                    }
                }

                // ==========================================
                // Update / Add items
                // ==========================================

                foreach (var itemVM in model.OrderItems)
                {
                    // Existing item
                    if (itemVM.Id > 0)
                    {
                        var existingItem =
                            existingItems.FirstOrDefault(
                                x => x.Id == itemVM.Id
                            );

                        if (existingItem == null)
                        {
                            continue;
                        }

                        var product =
                            await _productRepository.GetOneAsync(
                                filter: p => p.Id == itemVM.ProductId
                            );

                        existingItem.ProductId =
                            product!.Id;

                        existingItem.Quantity =
                            itemVM.Quantity;

                        // Always use price from database
                        existingItem.UnitPrice =
                            product.Price;

                        existingItem.TotalPrice =
                            product.Price * itemVM.Quantity;

                        _itemRepository.Update(existingItem);
                    }
                    else
                    {
                        // New item
                        var product =
                            await _productRepository.GetOneAsync(
                                filter: p => p.Id == itemVM.ProductId
                            );

                        if (product == null)
                        {
                            continue;
                        }

                        var newItem = new OrderItem
                        {
                            OrderId = order.Id,
                            ProductId = product.Id,
                            Quantity = itemVM.Quantity,
                            UnitPrice = product.Price,
                            TotalPrice =
                                product.Price * itemVM.Quantity
                        };

                        await _itemRepository.CreateAsync(newItem);
                    }
                }
            }

            // ==========================================
            // Recalculate total from existing DB items
            // ==========================================

            var updatedOrder = await _orderRepository.GetOneAsync(
                filter: o => o.Id == model.Id,
                includes:
                [
                    o => o.OrderItems
                ]
            );

            if (updatedOrder == null)
            {
                return NotFound();
            }

            decimal total =
                updatedOrder.OrderItems.Sum(
                    x => x.Quantity * x.UnitPrice
                );

            // ==========================================
            // Save old status
            // ==========================================

            var oldStatus = order.Status;

            // ==========================================
            // Update Order
            // ==========================================

            order.OrderNumber =
                model.OrderNumber;

            order.OrderDate =
                model.OrderDate;

            order.Status =
                model.Status;

            order.Discount =
                model.Discount;

            order.DeliveryFees =
                model.DeliveryFees;

            order.PaymentMethod =
                model.PaymentMethod;

            order.DeliveryAddress =
                model.DeliveryAddress;

            order.Notes =
                model.Notes;

            order.ApplicationUserId =
                model.ApplicationUserId;

            order.TotalAmount =
                total;

            order.NetAmount =
                total
                - model.Discount
                + model.DeliveryFees;

            _orderRepository.Update(order);

            await _orderRepository.CommitAsync();

            // ==========================================
            // Notification only if status changed
            // ==========================================

            if (oldStatus != model.Status)
            {
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
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // DELETE - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var order = await _orderRepository.GetOneAsync(
                filter: o => o.Id == id,
                includes:
                [
                    o => o.ApplicationUser,
                    o => o.OrderItems,
                    o => o.Notifications
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

                OrderItems = order.OrderItems
                    .Select(item => new OrderItemVM
                    {
                        Id = item.Id,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.TotalPrice
                    })
                    .ToList()
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
            var order = await _orderRepository.GetOneAsync(
                filter: o => o.Id == id,
                includes:
                [
                    o => o.OrderItems,
                    o => o.Notifications
                ]
            );

            if (order == null)
            {
                return NotFound();
            }

            // Delete notifications first
            foreach (var notification in order.Notifications.ToList())
            {
                _notificationRepository.Delete(notification);
            }

            // Delete order items
            foreach (var item in order.OrderItems.ToList())
            {
                _itemRepository.Delete(item);
            }

            // Delete order
            _orderRepository.Delete(order);

            await _orderRepository.CommitAsync();

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // HELPERS
        // =========================================================

        private async Task LoadUsers(OrderVM model)
        {
            var users = _userManager.Users.ToList();

            model.Users = users
                .Select(u => new SelectListItem
                {
                    Value = u.Id,
                    Text = $"{u.FirstName} {u.LastName} - {u.Email}",
                    Selected = u.Id == model.ApplicationUserId
                })
                .ToList();
        }

        private async Task LoadProducts(OrderVM model)
        {
            var products = await _productRepository.GetAllAsync();

            model.Products = products
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = $"{p.Name} - {p.Price:0.00} EGP"
                })
                .ToList();
        }
    }
}