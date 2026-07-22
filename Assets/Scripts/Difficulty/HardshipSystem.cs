using UnityEngine;

// 고난(Hardship) — 덱을 한 바퀴 소진할 때마다 플레이어가 3중 1택으로 고르는 "적 강화".
// 스펠 마블을 남발할수록 덱이 빨리 돌고, 그만큼 적이 영구히 강해진다(자원 관리 압박).
//
// 규칙
//  - 누적: 같은 고난을 또 고르면 스택이 쌓인다(중복 선택 허용).
//  - 가산(加算): 스택당 고정 %p를 더한다. 곱연산은 후반에 폭발해서 쓰지 않는다.
//    예) 체력 3스택 = +75%(1.75배). 곱연산이면 1.25³ = 1.95배로 급등.
//  - 판당 초기화: GameManager가 게임 시작 시 ResetAll().
public enum HardshipId
{
    Muscle,       // 근섬유 강화 — 체력
    Nerve,        // 신경 가속 — 이동/공격속도
    Instinct,     // 공격 본능 — 공격력
    Overcrowd,    // 과밀 배양 — 동시 스폰 수
    RapidCulture, // 급속 배양 — 스폰 간격
    Hardened,     // 경화 외피 — 넉백 저항 + 피해 감쇄
    Volatile,     // 자폭 조직 — 사망 시 폭발
    Regen,        // 재생 조직 — 지속 회복
    Frenzy,       // 광폭화 — 빈사 시 가속
    Velocity      // 탄속 개선 — 적 총알 속도
}

public static class HardshipSystem
{
    public const int Count = 10;

    // 스택당 증가량 — 밸런스 조정은 여기만 만지면 된다.
    const float MusclePerStack = 0.25f;   // 최대 체력 +25%p
    const float NervePerStack = 0.15f;    // 이동/공격속도 +15%p
    const float InstinctPerStack = 0.25f; // 공격력 +25%p
    const float RapidPerStack = 0.15f;    // 스폰 간격 -15%p
    const float MinIntervalMult = 0.4f;   // 간격이 0이 되면 시스템이 감당 못 함
    // 경화 외피는 "체감형(1/(1+k·n))"으로 계산한다.
    // 고정 감쇄/선형 저항으로 하면 2스택 만에 바닥(피해 하한·넉백 0)에 닿아
    // 3스택부터는 아무 효과가 없어진다 → 플레이어가 이것만 골라 난이도 상승을 무력화할 수 있다.
    // 체감형은 절대 0이 되지 않고(불사 방지) 스택마다 계속 의미가 있다.
    const float HardenKnockPerStack = 0.5f;
    const float HardenDamagePerStack = 0.25f;
    const float VolatileDamagePerStack = 1f;
    const float RegenRatioPerStack = 0.01f; // 초당 maxHp의 1%
    const float FrenzySpeedPerStack = 1f;   // 빈사 시 +100%p
    const float VelocityPerStack = 0.25f;   // 적 총알 속도 +25%p

    public const float FrenzyHpThreshold = 0.3f; // 이 비율 이하에서 광폭화
    public const float VolatileRadius = 1.7f;
    public const float VolatileDelay = 0.35f;
    const int OvercrowdCap = 4;              // 동시 스폰 추가 상한(풀 고갈 방지)

    static readonly int[] stacks = new int[Count];

    // 스택 변동 통지 — HUD가 구독해 표시를 갱신한다.
    public static event System.Action OnChanged;

    public static void ResetAll()
    {
        for (int i = 0; i < Count; i++) stacks[i] = 0;
        if (OnChanged != null) OnChanged();
    }

    // 종류당 최대 중첩. 이 수치에 도달한 고난은 선택지에 더 이상 나오지 않는다.
    public const int MaxStack = 3;

    public static int Stack(HardshipId id) { return stacks[(int)id]; }

    public static bool IsMaxed(HardshipId id) { return stacks[(int)id] >= MaxStack; }

    public static void Add(HardshipId id)
    {
        if (stacks[(int)id] >= MaxStack) return; // 상한 초과 방어(선택지에서 걸러지지만 이중 안전)
        stacks[(int)id]++;
        if (OnChanged != null) OnChanged();
    }

    // 아직 더 쌓을 수 있는 고난 수 — 선택지를 몇 장 띄울지 결정할 때 쓴다.
    public static int AvailableCount
    {
        get
        {
            int n = 0;
            for (int i = 0; i < Count; i++) if (stacks[i] < MaxStack) n++;
            return n;
        }
    }

    public static int TotalStacks
    {
        get { int t = 0; for (int i = 0; i < Count; i++) t += stacks[i]; return t; }
    }

    // ---- 파생 수치(각 시스템이 참조) ----

    public static float EnemyHpMult { get { return 1f + MusclePerStack * stacks[(int)HardshipId.Muscle]; } }
    public static float EnemySpeedMult { get { return 1f + NervePerStack * stacks[(int)HardshipId.Nerve]; } }
    public static float EnemyDamageMult { get { return 1f + InstinctPerStack * stacks[(int)HardshipId.Instinct]; } }
    public static int SpawnBonus { get { return Mathf.Min(stacks[(int)HardshipId.Overcrowd], OvercrowdCap); } }

    public static float SpawnIntervalMult
    {
        get { return Mathf.Max(MinIntervalMult, 1f - RapidPerStack * stacks[(int)HardshipId.RapidCulture]); }
    }

    // 넉백 배율(작을수록 안 밀림). 완전 면역은 되지 않는다.
    public static float KnockbackMult
    {
        get { return 1f / (1f + HardenKnockPerStack * stacks[(int)HardshipId.Hardened]); }
    }

    // 적이 받는 피해 배율(작을수록 단단함). 0이 되지 않으므로 불사 방어가 따로 필요 없다.
    public static float DamageTakenMult
    {
        get { return 1f / (1f + HardenDamagePerStack * stacks[(int)HardshipId.Hardened]); }
    }

    public static bool VolatileOn { get { return stacks[(int)HardshipId.Volatile] > 0; } }
    public static float VolatileDamage { get { return VolatileDamagePerStack * stacks[(int)HardshipId.Volatile]; } }

    // 초당 회복량(최대 체력 비율)
    public static float RegenRatioPerSec { get { return RegenRatioPerStack * stacks[(int)HardshipId.Regen]; } }

    public static bool FrenzyOn { get { return stacks[(int)HardshipId.Frenzy] > 0; } }
    public static float FrenzySpeedMult { get { return 1f + FrenzySpeedPerStack * stacks[(int)HardshipId.Frenzy]; } }

    public static float EnemyBulletSpeedMult { get { return 1f + VelocityPerStack * stacks[(int)HardshipId.Velocity]; } }

    // ---- 표시용 정의 ----

    // 표시 문자열은 키로만 들고 있고, 실제 글자는 Loc가 현재 언어로 돌려준다.
    public struct Def
    {
        public HardshipId id;
        public string key;    // "hs.muscle" → 이름 ".n" / 설명 ".d"
        public Color color;

        public string Name { get { return Loc.T(key + ".n"); } }
        public string Desc { get { return Loc.T(key + ".d"); } }
    }

    static readonly Def[] defs = new Def[]
    {
        Def_(HardshipId.Muscle,       "hs.muscle",   new Color(1f, 0.55f, 0.45f)),
        Def_(HardshipId.Nerve,        "hs.nerve",    new Color(0.6f, 0.9f, 1f)),
        Def_(HardshipId.Instinct,     "hs.instinct", new Color(1f, 0.45f, 0.55f)),
        Def_(HardshipId.Overcrowd,    "hs.crowd",    new Color(1f, 0.8f, 0.4f)),
        Def_(HardshipId.RapidCulture, "hs.rapid",    new Color(1f, 0.7f, 0.35f)),
        Def_(HardshipId.Hardened,     "hs.hard",     new Color(0.75f, 0.78f, 0.85f)),
        Def_(HardshipId.Volatile,     "hs.volatile", new Color(1f, 0.6f, 0.2f)),
        Def_(HardshipId.Regen,        "hs.regen",    new Color(0.6f, 1f, 0.7f)),
        Def_(HardshipId.Frenzy,       "hs.frenzy",   new Color(1f, 0.4f, 0.75f)),
        Def_(HardshipId.Velocity,     "hs.velocity", new Color(0.8f, 0.7f, 1f)),
    };

    static Def Def_(HardshipId id, string key, Color c)
    {
        Def x; x.id = id; x.key = key; x.color = c; return x;
    }

    public static Def GetDef(HardshipId id) { return defs[(int)id]; }

    // 선택지 n개 추첨(중복 없이). 최대 중첩에 도달한 고난은 후보에서 제외한다.
    // 남은 후보가 n보다 적으면 있는 만큼만 반환하고, 하나도 없으면 빈 배열을 준다
    // (호출부는 빈 배열일 때 선택 화면을 건너뛰어야 한다 — 안 그러면 진행이 막힌다).
    public static HardshipId[] PickChoices(int n)
    {
        System.Collections.Generic.List<int> pool = new System.Collections.Generic.List<int>();
        for (int i = 0; i < Count; i++) if (stacks[i] < MaxStack) pool.Add(i);

        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int t = pool[i]; pool[i] = pool[j]; pool[j] = t;
        }

        int take = Mathf.Min(Mathf.Max(n, 0), pool.Count);
        HardshipId[] res = new HardshipId[take];
        for (int i = 0; i < take; i++) res[i] = (HardshipId)pool[i];
        return res;
    }
}
