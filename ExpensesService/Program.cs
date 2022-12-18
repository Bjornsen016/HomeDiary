using ExpensesService.Data;
using ExpensesService.Model;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ExpensesDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration
            .GetConnectionString("AzureConnection")));
builder.Services.AddHttpClient<HttpClient>("AuthClient", client => client.BaseAddress = new Uri("https://authservice-app--jpjbmwb.agreeablefield-a48c6a06.eastus.azurecontainerapps.io/"));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidateIssuer = true,
            ValidateActor = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Missing key")))

        };
        options.SaveToken = true;
    });

builder.Services.AddAuthorization();

// Add services to the container.

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<ExpensesDbContext>();
    db.Database.Migrate();
}

app.UseAuthentication();
app.UseAuthorization();

// Configure the HTTP request pipeline.

app.MapPost("/Expense",
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
async (NewExpense? newExpense, ExpensesDbContext db, HttpContext http, IHttpClientFactory iHttpClientFactory) =>
    {
        if (newExpense is null) return Results.BadRequest("Please include correct data");

        var userId = http.User.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)
        ?.Value;

        var client = iHttpClientFactory.CreateClient("AuthClient");
        var exists = await client.GetAsync($"/user/{userId}");
        if (!exists.IsSuccessStatusCode) return Results.NotFound($"User with id: {userId} not found. Can't create a task.");

        var expense = new Expense()
        {
            Title = newExpense.Title,
            Description = newExpense.Description,
            Category = newExpense.Category,
            ExpenseDate = newExpense.ExpenseDate,
            WarrantyEndDate = newExpense.WarrantyEndDate,
            ExpenseImageUri = newExpense.ExpenseImageUri,
            UserId = userId,
            TaskId = newExpense.TaskId
        };

        try
        {
            db.Expenses.Add(expense);
            await db.SaveChangesAsync();
            return Results.Created($"/Expense/{expense.ExpenseId}",
                "Expense successfully added!");
        }
        catch (Exception ex)
        {
            return Results.Conflict($"Error: {ex.Message}" +
                                    "\n Failed to add expense, try again");
        }

    });


app.MapGet("/Expense/{ExpenseId}", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
async (Guid expenseId, ExpensesDbContext db, HttpContext http) =>
    {
        if (db.Expenses is null) return Results.NotFound("No expenses in database");

        var userId = http.User.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)
            ?.Value;

        var expense = await db.Expenses
            .FirstOrDefaultAsync(e => e.ExpenseId == expenseId && e.UserId == userId);

        if (expense is null)
            return Results.NotFound(
                "Expense not found, please check expense Id");

        return Results.Ok(expense);
    });


app.MapGet("/Expense", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
async (Guid expenseId, ExpensesDbContext db, HttpContext http) =>
    {
        if (db.Expenses is null) return Results.NotFound("No expenses in database");

        var userId = http.User.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)
            ?.Value;

        var expenseList = await db.Expenses
            .Where(e => e.UserId == userId).ToListAsync();

        return expenseList.IsNullOrEmpty() ? Results.NotFound("No expenses was found!") : Results.Ok(expenseList);
    });


app.MapGet("/Expense/Category/{category}", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
async (string? category, ExpensesDbContext db, HttpContext http) =>
    {
        if (category is null) return Results.BadRequest("Please include correct data");
        if (db.Expenses is null) return Results.NotFound("No expenses in database");

        var userId = http.User.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)
            ?.Value;

        var expenseList = await db.Expenses
            .Where(e => e.Category != null && e.Category.ToUpper().Equals(category.ToUpper()) && e.UserId == userId)
            .ToListAsync();

        if (expenseList.IsNullOrEmpty())
            return Results.NotFound(
                $"Expenses under {category} not found");

        return Results.Ok(expenseList);
    });

app.MapGet("/Expense/Category/", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
async (ExpensesDbContext db) =>
    {
        if (db.Expenses is null) return Results.NotFound("No expenses found in database");

        var categoryList =
            await db.Expenses.Select(e => e.Category).ToListAsync();

        if (categoryList.IsNullOrEmpty())
            return Results.NotFound(
                "Expenses under category not found");

        return Results.Ok(categoryList);
    });


app.MapPut("/Expanse/{expenseId}", //endpoint naming wrong, should be "expense"
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
async (Guid? expenseId, NewExpense? newExpense, ExpensesDbContext db, HttpContext http) =>
    {
        if (expenseId is null || newExpense is null) return Results.BadRequest("Please include correct data");

        if (db.Expenses is null) return Results.NotFound("No expenses in database");

        var expense = await db.Expenses.FindAsync(expenseId);
        if (expense is null) return Results.BadRequest(
            "Expense not found, please check expense Id");

        var userId = http.User.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)
            ?.Value;
        if (userId != expense.UserId) return Results.Unauthorized();

        expense.Title = newExpense.Title;
        expense.Description = newExpense.Description;
        expense.Category = newExpense.Category;
        expense.ExpenseDate = newExpense.ExpenseDate;
        expense.WarrantyEndDate = newExpense.WarrantyEndDate;
        expense.ExpenseImageUri = newExpense.ExpenseImageUri;
        expense.TaskId = newExpense.TaskId;
        expense.LastUpdatedAt = DateTime.UtcNow;

        return Results.Ok(expense.Title + " updated successfully!");

    });

app.MapDelete("/Expanse/{expenseId}", //endpoint naming wrong, should be "expense"
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
async (Guid? expenseId, ExpensesDbContext db, HttpContext http) =>
    {
        if (expenseId is null) return Results.BadRequest("Please include correct data");
        if (db.Expenses is null) return Results.NotFound("No expenses in database");

        var userId = http.User.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)
            ?.Value;

        var expense = await db.Expenses.FindAsync(expenseId);
        if (expense is null) return Results.BadRequest(
            $"{expenseId} not found in database, please check expense Id and try again");

        if (expense.UserId != userId) return Results.Unauthorized();

        db.Expenses.Remove(expense);
        await db.SaveChangesAsync();

        return Results.Ok(expense.Title + "deleted successfully!");

    });

app.Run();
