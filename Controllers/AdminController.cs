using eays.Data;
using eays.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace eays.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ================= DASHBOARD (DYNAMIC) =================
        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;

            ViewBag.TotalOrders = await _context.Orders.CountAsync();
            ViewBag.PendingOrders = await _context.Orders.CountAsync(o => o.Status == "Pending");
            ViewBag.ProcessingOrders = await _context.Orders.CountAsync(o => o.Status == "Processing");
            ViewBag.ShippedOrders = await _context.Orders.CountAsync(o => o.Status == "Shipped");
            ViewBag.DeliveredOrders = await _context.Orders.CountAsync(o => o.Status == "Delivered");
            ViewBag.CancelledOrders = await _context.Orders.CountAsync(o => o.Status == "Cancelled");

            ViewBag.TodayIncome = await _context.Orders
                .Where(o => o.OrderDate.Date == today && o.Status != "Cancelled")
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            ViewBag.TotalRevenue = await _context.Orders
                .Where(o => o.Status != "Cancelled")
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            ViewBag.TotalProducts = await _context.Products.CountAsync();
            ViewBag.TotalUsers = _userManager.Users.Count();

            // Recent 5 orders for dashboard table
            ViewBag.RecentOrders = await _context.Orders
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .Include(o => o.OrderItems)
                .ToListAsync();

            return View();
        }

        // ================= ORDERS LIST =================
        public async Task<IActionResult> Orders(string status)
        {
            var query = _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.OrderDate)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && status != "All")
                query = query.Where(o => o.Status == status);

            var orders = await query.ToListAsync();

            ViewBag.CurrentFilter = status ?? "All";
            ViewBag.PendingCount = await _context.Orders.CountAsync(o => o.Status == "Pending");
            ViewBag.ProcessingCount = await _context.Orders.CountAsync(o => o.Status == "Processing");
            ViewBag.ShippedCount = await _context.Orders.CountAsync(o => o.Status == "Shipped");
            ViewBag.DeliveredCount = await _context.Orders.CountAsync(o => o.Status == "Delivered");
            ViewBag.CancelledCount = await _context.Orders.CountAsync(o => o.Status == "Cancelled");

            return View(orders);
        }

        // ================= ORDER DETAIL =================
        public async Task<IActionResult> OrderDetail(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }

        // ================= UPDATE ORDER STATUS =================
        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(int id, string status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            order.Status = status;
            await _context.SaveChangesAsync();

            TempData["OrderMessage"] = $"Order #{id} status updated to {status}";
            return RedirectToAction("OrderDetail", new { id });
        }

        // ================= DELETE ORDER =================
        [HttpPost]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order != null)
            {
                _context.OrderItems.RemoveRange(order.OrderItems);
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
            }

            TempData["OrderMessage"] = $"Order #{id} deleted successfully";
            return RedirectToAction("Orders");
        }

        // ================= CATEGORIES LIST =================
        public async Task<IActionResult> Categories()
        {
            var categories = await _context.Categories
                .Include(c => c.Products)
                .ToListAsync();
            return View(categories);
        }

        // ================= ADD CATEGORY =================
        [HttpGet]
        public IActionResult AddCategory()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddCategory(Category model)
        {
            if (ModelState.IsValid)
            {
                _context.Categories.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction("Categories");
            }
            return View(model);
        }

        // ================= EDIT CATEGORY =================
        [HttpGet]
        public async Task<IActionResult> EditCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();
            return View(category);
        }

        [HttpPost]
        public async Task<IActionResult> EditCategory(Category model)
        {
            var category = await _context.Categories.FindAsync(model.Id);
            if (category == null) return NotFound();

            if (ModelState.IsValid)
            {
                category.Name = model.Name;
                category.Icon = model.Icon;
                await _context.SaveChangesAsync();
                return RedirectToAction("Categories");
            }
            return View(model);
        }

        // ================= DELETE CATEGORY =================
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Categories");
        }

        // ================= PRODUCTS LIST (updated) =================
        public async Task<IActionResult> Products()
        {
            var products = await _context.Products.Include(p => p.Category).ToListAsync();
            return View(products);
        }

        // ================= ADD PRODUCT (updated) =================
        [HttpGet]
        public async Task<IActionResult> AddProduct()
        {
            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddProduct(Product model, IFormFile ImageFile)
        {
            if (ModelState.IsValid)
            {
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/products");

                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);

                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                    string filePath = Path.Combine(folder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(stream);
                    }

                    model.ImageUrl = "/images/products/" + fileName;
                }

                _context.Products.Add(model);
                await _context.SaveChangesAsync();

                return RedirectToAction("Products");
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(model);
        }

        // ================= EDIT PRODUCT (updated) =================
        public async Task<IActionResult> EditProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> EditProduct(Product model, IFormFile ImageFile)
        {
            var product = await _context.Products.FindAsync(model.Id);
            if (product == null) return NotFound();

            if (ImageFile != null && ImageFile.Length > 0)
            {
                string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/products");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                string filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }

                product.ImageUrl = "/images/products/" + fileName;
            }

            product.Name = model.Name;
            product.Price = model.Price;
            product.Stock = model.Stock;
            product.CategoryId = model.CategoryId;

            await _context.SaveChangesAsync();

            ViewBag.Categories = await _context.Categories.ToListAsync();
            return RedirectToAction("Products");
        }

        // ================= DELETE PRODUCT =================
        public IActionResult DeleteProduct(int id)
        {
            var product = _context.Products.Find(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                _context.SaveChanges();
            }

            return RedirectToAction("Products");
        }

        // ================= USERS LIST =================
        public IActionResult Users()
        {
            var users = _userManager.Users.ToList();
            return View(users);
        }

        // ================= DELETE USER =================
        public async Task<IActionResult> DeleteUser(string id)
        {
            if (id == null)
                return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            await _userManager.DeleteAsync(user);

            return RedirectToAction("Users");
        }

        // ================= LOGOUT =================
        public async Task<IActionResult> Logout()
        {
            // clear authentication cookie
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // clear session (optional)
            HttpContext.Session.Clear();

            return RedirectToAction("Login", "Account");
        }
    }
}