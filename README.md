# Star Forge / 별의 제련소

Unity 모바일 강화형 수집 게임 MVP입니다. 기획서의 1차 MVP 기준에 맞춰 오프라인 강화 루프, 로컬 저장, 기본 3D 행성, HUD, 파티클 연출을 코드 중심으로 구성했습니다.

## 실행 방법

1. Unity Hub에서 이 폴더(`C:\Users\ohc42\My project`)를 프로젝트로 엽니다.
2. Unity가 패키지와 Library를 생성할 때까지 기다립니다.
3. 빈 씬이어도 Play를 누르면 `StarForgeBootstrap`이 런타임 오브젝트를 자동 생성합니다.
4. 명시적인 씬 파일이 필요하면 Unity 상단 메뉴에서 `Star Forge > Build MVP Scene`을 실행합니다.
5. 생성된 씬은 `Assets/_Project/Scenes/StarForge_MVP.unity`에 저장됩니다.

## 시작 흐름

- `Assets/StreamingAssets/loading.mp4`를 배경에서 재생합니다.
- 첫 실행 시 개인정보처리방침과 이용약관 동의 체크박스를 표시합니다.
- 두 항목에 동의하면 `화면을 터치하세요` 화면을 거쳐 게임을 시작합니다.
- 동의 이후부터는 같은 앱 설치 상태에서 바로 시작 화면으로 이동합니다.
- 현재 로그인, 회원가입, 서버 계정 저장은 사용하지 않습니다.

## 정책 문서

- 개인정보처리방침: `docs/privacy.html`
- 이용약관: `docs/terms.html`
- 스토어 제출 체크리스트: `docs/store-submission-checklist.md`
- GitHub Pages 배포 시 예상 URL:
  - `https://ohc4252-great.github.io/starforge-policies/privacy.html`
  - `https://ohc4252-great.github.io/starforge-policies/terms.html`

## 주요 구조

- `Assets/_Project/Resources/StarForgeBalance.json`
  - 강화 성공률, 재화 소모량, 균열률, 소멸률, 파괴 보상, 단계명을 관리합니다.
- `Assets/_Project/Scripts/StarForge/Core`
  - 강화 판정과 결과 타입을 관리합니다.
- `Assets/_Project/Scripts/StarForge/Save`
  - PlayerPrefs 기반 로컬 저장을 관리합니다.
- `Assets/_Project/Scripts/StarForge/Presentation`
  - 런타임 카메라, 행성, HUD, 파티클, 입력 흐름을 관리합니다.
- `Assets/_Project/Editor/StarForgeSceneBuilder.cs`
  - MVP 씬을 생성하는 Unity Editor 메뉴입니다.

## 구현된 MVP 기능

- 0~30강 데이터 테이블
- 재화 5종 데이터 구조
- 재료별 성공률, 소모량, 사용 가능 구간
- 일반 성공, 대성공, 일반 실패, 균열, 소멸
- 25강 이후 대성공 차단
- 운석 파편 20강 이후 사용 불가
- 별의 조각 25강 이후 사용 불가
- 소멸 시 파괴 보상 지급 및 0강 복귀
- 소멸 시 세이브포인트 부활 (5/10/15/20/25강, 재화 소모, 파괴 단계 이하 지점만 선택 가능)
- PlayerPrefs 로컬 저장
- 데이터 초기화 확인 팝업
- 런타임 기본 Sphere 행성, Particle System, 카메라 반응

## 이번 MVP에서 아직 제외한 것

현재 로그인, 인앱결제, Supabase, 온라인 저장, 랭킹, Addressables, 고급 VFX Graph는 구현하지 않았습니다.
광고는 Google AdMob 보상형 광고 기준으로 구현되어 있으며, 도감은 로컬 진행도 기반으로 동작합니다.

## 행성 모양과 분해

- 행성 모양 3종: 기본형 80% / 하트형 12% / 고양이형 8% (`StarForgeBalance.json`의 `shapeChancesPercent`).
  새 행성이 생길 때(첫 시작, 소멸, 분해) 추첨하며, 부활 시에는 파괴된 행성의 모양을 복원합니다.
- 모양별 단계 이름은 `stages`의 `heartName`/`catName`에서 관리합니다.
- 이름 카드 우측 하단 `행성 분해` 버튼: 현재 단계 가치의 재화를 받고 0강 새 행성으로 시작합니다.
  보상은 "0강→해당 단계 재등반 기대 비용의 72%"로 책정해(`stages`의 `disassembleReward`)
  부활가(115~125%)보다 싸게, 파괴 보상보다 훨씬 크게 잡았습니다. 강화 밸런스를 바꾸면 재계산이 필요합니다.
- 강화 연출: 시작 시 카메라 줌 인 + 행성 떨림 → 성공 시 1.3^(상승 단계)만큼 부풀어 오른 뒤 줌 아웃하며 정착.

## 밸런스 조정

성공률과 소모량은 기획서 수치를 반영했습니다. 균열률과 소멸률은 기획서에 구체 숫자가 없어 `StarForgeBalance.json`에 기본값으로 분리했습니다. 출시 전에는 플레이 테스트 후 이 값을 조정해야 합니다.

### 부활 가격 책정 근거

부활 가격은 `StarForgeBalance.json`의 `revivePoints`에서 관리합니다. 가격은 "0강에서 해당 단계까지 다시 올리는 기대 비용(최적 재료 선택, 소멸 시 손실·파괴 보상 환급·대성공 확률 포함, 교환 비율 기준 운석 파편 가치 환산)"에 15~25% 프리미엄을 붙여 책정했습니다.

| 부활 지점 | 재등반 기대 비용(운석 가치) | 책정 가격 |
|---:|---:|---|
| 5강 | 약 21 | 운석 파편 25 |
| 10강 | 약 383 | 운석 파편 460 |
| 15강 | 약 4,904 | 별의 조각 200 |
| 20강 | 약 56,021 | 별의 조각 1,300 + 온전한 별핵 조각 60 |
| 25강 | 약 3,417,082 | 온전한 별핵 조각 4,400 + 특이성 조각 400 + 원초의 별 8 |

주의: 25강 가격이 큰 이유는 현재 소멸률(실패 시 30%)이 매우 높아 재등반 기대 비용 자체가 천문학적이기 때문입니다. 소멸률을 조정하면 부활 가격도 재계산해야 합니다.
