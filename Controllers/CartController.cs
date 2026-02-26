using eays.Data;
using eays.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eays.Controllers
{
    [Authorize]
    public class CartController : BaseController
    {
        public CartController(AppDbContext context,
                              UserManager<ApplicationUser> userManager)
            : base(context, userManager)
        {
        }

        // CART PAGE
        public IActionResult Index()
        {
            var userId = _userManager.GetUserId(User);

            var items = _context.CartItems
                .Include(x => x.Product)
                .Where(x => x.UserId == userId)
                .ToList();

            return View(items);
        }

        // ADD TO CART
        public IActionResult Add(int productId)
        {
            var userId = _userManager.GetUserId(User);

            var exists = _context.CartItems
                .FirstOrDefault(x => x.ProductId == productId && x.UserId == userId);

            if (exists == null)
            {
                var item = new CartItem
                {
                    ProductId = productId,
                    UserId = userId,
                    Quantity = 1
                };

                _context.CartItems.Add(item);
            }
            else
            {
                exists.Quantity++;
            }

            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        // REMOVE
        public IActionResult Remove(int id)
        {
            var item = _context.CartItems.Find(id);

            if (item != null)
            {
                _context.CartItems.Remove(item);
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }
    }
}