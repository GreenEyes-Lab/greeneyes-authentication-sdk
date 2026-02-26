namespace GreenEyes.Auth
{
    public enum CredentialState
    {
        Authorized,   // Apple credential 유효
        Revoked,      // 사용자가 앱 연동 해제
        NotFound,     // 해당 userId로 Sign in with Apple 기록 없음
        Transferred   // 앱이 다른 팀으로 이전됨 (iOS 전용)
    }
}
