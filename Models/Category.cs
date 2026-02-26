using System.ComponentModel.DataAnnotations;

namespace eays.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = "";

        public string? Icon { get; set; }

        public List<Product> Products { get; set; } = new();
    }
}
