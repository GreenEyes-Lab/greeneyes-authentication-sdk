#if UNITY_IOS

using System;
using System.Runtime.InteropServices;

namespace GreenEyes.Auth
{
    public sealed partial class AppleAuthProvider
    {
        [DllImport("__Internal")]
        private static extern void _GreenEyes_Apple_SignIn(string bridgeObjectName);

        [DllImport("__Internal")]
        private static extern void _GreenEyes_Apple_GetCredentialState(string userId, string bridgeObjectName);

        private void SignInInternal()
        {
            _GreenEyes_Apple_SignIn(AppleAuthNativeBridge.GameObjectName);
        }

        private void SignOutInternal(Action<AuthError> callback)
        {
            callback?.Invoke(null);
        }

        private void GetCredentialStateInternal(string userId)
        {
            _GreenEyes_Apple_GetCredentialState(userId, AppleAuthNativeBridge.GameObjectName);
        }

        private AuthResult ParseResult(string json)
        {
            var data = AppleAuthJsonPayload.FromJson(json);
            return new AuthResult(
                identityToken: data.identityToken,
                authorizationCode: data.authorizationCode,
                userId: data.userId,
                email: string.IsNullOrEmpty(data.email) ? null : data.email,
                fullName: string.IsNullOrEmpty(data.fullName) ? null : data.fullName
            );
        }

        private AuthError ParseError(string errorPayload)
        {
            // ASAuthorizationError code mapping
            // 1001: canceled, 1002: failed, 1003: invalidResponse, 1004: notHandled
            if (int.TryParse(errorPayload, out int code))
            {
                return code switch
                {
                    1001 => new AuthError(AuthErrorCode.UserCancelled, "User cancelled Apple sign-in."),
                    1002 => new AuthError(AuthErrorCode.ProviderError, "Apple sign-in failed."),
                    1003 => new AuthError(AuthErrorCode.InvalidCredential, "Invalid response from Apple."),
                    _ => new AuthError(AuthErrorCode.Unknown, $"Apple sign-in error (code: {code}).")
                };
            }

            return new AuthError(AuthErrorCode.Unknown, $"Unknown error: {errorPayload}");
        }
    }
}

#endif
