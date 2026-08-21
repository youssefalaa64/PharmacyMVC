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
        private readonly UserManager<ApplicationUser> _userManager;

        public OrderController(
            IRepository<Cart> cartRepository,
            IRepository<CartItem> cartItemRepository,
            IRepository<Product> productRepository,
            IRepository<Order> orderRepository,
            IRepository<OrderItem> orderItemRepository,
            UserManager<ApplicationUser> userManager)
        {
            _cartRepository = cartRepository;
            _cartItemRepository = cartItemRepository;
            _productRepository = productRepository;
            _orderRepository = orderRepository;
            _orderItemRepository = orderItemRepository;
            _userManager = userManager;
        }


        // ==========================================
        // GET: Customer/Order/Checkout
        // ==========================================

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
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    TotalPrice = item.TotalPrice
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


        // ==========================================
        // POST: Customer/Order/Checkout
        // ==========================================

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


            // Get Cart Items
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


            // ==========================================
            // Recalculate everything on the server
            // Don't trust prices coming from the View
            // ==========================================

            decimal totalAmount = 0;


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


                // Get latest product price
                item.UnitPrice = product.Price;

                item.TotalPrice =
                    item.Quantity * item.UnitPrice;

                totalAmount += item.TotalPrice;
            }


            decimal discount = checkoutVM.Discount;

            if (discount < 0)
            {
                discount = 0;
            }


            // Don't allow discount greater than total
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


            // ==========================================
            // Validate Model
            // ==========================================

            if (!ModelState.IsValid)
            {
                checkoutVM.Items = new List<CheckoutItemVM>();

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

                    checkoutVM.Items.Add(new CheckoutItemVM
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        UnitPrice = product.Price,
                        Quantity = item.Quantity,
                        TotalPrice =
                            item.Quantity * product.Price
                    });
                }


                checkoutVM.TotalAmount = totalAmount;
                checkoutVM.DeliveryFees = deliveryFees;
                checkoutVM.Discount = discount;
                checkoutVM.NetAmount = netAmount;

                return View(checkoutVM);
            }


            // ==========================================
            // Create Order
            // ==========================================

            var order = new Order
            {
                OrderNumber = $"ORD-{DateTime.Now:yyyyMMddHHmmssfff}",

                OrderDate = DateTime.Now,

                Status = OrderStatus.Pending,

                TotalAmount = totalAmount,

                Discount = discount,

                DeliveryFees = deliveryFees,

                NetAmount = netAmount,

                PaymentMethod = checkoutVM.PaymentMethod,

                DeliveryAddress =
                    checkoutVM.DeliveryAddress,

                ApplicationUserId = userId
            };


            await _orderRepository.CreateAsync(order);

            await _orderRepository.CommitAsync();


            // ==========================================
            // Create Order Items
            // ==========================================

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
            }


            await _orderItemRepository.CommitAsync();


            // ==========================================
            // Clear Cart
            // ==========================================

            foreach (var item in cartItems)
            {
                _cartItemRepository.Delete(item);
            }


            await _cartItemRepository.CommitAsync();


            TempData["Success_Notification"] =
                $"Order {order.OrderNumber} placed successfully.";


            return RedirectToAction(
                nameof(Success),
                new { id = order.Id }
            );
        }


        // ==========================================
        // GET: Customer/Order/Success/5
        // ==========================================

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
    }
}