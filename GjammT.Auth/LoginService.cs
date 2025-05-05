using GjammT.Auth.Models;

namespace GjammT.Auth;

public class LoginService : ILoginService
{
    public async Task<bool> UserNameSignIn(UserNameSigninRequest request)
    {
        if (request.UserName == "idavall" && request.Password == "1337") return await Task.FromResult(true);
        return await Task.FromResult(false);
    }  
}