#if !UNITY_IOS && !UNITY_ANDROID

namespace GreenEyes.Auth
{
    public sealed partial class AppleAuthProvider
    {
        private void SignInInternal()
        {
            var cb = _pendingCallback;
            _pendingCallback = null;
            cb?.Invoke(null, new AuthError(
                AuthErrorCode.NotSupported,
                "Apple sign-in is only supported on iOS and Android."));
        }

        private AuthResult ParseResult(string json) => null;

        private AuthError ParseError(string errorPayload) => new AuthError(
            AuthErrorCode.NotSupported,
            "Apple sign-in is only supported on iOS and Android.");
    }
}

#endif
