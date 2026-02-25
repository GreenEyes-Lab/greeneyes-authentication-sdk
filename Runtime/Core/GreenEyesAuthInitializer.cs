using UnityEngine;

namespace GreenEyes.Auth
{
    internal static class GreenEyesAuthInitializer
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
#if UNITY_IOS || UNITY_ANDROID
            var provider = new AppleAuthProvider();

            // Create a hidden persistent GameObject for UnitySendMessage bridge
            var go = new GameObject(AppleAuthNativeBridge.GameObjectName);
            go.hideFlags = HideFlags.HideAndDontSave;
            Object.DontDestroyOnLoad(go);

            var bridge = go.AddComponent<AppleAuthNativeBridge>();
            bridge.Provider = provider;

            AuthManager.Instance.RegisterProvider(AuthProviderType.Apple, provider);
#endif
        }
    }
}
