using UnityEngine;

namespace GreenEyes.Auth
{
    // Receives UnitySendMessage callbacks from native plugins.
    // Attached to a hidden, persistent GameObject created by GreenEyesAuthInitializer.
    internal sealed class AppleAuthNativeBridge : MonoBehaviour
    {
        internal const string GameObjectName = "GreenEyes_AppleAuthBridge";

        internal AppleAuthProvider Provider { get; set; }

        // Called by native: UnitySendMessage(GameObjectName, "OnSuccess", json)
        private void OnSuccess(string json) => Provider?.OnNativeSuccess(json);

        // Called by native: UnitySendMessage(GameObjectName, "OnFailure", errorPayload)
        private void OnFailure(string errorPayload) => Provider?.OnNativeFailure(errorPayload);
    }
}
