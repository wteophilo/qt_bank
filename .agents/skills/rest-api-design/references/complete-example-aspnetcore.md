# Complete Example: ASP.NET Core

```csharp
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase("UserDb"));
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

var usersApi = app.MapGroup("/api/v1/users");

#region Rotas da API

// 1. List users with pagination (GET /api/v1/users)
usersApi.MapGet("/", async (HttpContext context, AppDbContext db) =>
{
    try
    {
        // Tratamento de query strings com valores padrão (page=1, limit=20)
        int page = int.TryParse(context.Request.Query["page"], out var p) ? p : 1;
        int limit = int.TryParse(context.Request.Query["limit"], out var l) ? l : 20;
        int offset = (page - 1) * limit;

        var total = await db.Users.CountAsync();
        var users = await db.Users
            .Skip(offset)
            .Take(limit)
            .Select(u => new { u.Id, u.Email, u.FirstName, u.LastName }) // Projeção (attributes)
            .ToListAsync();

        return Results.Ok(new
        {
            data = users,
            pagination = new
            {
                page,
                limit,
                total,
                totalPages = (int)Math.Ceiling((double)total / limit)
            }
        });
    }
    catch (Exception)
    {
        return Results.Json(new
        {
            error = new { code = "INTERNAL_ERROR", message = "An error occurred while fetching users" }
        }, statusCode: 500);
    }
});

// 2. Get single user (GET /api/v1/users/:id)
usersApi.MapGet("/{id:int}", async (int id, AppDbContext db) =>
{
    try
    {
        var user = await db.Users.FindAsync(id);

        if (user is null)
        {
            return Results.Json(new
            {
                error = new { code = "NOT_FOUND", message = "User not found" }
            }, statusCode: 404);
        }

        return Results.Ok(new { data = user });
    }
    catch (Exception)
    {
        return Results.Json(new
        {
            error = new { code = "INTERNAL_ERROR", message = "An error occurred" }
        }, statusCode: 500);
    }
});

// 3. Create user (POST /api/v1/users)
usersApi.MapPost("/", async (User inputUser, AppDbContext db) =>
{
    try
    {
        var details = new List<object>();
        if (string.IsNullOrWhiteSpace(inputUser.Email)) 
            details.Add(new { field = "email", message = "Email is required" });
        if (string.IsNullOrWhiteSpace(inputUser.FirstName)) 
            details.Add(new { field = "firstName", message = "First name is required" });
        if (string.IsNullOrWhiteSpace(inputUser.LastName)) 
            details.Add(new { field = "lastName", message = "Last name is required" });

        if (details.Count > 0)
        {
            return Results.Json(new
            {
                error = new { code = "VALIDATION_ERROR", message = "Missing required fields", details }
            }, statusCode: 400);
        }

        db.Users.Add(inputUser);
        await db.SaveChangesAsync();

        return Results.Created($"/api/v1/users/{inputUser.Id}", new { data = inputUser });
    }
    catch (DbUpdateException ex)
    {
        if (ex.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) == true || 
            ex.Message.Contains("unique", StringComparison.OrdinalIgnoreCase))
        {
            return Results.Json(new
            {
                error = new { code = "CONFLICT", message = "Email already exists" }
            }, statusCode: 409);
        }

        return Results.Json(new
        {
            error = new { code = "INTERNAL_ERROR", message = "An error occurred" }
        }, statusCode: 500);
    }
    catch (Exception)
    {
        return Results.Json(new
        {
            error = new { code = "INTERNAL_ERROR", message = "An error occurred" }
        }, statusCode: 500);
    }
});

#endregion
app.Run();
```
