using eays.Data;
using eays.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eays.Controllers
{
    public class ProductsController : BaseController
    {
        private readonly AppDbContext _context;

        public ProductsController(AppDbContext context,
                                  UserManager<ApplicationUser> userManager)
            : base(context, userManager)
        {
            _context = context;
        }

        // PRODUCT LIST
        public IActionResult Index()
        {
            var products = _context.Products
                .Include(p => p.Category)
                .ToList();
            return View(products);
        }

        // PRODUCT DETAILS
        public IActionResult Details(int id)
        {
            var product = _context.Products
                .Include(p => p.Category)
                .FirstOrDefault(x => x.Id == id);

            if (product == null)
                return NotFound();

            // Related products from same category (exclude current)
            var relatedProducts = new List<Product>();
            if (product.CategoryId != null)
            {
                relatedProducts = _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id)
                    .Take(4)
                    .ToList();
            }

            // If not enough related, fill with other products
            if (relatedProducts.Count < 4)
            {
                var ids = relatedProducts.Select(p => p.Id).Append(product.Id).ToList();
                var extra = _context.Products
                    .Include(p => p.Category)
                    .Where(p => !ids.Contains(p.Id))
                    .Take(4 - relatedProducts.Count)
                    .ToList();
                relatedProducts.AddRange(extra);
            }

            ViewBag.RelatedProducts = relatedProducts;

            return View(product);
        }
    }
}