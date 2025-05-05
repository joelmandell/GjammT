using System.Security.Claims;
using GjammT.Auth;
using GjammT.Auth.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace GjammT.Controllers;

[Microsoft.AspNetCore.Mvc.Route("[controller]/[action]")]
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
    public async Task<IActionResult> SignIn(UserNameSigninRequest loginModel, [FromServices] ILoginService loginService)
    {
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
    
    [HttpGet]
    public async Task<IActionResult> SignOut()
    {
        await HttpContext.SignOutAsync();
        
        return Redirect("/");
    }
}