using UnityEngine;

// ♦ Fortress 상태 — 지속시간 동안 이동 불가 + 받는 피해 완전 무효.
public class FortressStatus : MonoBehaviour, IPlayerDamageModifier
{
    private PlayerController pc;
    private float remain;

    public static void Apply(GameObject player, float duration)
    {
        FortressStatus s = player.GetComponent<FortressStatus>();
        if (s == null)
        {
            s = player.AddComponent<FortressStatus>();
            s.pc = player.GetComponent<PlayerController>();
            if (s.pc != null) s.pc.movementLocked = true;
        }
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
