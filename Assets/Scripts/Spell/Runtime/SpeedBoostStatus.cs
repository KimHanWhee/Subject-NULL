using UnityEngine;

// ♥ 이동속도 버프 — PlayerController.speed를 일시 배율 적용 후 원복.
// 재적용 시 지속만 갱신(배율 중첩 방지). 대시(dashSpeed)는 건드리지 않음.
public class SpeedBoostStatus : MonoBehaviour, IBuffDisplay
{
    private PlayerController pc;
    private float remain;
    private SpellMarble marble;

    public SpellMarble BuffMarble { get { return marble; } }
    public bool BuffTimed { get { return true; } }
    public float BuffRemaining { get { return remain; } }
    public int BuffCharges { get { return 0; } }

    public static void Apply(GameObject player, float multiplier, float duration, SpellMarble marble = null)
    {
        SpeedBoostStatus s = player.GetComponent<SpeedBoostStatus>();
        if (s == null)
        {
            s = player.AddComponent<SpeedBoostStatus>();
            if (!s.Bind(multiplier)) { Destroy(s); return; }
        }
        s.marble = marble;
        s.remain = Mathf.Max(s.remain, duration);
    }

    bool Bind(float multiplier)
    {
        pc = GetComponent<PlayerController>();
        if (pc == null) return false;
        // 배율로 등록 — speed 필드를 덮어쓰지 않으므로 다른 속도 버프와 안전하게 공존
        PlayerSpeedModifiers.Set(this, Mathf.Max(1f, multiplier));
        return true;
    }

    void Update()
    {
        remain -= Time.deltaTime;
        if (remain <= 0f) { Restore(); Destroy(this); }
    }

    void Restore()
    {
        PlayerSpeedModifiers.Clear(this);
        pc = null;
    }

    void OnDisable() { Restore(); Destroy(this); }
}
