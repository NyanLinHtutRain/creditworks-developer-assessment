using Microsoft.EntityFrameworkCore;
using VehicleManager.Api.Endpoints;
using VehicleManager.Api.ErrorHandling;
using VehicleManager.Api.Services;
using VehicleManager.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<VehicleService>();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Development only: a reviewer who runs the app gets a migrated, seeded database.
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
}

app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .WithName("GetHealth")
    .WithOpenApi();

app.MapCategoryEndpoints();
app.MapManufacturerEndpoints();
app.MapVehicleEndpoints();

app.Run();

// Lets the integration tests start the app in memory.
public partial class Program { }
