using System.Security.Claims;
using GjammT.Auth;
using GjammT.Models.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;

namespace GjammT.Controllers;

[Microsoft.AspNetCore.Mvc.Route("[controller]/[action]")]
public class AuthController : ControllerBase
{
    public AuthController()
    {
        
    }

    [HttpPost]
    public async Task<IActionResult> SignIn(UserNameSigninRequest loginModel, [FromServices] ILoginService loginService)
    {
        if(loginService.UserNameSignIn(loginModel)) {
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