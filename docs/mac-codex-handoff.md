# MacBook iOS Build Handoff

기준일: 2026-07-01

이 문서는 Windows에서 준비한 현재 StarForge 상태를 MacBook에서 Unity/Xcode/Codex로 이어받아 iOS 빌드할 때의 작업 지침이다.

## 1. 현재 저장소 기준

- GitHub 저장소: `https://github.com/ohc4252-great/My-project.git`
- 브랜치: `main`
- 최신 푸시 커밋: `6203d6a Use premium audio for black hole discovery`
- Unity: `6000.4.4f1`
- 프로젝트 경로 권장: `~/Projects/My-project`
- Bundle ID: `com.starforge.stellarsmith`
- Marketing version: `1.0.0`
- iOS build number: `2`
- iOS minimum target: `15.0`
- Google Mobile Ads Unity package: `com.google.ads.mobile` `11.2.0`

주의:

- 예전 원격 `star_enhance`는 사용하지 않는다. Mac에서는 반드시 `origin`의 `My-project` 저장소를 클론한다.
- App Store Connect에 `1.0 (1)` 제출 이력이 있으므로, 이번 재제출은 최소 `1.0.0 (2)` 이상이어야 한다.
- App Store Connect에 이미 build `2`를 올린 이력이 생겼다면, 새 빌드 전에 iOS build number를 `3` 이상으로 올린다.

## 2. 먼저 읽을 문서

Mac 빌드 AI는 작업 전에 아래 문서를 순서대로 읽는다.

```text
docs/ios-current-build-handoff.md
docs/ios-app-store-launch-crash-build-notes.md
docs/store-submission-checklist.md
```

핵심 목적은 App Store 거부 원인이었던 iOS 런치 크래시를 막고, 현재 UI 수정이 포함된 빌드를 실제 기기에서 검증한 뒤 업로드하는 것이다.

## 3. Windows 현재 작업트리 주의

현재 Windows 로컬에는 iOS 빌드에 직접 필요하지 않은 미커밋 변경/중복 파일이 남아 있을 수 있다.

- `My project.slnx`: 줄바꿈 차이
- `ProjectSettings/ProjectSettings.asset`: Android `versionCode` 로컬 변경
- `docs/ios-app-store-launch-crash-build-notes.md`: 내용 차이 없는 줄바꿈/인덱스 노이즈 가능성
- `Assets/` 루트의 `*-Photoroom.png`: HUD 버튼 원본 중복 이미지

Mac에서는 Windows 작업 폴더를 복사하지 말고, GitHub의 `origin/main`을 새로 클론한다. 런타임 HUD 버튼은 이미 아래 경로에 포함되어 있다.

```text
Assets/_Project/Resources/HudButtons/
```

## 4. Mac 초기 세팅

### Xcode

Intel MacBook이면 `Silicon.xip`가 아니라 `Universal.xip`를 설치한다.

```bash
sudo xcode-select -s /Applications/Xcode.app/Contents/Developer
sudo xcodebuild -license accept
xcodebuild -runFirstLaunch
xcodebuild -version
xcodebuild -showsdks
```

`xcodebuild -showsdks` 결과에 `iphoneos`가 있어야 한다.

### Homebrew

```bash
brew --version
```

Homebrew가 없으면 먼저 설치한다.

```bash
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
```

### Git LFS

```bash
brew install git-lfs
git lfs install
```

### 프로젝트 클론

```bash
mkdir -p ~/Projects
cd ~/Projects
git clone https://github.com/ohc4252-great/My-project.git
cd My-project
git lfs pull
git status -sb
git log --oneline -3
```

## 5. Unity 빌드 절차

1. Unity Hub에서 Unity `6000.4.4f1`과 `iOS Build Support`를 설치한다.
2. `~/Projects/My-project`를 연다.
3. 최초 import가 끝날 때까지 기다린다.
4. Console compile error가 없는지 확인한다.
5. iOS 플랫폼으로 Switch Platform 한다.
6. 아래 CI 진입점으로 iOS Xcode 프로젝트를 생성한다.

```text
StarForge.EditorTools.StarForgeBuildPreparation.BuildiOSForCI
```

기본 export 위치:

```text
ios/
```

Codemagic을 쓸 경우 `codemagic.yaml`의 `ios-release` workflow도 같은 Unity 진입점을 사용한다.

## 6. iOS export 후 필수 검증

Unity export 직후 Mac 터미널에서 반드시 확인한다.

```bash
/usr/libexec/PlistBuddy -c "Print :GADApplicationIdentifier" ios/Info.plist
/usr/libexec/PlistBuddy -c "Print :ITSAppUsesNonExemptEncryption" ios/Info.plist
```

기대값:

```text
ca-app-pub-3971219491693844~2351507763
false
```

`GADApplicationIdentifier`가 없거나 값에 `/`가 들어가면 광고 유닛 ID를 잘못 넣은 상태이므로 업로드하지 않는다.

현재 해당 값은 아래 후처리 코드가 export마다 강제 기록한다.

```text
Assets/_Project/Editor/StarForgeIosExportCompliance.cs
```

## 7. Xcode 작업 순서

1. `ios/Unity-iPhone.xcworkspace`가 있으면 workspace를 열고, 없으면 `ios/Unity-iPhone.xcodeproj`를 연다.
2. `Signing & Capabilities`에서 Team을 선택한다.
3. Automatically manage signing을 켠다.
4. Bundle Identifier가 `com.starforge.stellarsmith`인지 확인한다.
5. 연결한 iPhone 또는 iPad에서 Run으로 cold launch를 먼저 확인한다.
6. 실제 기기 실행이 되면 `Product > Archive`.
7. `Distribute App > App Store Connect > Upload`.

## 8. 실기기 검증 체크리스트

최소한 아래는 업로드 전에 확인한다.

- 앱 삭제 후 재설치한 첫 실행이 크래시 없이 메인까지 진입
- 네트워크 켠 상태 cold launch
- 네트워크 끈 상태 cold launch
- iPhone 화면에서 상단 이미지 버튼 4개가 잘리지 않고 눌림
- iPad 또는 iPad 시뮬레이터에서 상단 버튼/설정 팝업/업적 보상 팝업이 잘리지 않음
- `별 탐사하기` 남은 횟수는 메인이 아니라 탐사 화면 안에서만 표시
- 설정 팝업 좌우 프레임이 잘리지 않음
- 업적 보상 5개 동시 수령 시 수량이 아이콘 아래에 표시되고 잘리지 않음
- 블랙홀 등장 강화 연출 사운드가 28-30강 고급 연출 사운드로 재생
- 광고 로드 실패나 네트워크 실패가 앱 종료로 이어지지 않음
- 보상형 광고 완료 보상은 한 번만 지급
- 개인정보처리방침/이용약관 링크가 열림

App Review는 iPad Pro 11-inch (M4)와 iPhone 17 Pro Max에서 런치 크래시를 확인했으므로 iPad 검증을 생략하지 않는다.

## 9. 업로드 금지 조건

아래 조건 중 하나라도 해당하면 App Store Connect에 올리지 않는다.

- iOS build number가 이전 제출 빌드와 같음
- Unity Console compile error가 있음
- `ios/Info.plist`에 `GADApplicationIdentifier`가 없음
- `GADApplicationIdentifier` 값에 `/`가 있음
- `ITSAppUsesNonExemptEncryption`이 `false`가 아님
- 실제 기기 또는 TestFlight cold launch를 확인하지 않음
- iPhone/iPad에서 상단 버튼, 설정 팝업, 업적 보상 팝업 중 하나라도 잘림
- iOS App Store 아이콘이 비어 있거나 알파가 있는 아이콘만 들어 있음
- signing team/provisioning이 비어 있음

## 10. Mac Codex 프롬프트

MacBook에서 Codex를 실행한 뒤 아래처럼 지시한다.

```text
이 저장소는 StarForge Unity 프로젝트다. 먼저 docs/mac-codex-handoff.md, docs/ios-current-build-handoff.md, docs/ios-app-store-launch-crash-build-notes.md를 읽고 현재 iOS 재제출 상태를 파악해줘.

목표:
1. Unity 6000.4.4f1로 프로젝트 import/compile 문제를 확인하고 고쳐줘.
2. iOS 플랫폼으로 전환한 뒤 StarForge.EditorTools.StarForgeBuildPreparation.BuildiOSForCI로 ios/ Xcode 프로젝트를 생성해줘.
3. 생성된 ios/Info.plist의 GADApplicationIdentifier와 ITSAppUsesNonExemptEncryption 값을 검사해줘.
4. Xcode signing/build/archive 단계에서 막히는 문제를 진단해줘.
5. 가능하면 실제 iPhone/iPad 또는 TestFlight에서 cold launch와 UI 잘림 체크리스트를 검증해줘.

주의:
- 기존 게임 밸런스/UI 변경은 되돌리지 말 것.
- Unity 생성 에셋 포맷을 불필요하게 대량 정리하지 말 것.
- 예전 star_enhance 원격을 쓰지 말고 origin/main의 My-project 저장소를 기준으로 작업할 것.
- 변경이 필요하면 최소 변경으로 고치고, 변경 파일과 검증 결과를 한국어로 정리할 것.
```
