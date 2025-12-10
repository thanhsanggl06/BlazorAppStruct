using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Client;
using Client.Services;
using Client.ViewModels;
using System.Globalization;
using Microsoft.JSInterop;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Localization
builder.Services.AddLocalization();

// Api infrastructure
builder.Services.AddScoped<IApiClient, ApiClient>();
builder.Services.AddScoped<ITodoApi, TodoApi>();
builder.Services.AddScoped<TodosViewModel>();

// SP demo services
builder.Services.AddScoped<ITodoSpApi, TodoSpApi>();
builder.Services.AddScoped<TodoSpViewModel>();

// EF demo services
builder.Services.AddScoped<ITodoEfApi, TodoEfApi>();
builder.Services.AddScoped<TodoEfViewModel>();

// EFExtensions SP demo services
builder.Services.AddScoped<ITodoExtSpApi, TodoExtSpApi>();
builder.Services.AddScoped<TodoExtSpViewModel>();

var host = builder.Build();

// Initialize culture from localStorage before running the app
var js = host.Services.GetRequiredService<IJSRuntime>();
var savedCulture = await js.InvokeAsync<string>("blazorCulture.get");
if (!string.IsNullOrWhiteSpace(savedCulture))
{
    try
    {
        var culture = new CultureInfo(savedCulture);
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }
    catch
    {
        // fallback to default if invalid
    }
}

await host.RunAsync();
