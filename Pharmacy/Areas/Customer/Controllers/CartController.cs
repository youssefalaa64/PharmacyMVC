using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Pharmacy.Models;
using Pharmacy.Repositories;
using Pharmacy.Utils;
using Pharmacy.ViewModels;

namespace Pharmacy.Areas.Customer.Controllers
{
    [Area(CD.CUSTOMER_AREA)]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IRepository<Cart> _cartRepository;
        private readonly IRepository<CartItem> _cartItemRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly UserManager<ApplicationUser> _userManager;

        public CartController(
            IRepository<Cart> cartRepository,
            IRepository<CartItem> cartItemRepository,
            IRepository<Product> productRepository,
            UserManager<ApplicationUser> userManager)
        {
            _cartRepository = cartRepository;
            _cartItemRepository = cartItemRepository;
            _productRepository = productRepository;
            _userManager = userManager;
        }


        // ==========================================
        // GET: Customer/Cart
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Index()
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


            // User doesn't have a cart yet
            if (cart == null)
            {
                return View(new CartVM());
            }


            // Get cart items
            var cartItems = await _cartItemRepository.GetAllAsync(
                filter: ci => ci.CartId == cart.Id
            );


            var viewModel = new CartVM
            {
                CartId = cart.Id
            };


            // Get products
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


                viewModel.Items.Add(new CartItemVM
                {
                    CartItemId = item.Id,
                    ProductId = product.Id,
                    ProductName = product.Name,
                    GenericName = product.GenericName,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    TotalPrice = item.TotalPrice
                });
            }


            return View(viewModel);
        }


        // ==========================================
        // POST: Customer/Cart/Add
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(
            int productId,
            int quantity = 1)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }


            if (quantity <= 0)
            {
                quantity = 1;
            }


            // Get product
            var product = await _productRepository.GetOneAsync(
                filter: p => p.Id == productId,
                IsTracking: false
            );


            if (product == null)
            {
                return NotFound();
            }


            // Get user's cart
            var cart = await _cartRepository.GetOneAsync(
                filter: c => c.ApplicationUserId == userId
            );


            // Create cart if it doesn't exist
            if (cart == null)
            {
                cart = new Cart
                {
                    ApplicationUserId = userId
                };

                await _cartRepository.CreateAsync(cart);

                await _cartRepository.CommitAsync();
            }


            // Check if product already exists
            var cartItem = await _cartItemRepository.GetOneAsync(
                filter: ci =>
                    ci.CartId == cart.Id &&
                    ci.ProductId == productId
            );


            if (cartItem != null)
            {
                // Increase quantity
                cartItem.Quantity += quantity;

                cartItem.UnitPrice = product.Price;

                cartItem.TotalPrice =
                    cartItem.Quantity * cartItem.UnitPrice;

                _cartItemRepository.Update(cartItem);
            }
            else
            {
                // Add new item
                cartItem = new CartItem
                {
                    CartId = cart.Id,
                    ProductId = product.Id,
                    Quantity = quantity,
                    UnitPrice = product.Price,
                    TotalPrice = quantity * product.Price
                };

                await _cartItemRepository.CreateAsync(cartItem);
            }


            await _cartItemRepository.CommitAsync();

            return RedirectToAction(nameof(Index));
        }


        // ==========================================
        // POST: Customer/Cart/Update
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(
            int cartItemId,
            int quantity)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }


            if (quantity <= 0)
            {
                return RedirectToAction(
                    nameof(Remove),
                    new { cartItemId }
                );
            }


            var cartItem = await _cartItemRepository.GetOneAsync(
                filter: ci => ci.Id == cartItemId,
                includes: [ci => ci.Cart!]
            );


            if (cartItem == null)
            {
                return NotFound();
            }


            // Make sure this item belongs to current user
            if (cartItem.Cart?.ApplicationUserId != userId)
            {
                return Forbid();
            }


            cartItem.Quantity = quantity;

            cartItem.TotalPrice =
                cartItem.Quantity * cartItem.UnitPrice;


            _cartItemRepository.Update(cartItem);

            await _cartItemRepository.CommitAsync();


            return RedirectToAction(nameof(Index));
        }


        // ==========================================
        // POST: Customer/Cart/Remove
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int cartItemId)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }


            var cartItem = await _cartItemRepository.GetOneAsync(
                filter: ci => ci.Id == cartItemId,
                includes: [ci => ci.Cart!]
            );


            if (cartItem == null)
            {
                return NotFound();
            }


            // Security check
            if (cartItem.Cart?.ApplicationUserId != userId)
            {
                return Forbid();
            }


            _cartItemRepository.Delete(cartItem);

            await _cartItemRepository.CommitAsync();


            return RedirectToAction(nameof(Index));
        }


        // ==========================================
        // POST: Customer/Cart/Clear
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clear()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }


            var cart = await _cartRepository.GetOneAsync(
                filter: c => c.ApplicationUserId == userId
            );


            if (cart == null)
            {
                return RedirectToAction(nameof(Index));
            }


            var cartItems = await _cartItemRepository.GetAllAsync(
                filter: ci => ci.CartId == cart.Id
            );


            foreach (var item in cartItems)
            {
                _cartItemRepository.Delete(item);
            }


            await _cartItemRepository.CommitAsync();


            return RedirectToAction(nameof(Index));
        }
    }
}