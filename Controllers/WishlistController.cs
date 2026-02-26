using eays.Data;
using eays.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eays.Controllers
{
    [Authorize]
    public class WishlistController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public WishlistController(AppDbContext context,
                                  UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // SHOW WISHLIST
        public IActionResult Index()
        {
            var userId = _userManager.GetUserId(User);

            var items = _context.WishlistItems
                .Include(x => x.Product)   // product data load
                .Where(x => x.UserId == userId)
                .ToList();

            return View(items);
        }

        // ADD TO WISHLIST
        public IActionResult Add(int productId)
        {
            var userId = _userManager.GetUserId(User);

            var exists = _context.WishlistItems
                .Any(x => x.ProductId == productId && x.UserId == userId);

            if (!exists)
            {
                var item = new WishlistItem
                {
                    ProductId = productId,
                    UserId = userId
                };

                _context.WishlistItems.Add(item);
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        // REMOVE FROM WISHLIST
        public IActionResult Remove(int id)
        {
            var item = _context.WishlistItems.Find(id);

            if (item != null)
            {
                _context.WishlistItems.Remove(item);
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }
    }
}