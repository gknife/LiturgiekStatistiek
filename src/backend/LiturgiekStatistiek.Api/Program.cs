using LiturgiekStatistiek.Infrastructure;
using LiturgiekStatistiek.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

// Authentication (skip in dev if disabled)
var disableAuth = builder.Configuration.GetValue<bool>("DisableAuthentication");
if (!disableAuth)
{
    builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration);
}
else
{
    builder.Services.AddAuthentication(LiturgiekStatistiek.Api.Auth.DevAuthHandler.SchemeName)
        .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, LiturgiekStatistiek.Api.Auth.DevAuthHandler>(
            LiturgiekStatistiek.Api.Auth.DevAuthHandler.SchemeName, _ => { });
    builder.Services.AddAuthorization();
}

// Infrastructure (EF Core, etc.)
var useInMemory = builder.Configuration.GetValue<bool>("UseInMemoryDatabase");
builder.Services.AddInfrastructure(builder.Configuration, useInMemory);

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? ["http://localhost:4200"])
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();

if (!useInMemory)
{
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<ApplicationDbContext>();
}
else
{
    builder.Services.AddHealthChecks();
}

var app = builder.Build();

// Apply migrations and seed data (all environments)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (useInMemory)
    {
        db.Database.EnsureCreated();
    }
    else
    {
        await db.Database.MigrateAsync();
    }
    await DataSeeder.SeedAsync(db, includeDemoData: app.Environment.IsDevelopment());
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
