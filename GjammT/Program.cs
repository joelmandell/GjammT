using System.Diagnostics.CodeAnalysis;
using GjammT.Auth;
using GjammT.Components;
using GjammT.SharedKernel;
using Microsoft.AspNetCore.Authentication.Cookies;
using Syncfusion.Blazor;

var builder = WebApplication.CreateBuilder(args);

var syncfusionKey = builder.Configuration["Syncfusion:Key"];
Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(syncfusionKey);

// Bind configuration directly to instance
var appSettings = new AppSettings 
{
    ProjectPath = builder.Configuration["AppSettings:ProjectPath"]
};

// Add services to the container.
builder.Services.AddSingleton<ILoginService, LoginService>();
builder.Services.AddSingleton<ProgramInfo>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie();
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();
builder.Services.AddRazorComponents(options => 
        options.DetailedErrors = builder.Environment.IsDevelopment())
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

app.Use(async (context, next) =>
{
    if(!context.Request.Path.Value?.Contains("blazor", StringComparison.InvariantCultureIgnoreCase) ?? false) {
        var ProgramInfo = context.RequestServices.GetRequiredService<ProgramInfo>();
        await ProgramInfo.LoadComponentAssembly("GjammT.CustomerRegister");
        await ProgramInfo.LoadComponentAssembly("GjammT.AccessSystem");
        await ProgramInfo.LoadComponentAssembly("GjammT.Booking");
    }
    
    // Call the next delegate/middleware in the pipeline.
    await next(context);
});

app.UseStaticFiles();

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapControllers();
var appBuilder = app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode();

ProgramInfo.SetRazorBuilder(appBuilder);
app.Run();