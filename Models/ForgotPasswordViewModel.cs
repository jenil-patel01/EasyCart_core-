using System.ComponentModel.DataAnnotations;

namespace eays.Models
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = "";
    }
}
