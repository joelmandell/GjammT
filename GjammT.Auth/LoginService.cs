using GjammT.Auth.Models;
using GjammT.SharedKernel;
using Microsoft.Extensions.Options;

namespace GjammT.Auth;

public class LoginService : ILoginService
{
    private readonly GjAdminSettings _gjAdminSettings;
    private readonly UserService _userService;

    public LoginService(IOptions<GjAdminSettings> gjAdminSettings, UserService userService)
    {
        _gjAdminSettings = gjAdminSettings.Value;
        _userService = userService;
    }

    public async Task<bool> UserNameSignIn(UserNameSigninRequest request)
    {
        // Try to find user by email (username is email in this system)
        var user = await _userService.GetUserByEmailAsync(request.UserName);
        
        if (user == null || !user.IsActive)
        {
            return false;
        }

        // Verify password using BCrypt
        return await _userService.VerifyPasswordAsync(user, request.Password);
    }

    public async Task<bool> GjAdminSignIn(UserNameSigninRequest request)
    {
        if (request.UserName == _gjAdminSettings.Username && request.Password == _gjAdminSettings.Password)
        {
            return await Task.FromResult(true);
        }
        return await Task.FromResult(false);
    }
}