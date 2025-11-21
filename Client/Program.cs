using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Client;
using Client.Services;
using Client.ViewModels;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

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

await builder.Build().RunAsync();
