using Data;
using Services.Implements;
using Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using Server.Middlewares;
using Microsoft.AspNetCore.HttpOverrides;

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
builder.Services.AddScoped<ITodoSpService, TodoSpService>();
builder.Services.AddScoped<ITodoEfService, TodoEfService>();
builder.Services.AddScoped<ITodoExtSpService, TodoExtSpService>();
builder.Services.AddScoped<ITodoManualConnDemoService, TodoManualConnDemoService>();

// SP executors
builder.Services.AddScoped<IStoredProcedureExecutor, StoredProcedureExecutor>(); // EF FromSql executor
builder.Services.AddScoped<IAdoStoredProcedureExecutor, AdoStoredProcedureExecutor>(); // ADO.NET executor

// HttpClient (nếu cần)
builder.Services.AddHttpClient();

// Forwarded headers (nếu chạy sau reverse proxy)
builder.Services.Configure<ForwardedHeadersOptions>(opts =>
{
    opts.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

// Caching for reverse DNS results
builder.Services.AddMemoryCache();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Enable WebAssembly debugging endpoints during development
    app.UseWebAssemblyDebugging();

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Global API exception handling
app.UseApiExceptionHandling();

// Áp dụng forwarded headers trước khi lấy RemoteIpAddress
app.UseForwardedHeaders();

// Custom middleware lấy IP + hostname
app.UseRequestClientInfo();

// Host Blazor WASM client (cần package Microsoft.AspNetCore.Components.WebAssembly.Server)
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();
app.MapControllers();

// Fallback cho client routing
app.MapFallbackToFile("index.html");

app.Run();