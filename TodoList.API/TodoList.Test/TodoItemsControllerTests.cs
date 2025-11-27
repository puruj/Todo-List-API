using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoList.API;
using TodoList.API.Controllers;
using TodoList.API.Models.Entities;
using TodoList.API.Models.Todos;

namespace TodoList.Test;

public class TodoItemsControllerTests
{
    private static ApplicationDbContext CreateContext()
    {
        // Isolated in-memory database per test to keep state clean.
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static TodoItemsController CreateController(ApplicationDbContext context, Guid userId)
    {
        var controller = new TodoItemsController(context);

        // Attach a fake authenticated user with the given userId.
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) },
                "TestAuth"))
        };

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        return controller;
    }

    [Fact]
    public async Task CreateTodo_ReturnsCreated_TodoForUser()
    {
        var userId = Guid.NewGuid();
        var context = CreateContext();
        var controller = CreateController(context, userId);

        var result = await controller.CreateTodo(new CreateTodoItemDto
        {
            Title = "Test Todo",
            Description = "Desc"
        });

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<TodoItemDto>(created.Value);

        Assert.Equal("Test Todo", dto.Title);

        var saved = await context.TodoItems.SingleAsync();
        Assert.Equal(userId, saved.UserId);
        Assert.Equal("Test Todo", saved.Title);
    }

    [Fact]
    public async Task GetTodoById_ReturnsNotFound_WhenMissing()
    {
        var userId = Guid.NewGuid();
        var context = CreateContext();
        var controller = CreateController(context, userId);

        var result = await controller.GetTodoById(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetTodoById_ReturnsTodo_WhenOwnedByUser()
    {
        var userId = Guid.NewGuid();
        var todoId = Guid.NewGuid();
        var context = CreateContext();
        context.TodoItems.Add(new TodoItem
        {
            Id = todoId,
            UserId = userId,
            Title = "Mine",
            Description = "Desc"
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context, userId);

        var result = await controller.GetTodoById(todoId);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<TodoItemDto>(ok.Value);
        Assert.Equal("Mine", dto.Title);
    }

    [Fact]
    public async Task UpdateTodo_ReturnsNotFound_WhenNotOwned()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var todoId = Guid.NewGuid();
        var context = CreateContext();
        context.TodoItems.Add(new TodoItem
        {
            Id = todoId,
            UserId = otherUserId,
            Title = "Other's task"
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context, userId);

        var result = await controller.UpdateTodo(todoId, new UpdateTodoItemDto
        {
            Title = "Updated",
            Description = "New",
            IsCompleted = true
        });

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task UpdateTodo_UpdatesFields_WhenOwned()
    {
        var userId = Guid.NewGuid();
        var todoId = Guid.NewGuid();
        var context = CreateContext();
        context.TodoItems.Add(new TodoItem
        {
            Id = todoId,
            UserId = userId,
            Title = "Old",
            Description = "Old Desc",
            IsCompleted = false
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context, userId);

        var result = await controller.UpdateTodo(todoId, new UpdateTodoItemDto
        {
            Title = "New Title",
            Description = "New Desc",
            IsCompleted = true
        });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<TodoItemDto>(ok.Value);
        Assert.Equal("New Title", dto.Title);
        Assert.True(dto.IsCompleted);

        var saved = await context.TodoItems.SingleAsync(t => t.Id == todoId);
        Assert.Equal("New Title", saved.Title);
        Assert.True(saved.IsCompleted);
    }

    [Fact]
    public async Task DeleteTodo_RemovesItem_WhenOwned()
    {
        var userId = Guid.NewGuid();
        var todoId = Guid.NewGuid();
        var context = CreateContext();
        context.TodoItems.Add(new TodoItem
        {
            Id = todoId,
            UserId = userId,
            Title = "To delete"
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context, userId);

        var result = await controller.DeleteTodo(todoId);

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(context.TodoItems);
    }

    [Fact]
    public async Task GetTodos_ReturnsPagedUserItems()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var context = CreateContext();

        context.TodoItems.AddRange(
            new TodoItem
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = "Newest",
                CreatedAt = DateTime.UtcNow.AddMinutes(2)
            },
            new TodoItem
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = "Older",
                CreatedAt = DateTime.UtcNow.AddMinutes(1)
            },
            new TodoItem
            {
                Id = Guid.NewGuid(),
                UserId = otherUserId,
                Title = "Someone else's"
            });

        await context.SaveChangesAsync();

        var controller = CreateController(context, userId);

        var result = await controller.GetTodos(page: 1, limit: 2);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var paged = Assert.IsType<PagedResult<TodoItemDto>>(ok.Value);

        Assert.Equal(2, paged.Total);
        Assert.Equal(2, paged.Data.Count());
        Assert.Equal("Newest", paged.Data.First().Title); // ordered desc by CreatedAt
    }
}
