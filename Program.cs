using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MaterDomus.Web;
using MaterDomus.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton<IProductCatalogService, ProductCatalogService>();
builder.Services.AddSingleton<ICatalogMetaService, CatalogMetaService>();
builder.Services.AddScoped<FavoritesService>();

await builder.Build().RunAsync();
