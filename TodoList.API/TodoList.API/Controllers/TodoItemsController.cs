using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using TodoList.API.Models.Entities;
using TodoList.API.Models.Todos;

namespace TodoList.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("todos")] // will give you /todos exactly; change to "api/todos" if you prefer
    public class TodoItemsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TodoItemsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private Guid GetUserId()
        {
            // Default mapping: "sub" -> ClaimTypes.NameIdentifier
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                       ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub); // fallback if you later disable mapping

            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var guid))
            {
                throw new UnauthorizedAccessException("Invalid user id claim.");
            }

            return guid;
        }

        private static TodoItemDto MapToDto(TodoItem todo) => new()
        {
            Id = todo.Id,
            Title = todo.Title,
            Description = todo.Description,
            IsCompleted = todo.IsCompleted
        };

        // POST /todos
        [HttpPost]
        public async Task<ActionResult<TodoItemDto>> CreateTodo([FromBody] CreateTodoItemDto request)
        {
            var userId = GetUserId();

            var todo = new TodoItem
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = request.Title,
                Description = request.Description,
                IsCompleted = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.TodoItems.Add(todo);
            await _context.SaveChangesAsync();

            var dto = MapToDto(todo);

            // 201 Created + body with todo
            return CreatedAtAction(nameof(GetTodoById), new { id = todo.Id }, dto);
        }

        // GET /todos/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<TodoItemDto>> GetTodoById(Guid id)
        {
            var userId = GetUserId();

            var todo = await _context.TodoItems
                .Where(t => t.UserId == userId && t.Id == id)
                .FirstOrDefaultAsync();

            if (todo == null)
                return NotFound();

            return Ok(MapToDto(todo));
        }

        // PUT /todos/{id}
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<TodoItemDto>> UpdateTodo(Guid id, [FromBody] UpdateTodoItemDto request)
        {
            var userId = GetUserId();

            var todo = await _context.TodoItems
                .Where(t => t.UserId == userId && t.Id == id)
                .FirstOrDefaultAsync();

            if (todo == null)
                return NotFound();

            todo.Title = request.Title;
            todo.Description = request.Description;
            todo.IsCompleted = request.IsCompleted;
            todo.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(MapToDto(todo));
        }

        // DELETE /todos/{id}
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteTodo(Guid id)
        {
            var userId = GetUserId();

            var todo = await _context.TodoItems
                .Where(t => t.UserId == userId && t.Id == id)
                .FirstOrDefaultAsync();

            if (todo == null)
                return NotFound(); // or Forbid() if you want distinct "not your task" behaviour

            _context.TodoItems.Remove(todo);
            await _context.SaveChangesAsync();

            return NoContent(); // 204
        }

        // GET /todos?page=1&limit=10
        [HttpGet]
        public async Task<ActionResult<PagedResult<TodoItemDto>>> GetTodos(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10)
        {
            var userId = GetUserId();

            if (page <= 0) page = 1;
            if (limit <= 0) limit = 10;

            var query = _context.TodoItems
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt);

            var total = await query.CountAsync();

            var items = await query
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            var dtos = items.Select(MapToDto).ToList();

            var result = new PagedResult<TodoItemDto>
            {
                Data = dtos,
                Page = page,
                Limit = limit,
                Total = total
            };

            return Ok(result);
        }
    }
}
