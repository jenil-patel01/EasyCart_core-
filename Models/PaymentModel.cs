using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace eays.Models
{
    public class RazorpayPaymentViewModel
    {
        public int OrderId { get; set; }
        public string RazorpayOrderId { get; set; } = "";
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "INR";
        public string RazorpayKey { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string CustomerEmail { get; set; } = "";
        public string Description { get; set; } = "";
    }
}