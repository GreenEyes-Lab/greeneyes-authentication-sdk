#if UNITY_ANDROID

using System;
using UnityEngine;

namespace GreenEyes.Auth
{
    public sealed partial class AppleAuthProvider
    {
        private void SignInInternal()
        {
            if (_androidConfig == null)
            {
                var cb = _pendingCallback;
                _pendingCallback = null;
                cb?.Invoke(null, new AuthError(
                    AuthErrorCode.NotSupported,
                    "AppleAuthAndroidConfig is required on Android. " +
                    "Register AppleAuthProvider with AppleAuthAndroidConfig instead of using the default constructor."));
                return;
            }

            var authUrl = BuildAuthUrl();

            // Link this provider to the bridge (created by GreenEyesAuthInitializer)
            var bridgeGo = GameObject.Find(AppleAuthNativeBridge.GameObjectName);
            if (bridgeGo != null)
            {
                var bridge = bridgeGo.GetComponent<AppleAuthNativeBridge>();
                if (bridge != null && bridge.Provider == null)
                    bridge.Provider = this;
            }

            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            using var activityClass = new AndroidJavaClass("com.greeneyes.auth.AppleAuthWebViewActivity");
            using var intent = new AndroidJavaObject(
                "android.content.Intent",
                activity,
                activityClass.GetStatic<AndroidJavaObject>("class"));

            intent.Call<AndroidJavaObject>("putExtra", "GREENEYES_AUTH_URL", authUrl);
            intent.Call<AndroidJavaObject>("putExtra", "GREENEYES_REDIRECT_URL", _androidConfig.RedirectUrl);
            intent.Call<AndroidJavaObject>("putExtra", "GREENEYES_CALLBACK_OBJECT", AppleAuthNativeBridge.GameObjectName);

            activity.Call("startActivity", intent);
        }

        private string BuildAuthUrl()
        {
            var encodedRedirect = Uri.EscapeDataString(_androidConfig.RedirectUrl);
            return "https://appleid.apple.com/auth/authorize" +
                   $"?client_id={_androidConfig.ServiceId}" +
                   $"&redirect_uri={encodedRedirect}" +
                   "&response_type=code%20id_token" +
                   "&scope=name%20email" +
                   "&response_mode=fragment";
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
            return errorPayload switch
            {
                "cancelled" => new AuthError(AuthErrorCode.UserCancelled, "User cancelled Apple sign-in."),
                "network"   => new AuthError(AuthErrorCode.NetworkError, "Network error during Apple sign-in."),
                _           => new AuthError(AuthErrorCode.Unknown, $"Apple sign-in error: {errorPayload}")
            };
        }
    }
}

#endif
