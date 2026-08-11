using UnityEngine;

namespace EndlessGuard.Unit.Data
{
    public enum UnitGrade
    {
        [InspectorName("미설정")]
        None = 0,

        [InspectorName("1성")]
        OneStar = 1,

        [InspectorName("2성")]
        TwoStar = 2,

        [InspectorName("3성")]
        ThreeStar = 3,

        [InspectorName("4성")]
        FourStar = 4,

        [InspectorName("5성")]
        FiveStar = 5,

        [InspectorName("6성")]
        SixStar = 6
    }

    public enum UnitClass
    {
        [InspectorName("미설정")]
        None = 0,

        [InspectorName("뱅가드")]
        Vanguard = 1,

        [InspectorName("가드")]
        Guard = 2,

        [InspectorName("디펜더")]
        Defender = 3,

        [InspectorName("서포터")]
        Supporter = 4,

        [InspectorName("스나이퍼")]
        Sniper = 5,

        [InspectorName("스페셜리스트")]
        Specialist = 6
    }

    public enum UnitSubclass
    {
        [InspectorName("미설정")]
        None = 0,

        [InspectorName("척후병")]
        VanguardPioneer = 100,

        [InspectorName("돌격수")]
        VanguardCharger = 101,

        [InspectorName("기수")]
        VanguardStandardBearer = 102,

        [InspectorName("전술가")]
        VanguardTactician = 103,

        [InspectorName("에이전트")]
        VanguardAgent = 104,

        [InspectorName("책사")]
        VanguardStrategist = 105,

        [InspectorName("드래드노트")]
        GuardDreadnought = 200,

        [InspectorName("공격수")]
        GuardFighter = 201,

        [InspectorName("로드")]
        GuardLord = 202,

        [InspectorName("아츠 파이터")]
        GuardArtsFighter = 203,

        [InspectorName("교관")]
        GuardInstructor = 204,

        [InspectorName("솔로블레이드")]
        GuardSoloBlade = 205,

        [InspectorName("프로텍터")]
        DefenderProtector = 300,

        [InspectorName("가디언")]
        DefenderGuardian = 301,

        [InspectorName("저거너트")]
        DefenderJuggernaut = 302,

        [InspectorName("아츠 프로텍터")]
        DefenderArtsProtector = 303,

        [InspectorName("결전자")]
        DefenderDuelist = 304,

        [InspectorName("포트리스")]
        DefenderFortress = 305,

        [InspectorName("감속자")]
        SupporterSlower = 400,

        [InspectorName("비호자")]
        SupporterShelterer = 401,

        [InspectorName("약화자")]
        SupporterWeakener = 402,

        [InspectorName("명사수")]
        SniperMarksman = 500,

        [InspectorName("포격수")]
        SniperArtillery = 501,

        [InspectorName("저격수")]
        SniperSharpshooter = 502,

        [InspectorName("공성사수")]
        SniperSiegeArcher = 503,

        [InspectorName("마스터")]
        SpecialistMaster = 600
    }

    public enum UnitPlacement
    {
        [InspectorName("미설정")]
        None = 0,

        [InspectorName("지상")]
        Ground = 1,

        [InspectorName("언덕")]
        HighGround = 2,

        [InspectorName("지상·언덕")]
        GroundAndHighGround = 3
    }
}