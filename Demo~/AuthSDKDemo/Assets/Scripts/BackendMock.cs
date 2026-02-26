using System;
using System.Collections;
using UnityEngine;

namespace AuthSDKDemo
{
    // 서버 없이 동작하는 백엔드 API 모의 구현.
    // 프로덕션에서는 GreenEyes 백엔드 DELETE /users/me 를 호출한다.
    // 백엔드가 Apple /auth/revoke 를 처리하므로 클라이언트는 userId만 전달하면 된다.
    internal static class BackendMock
    {
        // 회원탈퇴: 1초 지연으로 서버 통신을 모의한다.
        internal static IEnumerator DeleteAccount(
            string userId,
            Action onSuccess,
            Action<string> onFailure)
        {
            Debug.Log($"[BackendMock] 회원탈퇴 요청 (userId: {userId})");
            yield return new WaitForSeconds(1f);
            Debug.Log("[BackendMock] 회원탈퇴 완료");
            onSuccess?.Invoke();
        }
    }
}
