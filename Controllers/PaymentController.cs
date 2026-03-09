using eays.Data;
using eays.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Razorpay.Api;
using RazorpayOrder = Razorpay.Api.Order;



namespace eays.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _config;

        public PaymentController(AppDbContext context,
                                 UserManager<ApplicationUser> userManager,
                                 IConfiguration config)
        {
            _context = context;
            _userManager = userManager;
            _config = config;
        }

        // =============================
        // CREATE RAZORPAY ORDER + SHOW PAY PAGE
        // =============================
        [HttpGet]
        public async Task<IActionResult> Pay(int orderId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == user.Id);

            if (order == null) return NotFound();

            if (order.PaymentStatus == "Paid")
            {
                TempData["OrderId"] = order.Id;
                return RedirectToAction("Success", "Checkout");
            }

            string key = _config["Razorpay:Key"];
            string secret = _config["Razorpay:Secret"];

            RazorpayClient client = new RazorpayClient(key, secret);

            int amountInPaise = (int)(order.TotalAmount * 100);

            var options = new Dictionary<string, object>
            {
                { "amount", amountInPaise },
                { "currency", "INR" },
                { "receipt", $"order_{order.Id}" },
                { "payment_capture", 1 }
            };

            Razorpay.Api.Order razorpayOrder = client.Order.Create(options);
            string razorpayOrderId = razorpayOrder["id"].ToString();

            order.RazorpayOrderId = razorpayOrderId;
            await _context.SaveChangesAsync();

            var vm = new RazorpayPaymentViewModel
            {
                OrderId = order.Id,
                RazorpayOrderId = razorpayOrderId,
                Amount = order.TotalAmount,
                RazorpayKey = key,
                CustomerName = order.FullName,
                CustomerEmail = order.Email,
                Description = $"EasyCart Order #{order.Id}"
            };

            return View("Pay", vm);
        }

        // =============================
        // PAYMENT SUCCESS (VERIFY SIGNATURE)
        // =============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PaymentSuccess(
     int orderId,
     string razorpay_payment_id,
     string razorpay_order_id,
     string razorpay_signature)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == user.Id);

            if (order == null) return NotFound();

            try
            {
                var attributes = new Dictionary<string, string>
    {
        { "razorpay_order_id", razorpay_order_id },
        { "razorpay_payment_id", razorpay_payment_id },
        { "razorpay_signature", razorpay_signature }
    };

                Utils.verifyPaymentSignature(attributes);
            }
            catch (Exception)
            {
                TempData["PaymentError"] = "Payment verification failed.";
                return RedirectToAction("PaymentFailed", new { orderId = order.Id });
            }

            order.RazorpayPaymentId = razorpay_payment_id;
            order.PaymentStatus = "Paid";
            order.PaymentDate = DateTime.Now;
            order.Status = "Placed";

            await _context.SaveChangesAsync();

            TempData["OrderId"] = order.Id;
            return RedirectToAction("Success", "Checkout", new { orderId = order.Id }); // better redirect
        }
        // =============================
        // PAYMENT FAILED
        // =============================
        [HttpGet]
        public async Task<IActionResult> PaymentFailed(int orderId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == user.Id);

            if (order == null) return NotFound();

            return View(order);
        }
    }
}

