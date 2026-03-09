using eays.Data;
using eays.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace eays.Controllers
{
    public class BaseController : Controller
    {
        protected readonly AppDbContext _context;
        protected readonly UserManager<ApplicationUser> _userManager;

        public BaseController(AppDbContext context,
                              UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public override void OnActionExecuting(
            Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userId = _userManager.GetUserId(User);

                ViewBag.CartCount = _context.CartItems
                    .Count(x => x.UserId == userId);

                ViewBag.WishlistCount = _context.WishlistItems
                    .Count(x => x.UserId == userId);
            }
            else
            {
                ViewBag.CartCount = 0;
                ViewBag.WishlistCount = 0;
            }

            base.OnActionExecuting(context);
        }
    }
}