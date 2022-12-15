using HomeMaintenanceService.Data;
using HomeMaintenanceService.Model;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<HomeDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient<HttpClient>("AuthClient", client => client.BaseAddress = new Uri("http://authservice"));

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

//APP
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<HomeDbContext>();
    db.Database.Migrate();
}

app.UseAuthentication();
app.UseAuthorization();


// Configure the HTTP request pipeline.

app.MapPost("/HomeTask", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
async (NewHomeTask homeTask, HomeDbContext db, HttpContext http, IHttpClientFactory iHttpClientFactory) =>
    {
        if (homeTask is null) return Results.BadRequest("Please include correct data");

        var userId = http.User.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)
            ?.Value;

        var client = iHttpClientFactory.CreateClient("AuthClient");
        var exists = await client.GetAsync($"/user/{userId}");
        if (!exists.IsSuccessStatusCode) return Results.NotFound($"User with id: {userId} not found. Can't create a task.");

        var task = new HomeTask
        {
            Name = homeTask.Name,
            Category = homeTask.Category,
            Description = homeTask.Description,
            NotesList = homeTask.NotesList.Select(note => new Note() { Text = note }).ToList(),
            UserId = userId
        };

        try
        {
            db.HomeTasks.Add(task);
            await db.SaveChangesAsync();
            return Results.Created($"/HomeTask/{task.Id}", "Home task added successfully!");
        }
        catch (Exception ex)
        {
            return Results.Conflict($"Error: {ex.Message} \n" +
                                    "Failed to add task, try again");
        }
    });

app.MapGet("/HomeTask/{taskId}", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
async (Guid taskId, HomeDbContext db, HttpContext http) =>
    {
        var userId = http.User.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)
            ?.Value;

        var task = await db.HomeTasks.FirstOrDefaultAsync(t => t.Id == taskId && t.UserId == userId);
        if (task is null)
            return Results.NotFound(
                "Home task not found, please check task Id");

        return Results.Ok(task);
    });

app.MapGet("/HomeTask/", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
async (HomeDbContext db, HttpContext http) =>
    {
        var userId = http.User.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)
            ?.Value;

        var taskList = await db.HomeTasks.Where(t => t.UserId == userId).ToListAsync();
        return taskList.IsNullOrEmpty() ? Results.NotFound($"No tasks was found!") : Results.Ok(taskList);
    });

app.MapGet("/HomeTask/Category/{category}", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
async (string? category, HomeDbContext db, HttpContext http) =>
    {
        var userId = http.User.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)
            ?.Value;

        var taskList = await db.HomeTasks
            .Where(t => category != null && t.Category.ToUpper().Equals(category.ToUpper()) && t.UserId == userId)
            .ToListAsync();

        if (taskList.IsNullOrEmpty())
            return Results.NotFound(
                $"No task in {category} was found!");

        return Results.Ok(taskList);
    });

app.MapGet("/HomeTask/Category", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
async (HomeDbContext db) =>
    {
        var categoryList =
            await db.HomeTasks.Select(c => c.Category).ToListAsync();

        if (categoryList.IsNullOrEmpty())
            return Results.NotFound(
                $"No categories were found!");

        return Results.Ok(categoryList);
    });

app.MapPut("/HomeTask/{taskId}", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
async (Guid? taskId, NewHomeTask? newHomeTask, HomeDbContext db, HttpContext http) =>
    {
        if (taskId is null || newHomeTask is null) return Results.BadRequest("Please include correct data");

        var task = await db.HomeTasks.FindAsync(taskId);
        if (task is null)
            return Results.BadRequest(
                "Home task not found, please check task Id");

        var userId = http.User.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)
            ?.Value;
        if (userId != task.UserId) return Results.Unauthorized();

        task.Name = newHomeTask.Name;
        task.Category = newHomeTask.Category;
        task.Description = newHomeTask.Description;
        task.NotesList = newHomeTask.NotesList.Select(note => new Note() { Text = note }).ToList();
        task.UpdatedAt = DateTime.UtcNow;

        return Results.Ok(task.Name + " updated successfully");
    });

app.MapDelete("/HomeTask/{taskId}", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
async (Guid? taskId, HomeDbContext db, HttpContext http) =>
    {
        if (taskId is null) return Results.BadRequest("Please include correct data");

        var userId = http.User.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)
            ?.Value;

        var task = await db.HomeTasks.FindAsync(taskId);
        if (task is null)
            return Results.NotFound($"Todo-id:{taskId} not found in database, check id and try again.");

        if (task.UserId != userId) return Results.Unauthorized();

        db.HomeTasks.Remove(task);
        await db.SaveChangesAsync();

        return Results.Ok(task.Name + ": deleted successfully");
    });



app.Run();
