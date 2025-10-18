using System.Security.Claims;
using GjammT.Auth;
using GjammT.Auth.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace GjammT.Controllers;

public class AuthController : ControllerBase
{
    public AuthController()
    {
        
    }

    public static string GetSubdomain(string host)
    {
        if (host.Contains("localhost") || host.Contains("127.0.0.1"))
            return null;

        var parts = host.Split('.');
    
        if (parts.Length > 2)
        {
            return parts[0]; // Returns "sub"
        }
    
        return null; // No subdomain found
    }
    
    [HttpPost]
    [Route("/auth/signin")]
    public async Task<IActionResult> SignIn([FromForm] UserNameSigninRequest loginModel, [FromServices] ILoginService loginService)
    {
        if(await loginService.GjAdminSignIn(loginModel)) {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, loginModel.UserName),
                new Claim(ClaimTypes.Role, "GjAdmin"),
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await (HttpContext?.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal) ?? Task.CompletedTask);
            return Redirect("/gjadmin");
        }
        var tenant = GetSubdomain(Request.Host.Host);

        ArgumentNullException.ThrowIfNull(tenant);
        if(await loginService.UserNameSignIn(loginModel)) {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, loginModel.UserName),
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await (HttpContext?.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal) ?? Task.CompletedTask);
            return Redirect("/");
        }

        return Unauthorized();
    }

    [HttpPost]
    [Route("/auth/login")]
    public async Task<IActionResult> Login(UserNameSigninRequest loginModel, [FromServices] ILoginService loginService)
    {
        
        return Unauthorized();
    }
    
    [HttpGet]
    public async Task<IActionResult> SignOut()
    {
        await HttpContext.SignOutAsync();
        
        return Redirect("/");
    }
}