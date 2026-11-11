using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay REST API",
        Version = "v1",
        Description = "API for RaceDay event organisers and participants. Log in through POST /api/auth/login; the browser session cookie is then used by protected endpoints."
    });
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) options.IncludeXmlComments(xmlPath);
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".RaceDay.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

builder.Services.AddAuthentication(SessionAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>(SessionAuthenticationHandler.SchemeName, _ => { });
builder.Services.AddAuthorization();
builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();

var connectionString = builder.Configuration.GetConnectionString("RaceDay")
    ?? "Server=(localdb)\\MSSQLLocalDB;Database=RaceDayApiDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
if (builder.Environment.IsEnvironment("Testing") || builder.Configuration["Database:Provider"] == "InMemory")
{
    var dbName = builder.Configuration["Database:Name"] ?? "RaceDayTests";
    builder.Services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(dbName));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
}

builder.Services.AddProblemDetails();

var app = builder.Build();

// Code-First schema creation. For a coursework project this creates the database from the EF model.
// For a production deployment, replace EnsureCreated with EF Core migrations.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
}

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "RaceDay API v1"));
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
