using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pharmacy.Models;
using Pharmacy.Repositories;
using Pharmacy.Utils;
using Pharmacy.ViewModels;

namespace Pharmacy.Areas.Customer.Controllers
{
    [Area(CD.CUSTOMER_AREA)]
    public class CartController : Controller
    {
        private readonly Repository<Product> _productRepository;
        private const string CART_KEY = "CustomerCartSession";

        public CartController(Repository<Product> productRepository)
        {
            _productRepository = productRepository;
        }
        private List<CartIVM> GetCartFromSession()
        {
            var json = HttpContext.Session.GetString(CART_KEY);
            if (string.IsNullOrEmpty(json))
            {
                return new List<CartIVM>();
            }
            return JsonSerializer.Deserialize<List<CartIVM>>(json) ?? new List<CartIVM>();
        }

        private void SaveCartToSession(List<CartIVM> cart)
        {
            var json = JsonSerializer.Serialize(cart);
            HttpContext.Session.SetString(CART_KEY, json);
        }

        public IActionResult Index()
        {
            var items = GetCartFromSession();

            var cartVM = new CartVM
            {
                Items = items
            };

            return View(cartVM);
        }


        public async Task<IActionResult> AddToCart(int productId, int count = 1)
        {
            var product = await _productRepository.GetOneAsync(
                filter: p => p.Id == productId,
                IsTracking: false
            );

            if (product == null)
            {
                return NotFound();
            }

            var cart = GetCartFromSession();
            var existingItem = cart.FirstOrDefault(i => i.Id == productId);

            if (existingItem != null)
            {
                existingItem.Quantity += count;
            }
            else
            {
                cart.Add(new CartIVM
                {
                    Id = product.Id,
                    Name = product.Name,
                    UnitPrice = product.Price,
                    ProductImg = product.ProductImg, 
                    Quantity = count
                });
            }

            SaveCartToSession(cart);
            return RedirectToAction(nameof(Index));
        }

        
        public IActionResult Plus(int productId)
        {
            var cart = GetCartFromSession();
            var item = cart.FirstOrDefault(i => i.Id == productId);

            if (item != null)
            {
                item.Quantity += 1;
                SaveCartToSession(cart);
            }

            return RedirectToAction(nameof(Index));
        }

        
        public IActionResult Minus(int productId)
        {
            var cart = GetCartFromSession();
            var item = cart.FirstOrDefault(i => i.Id == productId);

            if (item != null)
            {
                if (item.Quantity > 1)
                {
                    item.Quantity -= 1;
                }
                else
                {
                    cart.Remove(item);
                }

                SaveCartToSession(cart);
            }

            return RedirectToAction(nameof(Index));
        }

        
        public IActionResult Remove(int productId)
        {
            var cart = GetCartFromSession();
            var item = cart.FirstOrDefault(i => i.Id == productId);

            if (item != null)
            {
                cart.Remove(item);
                SaveCartToSession(cart);
            }

            return RedirectToAction(nameof(Index));
        }

       
        public IActionResult Clear()
        {
            HttpContext.Session.Remove(CART_KEY);
            return RedirectToAction(nameof(Index));
        }
    }
}