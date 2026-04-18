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

        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression(@"^[0-9\-\+\s\(\)]{10,15}$", ErrorMessage = "Invalid phone number format.")]
        public string Phone { get; set; }

        [Required]
        public string Address { get; set; }

        [Required(ErrorMessage = "Please select a payment method.")]
        public string PaymentMethod { get; set; }

        public decimal Total =>
            CartItems?.Sum(x => x.Product.Price * x.Quantity) ?? 0;
    }
}