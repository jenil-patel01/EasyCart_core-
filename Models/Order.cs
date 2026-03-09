using System.ComponentModel.DataAnnotations;

namespace eays.Models
{
    public class Order
    {
        public int Id { get; set; }

        public string UserId { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string Address { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;

        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = "Pending";

        // Razorpay Payment Fields
        public string? RazorpayOrderId { get; set; }
        public string? RazorpayPaymentId { get; set; }
        public string? PaymentStatus { get; set; }
        public DateTime? PaymentDate { get; set; }

        public List<OrderItem> OrderItems { get; set; }
    }
}