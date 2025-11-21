using System.ComponentModel.DataAnnotations;

namespace TodoList.API.Models.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        [Required]
        public required string Name { get; set; } = string.Empty;       
        public required string Email { get; set; } = string.Empty;
        public byte[] PasswordHash { get; set; } = [];
        public byte[] PasswordSalt { get; set; } = [];
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<TodoItem> TodoItems { get; set; } = [];
    }
}
