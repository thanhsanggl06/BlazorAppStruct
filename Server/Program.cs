using Data;
using Data.Dapper.Implementations;
using Data.Dapper.Interfaces;
using Data.Dapper.Infrastructure;
using Services.Implements;
using Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using Server.Middlewares;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;
using Serilog.Events;
using Shared.Entities.Dtos;

// Configure Serilog early
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting BlazorAppStruct application...");

    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog from appsettings.json
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithEnvironmentName()
        .Enrich.WithThreadId()
        .Enrich.WithProcessId()
        .Enrich.WithProperty("ApplicationName", "BlazorAppStruct")
        .Enrich.WithProperty("EnvironmentName", context.HostingEnvironment.EnvironmentName)
    );

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

    // Dapper Infrastructure - Enterprise Pattern
    // Step 1: Factory tạo connections (Singleton - stateless)
    builder.Services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
    
    // Step 2: UnitOfWork quản lý transaction (Scoped - per request)
    // DI tự động inject IDbConnectionFactory vào UnitOfWork constructor
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
    
    // Step 3: Repository (Scoped - per request)
    // DI tự động inject IUnitOfWork vào TodoRepository constructor
    builder.Services.AddScoped<ITodoRepository, TodoRepository>();

    // Step 4: Configure Dapper GLOBAL Column Mapping
    // ✅ Cách 1: GLOBAL mapping - Tự động áp dụng cho TẤT CẢ DTOs
    DapperMapperExtensions.RegisterGlobalSnakeCaseMapper();
    
    // ❌ Cách 2: Per-type mapping - Phải đăng ký từng DTO (KHÔNG DÙNG nếu đã dùng global)
    // DapperMapperExtensions.RegisterSnakeCaseMapper<TodoItemDto>();
    // DapperMapperExtensions.RegisterSnakeCaseMapper<UserDto>();

    // App services
    builder.Services.AddScoped<ITodoService, TodoService>();
    builder.Services.AddScoped<ITodoSpService, TodoSpService>();
    builder.Services.AddScoped<ITodoEfService, TodoEfService>();
    builder.Services.AddScoped<ITodoExtSpService, TodoExtSpService>();
    builder.Services.AddScoped<ITodoManualConnDemoService, TodoManualConnDemoService>();
    builder.Services.AddScoped<ITodoDapperService, TodoDapperService>();
    
    // Dapper services
    builder.Services.AddScoped<ITodoTransactionService, TodoTransactionService>();
    builder.Services.AddScoped<ITodoQueryService, TodoQueryService>();

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

    // Áp dụng forwarded headers trước khi lấy RemoteIpAddress
    app.UseForwardedHeaders();

    // Add correlation ID middleware early in pipeline (before Serilog request logging)
    app.UseCorrelationId();

    // Add Serilog request logging - AFTER correlation ID middleware
    app.UseSerilogRequestLogging(options =>
    {
        // Simpler message template
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} → {StatusCode} in {Elapsed:0.0000}ms";
        
        // Only log errors and warnings for failed requests
        options.GetLevel = (httpContext, elapsed, ex) =>
        {
            if (ex != null) return LogEventLevel.Error;
            if (httpContext.Response.StatusCode >= 500) return LogEventLevel.Error;
            if (httpContext.Response.StatusCode >= 400) return LogEventLevel.Warning;
            
            // Skip logging for static files and health checks
            var path = httpContext.Request.Path.Value ?? "";
            if (path.StartsWith("/_framework/") || 
                path.StartsWith("/css/") || 
                path.StartsWith("/js/") ||
                path.EndsWith(".wasm") ||
                path.EndsWith(".dat") ||
                path.EndsWith(".blat"))
            {
                return LogEventLevel.Debug; // Won't show in production
            }
            
            return LogEventLevel.Information;
        };
        
        // Enrich with client information
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            var request = httpContext.Request;
            var connection = httpContext.Connection;
            
            // Client information
            diagnosticContext.Set("RemoteIP", connection.RemoteIpAddress?.ToString() ?? "unknown");
            diagnosticContext.Set("UserAgent", request.Headers.UserAgent.ToString());
            diagnosticContext.Set("RequestHost", request.Host.Value);
            diagnosticContext.Set("RequestScheme", request.Scheme);
            
            // Additional context
            diagnosticContext.Set("ContentType", request.ContentType ?? "");
            diagnosticContext.Set("QueryString", request.QueryString.Value ?? "");
            
            // Response info
            diagnosticContext.Set("ResponseContentType", httpContext.Response.ContentType ?? "");
        };
    });

    // Global API exception handling
    app.UseApiExceptionHandling();

    // Custom middleware lấy IP + hostname
    app.UseRequestClientInfo();

    // Host Blazor WASM client (cần package Microsoft.AspNetCore.Components.WebAssembly.Server)
    app.UseBlazorFrameworkFiles();
    app.UseStaticFiles();

    app.UseRouting();
    app.MapControllers();

    // Fallback cho client routing
    app.MapFallbackToFile("index.html");

    Log.Information("Application started successfully on {Environment}", app.Environment.EnvironmentName);
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}