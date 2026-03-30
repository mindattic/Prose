using StreetSamurai.Blazor.Components;
using StreetSamurai.Core.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddStreetSamuraiServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
// Eagerly load the typed canon database (auto-converts YAML/MD → JSON on first run)
var canonDb = app.Services.GetRequiredService<StreetSamurai.Core.Services.CanonDatabaseService>();
canonDb.EnsureLoaded();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
