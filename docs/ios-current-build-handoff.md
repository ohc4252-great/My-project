# StarForge iOS Current Build Handoff

기준일: 2026-07-01

이 문서는 현재 작업 버전으로 iOS 빌드와 App Store 재제출을 진행할 빌드 AI용 인계서다.
빌드 AI는 이 문서와 `docs/ios-app-store-launch-crash-build-notes.md`를 먼저 읽고, 검증이 끝난 빌드만 업로드한다.

## 1. 이번 빌드 목적

- App Store 1차 제출은 `1.0 (1)` 빌드가 런치 직후 크래시되어 거부됐다.
- 크래시 핵심 시그니처는 `GADApplicationVerifyPublisherInitializedCorrectly`이며, 원인은 iOS `Info.plist`의 AdMob 앱 ID 누락 또는 오설정으로 본다.
- 이번 빌드는 해당 런치 크래시 재발 방지 후처리를 포함한 상태에서 다시 iOS 빌드를 만드는 것이 목적이다.

## 2. 현재 프로젝트 기준값

- Unity: `6000.4.4f1`
- Bundle ID: `com.starforge.stellarsmith`
- Marketing version: `1.0.0`
- 현재 iOS build number: `1`
- iOS minimum target: `15.0`
- Scripting backend: iOS 빌드 준비 코드에서 `IL2CPP`로 설정
- 방향: 세로 고정
- Google Mobile Ads Unity package: `com.google.ads.mobile` `11.2.0`

중요: App Store Connect에는 이미 `1.0 (1)`이 제출된 이력이 있으므로, 재제출 전 iOS build number를 `2` 이상으로 올려야 한다. 현재 `ProjectSettings/ProjectSettings.asset`의 `buildNumber.iPhone`은 아직 `1`이다.

## 3. 절대 확인할 AdMob 값

`Info.plist`의 `GADApplicationIdentifier`에는 앱 ID만 들어가야 한다.

```text
iOS AdMob App ID: ca-app-pub-3971219491693844~2351507763
```

아래는 광고 유닛 ID다. `/`가 들어가므로 `GADApplicationIdentifier`에 넣으면 안 된다.

```text
iOS mining rewarded: ca-app-pub-3971219491693844/4869523183
iOS destruction keep rewarded: ca-app-pub-3971219491693844/7495686527
```

현재 후처리 소스:

```text
Assets/_Project/Editor/StarForgeIosExportCompliance.cs
```

이 파일은 iOS export 후 `Info.plist`에 다음 값을 강제로 기록한다.

```text
GADApplicationIdentifier = ca-app-pub-3971219491693844~2351507763
ITSAppUsesNonExemptEncryption = false
```

`Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset`에도 같은 iOS 앱 ID가 있어야 한다.

## 4. 현재 버전의 사용자 변경 요약

이번 iOS 빌드에 포함될 가능성이 있는 최근 UI/게임플레이 변경은 아래와 같다.

- 메인 화면의 `별 탐사하기` 버튼에서 오늘 남은 탐사 횟수 문구를 제거했다.
- 오늘 남은 탐사 횟수, 광고 추가 탐험, 일일 완료 상태는 탐사 화면 내부에서만 다룬다.
- 메인 상단 버튼 4개를 이미지 기반 UI로 교체했다.
  - `Assets/_Project/Resources/HudButtons/search.png`
  - `Assets/_Project/Resources/HudButtons/change.png`
  - `Assets/_Project/Resources/HudButtons/dogam.png`
  - `Assets/_Project/Resources/HudButtons/setting.png`
- 설정 팝업 좌우 폭을 줄여 외곽 프레임이 잘리지 않도록 조정했다.
- 업적 보상 수령 결과 팝업에서 보상 수량을 아이콘 오른쪽이 아니라 아이콘 아래로 배치했다.
- 업적 보상 5개 동시 수령 케이스에서 슬롯과 수량이 잘리지 않아야 한다.
- 강화 연출 체크 UI 문구가 `스킵`에서 `연출`로 바뀐 상태다.

## 5. 빌드 전 작업트리 정리

빌드 전에 `git status --short`로 제출할 변경과 제외할 변경을 분리한다.

반드시 포함해야 하는 파일:

```text
Assets/_Project/Editor/StarForgeIosExportCompliance.cs
Assets/_Project/Editor/StarForgeIosExportCompliance.cs.meta
Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset
Assets/_Project/Scripts/StarForge/Presentation/StarForgeGameController.cs
Assets/_Project/Scripts/StarForge/Presentation/StarForgeHudView.cs
Assets/_Project/Resources/HudButtons.meta
Assets/_Project/Resources/HudButtons/search.png
Assets/_Project/Resources/HudButtons/search.png.meta
Assets/_Project/Resources/HudButtons/change.png
Assets/_Project/Resources/HudButtons/change.png.meta
Assets/_Project/Resources/HudButtons/dogam.png
Assets/_Project/Resources/HudButtons/dogam.png.meta
Assets/_Project/Resources/HudButtons/setting.png
Assets/_Project/Resources/HudButtons/setting.png.meta
```

포함 여부를 재검토할 파일:

```text
ProjectSettings/ProjectSettings.asset
My project.slnx
Assets/Resources/PerformanceTestRunInfo.json
Assets/Resources/PerformanceTestRunSettings.json
Assets/ChatGPT Image ... -Photoroom.png
Assets/별 탐사하기-Photoroom.png
```

현재 `ProjectSettings/ProjectSettings.asset` 변경은 Android `versionCode` 12 -> 13만 보인다. iOS 빌드와 직접 관련 없으므로, 이 변경을 포함할지는 별도 판단한다.
원본 이미지 파일이 `Assets/` 루트에 중복으로 남아 있으면 앱 용량과 커밋 노이즈가 늘어난다. 실제 런타임은 `Assets/_Project/Resources/HudButtons/` 경로를 사용한다.

## 6. 빌드 순서

1. Unity 에디터에서 컴파일 에러가 없는지 확인한다.
2. iOS build number를 이전 제출 빌드보다 높인다. 이번 재제출은 최소 `2`.
3. iOS 플랫폼으로 전환한다.
4. 다음 CI 진입점 또는 Unity 메뉴/CI 설정으로 iOS export를 만든다.

```text
StarForge.EditorTools.StarForgeBuildPreparation.BuildiOSForCI
```

기본 export 위치:

```text
ios/
```

5. export 직후 `ios/Info.plist`를 검사한다.
6. CocoaPods가 필요한 경우 `ios/Podfile` 기준으로 `pod install`을 실행한다.
7. Xcode에서 signing, archive, upload를 진행한다.
8. 업로드 전에 실제 기기 또는 TestFlight 설치본으로 cold launch를 확인한다.

## 7. Info.plist 필수 검증

macOS에서 Unity iOS export 후 실행한다.

```bash
/usr/libexec/PlistBuddy -c "Print :GADApplicationIdentifier" ios/Info.plist
/usr/libexec/PlistBuddy -c "Print :ITSAppUsesNonExemptEncryption" ios/Info.plist
```

기대값:

```text
ca-app-pub-3971219491693844~2351507763
false
```

보조 확인:

```bash
plutil -p ios/Info.plist | grep -E "GADApplicationIdentifier|ITSAppUsesNonExemptEncryption"
grep -n "GADApplicationIdentifier" -A1 ios/Info.plist
```

`GADApplicationIdentifier`가 없거나, 값에 `/`가 들어가 있으면 그 빌드는 업로드하지 않는다.

## 8. iOS 실기기 검증 체크리스트

최소 검증:

- 앱 삭제 후 재설치
- 네트워크 켠 상태 cold launch
- 네트워크 끈 상태 cold launch
- 첫 실행 약관/개인정보 동의 화면
- 메인 화면 진입
- 상단 이미지 버튼 4개가 잘리지 않고 눌리는지 확인
- `별 탐사하기` 진입 후 남은 횟수/광고 추가 탐험 상태가 탐사 화면 안에서 표시되는지 확인
- 설정 팝업 좌우 프레임이 잘리지 않는지 확인
- 설정 화면의 개인정보처리방침/이용약관 링크가 열리는지 확인
- 도감 > 업적 > `모두 수령`에서 보상 5개가 한 번에 뜰 때 수량이 아이콘 아래에 표시되고 잘리지 않는지 확인
- 광고 로드 실패 상태에서도 앱이 종료되지 않는지 확인
- 보상형 광고 완료 시 보상이 한 번만 지급되는지 확인

기기 범위:

- iPhone 실제 기기
- iPad 실제 기기 또는 iPad 시뮬레이터
- 가능하면 TestFlight 설치본

App Review는 iPad Pro 11-inch (M4)와 iPhone 17 Pro Max에서 확인했으므로, iPad 호환 실행을 생략하지 않는다.

## 9. 업로드 금지 조건

아래 중 하나라도 해당하면 App Store Connect에 업로드하지 않는다.

- iOS build number가 이전 제출 빌드와 같다.
- Unity Console에 컴파일 에러가 있다.
- `ios/Info.plist`에 `GADApplicationIdentifier`가 없다.
- `GADApplicationIdentifier` 값에 `/`가 들어 있다.
- `ITSAppUsesNonExemptEncryption`이 `false`로 기록되지 않았다.
- 실제 기기 또는 TestFlight에서 cold launch를 확인하지 않았다.
- 상단 이미지 버튼, 설정 팝업, 업적 보상 결과 팝업 중 하나라도 iPhone/iPad에서 잘린다.
- iOS App Store 아이콘 슬롯이 비어 있거나 알파가 있는 아이콘만 들어 있다.
- signing team/provisioning 설정이 비어 있는 상태로 archive를 시도한다.

## 10. App Review 답변 초안

새 빌드를 업로드한 뒤 App Review 답변에는 아래처럼 짧게 설명한다.

```text
The launch crash was caused by a missing/misconfigured Google Mobile Ads iOS application identifier in the generated Info.plist. We added an iOS build post-processing step that always stamps GADApplicationIdentifier with the correct AdMob iOS app ID, verified the generated Info.plist, and confirmed the new build launches on device before resubmitting.
```

한국어로 답변할 경우:

```text
첨부된 크래시 로그에서 Google Mobile Ads SDK의 GADApplicationVerifyPublisherInitializedCorrectly 단계에서 런치 크래시가 발생한 것을 확인했습니다. iOS 빌드 후처리에서 Info.plist에 올바른 GADApplicationIdentifier가 항상 기록되도록 수정했고, 생성된 Info.plist와 새 빌드의 실제 기기 실행을 확인한 뒤 다시 제출했습니다.
```
