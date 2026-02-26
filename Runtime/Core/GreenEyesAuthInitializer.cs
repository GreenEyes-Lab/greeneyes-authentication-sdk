using UnityEngine;

namespace GreenEyes.Auth
{
    internal static class GreenEyesAuthInitializer
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
#if UNITY_IOS
            // iOS: auto-register. AuthenticationServices uses the app's Bundle ID directly.
            var provider = new AppleAuthProvider();

            var go = new GameObject(AppleAuthNativeBridge.GameObjectName);
            go.hideFlags = HideFlags.HideAndDontSave;
            Object.DontDestroyOnLoad(go);

            var bridge = go.AddComponent<AppleAuthNativeBridge>();
            bridge.Provider = provider;

            AuthManager.Instance.RegisterProvider(AuthProviderType.Apple, provider);

#elif UNITY_ANDROID
            // Android: client must register manually with AppleAuthAndroidConfig.
            // Web OAuth requires a Service ID and Redirect URL specific to each app.
            // Example:
            //   var config = new AppleAuthAndroidConfig("com.example.service", "https://example.com/apple/callback");
            //   var provider = new AppleAuthProvider(config);
            //   AuthManager.Instance.RegisterProvider(AuthProviderType.Apple, provider);
            //
            // The bridge GameObject is created here so UnitySendMessage works once registered.
            var go = new GameObject(AppleAuthNativeBridge.GameObjectName);
            go.hideFlags = HideFlags.HideAndDontSave;
            Object.DontDestroyOnLoad(go);
            go.AddComponent<AppleAuthNativeBridge>();
#endif
        }
    }
}
