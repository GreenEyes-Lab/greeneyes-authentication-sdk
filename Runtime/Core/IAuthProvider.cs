using System;

namespace GreenEyes.Auth
{
    public interface IAuthProvider
    {
        void SignIn(Action<AuthResult, AuthError> callback);
    }
}
