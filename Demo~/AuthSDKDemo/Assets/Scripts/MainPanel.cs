using System;
using UnityEngine;
using UnityEngine.UI;

namespace AuthSDKDemo
{
    internal sealed class MainPanel : MonoBehaviour
    {
        [SerializeField] private Text _welcomeText;
        [SerializeField] private Text _userInfoText;
        [SerializeField] private Button _credentialStateButton;
        [SerializeField] private Text _credentialStateText;
        [SerializeField] private Button _signOutButton;
        [SerializeField] private Button _deleteAccountButton;
        [SerializeField] private GameObject _confirmDialog;
        [SerializeField] private Text _confirmDialogText;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;

        private Action _onSignOut;
        private Action _onCheckCredentialState;
        private Action _onDeleteAccount;

        internal void Init(
            Action onSignOut,
            Action onCheckCredentialState,
            Action onDeleteAccount)
        {
            _onSignOut = onSignOut;
            _onCheckCredentialState = onCheckCredentialState;
            _onDeleteAccount = onDeleteAccount;

            _signOutButton.onClick.AddListener(OnSignOutClicked);
            _credentialStateButton.onClick.AddListener(OnCheckCredentialStateClicked);
            _deleteAccountButton.onClick.AddListener(OnDeleteAccountButtonClicked);
            _confirmButton.onClick.AddListener(OnDeleteAccountConfirmed);
            _cancelButton.onClick.AddListener(OnDeleteAccountCancelled);

            _confirmDialog.SetActive(false);
            _credentialStateText.text = string.Empty;
        }

        internal void Setup(string userId, string email, string fullName, bool isAutoLogin)
        {
            _credentialStateText.text = string.Empty;
            _confirmDialog.SetActive(false);
            _deleteAccountButton.interactable = true;

            if (isAutoLogin)
            {
                _welcomeText.text = "자동 로그인되었습니다.";
            }
            else if (!string.IsNullOrEmpty(fullName))
            {
                _welcomeText.text = $"안녕하세요, {fullName}님!";
            }
            else
            {
                _welcomeText.text = "반갑습니다!";
            }

            var info = $"UserId:\n{userId}";
            if (!string.IsNullOrEmpty(email))
                info += $"\n\nEmail:\n{email}";
            if (!string.IsNullOrEmpty(fullName))
                info += $"\n\nFullName: {fullName}";

            _userInfoText.text = info;
        }

        internal void SetCredentialStateText(string message) =>
            _credentialStateText.text = message;

        internal void SetDeleteAccountError(string message)
        {
            _confirmDialog.SetActive(false);
            _deleteAccountButton.interactable = true;
            _credentialStateText.text = message;
        }

        private void OnSignOutClicked() => _onSignOut?.Invoke();

        private void OnCheckCredentialStateClicked() => _onCheckCredentialState?.Invoke();

        private void OnDeleteAccountButtonClicked()
        {
            _confirmDialogText.text = "정말 탈퇴하시겠습니까?\n이 작업은 되돌릴 수 없습니다.";
            _confirmDialog.SetActive(true);
            _deleteAccountButton.interactable = false;
        }

        private void OnDeleteAccountConfirmed()
        {
            _confirmDialog.SetActive(false);
            _onDeleteAccount?.Invoke();
        }

        private void OnDeleteAccountCancelled()
        {
            _confirmDialog.SetActive(false);
            _deleteAccountButton.interactable = true;
        }
    }
}
