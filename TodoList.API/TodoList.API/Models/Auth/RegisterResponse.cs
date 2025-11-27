using System.ComponentModel.DataAnnotations;

namespace TodoList.API.Models.Auth
{
    public class RegisterResponse
    {
        [Required]
        public AuthUserDto User { get; set; } = new();

        public DateTime CreatedAt { get; set; }
    }
}
