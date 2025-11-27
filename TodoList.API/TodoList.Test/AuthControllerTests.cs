using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TodoList.API;
using TodoList.API.Controllers;
using TodoList.API.Helpers;
using TodoList.API.Models.Auth;
using TodoList.API.Models.Entities;

namespace TodoList.Test;

public class AuthControllerTests
{
    private static ApplicationDbContext CreateContext()
    {
        // Isolated in-memory database per test to avoid cross-test pollution.
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static IConfiguration BuildConfig()
    {
        // Minimal JWT settings so the controller can issue tokens in tests.
        var settings = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "super-secret-test-key-1234567890",
            ["Jwt:Issuer"] = "test-issuer",
            ["Jwt:Audience"] = "test-audience",
            ["Jwt:ExpiresMinutes"] = "60"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }

    [Fact]
    public async Task Register_ReturnsBadRequest_WhenEmailAlreadyExists()
    {
        var context = CreateContext();
        context.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Name = "Existing User",
            Email = "user@example.com"
        });
        await context.SaveChangesAsync();

        var controller = new AuthController(context, BuildConfig());

        var result = await controller.Register(new RegisterRequest
        {
            Name = "New User",
            Email = "user@example.com",
            Password = "Password123!"
        });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Register_CreatesUser_AndReturnsResponse()
    {
        var context = CreateContext();
        var controller = new AuthController(context, BuildConfig());

        var result = await controller.Register(new RegisterRequest
        {
            Name = "New User",
            Email = "new@example.com",
            Password = "Password123!"
        });

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<RegisterResponse>(okResult.Value);

        Assert.Equal("New User", response.User.Name);
        Assert.Equal("new@example.com", response.User.Email);
        Assert.NotEqual(default, response.CreatedAt);

        var savedUser = await context.Users.SingleAsync(u => u.Email == "new@example.com");
        Assert.NotEmpty(savedUser.PasswordHash);
        Assert.NotEmpty(savedUser.PasswordSalt);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenPasswordIsWrong()
    {
        var context = CreateContext();
        PasswordHelper.CreatePasswordHash("CorrectPassword123", out var hash, out var salt);
        context.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Name = "Test User",
            Email = "login@example.com",
            PasswordHash = hash,
            PasswordSalt = salt
        });
        await context.SaveChangesAsync();

        var controller = new AuthController(context, BuildConfig());

        var result = await controller.Login(new LoginRequest
        {
            Email = "login@example.com",
            Password = "WrongPassword!"
        });

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task Login_ReturnsToken_WithExpectedClaims_WhenCredentialsValid()
    {
        var userId = Guid.NewGuid();
        var context = CreateContext();
        PasswordHelper.CreatePasswordHash("CorrectPassword123", out var hash, out var salt);
        context.Users.Add(new User
        {
            Id = userId,
            Name = "Test User",
            Email = "login@example.com",
            PasswordHash = hash,
            PasswordSalt = salt
        });
        await context.SaveChangesAsync();

        var controller = new AuthController(context, BuildConfig());

        var result = await controller.Login(new LoginRequest
        {
            Email = "login@example.com",
            Password = "CorrectPassword123"
        });

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AuthResponse>(okResult.Value);

        Assert.False(string.IsNullOrWhiteSpace(response.Token));
        Assert.True(response.ExpiresAt > DateTime.UtcNow);
        Assert.Equal(userId, response.User.Id);
        Assert.Equal("login@example.com", response.User.Email);

        // Decode the JWT to assert claim contents, not just the shape.
        var token = new JwtSecurityTokenHandler().ReadJwtToken(response.Token);
        Assert.Equal(userId.ToString(), token.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal("login@example.com", token.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Email).Value);
    }
}
