using System;
using UnityEngine;
using UnityEngine.UI;

namespace AuthSDKDemo
{
    internal sealed class LoginPanel : MonoBehaviour
    {
        [SerializeField] private Text _statusText;
        [SerializeField] private Button _signInButton;

        private Action _onSignIn;

        internal void Init(Action onSignIn)
        {
            _onSignIn = onSignIn;
            _signInButton.onClick.AddListener(OnSignInClicked);
        }

        internal void SetMessage(string message) => _statusText.text = message;

        internal void SetInteractable(bool interactable) =>
            _signInButton.interactable = interactable;

        private void OnSignInClicked() => _onSignIn?.Invoke();
    }
}
