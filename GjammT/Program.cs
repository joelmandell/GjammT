using GjammT.Auth;
using GjammT.Components;
using Microsoft.AspNetCore.Authentication.Cookies;
using Syncfusion.Blazor;

var builder = WebApplication.CreateBuilder(args);

var syncfusionKey = builder.Configuration["Syncfusion:Key"];
Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(syncfusionKey);

// Add services to the container.
builder.Services.AddSingleton<ILoginService, LoginService>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie();
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents().AddInteractiveWebAssemblyComponents();
builder.Services.AddSyncfusionBlazor();
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode();

app.Run();