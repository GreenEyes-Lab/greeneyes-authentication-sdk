# Demo 시나리오 기획

## 개요

`Demo~/AuthSDKDemo`는 Apple 로그인 SDK의 동작을 실제로 확인하는 샌드박스 프로젝트다.
현재 로그인 버튼 하나만 존재하며, 이번 작업에서 아래 네 가지 시나리오를 완성한다.

| 시나리오 | 설명 |
|---|---|
| 회원가입 | 최초 Sign in with Apple → Email·FullName 수신 → 사용자 등록 |
| 로그인 | 재로그인 또는 앱 재시작 후 Credential 상태 확인 → 자동/수동 로그인 |
| 로그아웃 | SignOut 호출 → 로컬 세션 초기화 → 로그인 화면 복귀 |
| 회원탈퇴 | 백엔드 탈퇴 API 모의 호출 → Apple Revoke(서버 경유) → 계정 삭제 |

---

## 화면 구성

씬 하나에 세 가지 패널을 겹쳐 두고, 상태에 따라 활성 패널을 전환한다.

```
SampleScene
└── Canvas
    ├── LoadingPanel      ← 앱 시작 직후 Credential 확인 중
    ├── LoginPanel        ← 미로그인 상태
    └── MainPanel         ← 로그인 완료 상태
```

### LoadingPanel

```
[ 로고 or 앱 이름 ]
"인증 상태 확인 중..."
```

- 앱 시작 시 자동 표시.
- PlayerPrefs에 저장된 `userId`가 있으면 `GetCredentialState` 호출.
- `userId`가 없으면 즉시 LoginPanel로 전환.

### LoginPanel

```
[ Sign in with Apple 버튼 ]
[ 상태 메시지 텍스트 ]
```

- 최초 진입 시 메시지: `"Apple 계정으로 시작하세요."`
- 로그아웃·탈퇴 후 복귀 시 메시지: `"다시 로그인하세요."`

### MainPanel

```
[ 환영 메시지 ]              예: "안녕하세요, 홍길동님!" (회원가입 첫 진입 시)
                              또는 "반갑습니다!" (재로그인 시)

[ 사용자 정보 ]
  UserId : abc123...
  Email  : user@example.com  ← 최초 로그인 시만 표시
  FullName: 홍길동            ← 최초 로그인 시만 표시

[ Credential 상태 확인 버튼 ]  ← iOS 전용 실질 동작, Android는 안내 메시지
[ 상태 조회 결과 텍스트 ]

[ 로그아웃 버튼 ]
[ 회원탈퇴 버튼 ]            ← 탭 시 확인 다이얼로그 표시
```

---

## 앱 상태 머신

```
           ┌──────────────────────────────────────────────────────┐
           │                  앱 시작                             │
           └──────────────────────┬───────────────────────────────┘
                                  ▼
                      ┌──────────────────────┐
                      │    LoadingState       │
                      │ (Credential 확인 중) │
                      └──────┬───────────────┘
              userId 없음     │          userId 있음 → GetCredentialState
                  ▼          │               ▼
          ┌────────────┐     │    ┌─────────────────────────┐
          │            │     │    │  Authorized?             │
          │            │◄────┘    ├──Yes──► MainState        │
          │ LoginState │          ├──Revoked/NotFound──────► │
          │            │◄─────────┘  (LoginState로 전환)    │
          └─────┬──────┘                                     │
                │                                            │
          SignIn(Apple)                                       │
                │                                            │
                ▼                                            │
        ┌──────────────┐      SignOut / 탈퇴       ┌─────────┴──────┐
        │  Signing In  │                           │   MainState    │
        │  (중간 상태)  │──── 성공 ───────────────►│                │
        └──────────────┘                           └────────┬───────┘
                │                                           │
                └── 실패 → LoginState (에러 메시지)         │
                                              SignOut / 탈퇴 완료
                                                            │
                                                            ▼
                                                       LoginState
```

---

## 시나리오 상세

### 시나리오 1: 회원가입 (최초 로그인)

**전제 조건:** PlayerPrefs에 `userId` 없음.

```
1. 앱 시작 → LoadingPanel 표시
2. userId 없음 → 즉시 LoginPanel 전환
3. 메시지: "Apple 계정으로 시작하세요."
4. [Sign in with Apple] 탭
5. 버튼 비활성화, 메시지: "로그인 중..."
6. AuthManager.Instance.SignIn(Apple, callback)
7. 성공 → AuthResult.Email, FullName 포함 (최초)
8. userId를 PlayerPrefs에 저장
9. MainPanel 전환
10. 환영 메시지: "안녕하세요, [FullName]님!" (FullName이 있을 경우)
```

**확인 포인트:** `Email`과 `FullName`이 표시되는지.

---

### 시나리오 2: 재로그인 (수동)

**전제 조건:** PlayerPrefs에 `userId` 있음. GetCredentialState → `Revoked` 또는 `NotFound`.

```
1. 앱 시작 → LoadingPanel
2. GetCredentialState(userId) 호출
3. 결과: Revoked 또는 NotFound
4. LoginPanel 전환, 메시지: "다시 로그인하세요."
5. [Sign in with Apple] 탭
6. 성공 → Email/FullName null (재로그인)
7. userId 갱신 저장 (동일하거나 새 userId)
8. MainPanel 전환, 메시지: "반갑습니다!"
```

**확인 포인트:** Email/FullName이 "(제공되지 않음)"으로 표시되는지.

---

### 시나리오 3: 자동 로그인 (앱 재시작)

**전제 조건:** PlayerPrefs에 `userId` 있음. GetCredentialState → `Authorized`.

```
1. 앱 시작 → LoadingPanel
2. GetCredentialState(userId) 호출
3. 결과: Authorized
4. LoadingPanel에서 MainPanel로 바로 전환 (로그인 화면 건너뜀)
5. 메시지: "자동 로그인되었습니다."
```

**확인 포인트:** LoginPanel이 표시되지 않고 MainPanel로 바로 진입하는지.
**Android 주의:** GetCredentialState가 NotSupported를 반환하므로 항상 수동 로그인으로 처리.

---

### 시나리오 4: 로그아웃

**전제 조건:** 로그인 상태 (MainPanel).

```
1. [로그아웃] 버튼 탭
2. AuthManager.Instance.SignOut(Apple, callback)
3. 성공 → PlayerPrefs에서 userId 삭제
4. LoginPanel 전환
5. 메시지: "로그아웃되었습니다. 다시 로그인하세요."
```

**확인 포인트:** 재진입 시 LoginPanel이 표시되고 자동 로그인이 되지 않는지.

---

### 시나리오 5: Credential 상태 조회

**전제 조건:** 로그인 상태 (MainPanel). iOS 기기 또는 시뮬레이터.

```
1. [Credential 상태 확인] 버튼 탭
2. AuthManager.Instance.GetCredentialState(Apple, userId, callback)
3. 결과 텍스트 갱신:
   - Authorized  → "✓ 유효한 자격증명"
   - Revoked     → "✗ 연동 해제됨 — 재로그인 필요"
   - NotFound    → "✗ 자격증명 없음"
   - NotSupported (Android) → "이 플랫폼에서는 지원되지 않습니다."
```

---

### 시나리오 6: 회원탈퇴

**전제 조건:** 로그인 상태 (MainPanel).

```
1. [회원탈퇴] 버튼 탭
2. 확인 다이얼로그: "정말 탈퇴하시겠습니까? 이 작업은 되돌릴 수 없습니다."
   - [취소] → 다이얼로그 닫기
   - [탈퇴] → 3번으로 진행
3. BackendMock.DeleteAccount(userId) 호출 (비동기 1초 지연으로 서버 통신 모의)
   └─ 실제 프로덕션: GreenEyes 백엔드 → Apple /auth/revoke
4. 성공 → PlayerPrefs 전체 초기화
5. LoginPanel 전환
6. 메시지: "계정이 삭제되었습니다."
```

**확인 포인트:** 탈퇴 후 재진입 시 회원가입 플로우로 진행되는지.

---

## 구현 계획

### 새로 작성할 파일

| 파일 | 역할 |
|---|---|
| `Scripts/DemoAppController.cs` | 앱 상태 머신 관리, 패널 전환, PlayerPrefs 저장 |
| `Scripts/LoginPanel.cs` | 로그인 패널 UI 제어 |
| `Scripts/MainPanel.cs` | 메인 패널 UI 제어, 각 버튼 이벤트 처리 |
| `Scripts/BackendMock.cs` | 회원탈퇴 백엔드 API 모의 (코루틴으로 1초 지연) |

### 제거할 파일

| 파일 | 이유 |
|---|---|
| `Scripts/AppleSignInDemo.cs` | 기존 단순 데모 스크립트, DemoAppController로 대체 |

### 씬 변경

현재 `SampleScene`의 단일 버튼 레이아웃을 아래 계층으로 교체한다.

```
Canvas (Screen Space - Overlay)
├── LoadingPanel
│   └── LoadingText
├── LoginPanel
│   ├── TitleText
│   ├── StatusText
│   └── SignInButton
│       └── Text
└── MainPanel
    ├── WelcomeText
    ├── UserInfoText          ← UserId / Email / FullName
    ├── CredentialStateButton
    │   └── Text
    ├── CredentialStateText
    ├── SignOutButton
    │   └── Text
    ├── DeleteAccountButton
    │   └── Text
    └── ConfirmDialog (기본 비활성화)
        ├── DialogText
        ├── ConfirmButton
        │   └── Text
        └── CancelButton
            └── Text
```

### PlayerPrefs 키

| 키 | 타입 | 설명 |
|---|---|---|
| `greeneyes_auth_user_id` | string | 로그인한 사용자의 Apple UserId |

---

## 플랫폼별 동작 차이 요약

| 기능 | iOS | Android |
|---|---|---|
| Sign In | AuthenticationServices (네이티브) | WebView OAuth |
| 회원가입/로그인 구분 | Email 포함 여부로 판단 | 동일 |
| Credential 상태 조회 | 실제 동작 (ASAuthorizationAppleIDProvider) | NotSupported 안내 |
| 자동 로그인 | GetCredentialState Authorized 시 | 항상 수동 로그인 |
| Sign Out | no-op (즉시 성공) | no-op (즉시 성공) |
| 회원탈퇴 | 백엔드 경유 (모의) | 백엔드 경유 (모의) |
