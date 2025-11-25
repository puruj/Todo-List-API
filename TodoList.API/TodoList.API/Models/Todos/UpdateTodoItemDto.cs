using System.ComponentModel.DataAnnotations;

namespace TodoList.API.Models.Todos
{
    public class UpdateTodoItemDto
    {
        [Required]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsCompleted { get; set; }
    }
}
