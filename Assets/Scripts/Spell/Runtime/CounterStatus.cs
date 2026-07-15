using UnityEngine;

// ♦ Counter 상태 — 버프 지속 중 피격 직후 counterWindow(0.5초) 내 발사한 공격의 데미지 배율 적용.
public class CounterStatus : MonoBehaviour, IPlayerHitListener, IPlayerOutgoingModifier, IBuffDisplay
{
    private float multiplier;
    private float counterWindow;
    private float remain;
    private float windowUntil = -1f; // 이 시각까지 발사 시 배율 적용
    private SpellMarble marble;

    public SpellMarble BuffMarble { get { return marble; } }
    public bool BuffTimed { get { return true; } }
    public float BuffRemaining { get { return remain; } }
    public int BuffCharges { get { return 0; } }

    public static void Apply(GameObject player, float multiplier, float counterWindow, float duration, SpellMarble marble = null)
    {
        CounterStatus s = player.GetComponent<CounterStatus>();
        if (s == null) s = player.AddComponent<CounterStatus>();
        s.multiplier = Mathf.Max(1f, multiplier);
        s.counterWindow = counterWindow;
        s.marble = marble;
        s.remain = Mathf.Max(s.remain, duration);
    }

    public void OnPlayerHit(float damage)
    {
        windowUntil = Time.time + counterWindow; // 피격 → 반격 윈도우 시작
    }

    public float ModifyOutgoingDamage(float damage)
        => Time.time <= windowUntil ? damage * multiplier : damage;

    void Update()
    {
        remain -= Time.deltaTime;
        if (remain <= 0f) Destroy(this);
    }
}
