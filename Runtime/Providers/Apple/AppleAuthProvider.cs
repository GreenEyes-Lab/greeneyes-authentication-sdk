using System;

namespace GreenEyes.Auth
{
    public sealed partial class AppleAuthProvider : IAuthProvider
    {
        private Action<AuthResult, AuthError> _pendingCallback;

        public void SignIn(Action<AuthResult, AuthError> callback)
        {
            if (_pendingCallback != null)
            {
                callback?.Invoke(null, new AuthError(
                    AuthErrorCode.Unknown,
                    "A sign-in is already in progress."));
                return;
            }

            _pendingCallback = callback;
            SignInInternal();
        }

        // Called by AppleAuthNativeBridge via UnitySendMessage
        internal void OnNativeSuccess(string jsonPayload)
        {
            var result = ParseResult(jsonPayload);
            var cb = _pendingCallback;
            _pendingCallback = null;
            cb?.Invoke(result, null);
        }

        // Called by AppleAuthNativeBridge via UnitySendMessage
        internal void OnNativeFailure(string errorPayload)
        {
            var error = ParseError(errorPayload);
            var cb = _pendingCallback;
            _pendingCallback = null;
            cb?.Invoke(null, error);
        }
    }
}
