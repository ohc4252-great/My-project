# 별 강화하기 스토어 제출 체크리스트

기준일: 2026-06-19

이 문서는 현재 앱 구조를 기준으로 한다.

- 로그인 없음
- 서버 저장 없음
- Google AdMob 보상형 광고 있음
- 게임 진행/설정/약관 동의는 PlayerPrefs 로컬 저장
- 출시 버전: Android `versionName 1.0.0` / `versionCode 1`, iOS `1.0.0 (1)`
- 출시 저장 키는 `StarForge.SaveData.v2`이며, 기존 테스트 저장 데이터는 출시 데이터로 이관하지 않는다.
- Android App ID: `ca-app-pub-3971219491693844~8174680087`
- iOS App ID: `ca-app-pub-3971219491693844~2351507763`
- Android 탐험/게임 횟수 +1 Rewarded: `ca-app-pub-3971219491693844/1293240258`
- Android 부활 Rewarded: `ca-app-pub-3971219491693844/5277054006`
- iOS 탐험 Rewarded: `ca-app-pub-3971219491693844/4869523183`
- iOS 부활 Rewarded: `ca-app-pub-3971219491693844/7495686527`

## 1. 공개 URL

GitHub Pages 배포 상태:

- 개인정보처리방침: `https://ohc4252-great.github.io/starforge-policies/privacy.html`
- 이용약관: `https://ohc4252-great.github.io/starforge-policies/terms.html`

`My-project` 저장소는 private이라 현재 GitHub Pages 배포가 제한된다. 실제 공개 배포는 정책 전용 public 저장소 `ohc4252-great/starforge-policies`의 `main/docs`를 사용한다.

정책 문서를 갱신할 때는 다음 파일을 정책 저장소에도 반영한다.

- `docs/index.html`
- `docs/privacy.html`
- `docs/terms.html`
- `docs/.nojekyll`

출시 전 반드시 실제 운영자 이메일 또는 고객지원 URL을 문서에 추가한다.

## 2. 앱 첫 실행 동의

구현 상태:

- 첫 접속 시 개인정보처리방침 동의 체크박스 표시
- 첫 접속 시 이용약관 동의 체크박스 표시
- 두 항목을 모두 체크해야 시작 가능
- 동의 상태는 `PlayerPrefs`에 저장
- 설정 화면에 개인정보처리방침/이용약관 링크 제공

주의:

- 이 동의 UI는 일반 약관/개인정보 고지 동의다.
- 앱 시작 시 Google UMP SDK가 동의 상태를 갱신하고, 필요한 경우 동의 양식을 표시한 뒤에만 광고 SDK를 초기화한다.
- 현재 AdMob 콘솔에 GDPR 동의 메시지를 게시하지 않았고 EEA 대상 개인화 광고를 제공하지 않으므로, 설정 화면의 `광고 개인정보 설정` 진입점은 제거했다. EEA/UK 대상으로 UMP 동의 메시지를 게시하게 되면 개인정보 선택지 재진입점을 다시 추가해야 한다.

## 3. Google Play Console 데이터 보안 섹션 초안

앱이 수집 또는 공유하는 데이터:

- 위치: 대략적인 위치
  - 근거: AdMob SDK가 IP 주소를 수집하고, IP 주소가 기기 일반 위치 추정에 사용될 수 있음
  - 목적: 광고 또는 마케팅, 분석, 사기 방지/보안
  - 공유: 예, Google AdMob
- 앱 활동: 앱 상호작용
  - 예: 앱 실행, 탭, 광고 조회/동영상 시청
  - 목적: 광고 또는 마케팅, 분석, 사기 방지/보안
  - 공유: 예, Google AdMob
- 앱 정보 및 성능: 진단 정보
  - 예: 앱 실행 시간, 중단률, 에너지 사용량 등 SDK/앱 성능 정보
  - 목적: 분석, 앱 기능, 사기 방지/보안
  - 공유: 예, Google AdMob
- 기기 또는 기타 ID: 광고 ID, 앱 세트 ID, 기기/계정 식별자
  - 목적: 광고 또는 마케팅, 분석, 사기 방지/보안
  - 공유: 예, Google AdMob

보안 관행:

- 전송 중 암호화: 예
- 데이터 삭제 요청 방법 제공: 개인정보처리방침의 문의 경로 및 앱 데이터 초기화 안내
- 계정 생성: 없음

광고 ID:

- 앱에서 광고 ID 사용 여부: 예
- 목적: 광고 또는 분석

타겟 연령:

- 제출 기준은 만 13세 이상이다.
- Play Console `앱 콘텐츠 > 타겟층 및 콘텐츠`에서는 13세 미만 연령대를 선택하지 않는다.
- 권장 선택: `13-15`, `16-17`, `18+`.
- 앱 설명, 스크린샷, 아이콘, 프로모션 문구가 만 13세 미만 어린이를 직접 겨냥하지 않도록 한다.
- 코드 기준: AdMob 초기화 전에 `TagForChildDirectedTreatment=False`, `MaxAdContentRating=T`를 설정한다.
- 만 13세 미만을 포함하는 방향으로 다시 변경할 경우에는 가족 정책, 중립적 연령 심사, Families self-certified 광고 SDK 조건을 재검토해야 한다.

## 4. App Store Connect 개인정보 라벨 초안

AdMob만 사용하는 현재 기준으로 다음 항목을 검토한다.

- Identifiers > Device ID
  - 사용 목적: Third-Party Advertising, Analytics, App Functionality/Fraud Prevention
  - Tracking 여부: 개인화 광고를 사용하면 Tracking 가능성이 높음. ATT와 UMP 동의 흐름 필요.
- Usage Data > Product Interaction
  - 사용 목적: Third-Party Advertising, Analytics
- Usage Data > Advertising Data
  - 사용 목적: Third-Party Advertising, Analytics
- Diagnostics > Performance Data / Other Diagnostic Data
  - 사용 목적: Analytics, App Functionality
- Location > Coarse Location
  - 근거: IP 주소 기반 일반 위치 추정 가능성
  - 사용 목적: Third-Party Advertising, Analytics, Fraud Prevention

App Store 필수 링크:

- Privacy Policy URL: `https://ohc4252-great.github.io/starforge-policies/privacy.html`
- Privacy Choices URL: 현재는 선택. UMP/광고 선택 화면을 붙이면 해당 안내 URL 추가 권장.

ATT:

- 개인화 광고 또는 광고 ID 기반 추적을 사용하는 경우 `NSUserTrackingUsageDescription`과 ATT 권한 요청 흐름을 검토해야 한다.
- 현재 앱은 ATT 권한을 요청하지 않는다. App Store Connect 개인정보 라벨은 실제 AdMob 계정의 광고 개인화·추적 설정과 일치하게 최종 선택해야 한다. 추적 기반 개인화 광고를 활성화할 경우 ATT 권한 요청 구현과 `NSUserTrackingUsageDescription`을 추가한다.

## 5. AdMob/광고 정책 체크

- 보상형 광고는 사용자가 명확히 선택한 경우에만 표시한다.
- 광고를 닫거나 실패한 경우 보상을 지급하지 않는다.
- 광고 버튼 문구는 보상 내용을 명확히 표시한다.
- 광고와 일반 UI가 오인되지 않도록 한다.
- 현재 코드의 AdMob reflection 초기화는 SDK 초기화 전에 `RequestConfiguration`을 설정한다.
- 만 13세 이상 타겟 기준으로 `TagForChildDirectedTreatment=False`를 설정한다.
- 최대 광고 콘텐츠 등급은 `T`로 제한한다.
- 만 13세 미만 어린이 대상 광고 흐름은 제공하지 않는다.
- 테스트 중에는 테스트 광고 또는 테스트 기기를 사용한다.
- 출시 전 실제 기기에서 Android/iOS 각각 광고 로드, 광고 완료, 광고 실패, 보상 지급을 확인한다.

## 5-1. Google Play 타겟층 체크

현재 제출 방향은 `13세 이상`이며, 13세 미만 어린이는 타겟층에서 제외한다.

- Play Console 타겟 연령에서 13세 미만 그룹을 선택하지 않는다.
- 앱이 만 13세 미만에게 의도치 않게 어필할 수 있는 마케팅 요소가 있으면 제거한다.
- 스토어 설명에 `만 13세 이상 대상`을 명시한다.
- 개인정보처리방침과 이용약관에 만 13세 미만 비대상 문구를 유지한다.
- 13세 미만 포함으로 바꾸는 경우에만 가족 정책과 중립적 연령 심사를 다시 검토한다.

Play Console 입력 권장:

- 타겟 연령: `13-15`, `16-17`, `18+`
- 앱이 어린이를 대상으로 하나요: 아니오
- 광고 포함 여부: 예
- 가족 정책 대상: 13세 미만을 선택하지 않는 한 해당 없음

## 6. 빌드 전 체크

- Google Mobile Ads Unity Plugin 설치 확인
- External Dependency Manager Android Resolver 실행
- Android `com.google.android.gms.permission.AD_ID` 반영 확인
- Android target SDK 35 및 ARM64 설정 확인
- Android 업로드 키스토어를 설정한 뒤 AAB를 생성
- iOS App ID와 ad unit ID 반영 확인
- iOS 빌드 후 Info.plist의 `GADApplicationIdentifier` 확인
- iOS 배포 인증서, 프로비저닝 프로파일, App Store Connect 앱 레코드와 Apple ID를 Codemagic에 연결
- 개인정보처리방침 URL 공개 접속 확인
- 이용약관 URL 공개 접속 확인
- 설정 화면 링크가 실제 기기에서 브라우저로 열리는지 확인
- 첫 실행 동의 화면이 앱 삭제/재설치 후 다시 나오는지 확인
- 데이터 초기화가 게임 데이터만 초기화하고 법적 동의 상태는 유지되는지 확인

## 7. 출시 전 남은 필수 입력

- 실제 개발자/사업자명
- 실제 고객지원 이메일
- 스토어 등록용 개인정보처리방침 URL
- 스토어 등록용 고객지원 URL
- 앱 아이콘, 스크린샷, 설명, 키워드
- 연령 등급 설문
- 광고 포함 여부
- 데이터 보안/개인정보 라벨 최종 검토
- 국가별 개인정보, 청소년 대상 광고, 연령 제한 정책 검토
