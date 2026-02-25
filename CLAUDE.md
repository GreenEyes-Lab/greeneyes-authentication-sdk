# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 프로젝트 개요

Unity UPM 패키지(`com.greeneyes.auth`)로 배포되는 소셜 로그인 통합 인증 SDK. Unity 6000.3.8f1 LTS 타겟. 현재 Apple 로그인만 구현 예정이며 나머지 프로바이더(Google, Kakao 등)는 TODO 상태.

## 브랜치 전략

Git Flow를 따른다. `main` → `develop` → `feature/*` 순서로 작업 후 머지.

## 아키텍처

### 인증 흐름
```
소셜 로그인 서버 → SDK (네이티브 처리) → 클라이언트 (Unity) → GreenEyes 백엔드
```
SDK는 토큰 수신 및 클라이언트 반환까지만 담당. 백엔드 통신은 클라이언트 책임.

### SDK 패키지 구조 (계획)
```
Runtime/
  Core/
    IAuthProvider.cs      # 통일된 인증 인터페이스
    AuthResult.cs         # 인증 결과 (IdentityToken, AuthorizationCode, UserId, Email?, FullName?)
    AuthManager.cs        # 진입점 (싱글턴)
  Providers/
    Apple/
      AppleAuthProvider.cs
      Plugins/iOS/AppleAuthNative.mm    # AuthenticationServices 프레임워크
      Plugins/Android/AppleAuthAndroid.java  # Web OAuth (WebView)
```

### AuthResult 주의사항
- `Email`, `FullName`은 Apple 정책상 최초 로그인 시에만 제공. 이후 로그인에서 null.
- `IdentityToken`, `AuthorizationCode`, `UserId`는 매 로그인마다 제공.

## 데모 프로젝트

`Demo~/AuthSDKDemo/`에 위치. 폴더명 끝의 `~`는 Unity가 에셋 스캔 시 무시하는 Unity 컨벤션.

데모 프로젝트에서 SDK를 로컬 UPM 패키지로 참조:
- 경로: `Demo~/AuthSDKDemo/Packages/manifest.json`
- 참조 방식: `"com.greeneyes.auth": "file:../../.."` (Packages/ 폴더 기준 상대 경로)

## Unity 관련 주의사항

- SDK 파일 추가/수정 시 `.meta` 파일도 함께 커밋해야 한다.
- `Demo~/` 폴더는 전역 gitignore의 `*~` 패턴에 의해 무시될 수 있어 루트 `.gitignore`에 `!Demo~/` 예외 처리가 되어 있다.
- `.npmignore`로 `Demo/` 디렉토리를 UPM 패키지 스캔에서 제외한다.
