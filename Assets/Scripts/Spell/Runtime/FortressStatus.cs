using UnityEngine;

// ♦ Fortress 상태 — 지속시간 동안 이동 불가 + 받는 피해 완전 무효.
public class FortressStatus : MonoBehaviour, IPlayerDamageModifier, IBuffDisplay
{
    private PlayerController pc;
    private float remain;
    private SpellMarble marble;

    public SpellMarble BuffMarble { get { return marble; } }
    public bool BuffTimed { get { return true; } }
    public float BuffRemaining { get { return remain; } }
    public int BuffCharges { get { return 0; } }

    public static void Apply(GameObject player, float duration, SpellMarble marble = null)
    {
        FortressStatus s = player.GetComponent<FortressStatus>();
        if (s == null)
        {
            s = player.AddComponent<FortressStatus>();
            s.pc = player.GetComponent<PlayerController>();
            if (s.pc != null) s.pc.movementLocked = true;
        }
        s.marble = marble;
        s.remain = Mathf.Max(s.remain, duration);
    }

    public float ModifyIncomingDamage(float damage, GameObject attacker) => 0f; // 완전 무효

    void Update()
    {
        remain -= Time.deltaTime;
        if (remain <= 0f) Destroy(this);
    }

    void Unlock()
    {
        if (pc != null) pc.movementLocked = false;
        pc = null;
    }

    void OnDisable() { Unlock(); }
    void OnDestroy() { Unlock(); }
}
