using KitchenManagement.Data;
using KitchenManagement.DTOs;
using KitchenManagement.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Database Configuration (Render Support) ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

if (!string.IsNullOrEmpty(databaseUrl))
{
    // Parse Render's postgres:// URL to .NET connection string
    var databaseUri = new Uri(databaseUrl);
    var userInfo = databaseUri.UserInfo.Split(':');
    var builderDb = new NpgsqlConnectionStringBuilder
    {
        Host = databaseUri.Host,
        Port = databaseUri.Port,
        Username = userInfo[0],
        Password = userInfo[1],
        Database = databaseUri.LocalPath.TrimStart('/'),
        SslMode = SslMode.Require
    };
    connectionString = builderDb.ToString();
}
else
{
    // Fallback to local default if no env var (Development)
    if (string.IsNullOrEmpty(connectionString))
    {
        connectionString = "Host=localhost;Database=KitchenDb;Username=postgres;Password=admin";
    }
}

builder.Services.AddDbContext<KitchenContext>(options =>
    options.UseNpgsql(connectionString));

// --- 2. Add Services ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// --- 3. Configure Pipeline ---
// Enable Swagger in ALL environments (including Production/Render)
app.UseSwagger();
app.UseSwaggerUI();

// Ensure DB is created (Use Migrations in prod usually, but for simple startup: EnsureCreated)
// Note: In real production with migrations, run 'dotnet ef database update' in CD pipeline.
// For this setup, we'll try to apply migration if possible or just ensure created.
// To keep it simple and robust for this requests:
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<KitchenContext>();
    db.Database.EnsureCreated(); // Ensures tables exist
}

app.UseHttpsRedirection();

// --- 4. API Endpoints (Minimal API) ---

var group = app.MapGroup("/api/products").WithTags("Products");

// GET All
group.MapGet("/", async (KitchenContext db) =>
    await db.Products
        .Select(p => new ProductDTO
        {
            Id = p.Id,
            Name = p.Name,
            Category = p.Category,
            Quantity = p.Quantity,
            ExpirationDate = p.ExpirationDate
        })
        .ToListAsync());

// GET By Id
group.MapGet("/{id}", async (int id, KitchenContext db) =>
    await db.Products.FindAsync(id)
        is Product p
            ? Results.Ok(new ProductDTO
            {
                Id = p.Id,
                Name = p.Name,
                Category = p.Category,
                Quantity = p.Quantity,
                ExpirationDate = p.ExpirationDate
            })
            : Results.NotFound());

// POST
group.MapPost("/", async (ProductDTO dto, KitchenContext db) =>
{
    var product = new Product
    {
        Name = dto.Name,
        Category = dto.Category,
        Quantity = dto.Quantity,
        ExpirationDate = dto.ExpirationDate
    };

    db.Products.Add(product);
    await db.SaveChangesAsync();

    dto.Id = product.Id;
    return Results.Created($"/api/products/{product.Id}", dto);
});

// PUT
group.MapPut("/{id}", async (int id, ProductDTO dto, KitchenContext db) =>
{
    var product = await db.Products.FindAsync(id);
    if (product is null) return Results.NotFound();

    product.Name = dto.Name;
    product.Category = dto.Category;
    product.Quantity = dto.Quantity;
    product.ExpirationDate = dto.ExpirationDate;

    await db.SaveChangesAsync();
    return Results.NoContent();
});

// DELETE
group.MapDelete("/{id}", async (int id, KitchenContext db) =>
{
    if (await db.Products.FindAsync(id) is Product product)
    {
        db.Products.Remove(product);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }
    return Results.NotFound();
});

app.Run();
