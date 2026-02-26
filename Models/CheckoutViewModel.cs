using System.ComponentModel.DataAnnotations;

namespace eays.Models
{
    public class CheckoutViewModel
    {
        public List<CartItem> CartItems { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string Address { get; set; }

        public decimal Total =>
            CartItems?.Sum(x => x.Product.Price * x.Quantity) ?? 0;
    }
}