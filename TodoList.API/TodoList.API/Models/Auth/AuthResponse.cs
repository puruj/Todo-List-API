using System.ComponentModel.DataAnnotations;

namespace TodoList.API.Models.Auth
{
    public class AuthResponse
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }

        [Required]
        public AuthUserDto User { get; set; } = new();
    }

    public class AuthUserDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
