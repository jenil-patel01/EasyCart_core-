using System.ComponentModel.DataAnnotations;

namespace eays.Models
{
    public class ContactQuery
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = "";

        [Required]
        public string Email { get; set; } = "";

        [Required]
        public string Subject { get; set; } = "";

        [Required]
        public string Message { get; set; } = "";

        public DateTime SubmittedAt { get; set; } = DateTime.Now;

        public bool IsRead { get; set; } = false;
    }
}
