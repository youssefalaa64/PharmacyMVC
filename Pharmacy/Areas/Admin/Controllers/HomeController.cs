using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pharmacy.DataAccess;
using Pharmacy.Enums;
using Pharmacy.ViewModels;

namespace Pharmacy.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var dashboard = new AdminDashboardVM
            {
                TotalUsers = await _context.Users.CountAsync(),

                TotalProducts = await _context.Products.CountAsync(),

                TotalProductBatches = await _context.ProductBatches.CountAsync(),

                TotalCustomers = await _context.Customers.CountAsync(),

                TotalOrders = await _context.Orders.CountAsync(),

                TotalSalesInvoices = await _context.SalesInvoices.CountAsync(),

                TotalSales = await _context.SalesInvoices
                    .Select(x => (decimal?)x.NetAmount)
                    .SumAsync() ?? 0,

                PendingOrders = await _context.Orders
                    .CountAsync(x => x.Status == OrderStatus.Pending),

                ProcessingOrders = await _context.Orders
                    .CountAsync(x => x.Status == OrderStatus.Processing),

                CompletedOrders = await _context.Orders
                    .CountAsync(x => x.Status == OrderStatus.Completed),

                CancelledOrders = await _context.Orders
                    .CountAsync(x => x.Status == OrderStatus.Cancelled)
            };

            return View(dashboard);
        }
    }
}