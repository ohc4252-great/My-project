# iOS App Store Launch Crash Build Notes

이 문서는 App Store 심사 런치 크래시 재발을 막기 위한 빌드 AI용 체크 문서다.
빌드/제출 AI는 iOS 빌드 전에 이 문서를 먼저 읽고, 아래 검증을 끝낸 뒤 새 빌드를 업로드해야 한다.

## 심사 거부 요약

- 거부 일자: 2026-06-30
- 심사 기기: iPad Pro 11-inch (M4), iPhone 17 Pro Max
- 심사 OS: iPadOS 26.5, iOS 26.5.1
- 증상: 앱이 런치 직후 크래시되어 심사 불가
- 첨부 로그:
  - `crashlog-EB93412A-427E-4032-B015-97444D6571E8.ips`
  - `crashlog-59E61EBA-160D-4262-8F53-E2D46D83B9BB.ips`

두 로그 모두 핵심 시그니처가 같다.

```text
GADApplicationVerifyPublisherInitializedCorrectly
abort() called
EXC_BAD_ACCESS / SIGSEGV
```

이 패턴은 Google Mobile Ads iOS SDK가 런치 중 publisher/app identifier 설정을 검증하다가 실패한 경우에 난다.
게임 로직이나 30강 밸런스 문제가 아니라, iOS `Info.plist`의 AdMob 앱 ID 누락/오설정이 우선 원인이다.

## 반드시 구분할 ID

AdMob에는 앱 ID와 광고 유닛 ID가 모두 있고, 형식이 다르다. 혼동하면 런치 크래시가 난다.

앱 ID는 `~`가 들어간다. `Info.plist`의 `GADApplicationIdentifier`에는 반드시 이 값을 넣는다.

```text
iOS AdMob App ID: ca-app-pub-3971219491693844~2351507763
Android AdMob App ID: ca-app-pub-3971219491693844~8174680087
```

광고 유닛 ID는 `/`가 들어간다. 이 값들은 `GADApplicationIdentifier`에 넣으면 안 된다.

```text
iOS mining rewarded: ca-app-pub-3971219491693844/4869523183
iOS destruction keep rewarded: ca-app-pub-3971219491693844/7495686527
Android mining rewarded: ca-app-pub-3971219491693844/1293240258
Android destruction keep rewarded: ca-app-pub-3971219491693844/5277054006
```

## 현재 수정 지점

빌드 후처리 파일:

```text
Assets/_Project/Editor/StarForgeIosExportCompliance.cs
```

이 파일은 iOS 빌드 산출물의 `Info.plist`에 다음 값을 강제로 기록한다.

```text
GADApplicationIdentifier = ca-app-pub-3971219491693844~2351507763
ITSAppUsesNonExemptEncryption = false
```

주의:

- 이 파일은 `#if UNITY_IOS`로 감싸면 안 된다.
- 파일은 `Editor` 폴더 아래에 있으므로 에디터 전용으로 컴파일된다.
- 내부에서 `target != BuildTarget.iOS`를 검사하므로 Android 빌드에는 영향을 주지 않는다.
- 앱 ID 형식에 `/`가 들어가면 빌드가 실패해야 정상이다.

Google Mobile Ads 설정 에셋도 같은 iOS 앱 ID를 갖고 있어야 한다.

```text
Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset
adMobIOSAppId: ca-app-pub-3971219491693844~2351507763
```

패키지 기본 후처리도 `GADApplicationIdentifier`를 쓸 수 있지만, App Store 제출 빌드에서 누락된 전력이 있으므로 StarForge 후처리 결과를 산출물에서 직접 확인해야 한다.

## 빌드 AI 작업 순서

1. Unity 에디터에서 컴파일 에러가 없는지 확인한다.
2. `Assets/_Project/Editor/StarForgeIosExportCompliance.cs`와 `.meta`가 빌드 대상 브랜치에 포함되어 있는지 확인한다.
3. Unity iOS export를 실행한다.
   - CI 진입점: `StarForge.EditorTools.StarForgeBuildPreparation.BuildiOSForCI`
   - 기본 산출물 위치: `ios/`
4. export 직후 `ios/Info.plist`를 검사한다.
5. CocoaPods 단계가 있으면 `ios/Podfile` 기준으로 `pod install`을 실행한다.
6. Xcode archive 또는 CI `build-ipa`를 수행한다.
7. App Store Connect 업로드 전에 실제 기기에서 cold launch를 확인한다.
8. iPhone뿐 아니라 iPad 호환 실행도 확인한다. App Store에서 iPad 다운로드가 가능하면 iPad에서도 런치 크래시가 없어야 한다.

## Info.plist 필수 검증

macOS에서 Unity iOS export 후 실행:

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

## 실기기 검증

최소 검증:

- 앱 삭제 후 재설치
- 네트워크 켠 상태로 cold launch
- 네트워크 끈 상태로 cold launch
- 첫 화면 진입
- 설정/약관/개인정보 링크 접근
- 광고 버튼이 있는 화면까지 이동
- 광고 로드 실패 시에도 앱이 죽지 않는지 확인

가능하면 추가 검증:

- iPhone 실제 기기
- iPad 실제 기기 또는 Xcode/iOS 시뮬레이터의 iPad 대상
- TestFlight 설치본 cold launch

## 제출 전 금지 사항

- `GADApplicationIdentifier`에 rewarded ad unit ID를 넣지 않는다.
- `Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset`를 삭제하지 않는다.
- `Assets/_Project/Editor/StarForgeIosExportCompliance.cs`를 `UNITY_IOS` 조건부 컴파일로 다시 감싸지 않는다.
- `Packages/com.google.ads.mobile` 내부 코드는 플러그인 업그레이드가 목적이 아니면 직접 수정하지 않는다.
- 심볼리케이션만 하고 새 빌드 검증 없이 재제출하지 않는다.
- App Store 제출 직전에는 Unity가 생성한 `Info.plist` 산출물을 확인하지 않은 채 업로드하지 않는다.

## App Review 답변 초안

새 빌드 업로드 후 App Review 답변에는 아래처럼 짧게 설명하면 된다.

```text
The launch crash was caused by a missing/misconfigured Google Mobile Ads iOS application identifier in the generated Info.plist. We updated the iOS build post-processing step to always stamp GADApplicationIdentifier with the correct AdMob iOS app ID and verified the app launches successfully before submitting this new build.
```

한국어로 답변할 경우:

```text
첨부된 크래시 로그에서 Google Mobile Ads SDK의 GADApplicationVerifyPublisherInitializedCorrectly 단계에서 런치 크래시가 발생한 것을 확인했습니다. iOS 빌드 후처리에서 Info.plist에 올바른 GADApplicationIdentifier가 항상 기록되도록 수정했고, 새 빌드에서 앱 실행을 검증한 뒤 제출했습니다.
```
