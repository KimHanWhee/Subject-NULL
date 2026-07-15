using UnityEngine;

// ♥ Second Wind(무한 질주) 표시 전용 상태 — 실제 효과(대시 스태미너 무소모)는 PlayerController가 처리.
// 이 컴포넌트는 버프 HUD에 남은 시간을 노출하기 위한 미러(같은 duration이라 동기).
public class SecondWindStatus : MonoBehaviour, IBuffDisplay
{
    private float remain;
    private SpellMarble marble;

    public SpellMarble BuffMarble { get { return marble; } }
    public bool BuffTimed { get { return true; } }
    public float BuffRemaining { get { return remain; } }
    public int BuffCharges { get { return 0; } }

    public static void Apply(GameObject player, float duration, SpellMarble marble = null)
    {
        SecondWindStatus s = player.GetComponent<SecondWindStatus>();
        if (s == null) s = player.AddComponent<SecondWindStatus>();
        s.marble = marble;
        s.remain = Mathf.Max(s.remain, duration);
    }

    void Update()
    {
        remain -= Time.deltaTime;
        if (remain <= 0f) Destroy(this);
    }
}
