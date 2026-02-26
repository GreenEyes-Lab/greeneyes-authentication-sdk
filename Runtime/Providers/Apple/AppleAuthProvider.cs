using System;

namespace GreenEyes.Auth
{
    public sealed partial class AppleAuthProvider : IAuthProvider
    {
        private Action<AuthResult, AuthError> _pendingCallback;
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
