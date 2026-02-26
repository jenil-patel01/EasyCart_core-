using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace eays.Models
{
    public class ProfileViewModel
    {
        // User Info
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public string Address { get; set; } = "";

        // Stats
        public int TotalOrders { get; set; }
        public decimal TotalSpent { get; set; }
        public int WishlistCount { get; set; }
        public int CartItemsCount { get; set; }

        // Recent Orders from database
        public List<Order> RecentOrders { get; set; } = new();
    }
}