using AuthService;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<AuthDbContext>(opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    db.Database.Migrate();
}

app.MapPost("/register", async (User user, AuthDbContext db) =>
{
    if (user is null) return Results.BadRequest("Please send correct data");
    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Created("/login", "Registered user successfully!");
});

app.MapPost("/login", async (UserLogin userLogin, AuthDbContext db) =>
{
    var secretKey = builder.Configuration["Jwt:Key"];

    if (secretKey is null)
        return Results.StatusCode(500);

    User? user = await db.Users.FirstOrDefaultAsync(user => user.Email.Equals(userLogin.Email) && user.Password.Equals(userLogin.Password));

    if (user is null)
        return Results.NotFound("The username or password is not correct!");

    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Email, user.Email),
    };

    var token = new JwtSecurityToken
    (
        issuer: builder.Configuration["Jwt:Issuer"],
        audience: builder.Configuration["Jwt:Audience"],
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(30),
        notBefore: DateTime.UtcNow,
        signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)), SecurityAlgorithms.HmacSha256)
    );

    var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

    return Results.Ok(tokenString);
});

app.Run();