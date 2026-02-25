# Core 레이어 설계

## 설계 결정 요약

| 항목 | 결정 |
|---|---|
| 비동기 패턴 | 콜백 1차, async 확장 메서드 2차 |
| 에러 타입 | `AuthError` 전용 클래스 |
| AuthManager | 순수 C# 싱글턴 (`Lazy<T>`) |
| 프로바이더 등록 | `RuntimeInitializeOnLoadMethod` 자동 등록 |
| 플랫폼 분기 | `partial class` 파일 분리 |
| 어셈블리 | Runtime 1개 + Tests 1개 |
| 네이티브 브리지 | 최소 MonoBehaviour (`HideAndDontSave`) |

---

## 파일 구조

```
Runtime/
├── GreenEyes.Auth.asmdef
├── Core/
│   ├── IAuthProvider.cs
│   ├── AuthProviderType.cs
│   ├── AuthResult.cs
│   ├── AuthError.cs
│   ├── AuthManager.cs
│   └── GreenEyesAuthInitializer.cs
└── Providers/
    └── Apple/
        ├── AppleAuthProvider.cs              # 공통 (partial)
        ├── AppleAuthProvider.iOS.cs          # iOS 구현 (partial, #if UNITY_IOS)
        ├── AppleAuthProvider.Android.cs      # Android 구현 (partial, #if UNITY_ANDROID)
        ├── AppleAuthProvider.Unsupported.cs  # 에디터/기타 stub
        ├── AppleAuthNativeBridge.cs          # UnitySendMessage 수신용 MonoBehaviour
        └── Plugins/
            ├── iOS/AppleAuthNative.mm
            └── Android/AppleAuthAndroid.java

Tests/
├── Runtime/
│   ├── GreenEyes.Auth.Tests.asmdef
│   ├── AuthManagerTests.cs
│   └── AuthResultTests.cs
└── Editor/
    ├── GreenEyes.Auth.Editor.Tests.asmdef
    └── AuthManagerEditorTests.cs
```

---

## 항목별 설계 근거

### 1. 비동기 패턴: 콜백 1차

iOS `AuthenticationServices`와 Android WebView 콜백이 모두 콜백 기반이다. 네이티브 → C# 브리지(`UnitySendMessage`)도 콜백 모델이므로, 콜백을 1차 API로 유지하는 것이 래핑 레이어를 최소화한다.

`SignInAsync()` 확장 메서드를 `AuthManagerExtensions.cs`에 별도 제공해 async/await도 지원한다.

```csharp
// 1차 API (콜백)
AuthManager.Instance.SignIn(AuthProviderType.Apple, (result, error) => { ... });

// 2차 API (async, 확장 메서드)
var (result, error) = await AuthManager.Instance.SignInAsync(AuthProviderType.Apple);
```

### 2. 에러 타입: `AuthError` 전용 클래스

iOS `ASAuthorizationError`와 Android WebView 에러를 동일한 `AuthErrorCode`로 정규화한다. 클라이언트는 플랫폼을 신경 쓰지 않고 에러 코드로만 분기할 수 있다.

```csharp
public enum AuthErrorCode
{
    Unknown = 0,
    UserCancelled = 1,       // 사용자가 직접 취소
    NetworkError = 2,        // 네트워크 연결 실패
    InvalidCredential = 3,   // 토큰 파싱/검증 실패
    NotSupported = 4,        // 해당 플랫폼에서 미지원
    ProviderError = 5        // 소셜 로그인 서버 측 에러
}

public sealed class AuthError
{
    public AuthErrorCode Code { get; }
    public string Message { get; }
    public Exception UnderlyingException { get; }  // 디버깅용, nullable
}
```

콜백 규칙: **성공 시 `error = null`, 실패 시 `result = null`.**

### 3. AuthManager: 순수 C# 싱글턴

MonoBehaviour 싱글턴은 씬에 GameObject가 필요하고 초기화 순서가 씬 로드에 의존한다. 순수 C# 싱글턴은 씬 독립적이고 클라이언트가 별도 설정 없이 즉시 사용 가능하다.

```csharp
public sealed class AuthManager
{
    private static readonly Lazy<AuthManager> _instance =
        new Lazy<AuthManager>(() => new AuthManager());

    public static AuthManager Instance => _instance.Value;

    private readonly Dictionary<AuthProviderType, IAuthProvider> _providers = new();

    private AuthManager() { }

    public void RegisterProvider(AuthProviderType type, IAuthProvider provider) { ... }
    public void SignIn(AuthProviderType type, Action<AuthResult, AuthError> callback) { ... }
}
```

### 4. 프로바이더 자동 등록: `RuntimeInitializeOnLoadMethod`

클라이언트가 초기화 코드를 직접 작성할 필요 없이 씬 로드 전 자동으로 플랫폼에 맞는 프로바이더가 등록된다.

```csharp
internal static class GreenEyesAuthInitializer
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
#if UNITY_IOS || UNITY_ANDROID
        AuthManager.Instance.RegisterProvider(
            AuthProviderType.Apple, new AppleAuthProvider());
#endif
    }
}
```

### 5. 플랫폼 분기: `partial class` 파일 분리

`AppleAuthProvider` 를 `partial class`로 선언하고 플랫폼별 파일로 분리한다. 각 파일 최상단에 `#if` 지시문을 적용해 파일 전체를 조건부 컴파일한다.

```
AppleAuthProvider.cs              → 공통: SignIn(), _pendingCallback, OnNativeSuccess/Failure
AppleAuthProvider.iOS.cs          → #if UNITY_IOS: _SignInInternal(), ParseResult(), ParseError()
AppleAuthProvider.Android.cs      → #if UNITY_ANDROID: _SignInInternal(), ParseResult(), ParseError()
AppleAuthProvider.Unsupported.cs  → #else: NotSupported 에러 반환
```

### 6. `UnitySendMessage` 브리지 문제

`UnitySendMessage`는 씬의 **GameObject 이름**을 대상으로 한다. 순수 C# 싱글턴에는 GameObject가 없으므로 네이티브 콜백을 직접 받을 수 없다.

해결책: `GreenEyesAuthInitializer`에서 `HideFlags.HideAndDontSave` GameObject를 생성하고 `AppleAuthNativeBridge` MonoBehaviour를 부착한다. 이 브리지가 네이티브 메시지를 받아 `AppleAuthProvider`로 전달한다.

```
네이티브 (ObjC/Java)
    → UnitySendMessage("AppleAuthNativeBridge", "OnSuccess", json)
    → AppleAuthNativeBridge.OnSuccess(json)
    → AppleAuthProvider.OnNativeSuccess(json)
    → 클라이언트 콜백 호출
```

### 7. 어셈블리 구성

현재 규모에서는 Runtime 단일 asmdef가 적합하다. 프로바이더가 늘어날 때 분리를 고려한다.

- `GreenEyes.Auth.asmdef`: `autoReferenced: false` (클라이언트가 명시적으로 asmdef 참조 필요)
- `GreenEyes.Auth.Tests.asmdef`: `optionalUnityReferences: ["TestAssemblies"]`

---

## AuthResult 필드

| 필드 | 타입 | 제공 시점 | 용도 |
|---|---|---|---|
| `IdentityToken` | `string` | 매 로그인 | JWT, 백엔드 검증용 |
| `AuthorizationCode` | `string` | 매 로그인 | Apple 서버 검증용 일회성 코드 |
| `UserId` | `string` | 매 로그인 | Apple 고유 사용자 식별자 |
| `Email` | `string?` | 최초 로그인만 | Apple 정책 제한 |
| `FullName` | `string?` | 최초 로그인만 | Apple 정책 제한 |
