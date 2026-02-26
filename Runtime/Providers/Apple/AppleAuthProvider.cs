using System;

namespace GreenEyes.Auth
{
    public sealed partial class AppleAuthProvider : IAuthProvider
    {
        private Action<AuthResult, AuthError> _pendingCallback;
        private Action<CredentialState, AuthError> _pendingCredentialStateCallback;
        private readonly AppleAuthAndroidConfig _androidConfig;

        /// <summary>iOS용 생성자. GreenEyesAuthInitializer가 자동으로 등록한다.</summary>
        public AppleAuthProvider() { }

        /// <summary>
        /// Android용 생성자. WebView OAuth에 필요한 설정을 주입한다.
        /// 클라이언트가 직접 RegisterProvider를 호출해 등록해야 한다.
        /// </summary>
        public AppleAuthProvider(AppleAuthAndroidConfig androidConfig)
        {
            _androidConfig = androidConfig ?? throw new ArgumentNullException(nameof(androidConfig));
        }

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

        public void SignOut(Action<AuthError> callback)
        {
            _pendingCallback = null;
            SignOutInternal(callback);
        }

        public void GetCredentialState(string userId, Action<CredentialState, AuthError> callback)
        {
            _pendingCredentialStateCallback = callback;
            GetCredentialStateInternal(userId);
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

        // Called by AppleAuthNativeBridge via UnitySendMessage
        internal void OnCredentialStateReceived(string statePayload)
        {
            var cb = _pendingCredentialStateCallback;
            _pendingCredentialStateCallback = null;

            CredentialState state;
            switch (statePayload)
            {
                case "authorized":  state = CredentialState.Authorized;  break;
                case "revoked":     state = CredentialState.Revoked;     break;
                case "notFound":    state = CredentialState.NotFound;    break;
                case "transferred": state = CredentialState.Transferred; break;
                default:
                    cb?.Invoke(default, new AuthError(
                        AuthErrorCode.Unknown,
                        $"Unknown credential state: {statePayload}"));
                    return;
            }

            cb?.Invoke(state, null);
        }

        // Called by AppleAuthNativeBridge via UnitySendMessage
        internal void OnCredentialStateFailure(string errorPayload)
        {
            var cb = _pendingCredentialStateCallback;
            _pendingCredentialStateCallback = null;
            cb?.Invoke(default, new AuthError(
                AuthErrorCode.Unknown,
                $"Failed to get credential state: {errorPayload}"));
        }
    }
}
