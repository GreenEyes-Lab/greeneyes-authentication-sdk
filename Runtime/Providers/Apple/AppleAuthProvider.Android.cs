#if UNITY_ANDROID

namespace GreenEyes.Auth
{
    public sealed partial class AppleAuthProvider
    {
        private void SignInInternal()
        {
            // TODO: Android Web OAuth implementation
            // WebView를 통한 Apple OAuth 흐름 구현 필요
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
            return new AuthError(AuthErrorCode.Unknown, $"Android Apple sign-in error: {errorPayload}");
        }
    }
}

#endif
