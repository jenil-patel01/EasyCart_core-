using System.Collections.Generic;
using eays.Models;

namespace eays.Data
{
    public static class UserStore
    {
        public static List<User> Users { get; set; } = new List<User>();
    }
}