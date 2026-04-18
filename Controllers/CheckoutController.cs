using eays.Data;
using eays.Models;
using eays.Services;
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
        private readonly IInvoiceService _invoiceService;

        public CheckoutController(AppDbContext context,
                                  UserManager<ApplicationUser> userManager,
                                  IInvoiceService invoiceService)
        {
            _context = context;
            _userManager = userManager;
            _invoiceService = invoiceService;
        }

        public IActionResult Success(int? orderId)
        {
            if (orderId == null || orderId == 0)
                return RedirectToAction("Index", "Home");

            return View(orderId.Value);
        }

        public async Task<IActionResult> DownloadInvoice(int orderId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == user.Id);

            if (order == null)
                return NotFound();

            var pdfBytes = await _invoiceService.GenerateInvoicePdfAsync(order);
            
            return File(pdfBytes, "application/pdf", $"Invoice_{order.Id}_{DateTime.Now:yyyyMMdd}.pdf");
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
                Phone = user.PhoneNumber ?? "",
                Address = user.Address,
                PaymentMethod = "RAZORPAY" // Default Check
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
                PhoneNumber = model.Phone,
                Address = model.Address,
                OrderDate = DateTime.Now,
                TotalAmount = cartItems.Sum(x => x.Product.Price * x.Quantity),
                Status = "Pending",
                PaymentStatus = model.PaymentMethod == "COD" ? "Pending" : "Pending",
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

            if (model.PaymentMethod == "COD")
            {
                return RedirectToAction("Success", "Checkout", new { orderId = order.Id });
            }

            // Redirect to Razorpay payment page
            return RedirectToAction("Pay", "Payment", new { orderId = order.Id });
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