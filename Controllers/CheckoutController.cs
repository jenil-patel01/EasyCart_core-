using eays.Data;
using eays.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eays.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CheckoutController(AppDbContext context,
                                  UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var userId = user.Id;

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any())
                return RedirectToAction("Index", "Cart");

            var vm = new CheckoutViewModel
            {
                CartItems = cartItems,
                FullName = user.FullName,
                Email = user.Email ?? "",
                Address = user.Address
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var userId = user.Id;

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any())
            {
                model.CartItems = cartItems;
                return View("Index", model);
            }

            // Remove CartItems validation since it's not posted
            ModelState.Remove("CartItems");

            if (!ModelState.IsValid)
            {
                model.CartItems = cartItems;
                return View("Index", model);
            }

            // Reduce stock
            foreach (var item in cartItems)
            {
                if (item.Product != null)
                {
                    item.Product.Stock -= item.Quantity;
                    if (item.Product.Stock < 0) item.Product.Stock = 0;
                }
            }

            var order = new Order
            {
                UserId = userId,
                FullName = model.FullName,
                Email = model.Email,
                Address = model.Address,
                OrderDate = DateTime.Now,
                TotalAmount = cartItems.Sum(x => x.Product.Price * x.Quantity),
                OrderItems = cartItems.Select(c => new OrderItem
                {
                    ProductId = c.ProductId,
                    Quantity = c.Quantity,
                    Price = c.Product.Price
                }).ToList()
            };

            _context.Orders.Add(order);
            _context.CartItems.RemoveRange(cartItems);

            await _context.SaveChangesAsync();

            TempData["OrderId"] = order.Id;
            return RedirectToAction(nameof(Success));
        }

        public async Task<IActionResult> Success()
        {
            var orderId = TempData["OrderId"] as int?;
            if (orderId == null)
                return RedirectToAction("Index", "Home");

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            return View(order);
        }

        // My Orders page
        [Route("orders")]
        public async Task<IActionResult> MyOrders()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var orders = await _context.Orders
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.OrderDate)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .ToListAsync();

            return View(orders);
        }
    }
}