using DotNetEnv;
using StudentAPI.Data;
using Microsoft.EntityFrameworkCore;

// Load environment variables from .env, supporting both src/ and repo root
// Env.Load();
Env.Load("../.env");

var builder = WebApplication.CreateBuilder(args);

// Build connection string from environment variables
var connectionString = $"Server={Environment.GetEnvironmentVariable("DB_SERVER")};" +
                       $"Database={Environment.GetEnvironmentVariable("DB_NAME")};" +
                       $"User Id={Environment.GetEnvironmentVariable("DB_USER")};" +
                       $"Password={Environment.GetEnvironmentVariable("DB_PASSWORD")};" +
                       "TrustServerCertificate=True;";

// Override the configuration value
builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionString;

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Log a one-time DB connectivity check at startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        if (await db.Database.CanConnectAsync())
        {
            app.Logger.LogInformation("Database connection OK.");
        }
        else
        {
            app.Logger.LogWarning("Database connection FAILED.");
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Database connectivity check failed.");
    }
}

app.MapControllers();

app.Run();

