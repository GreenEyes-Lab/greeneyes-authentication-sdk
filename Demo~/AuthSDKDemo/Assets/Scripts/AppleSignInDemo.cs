using UnityEngine;
using UnityEngine.UI;
using GreenEyes.Auth;

public class AppleSignInDemo : MonoBehaviour
{
    [SerializeField] private Button _signInButton;
    [SerializeField] private Text _statusText;

    private void Awake()
    {
        _signInButton.onClick.AddListener(OnSignInButtonClicked);
        _statusText.text = "Press the button to sign in with Apple.";
    }

    private void OnSignInButtonClicked()
    {
        _signInButton.interactable = false;
        _statusText.text = "Signing in...";
        AuthManager.Instance.SignIn(AuthProviderType.Apple, OnSignInComplete);
    }

    private void OnSignInComplete(AuthResult result, AuthError error)
    {
        _signInButton.interactable = true;

        if (error != null)
        {
            _statusText.text = $"Sign-in failed:\n{error}";
            Debug.LogError($"[AuthSDKDemo] {error}");
            return;
        }

        _statusText.text =
            $"Sign-in success!\n\n" +
            $"UserId: {result.UserId}\n" +
            $"Email: {result.Email ?? "(not provided)"}\n" +
            $"FullName: {result.FullName ?? "(not provided)"}";

        Debug.Log($"[AuthSDKDemo] IdentityToken: {result.IdentityToken}");
        Debug.Log($"[AuthSDKDemo] AuthorizationCode: {result.AuthorizationCode}");
    }
}
