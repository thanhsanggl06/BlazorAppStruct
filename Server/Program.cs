using Data;
using Services.Implements;
using Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;

var builder = WebApplication.CreateBuilder(args);

// API + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// EF Core DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connStr = builder.Configuration.GetConnectionString("DefaultConnection")
                 ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection");
    options.UseSqlServer(connStr);
});

// App services
builder.Services.AddScoped<ITodoService, TodoService>();

// HttpClient (nếu cần)
builder.Services.AddHttpClient();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Enable WebAssembly debugging endpoints during development
    app.UseWebAssemblyDebugging();

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Host Blazor WASM client (cần package Microsoft.AspNetCore.Components.WebAssembly.Server)
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();
app.MapControllers();

// Fallback cho client routing
app.MapFallbackToFile("index.html");

app.Run();