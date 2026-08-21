using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Pharmacy.Enums;
using Pharmacy.Models;
using Pharmacy.Repositories;
using Pharmacy.Utils;
using Pharmacy.ViewModels;

namespace Pharmacy.Areas.Customer.Controllers
{
    [Area(CD.CUSTOMER_AREA)]
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IRepository<Cart> _cartRepository;
        private readonly IRepository<CartItem> _cartItemRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<Order> _orderRepository;
        private readonly IRepository<OrderItem> _orderItemRepository;
        private readonly IRepository<ProductBatch> _batchRepository;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrderController(
            IRepository<Cart> cartRepository,
            IRepository<CartItem> cartItemRepository,
            IRepository<Product> productRepository,
            IRepository<Order> orderRepository,
            IRepository<OrderItem> orderItemRepository,
            IRepository<ProductBatch> batchRepository,
            UserManager<ApplicationUser> userManager)
        {
            _cartRepository = cartRepository;
            _cartItemRepository = cartItemRepository;
            _productRepository = productRepository;
            _orderRepository = orderRepository;
            _orderItemRepository = orderItemRepository;
            _batchRepository = batchRepository;
            _userManager = userManager;
        }


        // ============================================================
        // GET: Customer/Order/Checkout
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }


            // Get current user's cart
            var cart = await _cartRepository.GetOneAsync(
                filter: c => c.ApplicationUserId == userId
            );

            if (cart == null)
            {
                TempData["Error_Notification"] =
                    "Your cart is empty.";

                return RedirectToAction(
                    "Index",
                    "Cart"
                );
            }


            // Get cart items
            var cartItems = await _cartItemRepository.GetAllAsync(
                filter: ci => ci.CartId == cart.Id
            );

            if (!cartItems.Any())
            {
                TempData["Error_Notification"] =
                    "Your cart is empty.";

                return RedirectToAction(
                    "Index",
                    "Cart"
                );
            }


            var viewModel = new CheckoutVM();


            foreach (var item in cartItems)
            {
                var product = await _productRepository.GetOneAsync(
                    filter: p => p.Id == item.ProductId,
                    IsTracking: false
                );

                if (product == null)
                {
                    continue;
                }


                viewModel.Items.Add(new CheckoutItemVM
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    UnitPrice = product.Price,
                    Quantity = item.Quantity,
                    TotalPrice = item.Quantity * product.Price
                });
            }


            viewModel.TotalAmount =
                viewModel.Items.Sum(x => x.TotalPrice);

            viewModel.DeliveryFees = 0;

            viewModel.Discount = 0;

            viewModel.NetAmount =
                viewModel.TotalAmount
                - viewModel.Discount
                + viewModel.DeliveryFees;


            return View(viewModel);
        }


        // ============================================================
        // POST: Customer/Order/Checkout
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(
            CheckoutVM checkoutVM)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }


            // ========================================================
            // Get Cart
            // ========================================================

            var cart = await _cartRepository.GetOneAsync(
                filter: c => c.ApplicationUserId == userId
            );

            if (cart == null)
            {
                TempData["Error_Notification"] =
                    "Your cart is empty.";

                return RedirectToAction(
                    "Index",
                    "Cart"
                );
            }


            // ========================================================
            // Get Cart Items
            // ========================================================

            var cartItems = await _cartItemRepository.GetAllAsync(
                filter: ci => ci.CartId == cart.Id
            );

            if (!cartItems.Any())
            {
                TempData["Error_Notification"] =
                    "Your cart is empty.";

                return RedirectToAction(
                    "Index",
                    "Cart"
                );
            }


            // ========================================================
            // Prepare Checkout Items
            // ========================================================

            checkoutVM.Items = new List<CheckoutItemVM>();

            decimal totalAmount = 0;


            foreach (var item in cartItems)
            {
                var product = await _productRepository.GetOneAsync(
                    filter: p => p.Id == item.ProductId,
                    includes: [p => p.Batches],
                    IsTracking: false
                );

                if (product == null)
                {
                    ModelState.AddModelError(
                        "",
                        $"Product #{item.ProductId} was not found."
                    );

                    continue;
                }


                // Get latest product price
                decimal currentPrice = product.Price;


                // Recalculate item total
                decimal itemTotal =
                    item.Quantity * currentPrice;


                checkoutVM.Items.Add(new CheckoutItemVM
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    UnitPrice = currentPrice,
                    Quantity = item.Quantity,
                    TotalPrice = itemTotal
                });


                totalAmount += itemTotal;
            }


            // ========================================================
            // STOCK VALIDATION
            // ========================================================

            bool stockIsValid = true;


            foreach (var item in cartItems)
            {
                var product = await _productRepository.GetOneAsync(
                    filter: p => p.Id == item.ProductId,
                    includes: [p => p.Batches],
                    IsTracking: false
                );

                if (product == null)
                {
                    stockIsValid = false;

                    continue;
                }


                // Only valid, non-expired batches
                int availableStock = product.Batches
                    .Where(b =>
                        b.ExpiryDate.Date >= DateTime.Today &&
                        b.QuantityOnHand > 0)
                    .Sum(b => b.QuantityOnHand);


                // Requested quantity is greater than available stock
                if (item.Quantity > availableStock)
                {
                    stockIsValid = false;


                    ModelState.AddModelError(
                        "",
                        $"{product.Name}: only {availableStock} item(s) are available."
                    );
                }
            }


            // ========================================================
            // Discount & Delivery
            // ========================================================

            decimal discount = checkoutVM.Discount;

            if (discount < 0)
            {
                discount = 0;
            }


            if (discount > totalAmount)
            {
                discount = totalAmount;
            }


            decimal deliveryFees = checkoutVM.DeliveryFees;

            if (deliveryFees < 0)
            {
                deliveryFees = 0;
            }


            decimal netAmount =
                totalAmount
                - discount
                + deliveryFees;


            checkoutVM.TotalAmount = totalAmount;

            checkoutVM.Discount = discount;

            checkoutVM.DeliveryFees = deliveryFees;

            checkoutVM.NetAmount = netAmount;


            // ========================================================
            // Model Validation
            // ========================================================

            if (!ModelState.IsValid || !stockIsValid)
            {
                return View(checkoutVM);
            }


            // ========================================================
            // Create Order
            // ========================================================

            var order = new Order
            {
                OrderNumber =
                    $"ORD-{DateTime.Now:yyyyMMddHHmmssfff}",

                OrderDate = DateTime.Now,

                Status = OrderStatus.Pending,

                TotalAmount = totalAmount,

                Discount = discount,

                DeliveryFees = deliveryFees,

                NetAmount = netAmount,

                PaymentMethod =
                    checkoutVM.PaymentMethod,

                DeliveryAddress =
                    checkoutVM.DeliveryAddress,

                ApplicationUserId = userId
            };


            await _orderRepository.CreateAsync(order);

            await _orderRepository.CommitAsync();


            // ========================================================
            // Create Order Items
            // ========================================================

            foreach (var item in cartItems)
            {
                var product = await _productRepository.GetOneAsync(
                    filter: p => p.Id == item.ProductId,
                    includes: [p => p.Batches],
                    IsTracking: true
                );

                if (product == null)
                {
                    continue;
                }


                var orderItem = new OrderItem
                {
                    OrderId = order.Id,

                    ProductId = product.Id,

                    Quantity = item.Quantity,

                    UnitPrice = product.Price,

                    TotalPrice =
                        item.Quantity * product.Price
                };


                await _orderItemRepository.CreateAsync(orderItem);


                // ====================================================
                // Deduct Stock using FEFO
                // ====================================================

                int remainingQuantity =
                    item.Quantity;


                var batches = product.Batches
                    .Where(b =>
                        b.ExpiryDate.Date >= DateTime.Today &&
                        b.QuantityOnHand > 0)
                    .OrderBy(b => b.ExpiryDate)
                    .ToList();


                foreach (var batch in batches)
                {
                    if (remainingQuantity <= 0)
                    {
                        break;
                    }


                    int quantityToDeduct =
                        Math.Min(
                            batch.QuantityOnHand,
                            remainingQuantity
                        );


                    batch.QuantityOnHand -=
                        quantityToDeduct;


                    remainingQuantity -=
                        quantityToDeduct;
                }


                if (remainingQuantity > 0)
                {
                    // This should normally never happen
                    // because stock was validated above.

                    TempData["Error_Notification"] =
                        $"Insufficient stock for {product.Name}.";

                    return RedirectToAction(
                        "Index",
                        "Cart"
                    );
                }
            }


            // Save OrderItems + Batch changes
            await _orderItemRepository.CommitAsync();


            // ========================================================
            // Clear Cart
            // ========================================================

            foreach (var item in cartItems)
            {
                _cartItemRepository.Delete(item);
            }


            await _cartItemRepository.CommitAsync();


            // ========================================================
            // Success
            // ========================================================

            TempData["Success_Notification"] =
                $"Order {order.OrderNumber} placed successfully.";


            return RedirectToAction(
                nameof(Success),
                new { id = order.Id }
            );
        }


        // ============================================================
        // GET: Customer/Order/Success/5
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Success(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }


            var order = await _orderRepository.GetOneAsync(
                filter: o =>
                    o.Id == id &&
                    o.ApplicationUserId == userId
            );


            if (order == null)
            {
                return NotFound();
            }


            return View(order);
        }


        // ============================================================
        // GET: Customer/Order/Index
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }


            var orders = await _orderRepository.GetAllAsync(
                filter: o =>
                    o.ApplicationUserId == userId,
                includes: [o => o.OrderItems],
                IsTracking: false
            );


            var model = orders
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
                    ApplicationUserId = o.ApplicationUserId,

                    OrderItems = o.OrderItems
                        .Select(item => new OrderItemVM
                        {
                            Id = item.Id,
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice,
                            TotalPrice = item.TotalPrice
                        })
                        .ToList()
                })
                .ToList();


            return View(model);
        }


        // ============================================================
        // GET: Customer/Order/Details/5
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }


            var order = await _orderRepository.GetOneAsync(
                filter: o =>
                    o.Id == id &&
                    o.ApplicationUserId == userId,

                includes:
                [
                    o => o.OrderItems
                ],

                IsTracking: false
            );


            if (order == null)
            {
                return NotFound();
            }


            var model = new OrderVM
            {
                Id = order.Id,

                OrderNumber =
                    order.OrderNumber,

                OrderDate =
                    order.OrderDate,

                Status =
                    order.Status,

                TotalAmount =
                    order.TotalAmount,

                Discount =
                    order.Discount,

                DeliveryFees =
                    order.DeliveryFees,

                NetAmount =
                    order.NetAmount,

                PaymentMethod =
                    order.PaymentMethod,

                DeliveryAddress =
                    order.DeliveryAddress,

                Notes =
                    order.Notes,

                ApplicationUserId =
                    order.ApplicationUserId,

                OrderItems =
                    order.OrderItems
                        .Select(item => new OrderItemVM
                        {
                            Id = item.Id,

                            ProductId =
                                item.ProductId,

                            ProductName =
                                item.Product?.Name,

                            Quantity =
                                item.Quantity,

                            UnitPrice =
                                item.UnitPrice,

                            TotalPrice =
                                item.TotalPrice
                        })
                        .ToList()
            };


            return View(model);
        }
    }
}