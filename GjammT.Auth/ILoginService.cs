using GjammT.Models.Auth;

namespace GjammT.Auth;

public interface ILoginService
{
    public bool UserNameSignIn(UserNameSigninRequest request)
    {
        return false;
    }  
}