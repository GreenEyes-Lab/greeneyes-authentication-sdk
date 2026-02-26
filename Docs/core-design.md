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
            ├── iOS/
            │   ├── AppleAuthNative.h       # C 함수 선언
            │   ├── AppleAuthNative.mm      # ObjC 브리지 (DllImport 진입점)
            │   └── AppleAuthManager.swift  # Swift 로직 (AuthenticationServices)
            └── Android/AppleAuthWebViewActivity.kt

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
네이티브 흐름 (iOS):

Unity C# (DllImport)
    → AppleAuthNative.mm (ObjC 브리지, C 함수)
    → AppleAuthManager.swift (@objc class, 실제 로직)
    → ASAuthorizationController (AuthenticationServices)
    → AppleAuthManager.swift (delegate 콜백)
    → ObjC 브리지의 onSuccess/onFailure 블록
    → UnitySendMessage("GreenEyes_AppleAuthBridge", "OnSuccess/OnFailure", payload)
    → AppleAuthNativeBridge (MonoBehaviour)
    → AppleAuthProvider.OnNativeSuccess/Failure (C#)
    → 클라이언트 콜백 호출
```

**iOS 네이티브 레이어 역할 분담:**

| 파일 | 언어 | 역할 |
|---|---|---|
| `AppleAuthNative.h` | ObjC | C 함수 시그니처 선언 |
| `AppleAuthNative.mm` | ObjC | Unity `DllImport` 진입점, Swift 호출, `UnitySendMessage` 처리 |
| `AppleAuthManager.swift` | Swift | `AuthenticationServices` 로직, delegate 구현 |

ObjC `.mm` 파일이 `UnitySendMessage`를 담당하는 이유: Swift에서는 Unity 런타임 C 함수를 직접 호출하기 어렵기 때문에, 결과는 ObjC 블록 콜백을 통해 `.mm`으로 돌아온 뒤 `UnitySendMessage`를 호출한다.

### 7. Android Apple Sign In 설계

iOS와 달리 Android에는 Apple의 네이티브 SDK가 없어 Web OAuth 방식을 사용한다.

**흐름:**
```
Unity C# (SignIn)
    → AppleAuthProvider.Android.cs: Apple OAuth URL 빌드 + WebView 액티비티 시작
    → AppleAuthWebViewActivity.kt: WebView로 Apple 로그인 페이지 표시
    → 사용자 로그인 완료
    → Apple이 redirect_uri로 리다이렉트 (response_mode=fragment)
    → shouldOverrideUrlLoading으로 URL 인터셉트
    → fragment에서 code, id_token, user(최초만) 파싱
    → id_token JWT의 sub 클레임에서 userId 추출
    → UnitySendMessage → AppleAuthNativeBridge → 클라이언트 콜백
```

**`response_mode=fragment` 채택 이유:**
Apple은 `form_post`(POST)와 `fragment` 두 가지 response_mode를 지원한다.
`form_post`는 POST body 인터셉트가 필요해 복잡하고, `fragment`는 URL에 토큰이 포함되어
WebView의 `shouldOverrideUrlLoading`으로 단순하게 인터셉트할 수 있다.

**Redirect URL에 대한 중요 사항:**
- Redirect URL은 자동 생성되지 않는다. 앱마다 고유하게 설정해야 한다.
- Apple Developer Console에 HTTPS URL로 사전 등록이 필요하다.
- 실제 서버(웹 엔드포인트)는 필요 없다. WebView가 해당 URL로의 이동을 감지하는 순간 인터셉트하므로 서버 응답 없이 토큰 추출이 가능하다.
- SDK가 여러 프로젝트에서 쓰이므로, Service ID와 Redirect URL은 하드코딩하지 않고 `AppleAuthAndroidConfig`로 주입받는다.

**iOS와 Android 초기화 차이:**

| | iOS | Android |
|---|---|---|
| 초기화 방식 | `GreenEyesAuthInitializer`가 자동 등록 | 클라이언트가 `AppleAuthAndroidConfig`와 함께 수동 등록 |
| 이유 | Bundle ID를 자동 사용, 설정 불필요 | Service ID·Redirect URL이 앱마다 다름 |

### 8. 어셈블리 구성

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
