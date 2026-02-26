#if !UNITY_IOS && !UNITY_ANDROID

using System;

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

        private void SignOutInternal(Action<AuthError> callback)
        {
            callback?.Invoke(null);
        }

        private void GetCredentialStateInternal(string userId)
        {
            var cb = _pendingCredentialStateCallback;
            _pendingCredentialStateCallback = null;
            cb?.Invoke(default, new AuthError(
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
