using System.Text.Json;
using System.Text.Json.Serialization;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using SmartParking.Api.Auth;
using SmartParking.Api.Data;
using SmartParking.Api.Infrastructure;
using SmartParking.Api.Models;
using SmartParking.Api.Services;

// Smart Parking Slot Booking and Management System - ASP.NET Core 8 Web API + MySQL.
// C# port of the original Node.js/Express + SQLite backend. Same routes, same JSON.

SmartParking.Api.Infrastructure.EnvConfig.Apply();

var builder = WebApplication.CreateBuilder(args);

// Map snake_case columns (price_per_hour) to PascalCase properties (PricePerHour).
DefaultTypeMap.MatchNamesWithUnderscores = true;

builder.Services.AddSingleton<Db>();
builder.Services.AddSingleton<BookingReleaseService>();
builder.Services.AddHostedService<ExpiredBookingWorker>();

builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
    {
        // JSON uses snake_case (price_per_hour, start_time ...) to match the React frontend.
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        o.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
    });

// We validate by hand and return { "error": "..." } like the Node API, instead of
// ASP.NET's default ProblemDetails validation response.
builder.Services.Configure<ApiBehaviorOptions>(o => o.SuppressModelStateInvalidFilter = true);

builder.Services.AddJwtAuth(builder.Configuration);
builder.Services.AddSwaggerDocs();

// Allow the React dev server (and a phone on the same Wi-Fi) to call the API.
// CORS_ORIGINS=* or a comma-separated list, set in the project .env file.
var corsOrigins = builder.Configuration["CORS_ORIGINS"] ?? "*";
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
{
    p.AllowAnyHeader().AllowAnyMethod();
    if (string.IsNullOrWhiteSpace(corsOrigins) || corsOrigins.Trim() == "*")
        p.AllowAnyOrigin();
    else
        p.WithOrigins(corsOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}));

var app = builder.Build();

// Centralized error handler so unexpected errors never leak stack traces to clients.
app.UseExceptionHandler(errorApp => errorApp.Run(async ctx =>
{
    ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await ctx.Response.WriteAsJsonAsync(new ErrorResponse("Something went wrong on the server. Please try again."));
}));

if (app.Configuration.GetValue("Database:CreateAndSeedOnStartup", true))
{
    await DbInitializer.InitializeAsync(app.Services);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerDocs();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/health", () => Results.Json(new { status = "ok", service = "smart-parking-api" }));
app.MapControllers();
app.MapFallback(() => Results.Json(new ErrorResponse("Not found."), statusCode: StatusCodes.Status404NotFound));

app.Run();
