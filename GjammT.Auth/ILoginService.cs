using GjammT.Models.Auth;

namespace GjammT.Auth;

public interface ILoginService
{
    public bool UserNameSignIn(UserNameSigninRequest request)
    {
        if (request.UserName == "idavall" && request.Password == "1337") return true;
        return false;
    }  
}