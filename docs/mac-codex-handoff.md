# Mac Codex Handoff

이 문서는 Windows 작업 환경에서 진행한 StarForge 작업 내역과, MacBook에서 Codex/Unity/Xcode로 이어서 해야 할 일을 정리한 인계 문서입니다.

## 현재 저장소 상태

- GitHub 저장소: `https://github.com/ohc4252-great/star_enhance.git`
- 브랜치: `main`
- 마지막 푸시 커밋: `b80a0a1 Update StarForge black hole and balance assets`
- Unity 버전: `6000.4.4f1`
- 대상 Mac: 2019년형 13인치 MacBook Pro, Intel
- macOS: `15.7.7`
- 필요한 Xcode: `Xcode 26.3 Universal.xip`

주의:
- 2019 Intel MacBook Pro에서는 `Silicon.xip`가 아니라 `Universal.xip`를 사용해야 합니다.
- Xcode 첫 실행의 platform support 선택 화면에서는 `macOS` 기본값 + `iOS`만 선택합니다. `watchOS`, `tvOS`, `visionOS`는 저장공간 절약을 위해 제외합니다.

## 이미 적용된 주요 작업

### UI

- 메인 업적 토스트에서 업적 이름 크기를 약 1.3배로 키움.
- 업적 플레이버 텍스트 크기를 약 1.4배로 키움.
- 보상 수령 팝업의 카드 높이를 크게 압축.
- 보상 수령 팝업의 획득 아이템 이미지와 수량 텍스트를 키움.
- 수량 텍스트 크기 기준을 25로 맞춤.
- `별 채굴하기` 문구를 `별 탐사하기`로 변경.

### 탐험/업적

- 탐험 관련 업적 5개 추가:
  - `첫 항로`
  - `오늘도 탐험 완료`
  - `깊은 우주 진입`
  - `완전 항로 개척`
  - `탐사 베테랑`
- `오늘도 탐사대` 계열 문구는 `오늘도 탐험 완료`로 반영.
- 탐험 완료 시점에 업적 카운트/최고 점수를 기록하도록 연결.

### 밸런스

- 블랙홀 분해 보상 조정:
  - 1단계: 원초의 별 2, 특이성 조각 10
  - 2단계: 원초의 별 4, 특이성 조각 25
  - 3단계: 원초의 별 8, 특이성 조각 50
  - 4단계: 원초의 별 20, 특이성 조각 160
  - 5단계: 원초의 별 25, 특이성 조각 220
  - 6단계 이상: 기존 공식 유지, `원초의 별 = 5 * (level + 1)`, `특이성 조각 = 50 * level`
- 29강 업적 보상 3종 조정:
  - 원초의 별 10
  - 특이성 조각 100
- 탐험 보상 조정:
  - 100% 이상: 순수핵 100, 특이성 조각 5, 원초의 별 1
  - 50% 이상: 순수핵 100, 특이성 조각 2
  - 30% 이상: 순수핵 50, 특이성 조각 1
  - 하위 구간의 순수핵 수량은 유지
- 탐험 광고 추가 횟수를 일일 5회로 제한.
- 소멸 시 체크포인트 광고는 3분 제한 로직이 포함된 상태.

### 저장소에 포함된 큰 변경

- 블랙홀 URP 에셋 추가.
- `Assets/_Project/Resources/BlackHole.prefab` 추가.
- 스토어 스크린샷/프로모 이미지 추가.
- `Assets/_Recovery/0 (3).unity`도 현재 커밋에 포함됨.

## 검증 상태

Windows 환경에서 실행한 검증:

- `git diff --check`: `My project.slnx` 공백 수정 후 통과.
- `git push star_enhance main`: 성공.
- Git LFS 오브젝트 29개 업로드 성공.
- `dotnet`, `msbuild`: Windows 환경에서 찾지 못해 C# 컴파일 검증은 못 함.
- Unity Editor 컴파일/실행 검증은 아직 Mac에서 필요.

주의:
- 커밋 직전 `git diff --cached --check`는 Unity 생성 에셋의 trailing whitespace 때문에 실패했습니다. `.meta`, `.mat`, `.unity`, `.rtf`, `.shadergraph` 포맷을 임의 정리하지 않고 그대로 커밋했습니다.

## MacBook 초기 세팅

### 1. 터미널 열기

- `Command + Space`
- `Terminal` 또는 `터미널` 검색
- Enter

### 2. Xcode 확인

```bash
sudo xcode-select -s /Applications/Xcode.app/Contents/Developer
sudo xcodebuild -license accept
xcodebuild -runFirstLaunch
xcodebuild -version
xcodebuild -showsdks
```

`xcodebuild -showsdks` 결과에 `iphoneos`가 보이면 iOS 빌드 준비가 된 상태입니다.

### 3. Homebrew 설치 확인

```bash
brew --version
```

없으면 설치:

```bash
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
```

### 4. Git LFS 설치

```bash
brew install git-lfs
git lfs install
```

### 5. 프로젝트 클론

```bash
mkdir -p ~/Projects
cd ~/Projects
git clone https://github.com/ohc4252-great/star_enhance.git
cd star_enhance
git lfs pull
```

## Unity 작업 순서

1. Unity Hub 설치.
2. Unity `6000.4.4f1` 설치.
3. 설치 모듈은 `iOS Build Support`만 필수로 선택.
4. Unity Hub에서 `~/Projects/star_enhance` 프로젝트 열기.
5. 최초 import가 끝날 때까지 대기.
6. `File > Build Profiles` 또는 `File > Build Settings`로 이동.
7. `iOS` 선택 후 `Switch Platform`.
8. Player Settings 확인:
   - Bundle Identifier
   - Version
   - Build Number
   - Signing Team
9. iOS 빌드 위치 예:

```text
~/Builds/StarForge-iOS
```

## Xcode 작업 순서

1. Unity가 만든 iOS 빌드 폴더 열기.
2. `.xcworkspace`가 있으면 그것을 열고, 없으면 `.xcodeproj`를 엽니다.
3. `Xcode > Settings > Accounts`에서 Apple ID 로그인.
4. 프로젝트의 `Signing & Capabilities`에서:
   - Team 선택
   - Automatically manage signing 체크
   - Bundle Identifier 확인
5. 아이폰 실기기 연결.
6. 아이폰에서 `이 컴퓨터 신뢰` 허용.
7. 아이폰 설정에서 개발자 모드 활성화:

```text
설정 > 개인정보 보호 및 보안 > 개발자 모드
```

8. Xcode에서 연결한 iPhone 선택 후 Run.
9. 실기기 실행이 되면 `Product > Archive`.
10. `Distribute App > App Store Connect > Upload`.

## 저장공간 관리

현재 MacBook 여유 공간이 약 40GB라면 매우 빡빡합니다.

대략 필요한 공간:

- Xcode 26.3 설치 후: 8~10GB
- Unity 6000.4.4f1 + iOS Build Support: 8~10GB
- 프로젝트 소스 + LFS: 2GB 안팎
- Unity Library 캐시: 8~11GB
- iOS 빌드 결과물: 1~2GB
- Xcode DerivedData / Archive: 2~4GB
- macOS 임시 여유: 5~10GB

공간이 부족할 때 정리 명령:

```bash
rm -rf ~/Library/Developer/Xcode/DerivedData
rm -rf ~/Library/Developer/Xcode/Archives/*
rm -rf ~/Library/Unity/cache
xcrun simctl delete unavailable
sudo tmutil thinlocalsnapshots / 20000000000 4
```

가능하면 외장 SSD 사용이 가장 안정적입니다. 외장 SSD가 있다면 프로젝트, Unity 빌드 결과물, Xcode DerivedData를 외장으로 빼는 것을 권장합니다.

## Mac Codex에게 맡길 작업 프롬프트

맥북에서 Codex를 실행한 뒤 아래처럼 지시하면 됩니다.

```text
이 저장소는 StarForge Unity 프로젝트다. 먼저 docs/mac-codex-handoff.md를 읽고 현재 상태를 파악해줘.

목표:
1. Unity 6000.4.4f1로 프로젝트를 열었을 때 발생하는 컴파일/import 문제를 확인하고 고쳐줘.
2. iOS 플랫폼으로 Switch Platform 한 뒤 빌드가 가능한지 확인해줘.
3. 가능하면 Unity iOS 빌드 산출물을 생성해줘.
4. Xcode 프로젝트에서 signing/build/archive 단계에서 막히는 문제를 진단해줘.
5. 수정이 필요하면 최소 변경으로 고치고, 변경 파일과 검증 결과를 한국어로 정리해줘.

주의:
- 기존 게임 밸런스/UI 변경은 되돌리지 말 것.
- Unity 생성 에셋 포맷을 불필요하게 대량 정리하지 말 것.
- 저장공간이 부족하니 캐시/빌드 산출물 위치와 크기를 계속 확인할 것.
```

## 다음에 가장 먼저 확인할 것

Mac에서 Codex 또는 직접 터미널로 아래를 확인합니다.

```bash
cd ~/Projects/star_enhance
git status -sb
git log --oneline -3
xcodebuild -version
xcodebuild -showsdks
```

그 다음 Unity Hub에서 프로젝트를 열고, 콘솔 에러를 확인합니다.
