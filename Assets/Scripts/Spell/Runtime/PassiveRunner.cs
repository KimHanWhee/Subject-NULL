using System.Collections.Generic;
using UnityEngine;

// 패시브 오브 구동기 — 편성된 패시브를 게임 시작 시 읽어 "언제 발동할지"를 관리한다.
// "무엇을 할지"는 능력(SpellAbility)이 이미 알고 있으므로 여기서는 호출 시점만 정한다.
//
// HUD(PassiveHUD)가 각 슬롯의 상태를 읽어 표시하므로, 표시에 필요한 값을 공개한다.
public class PassiveRunner : MonoBehaviour
{
    public class Slot
    {
        public SpellMarble marble;
        public float duration;      // 능력 에셋에서 읽은 지속시간(0이면 지속 개념 없음)

        public float activeUntil;   // 이 시각까지 발동 중
        public float nextFireTime;  // Periodic — 다음 발동 시각
        public int killsLeft;       // OnKillCount — 남은 처치 수
        public bool fired;          // Once — 발동 완료
        public bool everFired;      // 첫 발동 여부(연출은 첫 발동에서만)

        public bool IsActive { get { return Time.time < activeUntil; } }
        public float RemainActive { get { return Mathf.Max(0f, activeUntil - Time.time); } }
        public float RemainCooldown { get { return Mathf.Max(0f, nextFireTime - Time.time); } }
    }

    public IReadOnlyList<Slot> Slots { get { return slots; } }
    private readonly List<Slot> slots = new List<Slot>();

    public static PassiveRunner Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void OnEnable()  { GameManager.EnemyKilled += OnEnemyKilled; }
    void OnDisable() { GameManager.EnemyKilled -= OnEnemyKilled; if (Instance == this) Instance = null; }

    // SpellCaster가 덱을 구성한 뒤 호출 — 편성된 패시브 목록을 넘긴다.
    public void Setup(List<SpellMarble> passives)
    {
        slots.Clear();
        if (passives == null) return;

        foreach (SpellMarble m in passives)
        {
            if (m == null || m.ability == null || !m.isPassive) continue;

            Slot s = new Slot();
            s.marble = m;
            s.duration = ReadDuration(m.ability);
            s.killsLeft = Mathf.Max(1, m.passiveKillCount);
            s.nextFireTime = Time.time; // Periodic은 즉시 1회 발동으로 시작
            slots.Add(s);
        }

        // 시작 시 한 번만 거는 것들.
        //  Always — 만료 없는 상태로 걸어두면 끝. 주기적 재적용이 필요 없다.
        //  Once   — 부활처럼 "가지고 시작해서 소모될 때까지" 남는 것.
        foreach (Slot s in slots)
        {
            PassiveMode mode = s.marble.passiveMode;
            if (mode == PassiveMode.Always || mode == PassiveMode.Once) Fire(s);
        }
    }

    // 능력 에셋의 duration 필드를 읽는다.
    // 능력마다 타입이 달라 공통 인터페이스가 없으므로 리플렉션으로 꺼낸다.
    // (여기서만 쓰는 표시용 값 — 실제 지속은 각 상태 컴포넌트가 관리한다)
    static float ReadDuration(SpellAbility a)
    {
        System.Reflection.FieldInfo f = a.GetType().GetField("duration");
        if (f != null && f.FieldType == typeof(float)) return (float)f.GetValue(a);
        return 0f;
    }

    void OnEnemyKilled()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            Slot s = slots[i];
            if (s.marble.passiveMode != PassiveMode.OnKillCount) continue;
            if (s.IsActive) continue; // 발동 중에는 세지 않는다(끝난 뒤 다시 100부터)

            s.killsLeft--;
            if (s.killsLeft <= 0)
            {
                Fire(s);
                s.killsLeft = Mathf.Max(1, s.marble.passiveKillCount); // 다시 처음부터
            }
        }
    }

    // 주기 발동만 매 프레임 확인하면 된다.
    // 상시·1회는 Setup에서 이미 걸었고 스스로 유지된다.
    void Update()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            Slot s = slots[i];
            if (s.marble.passiveMode != PassiveMode.Periodic) continue;
            if (Time.time < s.nextFireTime) continue;

            Fire(s);
            // 간격은 "효과가 끝난 시점부터" 센다.
            // 발동 시점부터 세면 지속(3초)이 간격(5초)을 파먹어 쉬는 틈이 2초밖에 안 남는다 —
            // 거의 계속 도는 것처럼 보여서 주기 패시브라는 느낌이 사라진다.
            s.nextFireTime = s.activeUntil + Mathf.Max(0.1f, s.marble.passiveInterval);
        }
    }

    void Fire(Slot s)
    {
        bool always = s.marble.passiveMode == PassiveMode.Always;

        SpellContext ctx = new SpellContext();
        ctx.caster = gameObject;
        ctx.targetPosition = transform.position;
        ctx.grade = s.marble.grade;
        ctx.marble = s.marble;
        ctx.permanent = always; // 상시는 만료 없이

        s.marble.ability.Activate(ctx);

        s.everFired = true;
        s.fired = true;
        // 상시는 계속 켜진 상태로 표시(HUD가 이 값을 본다)
        s.activeUntil = always ? float.PositiveInfinity
                               : (s.duration > 0f ? Time.time + s.duration : 0f);
    }
}
