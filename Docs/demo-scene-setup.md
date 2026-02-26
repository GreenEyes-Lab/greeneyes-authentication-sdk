# Demo 씬 셋업 가이드

## 작업 분담

- **스크립트 (완료):** Claude가 작성
- **씬 구성 (미완료):** Unity Editor에서 수동 작업 필요

---

## 현재 상태

| 항목 | 상태 |
|---|---|
| `Scripts/BackendMock.cs` | ✅ 완료 |
| `Scripts/LoginPanel.cs` | ✅ 완료 |
| `Scripts/MainPanel.cs` | ✅ 완료 |
| `Scripts/DemoAppController.cs` | ✅ 완료 |
| `Scripts/AppleSignInDemo.cs` | ✅ 삭제됨 |
| Unity Editor 씬 작업 | 🔲 미완료 (1단계 진행 중) |

---

## Unity Editor 작업 순서

### 1단계 — 깨진 오브젝트 정리 ← **여기서 중단**

- Hierarchy에서 `AppleSignInDemo` GameObject 선택 → **Delete**
- Hierarchy에서 `Canvas` 아래 `Button (Legacy)`, `Text (Legacy)` 두 개 → **Delete**

---

### 2단계 — 새 패널 3개 생성 (Canvas 아래)

**① LoadingPanel**

| 항목 | 값 |
|---|---|
| 종류 | UI > Panel |
| Anchor | Stretch 전체 (Alt+클릭으로 stretch-stretch) |
| Image Color | R:0 G:0 B:0 A:255 |

자식: `UI > Text (Legacy)` → 이름 `LoadingText`

| 항목 | 값 |
|---|---|
| 텍스트 | `"인증 상태 확인 중..."` |
| Anchor | Middle Center |
| 크기 | 400 × 50 |
| Font Size | 18 |
| Alignment | Middle Center |

---

**② LoginPanel**

| 항목 | 값 |
|---|---|
| 종류 | UI > Panel |
| Anchor | Stretch 전체 |
| Image Color | R:30 G:30 B:30 A:200 |

자식 3개:

| 이름 | 종류 | 위치 | 크기 | 텍스트 | Font | 기타 |
|---|---|---|---|---|---|---|
| `TitleText` | Text (Legacy) | (0, 60) | 400×40 | `"Sign in with Apple"` | 20 | Middle Center |
| `StatusText` | Text (Legacy) | (0, 0) | 400×60 | `""` | 16 | Middle Center, Vertical Overflow: Overflow |
| `SignInButton` | Button (Legacy) | (0, -70) | 240×50 | — | — | — |

`SignInButton`의 자식 Text:

| 텍스트 | Font Size | Alignment |
|---|---|---|
| `"Sign in with Apple"` | 16 | Middle Center |

---

**③ MainPanel**

| 항목 | 값 |
|---|---|
| 종류 | UI > Panel |
| Anchor | Stretch 전체 |
| Image Color | R:30 G:30 B:30 A:200 |

자식 7개:

| 이름 | 종류 | 위치 | 크기 | 텍스트 | Font | 기타 |
|---|---|---|---|---|---|---|
| `WelcomeText` | Text | (0, 120) | 400×40 | `""` | 20 | Middle Center |
| `UserInfoText` | Text | (0, 30) | 400×120 | `""` | 14 | Upper Left, Vertical Overflow: Overflow |
| `CredentialStateButton` | Button | (0, -60) | 260×40 | `"Credential 상태 확인"` | 14 | — |
| `CredentialStateText` | Text | (0, -110) | 400×30 | `""` | 14 | Middle Center |
| `SignOutButton` | Button | (-130, -160) | 200×40 | `"로그아웃"` | 14 | — |
| `DeleteAccountButton` | Button | (130, -160) | 200×40 | `"회원탈퇴"` | 14 | — |
| `ConfirmDialog` | Panel | (0, 0) | 360×180 | — | — | **기본 비활성화** |

`ConfirmDialog` 자식 4개:

| 이름 | 종류 | 위치 | 크기 | 텍스트 | 기타 |
|---|---|---|---|---|---|
| `DialogText` | Text | (0, 40) | 320×70 | `""` | Middle Center, Vertical Overflow: Overflow |
| `ConfirmButton` | Button | (90, -55) | 120×40 | `"탈퇴"` | — |
| `CancelButton` | Button | (-90, -55) | 120×40 | `"취소"` | — |

> `ConfirmDialog` Inspector에서 GameObject 체크 해제 (비활성화)

---

### 3단계 — DemoAppController GameObject 생성

1. Hierarchy 빈 곳 우클릭 → `Create Empty` → 이름: `DemoAppController`
2. Inspector → `Add Component` → `DemoAppController` 검색 후 추가
3. 필드 연결:

| 필드 | 연결 대상 (드래그) |
|---|---|
| `_loadingPanel` | `LoadingPanel` GameObject |
| `_loginPanel` | `LoginPanel` 의 LoginPanel 컴포넌트 |
| `_mainPanel` | `MainPanel` 의 MainPanel 컴포넌트 |

---

### 4단계 — LoginPanel 컴포넌트 연결

1. `LoginPanel` GameObject 선택 → `Add Component` → `LoginPanel`
2. 필드 연결:

| 필드 | 연결 대상 |
|---|---|
| `_statusText` | `StatusText` (Text 컴포넌트) |
| `_signInButton` | `SignInButton` (Button 컴포넌트) |

---

### 5단계 — MainPanel 컴포넌트 연결

1. `MainPanel` GameObject 선택 → `Add Component` → `MainPanel`
2. 필드 연결:

| 필드 | 연결 대상 |
|---|---|
| `_welcomeText` | `WelcomeText` (Text) |
| `_userInfoText` | `UserInfoText` (Text) |
| `_credentialStateButton` | `CredentialStateButton` (Button) |
| `_credentialStateText` | `CredentialStateText` (Text) |
| `_signOutButton` | `SignOutButton` (Button) |
| `_deleteAccountButton` | `DeleteAccountButton` (Button) |
| `_confirmDialog` | `ConfirmDialog` (GameObject) |
| `_confirmDialogText` | `DialogText` (Text) |
| `_confirmButton` | `ConfirmButton` (Button) |
| `_cancelButton` | `CancelButton` (Button) |

---

### 6단계 — 저장

`Ctrl+S` 로 씬 저장. 완료되면 Claude에게 커밋 요청.
