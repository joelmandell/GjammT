using System.Threading.RateLimiting;
using GjammT.Auth;
using GjammT.Components;
using GjammT.SharedKernel;
using Microsoft.AspNetCore.Authentication.Cookies;
using Syncfusion.Blazor;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

var syncfusionKey = builder.Configuration["Syncfusion:Key"];
Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(syncfusionKey);

var appSettings = new AppSettings 
{
    ProjectPath = builder.Configuration["AppSettings:ProjectPath"]
};

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests; // Or StatusCodes.Status503ServiceUnavailable

    options.AddConcurrencyLimiter(policyName: "concurrentPolicy", Soptions =>
    {
        Soptions.PermitLimit = 100; // Maximum number of concurrent requests allowed
        Soptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        Soptions.QueueLimit = 10;    // Maximum number of requests that can be queued
    });
});


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