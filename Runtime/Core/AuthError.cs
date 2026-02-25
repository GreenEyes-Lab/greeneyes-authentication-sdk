using System;

namespace GreenEyes.Auth
{
    public enum AuthErrorCode
    {
        Unknown = 0,
        UserCancelled = 1,
        NetworkError = 2,
        InvalidCredential = 3,
        NotSupported = 4,
        ProviderError = 5
    }

    public sealed class AuthError
    {
        public AuthErrorCode Code { get; }
        public string Message { get; }
        public Exception UnderlyingException { get; }

        public AuthError(AuthErrorCode code, string message, Exception underlyingException = null)
        {
            Code = code;
            Message = message;
            UnderlyingException = underlyingException;
        }

        public override string ToString() => $"AuthError[{Code}]: {Message}";
    }
}
