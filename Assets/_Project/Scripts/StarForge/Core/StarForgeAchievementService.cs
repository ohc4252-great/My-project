using System;
using System.Collections.Generic;
using StarForge.Save;

namespace StarForge.Core
{
    public enum StarForgeAchievementKind
    {
        NormalLevel,
        DefaultLevel,
        HeartLevel,
        CatLevel,
        PlanetShape,
        BlackHoleDiscovery,
        BlackHoleLevel,
        FailureCount,
        ConsecutiveFailure,
        FractureCount,
        ConsecutiveFracture,
        DestructionCount,
        SuccessCount,
        ConsecutiveSuccess,
        GreatSuccessCount,
        ConsecutiveGreatSuccess,
        MiningCompletedCount,
        MiningDailyCompletedCount,
        MiningBestScorePermyriad
    }

    public sealed class StarForgeAchievementDefinition
    {
        public readonly string id;
        public readonly StarForgeAchievementKind kind;
        public readonly int target;
        public readonly string condition;
        public readonly string stageName;
        public readonly string achievementName;
        public readonly string tooltip;
        public readonly CurrencyAmount[] rewards;

        public StarForgeAchievementDefinition(
            string id,
            StarForgeAchievementKind kind,
            int target,
            string condition,
            string stageName,
            string achievementName,
            string tooltip,
            params CurrencyAmount[] rewards)
        {
            this.id = id;
            this.kind = kind;
            this.target = target;
            this.condition = condition;
            this.stageName = stageName;
            this.achievementName = achievementName;
            this.tooltip = tooltip;
            this.rewards = rewards ?? new CurrencyAmount[0];
        }
    }

    public sealed class StarForgeAchievementUnlock
    {
        public readonly StarForgeAchievementDefinition definition;

        public StarForgeAchievementUnlock(
            StarForgeAchievementDefinition definition)
        {
            this.definition = definition;
        }
    }

    public sealed class StarForgeAchievementService
    {
        private static readonly StarForgeAchievementDefinition[] Definitions =
        {
            NormalLevel(
                5,
                "미소행성",
                "티끌 모아 행성",
                "우주 먼지가 드디어 행성 흉내를 낸다",
                Reward(StarForgeCurrencyType.MeteorFragment, 50),
                Reward(StarForgeCurrencyType.StarShard, 15)),
            NormalLevel(
                10,
                "용암 행성",
                "용암주의보",
                "부글부글 끓는 두 자릿수 강화 돌파",
                Reward(StarForgeCurrencyType.MeteorFragment, 150),
                Reward(StarForgeCurrencyType.StarShard, 25)),
            NormalLevel(
                15,
                "초거대 행성",
                "행성계의 거인",
                "이제 \"작다\"는 말은 금지",
                Reward(StarForgeCurrencyType.StarShard, 50),
                Reward(StarForgeCurrencyType.PureCoreShard, 15)),
            DefaultLevel(
                20,
                "적색거성",
                "은퇴는 화려하게",
                "늙은 별일수록 붉고 거대하게 타오른다",
                Reward(StarForgeCurrencyType.PureCoreShard, 50)),
            DefaultLevel(
                21,
                "초거성",
                "초(超)를 넘어서",
                "거대함의 한계를 또 갱신",
                Reward(StarForgeCurrencyType.PureCoreShard, 75),
                Reward(StarForgeCurrencyType.SingularityShard, 2)),
            DefaultLevel(
                22,
                "백색왜성",
                "작지만 단단하게",
                "크기는 줄었지만 밀도는 차원이 다르다",
                Reward(StarForgeCurrencyType.PureCoreShard, 100),
                Reward(StarForgeCurrencyType.SingularityShard, 3)),
            DefaultLevel(
                23,
                "중성자별",
                "한 숟갈에 10억 톤",
                "찻숟갈 하나가 산의 무게",
                Reward(StarForgeCurrencyType.PureCoreShard, 125),
                Reward(StarForgeCurrencyType.SingularityShard, 4)),
            DefaultLevel(
                24,
                "펄서",
                "우주의 등대지기",
                "정확한 박자로 빛을 쏘아 올린다",
                Reward(StarForgeCurrencyType.PureCoreShard, 150),
                Reward(StarForgeCurrencyType.SingularityShard, 5)),
            DefaultLevel(
                25,
                "마그네타",
                "끌어당기는 매력",
                "우주에서 가장 강력한 자석",
                Reward(StarForgeCurrencyType.PureCoreShard, 175),
                Reward(StarForgeCurrencyType.SingularityShard, 6)),
            DefaultLevel(
                26,
                "초신성 잔해",
                "장렬한 산화",
                "한 번쯤은 크게 터져줘야 별이지",
                Reward(StarForgeCurrencyType.PureCoreShard, 250),
                Reward(StarForgeCurrencyType.SingularityShard, 5)),
            DefaultLevel(
                27,
                "쿼크별",
                "물질의 끝판왕",
                "원자도 못 버틴 압력의 결정체",
                Reward(StarForgeCurrencyType.PureCoreShard, 300),
                Reward(StarForgeCurrencyType.SingularityShard, 8)),
            DefaultLevel(
                28,
                "성운핵별",
                "성운의 심장",
                "거대한 구름 한가운데서 박동한다",
                Reward(StarForgeCurrencyType.PureCoreShard, 350),
                Reward(StarForgeCurrencyType.SingularityShard, 10)),
            DefaultLevel(
                29,
                "은하왕성",
                "은하의 지배자",
                "별 하나가 은하급으로 군림한다",
                Reward(StarForgeCurrencyType.PrimordialStar, 10),
                Reward(StarForgeCurrencyType.SingularityShard, 100)),
            DefaultLevel(
                30,
                "창세성",
                "태초에, 별이 있었다",
                "더 오를 곳 없는 창조의 정점",
                Reward(StarForgeCurrencyType.PrimordialStar, 50)),
            HeartLevel(
                20,
                "적색거성",
                "붉게 뛰는 심장",
                "붉은 별빛 사이로 마음이 먼저 뛰기 시작한다",
                Reward(StarForgeCurrencyType.PureCoreShard, 40),
                Reward(StarForgeCurrencyType.StarShard, 50)),
            HeartLevel(
                21,
                "초거성",
                "사랑은 초거대하게",
                "사랑도 이쯤 되면 우주급 스케일",
                Reward(StarForgeCurrencyType.PureCoreShard, 60),
                Reward(StarForgeCurrencyType.SingularityShard, 1)),
            HeartLevel(
                22,
                "백색왜성",
                "작아도 깊은 마음",
                "작아질수록 진심은 더 단단해진다",
                Reward(StarForgeCurrencyType.PureCoreShard, 80),
                Reward(StarForgeCurrencyType.SingularityShard, 2)),
            HeartLevel(
                23,
                "중성자별",
                "압축된 고백",
                "한마디 고백이 별의 밀도로 눌렸다",
                Reward(StarForgeCurrencyType.PureCoreShard, 100),
                Reward(StarForgeCurrencyType.SingularityShard, 3)),
            HeartLevel(
                24,
                "펄서",
                "두근두근 펄스",
                "일정한 박자로 마음을 쏘아 올린다",
                Reward(StarForgeCurrencyType.PureCoreShard, 120),
                Reward(StarForgeCurrencyType.SingularityShard, 4)),
            HeartLevel(
                25,
                "마그네타",
                "끌림의 법칙",
                "가장 강한 중력은 결국 마음에서 온다",
                Reward(StarForgeCurrencyType.PureCoreShard, 140),
                Reward(StarForgeCurrencyType.SingularityShard, 5)),
            HeartLevel(
                26,
                "초신성 잔해",
                "사랑도 폭발한다",
                "참아온 마음이 우주에 번져 나간다",
                Reward(StarForgeCurrencyType.PureCoreShard, 200),
                Reward(StarForgeCurrencyType.SingularityShard, 4)),
            HeartLevel(
                27,
                "쿼크별",
                "마음의 끝단",
                "더는 쪼갤 수 없는 진심만 남았다",
                Reward(StarForgeCurrencyType.PureCoreShard, 240),
                Reward(StarForgeCurrencyType.SingularityShard, 6)),
            HeartLevel(
                28,
                "성운핵별",
                "성운에 남긴 하트",
                "별구름 속에서도 마음의 모양은 선명하다",
                Reward(StarForgeCurrencyType.PureCoreShard, 280),
                Reward(StarForgeCurrencyType.SingularityShard, 8)),
            HeartLevel(
                29,
                "은하왕성",
                "은하급 러브레터",
                "한 은하를 가득 채운 고백",
                Reward(StarForgeCurrencyType.PrimordialStar, 10),
                Reward(StarForgeCurrencyType.SingularityShard, 100)),
            HeartLevel(
                30,
                "창세성",
                "우주가 사랑한 별",
                "태초부터 사랑받기 위해 빛난 별",
                Reward(StarForgeCurrencyType.PrimordialStar, 40),
                Reward(StarForgeCurrencyType.SingularityShard, 10)),
            CatLevel(
                20,
                "적색거성",
                "고양이는 붉게 타오른다",
                "낮잠 끝난 고양이의 눈빛이 별처럼 뜨겁다",
                Reward(StarForgeCurrencyType.PureCoreShard, 45),
                Reward(StarForgeCurrencyType.SingularityShard, 1)),
            CatLevel(
                21,
                "초거성",
                "초거대 냥성",
                "커져도 결국 박스 안에 들어가고 싶다",
                Reward(StarForgeCurrencyType.PureCoreShard, 70),
                Reward(StarForgeCurrencyType.SingularityShard, 2)),
            CatLevel(
                22,
                "백색왜성",
                "작지만 단단한 냥발",
                "작은 발바닥 안에 우주가 눌려 있다",
                Reward(StarForgeCurrencyType.PureCoreShard, 90),
                Reward(StarForgeCurrencyType.SingularityShard, 3)),
            CatLevel(
                23,
                "중성자별",
                "중성자 꾹꾹이",
                "한 번 누를 때마다 공간이 휘어진다",
                Reward(StarForgeCurrencyType.PureCoreShard, 115),
                Reward(StarForgeCurrencyType.SingularityShard, 4)),
            CatLevel(
                24,
                "펄서",
                "우주의 골골송",
                "은하 전체에 낮은 진동이 퍼진다",
                Reward(StarForgeCurrencyType.PureCoreShard, 135),
                Reward(StarForgeCurrencyType.SingularityShard, 5)),
            CatLevel(
                25,
                "마그네타",
                "마그네타",
                "도망치려 해도 이상하게 끌려온다",
                Reward(StarForgeCurrencyType.PureCoreShard, 160),
                Reward(StarForgeCurrencyType.SingularityShard, 6)),
            CatLevel(
                26,
                "초신성 잔해",
                "우주급 우다다",
                "한밤중 질주가 초신성처럼 폭발한다",
                Reward(StarForgeCurrencyType.PureCoreShard, 225),
                Reward(StarForgeCurrencyType.SingularityShard, 5)),
            CatLevel(
                27,
                "쿼크별",
                "쿼크까지 냥펀치",
                "원자보다 작은 곳까지 발톱이 닿았다",
                Reward(StarForgeCurrencyType.PureCoreShard, 270),
                Reward(StarForgeCurrencyType.SingularityShard, 8)),
            CatLevel(
                28,
                "성운핵별",
                "성운 위 식빵자세",
                "거대한 별구름 위에 조용히 자리를 잡았다",
                Reward(StarForgeCurrencyType.PureCoreShard, 315),
                Reward(StarForgeCurrencyType.SingularityShard, 10)),
            CatLevel(
                29,
                "은하왕성",
                "은하를 접수한 고양이",
                "결국 은하도 고양이의 영역이 되었다",
                Reward(StarForgeCurrencyType.PrimordialStar, 10),
                Reward(StarForgeCurrencyType.SingularityShard, 100)),
            CatLevel(
                30,
                "창세성",
                "태초의 고양이",
                "태초에 별이 있었고, 그 위에 고양이가 있었다",
                Reward(StarForgeCurrencyType.PrimordialStar, 45),
                Reward(StarForgeCurrencyType.SingularityShard, 15)),
            Shape(
                StarForgePlanetShape.Cat,
                "고양이 별",
                "고양이가 우주를 정복했다냥",
                "결국 고양이가 별이 되어\n우주를 접수했다",
                Reward(StarForgeCurrencyType.PureCoreShard, 5),
                Reward(StarForgeCurrencyType.StarShard, 25),
                Reward(StarForgeCurrencyType.MeteorFragment, 50)),
            Shape(
                StarForgePlanetShape.Heart,
                "하트 별",
                "우주의 러브레터",
                "우주에서 피어난 사랑 이야기",
                Reward(StarForgeCurrencyType.PureCoreShard, 3),
                Reward(StarForgeCurrencyType.StarShard, 13),
                Reward(StarForgeCurrencyType.MeteorFragment, 25)),
            BlackHoleDiscovery(
                "심연을 들여다보다",
                "빛조차 빠져나오지 못하는 곳을 마주하다",
                Reward(StarForgeCurrencyType.PureCoreShard, 100),
                Reward(StarForgeCurrencyType.SingularityShard, 50)),
            BlackHoleLevel(
                1,
                "첫 입질",
                "갓 태어난 블랙홀의 첫 식사",
                Reward(StarForgeCurrencyType.SingularityShard, 20),
                Reward(StarForgeCurrencyType.PrimordialStar, 3)),
            BlackHoleLevel(
                2,
                "식욕 폭발",
                "멈출 수 없는 허기",
                Reward(StarForgeCurrencyType.SingularityShard, 30),
                Reward(StarForgeCurrencyType.PrimordialStar, 4)),
            BlackHoleLevel(
                3,
                "대식가",
                "주변 물질을 닥치는 대로",
                Reward(StarForgeCurrencyType.SingularityShard, 40),
                Reward(StarForgeCurrencyType.PrimordialStar, 5)),
            BlackHoleLevel(
                4,
                "빛조차 디저트",
                "광자마저 메뉴판에 올랐다",
                Reward(StarForgeCurrencyType.SingularityShard, 50),
                Reward(StarForgeCurrencyType.PrimordialStar, 6)),
            BlackHoleLevel(
                5,
                "항성 포식자",
                "별을 통째로 삼키기 시작",
                Reward(StarForgeCurrencyType.SingularityShard, 60),
                Reward(StarForgeCurrencyType.PrimordialStar, 30)),
            BlackHoleLevel(
                6,
                "시공간을 씹다",
                "공간과 시간마저 빨려든다",
                Reward(StarForgeCurrencyType.PrimordialStar, 50)),
            BlackHoleLevel(
                7,
                "은하의 구멍",
                "은하 중심급으로 성장",
                Reward(StarForgeCurrencyType.PrimordialStar, 70)),
            BlackHoleLevel(
                8,
                "탐욕의 특이점",
                "끝을 모르는 중력의 식탐",
                Reward(StarForgeCurrencyType.PrimordialStar, 100)),
            BlackHoleLevel(
                9,
                "초대질량의 군주",
                "수백만 태양질량의 위엄",
                Reward(StarForgeCurrencyType.PrimordialStar, 300)),
            BlackHoleLevel(
                10,
                "우주를 삼킨 자",
                "더 삼킬 것이 없는 포식의 끝",
                Reward(StarForgeCurrencyType.PrimordialStar, 9999)),
            Statistic(
                "failure_count_001",
                StarForgeAchievementKind.FailureCount,
                1,
                "실패 1회",
                "실패",
                "이것이 실패의 맛인가?",
                "첫 실패. 아직은 웃을 수 있다",
                Reward(StarForgeCurrencyType.MeteorFragment, 25)),
            Statistic(
                "failure_count_100",
                StarForgeAchievementKind.FailureCount,
                100,
                "실패 100회",
                "실패",
                "실패는 성공의 어머니",
                "곧 성공할 것이다.",
                Reward(StarForgeCurrencyType.MeteorFragment, 100),
                Reward(StarForgeCurrencyType.StarShard, 15)),
            Statistic(
                "failure_count_500",
                StarForgeAchievementKind.FailureCount,
                500,
                "실패 500회",
                "실패",
                "추진력을 얻기 위함이었다",
                "희망의 끈을 놓지 말자",
                Reward(StarForgeCurrencyType.MeteorFragment, 250),
                Reward(StarForgeCurrencyType.StarShard, 40)),
            Statistic(
                "failure_count_1000",
                StarForgeAchievementKind.FailureCount,
                1000,
                "실패 1,000회",
                "실패",
                "이젠 익숙해..",
                "오늘은 운이 없다",
                Reward(StarForgeCurrencyType.StarShard, 75),
                Reward(StarForgeCurrencyType.PureCoreShard, 5)),
            Statistic(
                "failure_count_3000",
                StarForgeAchievementKind.FailureCount,
                3000,
                "실패 3,000회",
                "실패",
                "실패 아티스트",
                "실패는 예술이다",
                Reward(StarForgeCurrencyType.StarShard, 150),
                Reward(StarForgeCurrencyType.PureCoreShard, 25)),
            Statistic(
                "failure_count_10000",
                StarForgeAchievementKind.FailureCount,
                10000,
                "실패 10,000회",
                "실패",
                "실패는 성공의 어머니...인가?",
                "어머니가 너무 많아졌다",
                Reward(StarForgeCurrencyType.PureCoreShard, 100),
                Reward(StarForgeCurrencyType.SingularityShard, 5)),
            Statistic(
                "failure_streak_003",
                StarForgeAchievementKind.ConsecutiveFailure,
                3,
                "연속 실패 3회",
                "연속 실패",
                "불길한 예감",
                "아직 기분 탓일 수도 있다",
                Reward(StarForgeCurrencyType.MeteorFragment, 50),
                Reward(StarForgeCurrencyType.StarShard, 10)),
            Statistic(
                "failure_streak_005",
                StarForgeAchievementKind.ConsecutiveFailure,
                5,
                "연속 실패 5회",
                "연속 실패",
                "5연속 실패는 버그 아닌가요?",
                "슬슬 개발자를 의심하기 시작했다",
                Reward(StarForgeCurrencyType.StarShard, 25),
                Reward(StarForgeCurrencyType.PureCoreShard, 3)),
            Statistic(
                "failure_streak_010",
                StarForgeAchievementKind.ConsecutiveFailure,
                10,
                "연속 실패 10회",
                "연속 실패",
                "10회 연속 실패는 이상한데",
                "증거 자료는 충분하다",
                Reward(StarForgeCurrencyType.PureCoreShard, 15),
                Reward(StarForgeCurrencyType.SingularityShard, 2)),
            Statistic(
                "fracture_count_001",
                StarForgeAchievementKind.FractureCount,
                1,
                "균열 1회",
                "균열",
                "어라? 별에 금이 갔어요",
                "별에도 마음에도 금이 갔다",
                Reward(StarForgeCurrencyType.StarShard, 10)),
            Statistic(
                "fracture_count_100",
                StarForgeAchievementKind.FractureCount,
                100,
                "균열 100회",
                "균열",
                "금성",
                "금이 많이 간 별",
                Reward(StarForgeCurrencyType.StarShard, 40),
                Reward(StarForgeCurrencyType.PureCoreShard, 3)),
            Statistic(
                "fracture_count_1000",
                StarForgeAchievementKind.FractureCount,
                1000,
                "균열 1,000회",
                "균열",
                "크랙 컬렉터",
                "금 간 별만 모으는 이상한 취미",
                Reward(StarForgeCurrencyType.PureCoreShard, 25),
                Reward(StarForgeCurrencyType.SingularityShard, 3)),
            Statistic(
                "fracture_streak_003",
                StarForgeAchievementKind.ConsecutiveFracture,
                3,
                "연속 균열 3회",
                "연속 균열",
                "쩌적 3연타",
                "소리가 점점 선명해진다",
                Reward(StarForgeCurrencyType.StarShard, 25),
                Reward(StarForgeCurrencyType.PureCoreShard, 3)),
            Statistic(
                "fracture_streak_005",
                StarForgeAchievementKind.ConsecutiveFracture,
                5,
                "연속 균열 5회",
                "연속 균열",
                "유리멘탈 항성",
                "별보다 내가 먼저 깨질 것 같다",
                Reward(StarForgeCurrencyType.PureCoreShard, 10),
                Reward(StarForgeCurrencyType.SingularityShard, 2)),
            Statistic(
                "fracture_streak_010",
                StarForgeAchievementKind.ConsecutiveFracture,
                10,
                "연속 균열 10회",
                "연속 균열",
                "곧 가십니다~",
                "조만간 소멸하실 것 같다.",
                Reward(StarForgeCurrencyType.PureCoreShard, 40),
                Reward(StarForgeCurrencyType.SingularityShard, 4)),
            Statistic(
                "destruction_count_001",
                StarForgeAchievementKind.DestructionCount,
                1,
                "소멸 1회",
                "소멸",
                "이 양반 갈때도 예술로 가는구만",
                "잘 가요, 영감",
                Reward(StarForgeCurrencyType.StarShard, 25),
                Reward(StarForgeCurrencyType.PureCoreShard, 3)),
            Statistic(
                "destruction_count_050",
                StarForgeAchievementKind.DestructionCount,
                50,
                "소멸 50회",
                "소멸",
                "잘 가라 내 별",
                "보낼 때마다 처음처럼 아프다",
                Reward(StarForgeCurrencyType.StarShard, 75),
                Reward(StarForgeCurrencyType.PureCoreShard, 8)),
            Statistic(
                "destruction_count_100",
                StarForgeAchievementKind.DestructionCount,
                100,
                "소멸 100회",
                "소멸",
                "우주 청소부",
                "별을 키우는 게임인지 치우는 게임인지",
                Reward(StarForgeCurrencyType.PureCoreShard, 15),
                Reward(StarForgeCurrencyType.SingularityShard, 2)),
            Statistic(
                "destruction_count_500",
                StarForgeAchievementKind.DestructionCount,
                500,
                "소멸 500회",
                "소멸",
                "소멸 맛집",
                "이상하게 여기만 잘 터진다",
                Reward(StarForgeCurrencyType.PureCoreShard, 60),
                Reward(StarForgeCurrencyType.SingularityShard, 6)),
            Statistic(
                "destruction_count_1000",
                StarForgeAchievementKind.DestructionCount,
                1000,
                "소멸 1,000회",
                "소멸",
                "장의사",
                "별들이 줄 서서 떠난다",
                Reward(StarForgeCurrencyType.PureCoreShard, 125),
                Reward(StarForgeCurrencyType.SingularityShard, 13)),
            Statistic(
                "destruction_count_3000",
                StarForgeAchievementKind.DestructionCount,
                3000,
                "소멸 3,000회",
                "소멸",
                "별들의 무덤",
                "별 소멸시키기 전문가",
                Reward(StarForgeCurrencyType.PrimordialStar, 5),
                Reward(StarForgeCurrencyType.SingularityShard, 25)),
            Statistic(
                "success_count_001",
                StarForgeAchievementKind.SuccessCount,
                1,
                "성공 1회",
                "성공",
                "첫 성공",
                "성공했다!",
                Reward(StarForgeCurrencyType.MeteorFragment, 50),
                Reward(StarForgeCurrencyType.StarShard, 15)),
            Statistic(
                "success_count_100",
                StarForgeAchievementKind.SuccessCount,
                100,
                "성공 100회",
                "성공",
                "운이 좋군",
                "오늘은 기운이 좋은데?",
                Reward(StarForgeCurrencyType.MeteorFragment, 150),
                Reward(StarForgeCurrencyType.StarShard, 35)),
            Statistic(
                "success_count_500",
                StarForgeAchievementKind.SuccessCount,
                500,
                "성공 500회",
                "성공",
                "손맛",
                "강화 버튼 누르는 맛이 있다",
                Reward(StarForgeCurrencyType.StarShard, 100),
                Reward(StarForgeCurrencyType.PureCoreShard, 10)),
            Statistic(
                "success_count_1000",
                StarForgeAchievementKind.SuccessCount,
                1000,
                "성공 1,000회",
                "성공",
                "성공 마스터",
                "성공의 달인이다.",
                Reward(StarForgeCurrencyType.PureCoreShard, 40),
                Reward(StarForgeCurrencyType.SingularityShard, 3)),
            Statistic(
                "success_count_5000",
                StarForgeAchievementKind.SuccessCount,
                5000,
                "성공 5,000회",
                "성공",
                "성공 중독",
                "크큭... 또 성공이군",
                Reward(StarForgeCurrencyType.PureCoreShard, 125),
                Reward(StarForgeCurrencyType.SingularityShard, 13)),
            Statistic(
                "success_streak_003",
                StarForgeAchievementKind.ConsecutiveSuccess,
                3,
                "연속 성공 3회",
                "연속 성공",
                "흐름인가?",
                "끊지 말고 계속 가자",
                Reward(StarForgeCurrencyType.StarShard, 50)),
            Statistic(
                "success_streak_005",
                StarForgeAchievementKind.ConsecutiveSuccess,
                5,
                "연속 성공 5회",
                "연속 성공",
                "도박신이 깃들었다!",
                "가장 무서운 착각이다.",
                Reward(StarForgeCurrencyType.StarShard, 100),
                Reward(StarForgeCurrencyType.PureCoreShard, 5)),
            Statistic(
                "success_streak_010",
                StarForgeAchievementKind.ConsecutiveSuccess,
                10,
                "연속 성공 10회",
                "연속 성공",
                "전설의 도박꾼",
                "지금, 이 순간이다.",
                Reward(StarForgeCurrencyType.PureCoreShard, 25),
                Reward(StarForgeCurrencyType.SingularityShard, 3)),
            Statistic(
                "success_streak_020",
                StarForgeAchievementKind.ConsecutiveSuccess,
                20,
                "연속 성공 20회",
                "연속 성공",
                "확률조작범",
                "확률 따위는 숫자에 불과하다",
                Reward(StarForgeCurrencyType.PureCoreShard, 75),
                Reward(StarForgeCurrencyType.SingularityShard, 8)),
            Statistic(
                "great_success_count_001",
                StarForgeAchievementKind.GreatSuccessCount,
                1,
                "대성공 1회",
                "대성공",
                "대!! 성공!!",
                "기대 안 했는데 별이 튀어 올랐다",
                Reward(StarForgeCurrencyType.PureCoreShard, 5)),
            Statistic(
                "great_success_count_005",
                StarForgeAchievementKind.GreatSuccessCount,
                5,
                "대성공 5회",
                "대성공",
                "대!!!!! 성공!!!!",
                "실력은 모르겠고 결과는 좋았다",
                Reward(StarForgeCurrencyType.PureCoreShard, 15),
                Reward(StarForgeCurrencyType.SingularityShard, 2)),
            Statistic(
                "great_success_count_010",
                StarForgeAchievementKind.GreatSuccessCount,
                10,
                "대성공 10회",
                "대성공",
                "대성공, 이제 익숙한데?",
                "남의 운까지 끌어다 쓴 것 같다",
                Reward(StarForgeCurrencyType.PureCoreShard, 35),
                Reward(StarForgeCurrencyType.SingularityShard, 4)),
            Statistic(
                "great_success_count_100",
                StarForgeAchievementKind.GreatSuccessCount,
                100,
                "대성공 100회",
                "대성공",
                "대성공 방정식을 알아냈다.",
                "이 정도면 확률이 너를 기억한다",
                Reward(StarForgeCurrencyType.PrimordialStar, 3),
                Reward(StarForgeCurrencyType.SingularityShard, 15)),
            Statistic(
                "great_success_streak_002",
                StarForgeAchievementKind.ConsecutiveGreatSuccess,
                2,
                "연속 대성공 2회",
                "연속 대성공",
                "2단 점프",
                "방금 게임이 살짝 당황했다",
                Reward(StarForgeCurrencyType.PureCoreShard, 25),
                Reward(StarForgeCurrencyType.SingularityShard, 3)),
            Statistic(
                "great_success_streak_003",
                StarForgeAchievementKind.ConsecutiveGreatSuccess,
                3,
                "연속 대성공 3회",
                "연속 대성공",
                "이게 된다고?",
                "기획자도 예상 못 한 장면",
                Reward(StarForgeCurrencyType.PureCoreShard, 50),
                Reward(StarForgeCurrencyType.SingularityShard, 5)),
            Statistic(
                "mining_completed_001",
                StarForgeAchievementKind.MiningCompletedCount,
                1,
                "별 탐사 1회 완료",
                "별 탐사",
                "첫 항로",
                "별빛 밖으로 첫 발을 내디뎠다",
                Reward(StarForgeCurrencyType.MeteorFragment, 100),
                Reward(StarForgeCurrencyType.StarShard, 30)),
            Statistic(
                "mining_daily_completed_003",
                StarForgeAchievementKind.MiningDailyCompletedCount,
                3,
                "하루 별 탐사 3회 완료",
                "별 탐사",
                "오늘도 탐험 완료",
                "오늘치 항로는 전부 훑었다",
                Reward(StarForgeCurrencyType.StarShard, 50),
                Reward(StarForgeCurrencyType.PureCoreShard, 5)),
            Statistic(
                "mining_best_score_5000",
                StarForgeAchievementKind.MiningBestScorePermyriad,
                5000,
                "별 탐사 점수 50% 이상",
                "별 탐사",
                "깊은 우주 진입",
                "돌아갈 길보다 앞으로 갈 길이 더 밝다",
                Reward(StarForgeCurrencyType.PureCoreShard, 25),
                Reward(StarForgeCurrencyType.SingularityShard, 2)),
            Statistic(
                "mining_best_score_10000",
                StarForgeAchievementKind.MiningBestScorePermyriad,
                10000,
                "별 탐사 점수 100%",
                "별 탐사",
                "완전 항로 개척",
                "지도 끝에 새 별을 찍었다",
                Reward(StarForgeCurrencyType.PrimordialStar, 3),
                Reward(StarForgeCurrencyType.SingularityShard, 10)),
            Statistic(
                "mining_completed_100",
                StarForgeAchievementKind.MiningCompletedCount,
                100,
                "별 탐사 100회 완료",
                "별 탐사",
                "탐사 베테랑",
                "이제 별길이 익숙하다",
                Reward(StarForgeCurrencyType.PrimordialStar, 5),
                Reward(StarForgeCurrencyType.SingularityShard, 15))
        };

        public StarForgeAchievementUnlock[] CompleteAvailable(
            StarForgeSaveData saveData)
        {
            if (saveData == null)
            {
                return new StarForgeAchievementUnlock[0];
            }

            saveData.EnsureAchievementProgress();
            List<StarForgeAchievementUnlock> unlocked =
                new List<StarForgeAchievementUnlock>();
            for (int i = 0; i < Definitions.Length; i++)
            {
                StarForgeAchievementDefinition definition = Definitions[i];
                if (!IsSatisfied(saveData, definition) ||
                    !saveData.TryCompleteAchievement(definition.id))
                {
                    continue;
                }

                // Mark as cleared only — rewards are claimed later in the 업적 tab.
                unlocked.Add(new StarForgeAchievementUnlock(definition));
            }

            return unlocked.ToArray();
        }

        public StarForgeAchievementDefinition GetDefinition(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            for (int i = 0; i < Definitions.Length; i++)
            {
                if (string.Equals(Definitions[i].id, id, StringComparison.Ordinal))
                {
                    return Definitions[i];
                }
            }

            return null;
        }

        // Number of completed achievements whose rewards are still unclaimed.
        public int GetClaimableCount(StarForgeSaveData saveData)
        {
            if (saveData == null)
            {
                return 0;
            }

            saveData.EnsureAchievementProgress();
            int count = 0;
            for (int i = 0; i < Definitions.Length; i++)
            {
                string id = Definitions[i].id;
                if (saveData.HasCompletedAchievement(id) &&
                    !saveData.HasClaimedAchievement(id))
                {
                    count++;
                }
            }

            return count;
        }

        public bool HasClaimableRewards(StarForgeSaveData saveData)
        {
            return GetClaimableCount(saveData) > 0;
        }

        // Grants and marks one completed-but-unclaimed achievement's reward.
        public bool TryClaimReward(StarForgeSaveData saveData, string id)
        {
            if (saveData == null)
            {
                return false;
            }

            StarForgeAchievementDefinition definition = GetDefinition(id);
            if (definition == null || !saveData.TryClaimAchievement(id))
            {
                return false;
            }

            GrantRewards(saveData, definition.rewards);
            return true;
        }

        // Claims every available reward; returns how many were claimed.
        public int ClaimAllRewards(StarForgeSaveData saveData)
        {
            if (saveData == null)
            {
                return 0;
            }

            int claimed = 0;
            for (int i = 0; i < Definitions.Length; i++)
            {
                if (TryClaimReward(saveData, Definitions[i].id))
                {
                    claimed++;
                }
            }

            return claimed;
        }

        public CurrencyAmount[] GetClaimableRewardTotals(
            StarForgeSaveData saveData)
        {
            if (saveData == null)
            {
                return new CurrencyAmount[0];
            }

            saveData.EnsureAchievementProgress();
            int[] totals = new int[5];
            for (int i = 0; i < Definitions.Length; i++)
            {
                StarForgeAchievementDefinition definition = Definitions[i];
                if (saveData.HasCompletedAchievement(definition.id) &&
                    !saveData.HasClaimedAchievement(definition.id))
                {
                    AddRewardTotals(totals, definition.rewards);
                }
            }

            return BuildRewardTotals(totals);
        }

        public StarForgeAchievementDefinition[] GetDefinitions()
        {
            return (StarForgeAchievementDefinition[])Definitions.Clone();
        }

        public StarForgeAchievementDefinition GetNormalLevelDefinition(
            int level)
        {
            return GetLevelDefinition(level, StarForgePlanetShape.Default);
        }

        public StarForgeAchievementDefinition GetLevelDefinition(
            int level,
            StarForgePlanetShape shape)
        {
            StarForgeAchievementKind targetKind =
                GetLevelAchievementKind(shape, level);
            for (int i = 0; i < Definitions.Length; i++)
            {
                StarForgeAchievementDefinition definition = Definitions[i];
                if (definition.kind == targetKind &&
                    definition.target == level)
                {
                    return definition;
                }
            }

            return null;
        }

        private static bool IsSatisfied(
            StarForgeSaveData saveData,
            StarForgeAchievementDefinition definition)
        {
            switch (definition.kind)
            {
                case StarForgeAchievementKind.NormalLevel:
                    return saveData.highestLevel >= definition.target;
                case StarForgeAchievementKind.DefaultLevel:
                    return saveData.GetShapeHighestLevel(
                        StarForgePlanetShape.Default) >= definition.target;
                case StarForgeAchievementKind.HeartLevel:
                    return saveData.GetShapeHighestLevel(
                        StarForgePlanetShape.Heart) >= definition.target;
                case StarForgeAchievementKind.CatLevel:
                    return saveData.GetShapeHighestLevel(
                        StarForgePlanetShape.Cat) >= definition.target;
                case StarForgeAchievementKind.PlanetShape:
                    return saveData.IsShapeDiscovered(
                        (StarForgePlanetShape)definition.target);
                case StarForgeAchievementKind.BlackHoleDiscovery:
                    return saveData.highestBlackHoleLevel > 0 ||
                           saveData.isBlackHole;
                case StarForgeAchievementKind.BlackHoleLevel:
                    return saveData.highestBlackHoleLevel >= definition.target;
                case StarForgeAchievementKind.FailureCount:
                    return saveData.failureCount >= definition.target;
                case StarForgeAchievementKind.ConsecutiveFailure:
                    return saveData.consecutiveFailureCount >=
                           definition.target;
                case StarForgeAchievementKind.FractureCount:
                    return saveData.totalFractureCount >= definition.target;
                case StarForgeAchievementKind.ConsecutiveFracture:
                    return saveData.consecutiveFractureCount >=
                           definition.target;
                case StarForgeAchievementKind.DestructionCount:
                    return saveData.destructionCount >= definition.target;
                case StarForgeAchievementKind.SuccessCount:
                    return saveData.successCount >= definition.target;
                case StarForgeAchievementKind.ConsecutiveSuccess:
                    return saveData.consecutiveSuccessCount >=
                           definition.target;
                case StarForgeAchievementKind.GreatSuccessCount:
                    return saveData.greatSuccessCount >= definition.target;
                case StarForgeAchievementKind.ConsecutiveGreatSuccess:
                    return saveData.consecutiveGreatSuccessCount >=
                           definition.target;
                case StarForgeAchievementKind.MiningCompletedCount:
                    return saveData.miningCompletedCount >= definition.target;
                case StarForgeAchievementKind.MiningDailyCompletedCount:
                    return saveData.miningDailyCompletedCount >=
                           definition.target;
                case StarForgeAchievementKind.MiningBestScorePermyriad:
                    return saveData.bestMiningScorePermyriad >=
                           definition.target;
                default:
                    return false;
            }
        }

        private static void GrantRewards(
            StarForgeSaveData saveData,
            CurrencyAmount[] rewards)
        {
            if (rewards == null)
            {
                return;
            }

            for (int i = 0; i < rewards.Length; i++)
            {
                CurrencyAmount reward = rewards[i];
                if (reward != null && reward.amount > 0)
                {
                    saveData.AddCurrency(reward.type, reward.amount);
                }
            }
        }

        private static void AddRewardTotals(
            int[] totals,
            CurrencyAmount[] rewards)
        {
            if (totals == null || rewards == null)
            {
                return;
            }

            for (int i = 0; i < rewards.Length; i++)
            {
                CurrencyAmount reward = rewards[i];
                int index = reward != null ? (int)reward.type : -1;
                if (reward != null &&
                    reward.amount > 0 &&
                    index >= 0 &&
                    index < totals.Length)
                {
                    totals[index] += reward.amount;
                }
            }
        }

        private static CurrencyAmount[] BuildRewardTotals(int[] totals)
        {
            if (totals == null)
            {
                return new CurrencyAmount[0];
            }

            List<CurrencyAmount> rewards = new List<CurrencyAmount>();
            for (int i = 0; i < totals.Length; i++)
            {
                if (totals[i] > 0)
                {
                    rewards.Add(new CurrencyAmount(
                        (StarForgeCurrencyType)i,
                        totals[i]));
                }
            }

            return rewards.ToArray();
        }

        private static StarForgeAchievementDefinition NormalLevel(
            int level,
            string stageName,
            string achievementName,
            string tooltip,
            params CurrencyAmount[] rewards)
        {
            return new StarForgeAchievementDefinition(
                "normal_level_" + level.ToString("00"),
                StarForgeAchievementKind.NormalLevel,
                level,
                level + "강",
                stageName,
                achievementName,
                tooltip,
                rewards);
        }

        private static StarForgeAchievementDefinition DefaultLevel(
            int level,
            string stageName,
            string achievementName,
            string tooltip,
            params CurrencyAmount[] rewards)
        {
            return ShapeLevel(
                StarForgePlanetShape.Default,
                level,
                stageName,
                achievementName,
                tooltip,
                rewards);
        }

        private static StarForgeAchievementDefinition HeartLevel(
            int level,
            string stageName,
            string achievementName,
            string tooltip,
            params CurrencyAmount[] rewards)
        {
            return ShapeLevel(
                StarForgePlanetShape.Heart,
                level,
                stageName,
                achievementName,
                tooltip,
                rewards);
        }

        private static StarForgeAchievementDefinition CatLevel(
            int level,
            string stageName,
            string achievementName,
            string tooltip,
            params CurrencyAmount[] rewards)
        {
            return ShapeLevel(
                StarForgePlanetShape.Cat,
                level,
                stageName,
                achievementName,
                tooltip,
                rewards);
        }

        private static StarForgeAchievementDefinition ShapeLevel(
            StarForgePlanetShape shape,
            int level,
            string stageName,
            string achievementName,
            string tooltip,
            params CurrencyAmount[] rewards)
        {
            string achievementId = shape == StarForgePlanetShape.Default
                ? "normal_level_" + level.ToString("00")
                : shape.ToString().ToLowerInvariant() +
                  "_level_" + level.ToString("00");
            return new StarForgeAchievementDefinition(
                achievementId,
                GetLevelAchievementKind(shape, level),
                level,
                GetShapeLabel(shape) + " " + level + "강",
                stageName,
                achievementName,
                tooltip,
                rewards);
        }

        private static StarForgeAchievementDefinition Shape(
            StarForgePlanetShape shape,
            string stageName,
            string achievementName,
            string tooltip,
            params CurrencyAmount[] rewards)
        {
            return new StarForgeAchievementDefinition(
                "shape_" + shape.ToString().ToLowerInvariant(),
                StarForgeAchievementKind.PlanetShape,
                (int)shape,
                stageName + " 발견",
                stageName,
                achievementName,
                tooltip,
                rewards);
        }

        private static StarForgeAchievementDefinition Statistic(
            string id,
            StarForgeAchievementKind kind,
            int target,
            string condition,
            string stageName,
            string achievementName,
            string tooltip,
            params CurrencyAmount[] rewards)
        {
            return new StarForgeAchievementDefinition(
                id,
                kind,
                target,
                condition,
                stageName,
                achievementName,
                tooltip,
                rewards);
        }

        private static StarForgeAchievementDefinition BlackHoleDiscovery(
            string achievementName,
            string tooltip,
            params CurrencyAmount[] rewards)
        {
            return new StarForgeAchievementDefinition(
                "black_hole_discovery",
                StarForgeAchievementKind.BlackHoleDiscovery,
                1,
                "블랙홀 최초 발견",
                "블랙홀",
                achievementName,
                tooltip,
                rewards);
        }

        private static StarForgeAchievementDefinition BlackHoleLevel(
            int level,
            string achievementName,
            string tooltip,
            params CurrencyAmount[] rewards)
        {
            return new StarForgeAchievementDefinition(
                "black_hole_level_" + level.ToString("00"),
                StarForgeAchievementKind.BlackHoleLevel,
                level,
                "블랙홀 " + level + "강",
                "블랙홀",
                achievementName,
                tooltip,
                rewards);
        }

        private static StarForgeAchievementKind GetLevelAchievementKind(
            StarForgePlanetShape shape,
            int level)
        {
            if (level < 20)
            {
                return StarForgeAchievementKind.NormalLevel;
            }

            switch (shape)
            {
                case StarForgePlanetShape.Heart:
                    return StarForgeAchievementKind.HeartLevel;
                case StarForgePlanetShape.Cat:
                    return StarForgeAchievementKind.CatLevel;
                default:
                    return StarForgeAchievementKind.DefaultLevel;
            }
        }

        private static string GetShapeLabel(StarForgePlanetShape shape)
        {
            switch (shape)
            {
                case StarForgePlanetShape.Heart:
                    return "하트 별";
                case StarForgePlanetShape.Cat:
                    return "고양이 별";
                default:
                    return "일반 별";
            }
        }

        private static CurrencyAmount Reward(
            StarForgeCurrencyType type,
            int amount)
        {
            return new CurrencyAmount(type, amount);
        }
    }
}
