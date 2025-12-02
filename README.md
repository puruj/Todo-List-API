# Todo List API

ASP.NET Core 8 implementation of the roadmap.sh [Todo List API](https://roadmap.sh/projects/todo-list-api) challenge. It covers user auth, token-based access, CRUD for todos, pagination, and basic validation/error handling.

## Features
- JWT-based registration/login and protected todo endpoints
- CRUD for personal todo items with soft validation rules
- Pagination on list endpoint (`page`, `limit`)
- Entity Framework Core + SQL Server persistence with migrations
- Swagger UI with JWT bearer support for quick manual testing

## Tech Stack
- .NET 8, ASP.NET Core Web API
- EF Core 8 + SQL Server
- JWT Bearer auth
- Swashbuckle/Swagger for API docs
- xUnit tests in `TodoList.API/TodoList.Test`

## Getting Started
1. Install .NET 8 SDK and have a SQL Server instance available.
2. Set your connection string in `TodoList.API/appsettings.json` (key: `ConnectionStrings:DefaultConnection`) and `Jwt` settings (`Key`, `Issuer`, `Audience`, `ExpiresMinutes`).
3. Apply database migrations:
   ```bash
   dotnet ef database update --project TodoList.API/TodoList.API
   ```
4. Run the API:
   ```bash
   dotnet run --project TodoList.API/TodoList.API
   ```
5. Browse Swagger at `https://localhost:5001/swagger` (or the port shown in the console). Use the `Authorize` button with `Bearer <token>` after logging in.

## API Surface
### Auth
- `POST /api/Auth/register` — create a user.
- `POST /api/Auth/login` — exchange credentials for a JWT.

### Todos (Bearer token required)
- `POST /todos` — create a todo.
- `GET /todos` — list todos with pagination (`page`, `limit`).
- `GET /todos/{id}` — fetch a single todo.
- `PUT /todos/{id}` — update title/description/completed flag.
- `DELETE /todos/{id}` — remove a todo.

## Testing
Run all tests:
```bash
dotnet test
```
