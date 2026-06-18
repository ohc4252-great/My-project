# 별 강화하기 스토어 제출 체크리스트

기준일: 2026-06-16

이 문서는 현재 앱 구조를 기준으로 한다.

- 로그인 없음
- 서버 저장 없음
- Google AdMob 보상형 광고 있음
- 게임 진행/설정/약관 동의는 PlayerPrefs 로컬 저장
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
- EEA/영국/스위스 또는 미국 일부 주 개인정보 규제 대응용 광고 동의는 Google UMP SDK 흐름을 별도로 붙이는 것이 안전하다.

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

아동 대상 여부:

- Play Console에서 타겟층에 어린이를 포함하면 가족 정책 대상이다.
- 현재처럼 중립적 연령 심사 없이 어린이 포함 전체 연령으로 제출하려면 앱 전체 콘텐츠와 모든 광고 요청을 어린이 적합 기준으로 맞춘다.
- 코드 기준: AdMob 초기화 전에 `TagForChildDirectedTreatment=True`, `MaxAdContentRating=G`를 설정한다.
- AdMob 외 광고 SDK, mediation custom event, 웹뷰 광고를 추가하면 해당 광고 소스도 Google Play Families self-certified 조건을 별도 확인해야 한다.

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
- 현재 코드에는 ATT 팝업 구현이 없다. iOS 출시 전 UMP + ATT 정책 플로우를 추가하는 것이 안전하다.

## 5. AdMob/광고 정책 체크

- 보상형 광고는 사용자가 명확히 선택한 경우에만 표시한다.
- 광고를 닫거나 실패한 경우 보상을 지급하지 않는다.
- 광고 버튼 문구는 보상 내용을 명확히 표시한다.
- 광고와 일반 UI가 오인되지 않도록 한다.
- 어린이 포함 타겟으로 제출할 경우 모든 광고 요청은 어린이 적합 광고로 제한한다.
- 현재 코드의 AdMob reflection 초기화는 SDK 초기화 전에 `RequestConfiguration`을 설정한다.
- 최대 광고 콘텐츠 등급은 `G`로 제한한다.
- 어린이 또는 연령 미확인 사용자에게 광고를 제공할 때는 Google Play Families self-certified 광고 SDK/소스만 사용한다.
- 테스트 중에는 테스트 광고 또는 테스트 기기를 사용한다.
- 출시 전 실제 기기에서 Android/iOS 각각 광고 로드, 광고 완료, 광고 실패, 보상 지급을 확인한다.

## 5-1. Google Play 가족 정책 체크

Play Console에서 타겟층에 어린이를 포함한다고 선언하면 다음을 만족해야 한다.

- 앱 콘텐츠가 어린이에게 적합해야 한다.
- 어린이가 볼 수 있는 광고는 어린이에게 적합해야 한다.
- 어린이 또는 연령 미확인 사용자에게 광고를 제공할 때는 Google Play Families self-certified 광고 SDK/소스를 사용해야 한다.
- 앱, API, SDK, 광고가 COPPA, GDPR 등 아동 관련 법률을 준수해야 한다.
- 앱 전체를 가족 정책 기준으로 맞추거나, 중립적인 연령 심사를 구현해서 어린이 사용자에게만 가족 정책 흐름을 적용해야 한다.

현재 권장 제출 방향:

- 연령 심사를 아직 구현하지 않았으므로 앱 전체를 가족 정책 기준으로 제출한다.
- AdMob mediation/custom event는 사용하지 않거나, 사용하는 경우 Families self-certified 소스만 연결한다.
- Play Console `앱 콘텐츠 > 타겟층 및 콘텐츠`에서 어린이 포함으로 선택한 경우, 광고 포함 여부를 `예`로 답하고 광고가 가족 정책 기준으로 제한된다고 설명한다.
- 개인정보처리방침에는 로그인 없음, 로컬 저장, AdMob 데이터 처리, 어린이 대상 광고 제한을 명시한다.

## 6. 빌드 전 체크

- Google Mobile Ads Unity Plugin 설치 확인
- External Dependency Manager Android Resolver 실행
- Android `com.google.android.gms.permission.AD_ID` 반영 확인
- Android min/target SDK와 Play 정책 확인
- iOS App ID와 ad unit ID 반영 확인
- iOS 빌드 후 Info.plist의 `GADApplicationIdentifier` 확인
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
- 국가별 개인정보 및 아동 대상 정책 검토
