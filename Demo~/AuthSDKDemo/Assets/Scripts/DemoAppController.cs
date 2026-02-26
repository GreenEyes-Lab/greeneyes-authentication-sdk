using UnityEngine;
using GreenEyes.Auth;

namespace AuthSDKDemo
{
    internal sealed class DemoAppController : MonoBehaviour
    {
        [SerializeField] private GameObject _loadingPanel;
        [SerializeField] private LoginPanel _loginPanel;
        [SerializeField] private MainPanel _mainPanel;

        private string _currentUserId;

        private const string UserIdKey = "greeneyes_auth_user_id";

        private void Awake()
        {
            _loginPanel.Init(onSignIn: HandleSignIn);
            _mainPanel.Init(
                onSignOut: HandleSignOut,
                onCheckCredentialState: HandleCheckCredentialState,
                onDeleteAccount: HandleDeleteAccount);
        }

        private void Start()
        {
            _currentUserId = PlayerPrefs.GetString(UserIdKey, string.Empty);

            if (string.IsNullOrEmpty(_currentUserId))
            {
                ShowLoginPanel("Apple 계정으로 시작하세요.");
                return;
            }

            ShowLoadingPanel();
            AuthManager.Instance.GetCredentialState(
                AuthProviderType.Apple,
                _currentUserId,
                OnCredentialStateChecked);
        }

        // ── Credential state check (app launch) ──────────────────────────────

        private void OnCredentialStateChecked(CredentialState state, AuthError error)
        {
            if (error != null)
            {
                // NotSupported (Android) 또는 예외 → 수동 로그인으로 fallback
                ShowLoginPanel("다시 로그인하세요.");
                return;
            }

            switch (state)
            {
                case CredentialState.Authorized:
                    ShowMainPanel(_currentUserId, email: null, fullName: null, isAutoLogin: true);
                    break;
                case CredentialState.Revoked:
                    ShowLoginPanel("연동이 해제되었습니다. 다시 로그인하세요.");
                    break;
                default:
                    ShowLoginPanel("다시 로그인하세요.");
                    break;
            }
        }

        // ── Event handlers ────────────────────────────────────────────────────

        private void HandleSignIn()
        {
            _loginPanel.SetInteractable(false);
            _loginPanel.SetMessage("로그인 중...");
            AuthManager.Instance.SignIn(AuthProviderType.Apple, OnSignInComplete);
        }

        private void OnSignInComplete(AuthResult result, AuthError error)
        {
            _loginPanel.SetInteractable(true);

            if (error != null)
            {
                _loginPanel.SetMessage($"로그인 실패: {error.Message}");
                Debug.LogError($"[DemoAppController] SignIn error: {error}");
                return;
            }

            _currentUserId = result.UserId;
            PlayerPrefs.SetString(UserIdKey, _currentUserId);
            PlayerPrefs.Save();

            Debug.Log($"[DemoAppController] IdentityToken: {result.IdentityToken}");
            Debug.Log($"[DemoAppController] AuthorizationCode: {result.AuthorizationCode}");

            ShowMainPanel(result.UserId, result.Email, result.FullName, isAutoLogin: false);
        }

        private void HandleSignOut()
        {
            AuthManager.Instance.SignOut(AuthProviderType.Apple, OnSignOutComplete);
        }

        private void OnSignOutComplete(AuthError error)
        {
            if (error != null)
                Debug.LogError($"[DemoAppController] SignOut error: {error.Message}");

            PlayerPrefs.DeleteKey(UserIdKey);
            PlayerPrefs.Save();
            _currentUserId = null;

            ShowLoginPanel("로그아웃되었습니다. 다시 로그인하세요.");
        }

        private void HandleCheckCredentialState()
        {
            _mainPanel.SetCredentialStateText("확인 중...");
            AuthManager.Instance.GetCredentialState(
                AuthProviderType.Apple,
                _currentUserId,
                (state, error) =>
                {
                    if (error != null)
                    {
                        var msg = error.Code == AuthErrorCode.NotSupported
                            ? "이 플랫폼에서는 지원되지 않습니다."
                            : $"오류: {error.Message}";
                        _mainPanel.SetCredentialStateText(msg);
                        return;
                    }

                    var label = state switch
                    {
                        CredentialState.Authorized  => "✓ 유효한 자격증명",
                        CredentialState.Revoked     => "✗ 연동 해제됨 — 재로그인 필요",
                        CredentialState.NotFound    => "✗ 자격증명 없음",
                        CredentialState.Transferred => "이전된 자격증명 (앱 팀 변경)",
                        _                           => "알 수 없음"
                    };
                    _mainPanel.SetCredentialStateText(label);
                });
        }

        private void HandleDeleteAccount()
        {
            StartCoroutine(BackendMock.DeleteAccount(
                _currentUserId,
                onSuccess: () =>
                {
                    PlayerPrefs.DeleteAll();
                    PlayerPrefs.Save();
                    _currentUserId = null;
                    ShowLoginPanel("계정이 삭제되었습니다.");
                },
                onFailure: msg => _mainPanel.SetDeleteAccountError($"탈퇴 실패: {msg}")));
        }

        // ── Panel transitions ─────────────────────────────────────────────────

        private void ShowLoadingPanel()
        {
            _loadingPanel.SetActive(true);
            _loginPanel.gameObject.SetActive(false);
            _mainPanel.gameObject.SetActive(false);
        }

        private void ShowLoginPanel(string message)
        {
            _loadingPanel.SetActive(false);
            _loginPanel.gameObject.SetActive(true);
            _mainPanel.gameObject.SetActive(false);
            _loginPanel.SetMessage(message);
            _loginPanel.SetInteractable(true);
        }

        private void ShowMainPanel(string userId, string email, string fullName, bool isAutoLogin)
        {
            _loadingPanel.SetActive(false);
            _loginPanel.gameObject.SetActive(false);
            _mainPanel.gameObject.SetActive(true);
            _mainPanel.Setup(userId, email, fullName, isAutoLogin);
        }
    }
}
