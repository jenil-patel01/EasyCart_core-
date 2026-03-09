using System.Diagnostics;
using eays.Models;
using eays.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace eays.Controllers
{
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(
            ILogger<HomeController> logger,
            AppDbContext context,
            UserManager<ApplicationUser> userManager
        ) : base(context, userManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            var products = _context.Products
                                   .Include(p => p.Category)
                                   .OrderByDescending(p => p.Id)
                                   .Take(8)
                                   .ToList();

            ViewBag.WishlistProductIds = GetWishlistProductIds();

            return View(products);
        }

        public IActionResult About()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Contact()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(ContactViewModel model)
        {
            if (ModelState.IsValid)
            {
                var query = new ContactQuery
                {
                    Name = model.Name,
                    Email = model.Email,
                    Subject = model.Subject,
                    Message = model.Message,
                    SubmittedAt = DateTime.Now
                };

                _context.ContactQueries.Add(query);
                await _context.SaveChangesAsync();

                TempData["ContactSuccess"] =
                    "Thank you for reaching out! We'll get back to you soon.";

                return RedirectToAction("Contact");
            }

            return View(model);
        }

        public async Task<IActionResult> Products(string search, int? categoryId)
        {
            var query = _context.Products
                                .Include(p => p.Category)
                                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.Name.Contains(search));

            if (categoryId.HasValue && categoryId > 0)
                query = query.Where(p => p.CategoryId == categoryId);

            var products = await query
                                .OrderByDescending(p => p.Id)
                                .ToListAsync();

            var categories = await _context.Categories.ToListAsync();

            ViewBag.Categories = categories;
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentCategory = categoryId;
            ViewBag.WishlistProductIds = GetWishlistProductIds();

            return View(products);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0,
                       Location = ResponseCacheLocation.None,
                       NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id
                           ?? HttpContext.TraceIdentifier
            });
        }

        private List<int> GetWishlistProductIds()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = _userManager.GetUserId(User);

                return _context.WishlistItems
                               .Where(w => w.UserId == userId)
                               .Select(w => w.ProductId)
                               .ToList();
            }

            return new List<int>();
        }
    }
}