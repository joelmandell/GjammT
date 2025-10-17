using GjammT.Auth.Models;
using Microsoft.Extensions.Options;

namespace GjammT.Auth;

public class LoginService : ILoginService
{
    private readonly GjAdminSettings _gjAdminSettings;

    public LoginService(IOptions<GjAdminSettings> gjAdminSettings)
    {
        _gjAdminSettings = gjAdminSettings.Value;
    }

    public async Task<bool> UserNameSignIn(UserNameSigninRequest request)
    {
        if (request.UserName == "idavall" && request.Password == "1337") return await Task.FromResult(true);
        return await Task.FromResult(false);
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