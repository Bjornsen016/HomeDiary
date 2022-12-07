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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))

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
async (HomeTaskDto homeTask, HomeDbContext db, HttpContext http) =>
    {
        if (homeTask is null) return Results.BadRequest("Please include correct data");

        var userId = http.User.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)
            ?.Value;

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
async (Guid? taskId, HomeDbContext db) =>
    {
        if (taskId is null) return Results.BadRequest("Please include correct data");

        var task = await db.HomeTasks.FindAsync(taskId);
        if (task is null)
            return Results.NotFound(
                "Home task not found, please check task Id");

        return Results.Ok(task);
    });

app.MapPut("/HomeTask/{taskId}", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
async (Guid? taskId, HomeTaskDto? updatedHomeTask, HomeDbContext db) =>
    {
        if (taskId is null || updatedHomeTask is null) return Results.BadRequest("Please include correct data");

        var task = await db.HomeTasks.FindAsync(taskId);
        if (task is null)
            return Results.BadRequest(
                "Home task not found, please check task Id");

        task.Name = updatedHomeTask.Name;
        task.Category = updatedHomeTask.Category;
        task.Description = updatedHomeTask.Description;
        task.NotesList = updatedHomeTask.NotesList.Select(note => new Note() { Text = note }).ToList();
        task.UpdatedAt = DateTime.UtcNow;

        return Results.Ok(task.Name + " updated successfully");
    });

app.MapDelete("/HomeTask/{taskId}", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
async (Guid? taskId, HomeDbContext db) =>
    {
        if (taskId is null) return Results.BadRequest("Please include correct data");

        var task = await db.HomeTasks.FindAsync(taskId);
        if (task is null)
            return Results.NotFound($"Todo-id:{taskId} not found in database, check id and try again.");
        db.HomeTasks.Remove(task);
        await db.SaveChangesAsync();

        return Results.Ok(task.Name + ": deleted successfully");
    });



app.Run();
