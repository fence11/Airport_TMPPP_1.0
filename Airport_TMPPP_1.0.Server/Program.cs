using Airport_TMPPP_1._0.Server.BusinessLogic.Interfaces;
using Airport_TMPPP_1._0.Server.BusinessLogic.Services;
using Airport_TMPPP_1._0.Server.Data.Context;
using Airport_TMPPP_1._0.Server.Data.Interfaces;
using Airport_TMPPP_1._0.Server.Data.Repositories;
using Airport_TMPPP_1._0.Server.Data.Seed;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Entity Framework Core with PostgreSQL database
// Following Dependency Inversion Principle (DIP) - using IDbContext abstraction
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<AirportDbContext>(options =>
    options.UseNpgsql(connectionString));

// Register IDbContext to use AirportDbContext implementation
builder.Services.AddScoped<IDbContext>(provider => 
    provider.GetRequiredService<AirportDbContext>());

// Register Unit of Work - scoped per HTTP request
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register business services - following Dependency Inversion Principle and Interface Segregation Principle
// Query services
builder.Services.AddScoped<IFlightQueryService, FlightService>();
builder.Services.AddScoped<IAirportQueryService, AirportService>();
builder.Services.AddScoped<IPassengerQueryService, PassengerService>();
// Command services
builder.Services.AddScoped<IFlightCommandService, FlightService>();
builder.Services.AddScoped<IAirportCommandService, AirportService>();
builder.Services.AddScoped<IPassengerCommandService, PassengerService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthorization();

app.MapControllers();

// Seed database on startup
using (var scope = app.Services.CreateScope())
{
    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
    try
    {
        await DatabaseSeeder.SeedAsync(unitOfWork);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

//app.MapFallbackToFile("/index.html");

app.Run();
