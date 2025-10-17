
using GjammT.Auth.Models;

namespace GjammT.Auth;

public interface ILoginService
{
    public Task<bool> UserNameSignIn(UserNameSigninRequest request);
    public Task<bool> GjAdminSignIn(UserNameSigninRequest request);
}