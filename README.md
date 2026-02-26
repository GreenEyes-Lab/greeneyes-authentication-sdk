# greeneyes-authentication-sdk

Unity용 소셜 로그인 통합 인증 SDK. 여러 플랫폼의 인증 방식을 하나의 통일된 인터페이스로 제공한다.

---

## 개요

- **타겟 엔진**: Unity 6000.3.8f1 LTS
- **배포 방식**: Unity Package Manager (UPM)
- **지원 플랫폼**: iOS (네이티브), Android (Web OAuth)

## 인증 흐름

```
Apple / 소셜 로그인 서버
         ↓
      SDK (토큰 수신)
         ↓
   클라이언트 (Unity)
         ↓
   GreenEyes 백엔드 (서버 검증)
```

SDK는 소셜 로그인을 처리하고 결과를 클라이언트에 반환하는 것까지만 담당한다.
백엔드와의 통신은 클라이언트 책임이다.

## 지원 로그인 프로바이더

| 프로바이더 | iOS | Android | 상태 |
|---|---|---|---|
| Apple | 네이티브 (`AuthenticationServices`) | Web OAuth (WebView) | ✅ 구현 완료 |
| Google | - | - | TODO |
| Kakao | - | - | TODO |
| Naver | - | - | TODO |

## 인증 결과 (`AuthResult`)

로그인 성공 시 SDK가 클라이언트에 반환하는 데이터.

| 필드 | 타입 | 설명 | 제공 시점 |
|---|---|---|---|
| `IdentityToken` | string | JWT 형식의 사용자 인증 토큰 | 매 로그인 |
| `AuthorizationCode` | string | 백엔드 서버의 Apple 검증용 일회성 코드 | 매 로그인 |
| `UserId` | string | Apple이 발급한 고유 사용자 식별자 | 매 로그인 |
| `Email` | string? | 사용자 이메일 | 최초 로그인만 |
| `FullName` | string? | 사용자 이름 | 최초 로그인만 |

> `Email`과 `FullName`은 Apple 정책상 최초 로그인 시에만 제공된다. 이후 로그인에서는 null이다.

## 사용 예시

### iOS
iOS는 SDK가 자동으로 초기화되므로 별도 설정 없이 바로 호출할 수 있다.

```csharp
AuthManager.Instance.SignIn(AuthProviderType.Apple, (result, error) => {
    if (error != null) {
        Debug.LogError(error);
        return;
    }
    // 클라이언트에서 백엔드로 전달
    string identityToken    = result.IdentityToken;
    string authorizationCode = result.AuthorizationCode;
});
```

### Android
Android는 Web OAuth 방식으로 동작하며, 앱별 설정을 직접 주입해야 한다.

```csharp
// 앱 초기화 시점에 한 번만 호출
var config = new AppleAuthAndroidConfig(
    serviceId:   "com.example.service",          // Apple Developer Console의 Services ID
    redirectUrl: "https://example.com/apple/callback"  // Apple에 등록한 Redirect URL
);
AuthManager.Instance.RegisterProvider(AuthProviderType.Apple, new AppleAuthProvider(config));

// 이후 사용 방법은 iOS와 동일
AuthManager.Instance.SignIn(AuthProviderType.Apple, (result, error) => { ... });
```

#### Android 사전 준비
1. [Apple Developer Console](https://developer.apple.com/account/)에서 **Services ID** 생성
2. Services ID의 **Sign In with Apple** 활성화 후 Redirect URL 등록
   - Redirect URL은 HTTPS여야 하며, 실제 서버가 없어도 됨 (WebView가 인터셉트)
   - 예) `https://example.com/apple/callback`
3. 위 두 값을 `AppleAuthAndroidConfig`에 전달

## 프로젝트 구조

```
greeneyes-authentication-sdk/
├── package.json
├── Runtime/
│   ├── Core/
│   │   ├── IAuthProvider.cs       # 통일된 인증 인터페이스
│   │   ├── AuthResult.cs          # 인증 결과 데이터 모델
│   │   └── AuthManager.cs         # SDK 진입점 (싱글턴)
│   └── Providers/
│       ├── Apple/
│       │   ├── AppleAuthProvider.cs       # C# 래퍼
│       │   ├── Plugins/iOS/
│       │   │   ├── AppleAuthNative.h      # C 함수 선언
│       │   │   ├── AppleAuthNative.mm     # ObjC 브리지 (DllImport 진입점)
│       │   │   └── AppleAuthManager.swift # Swift 로직 (AuthenticationServices)
│       │   └── Plugins/Android/
│       │       ├── AppleAuthWebViewActivity.kt    # Android WebView OAuth 액티비티
│       │       └── AndroidManifest.xml            # 액티비티 선언
│       ├── Google/                # TODO
│       └── Kakao/                 # TODO
└── Tests/
```
