using System.ComponentModel.DataAnnotations;

namespace TodoList.API.Models.Auth
{
    public class RegisterRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(64, MinimumLength = 8,
            ErrorMessage = "Password must be between 8 and 64 characters long.")]
        public string Password { get; set; } = string.Empty;
    }
}
