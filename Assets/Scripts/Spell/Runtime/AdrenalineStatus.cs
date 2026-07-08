using UnityEngine;

// ♥ Adrenaline 상태 — 지속시간 동안 체력이 낮을수록 이동속도/공격속도 증가.
// 배율 = 1 + (1 - HP비율) × maxBonus. (풀피 1.0배 ~ 빈사 1+maxBonus배)
public class AdrenalineStatus : MonoBehaviour
{
    private PlayerController pc;
    private Character ch;
    private float originalSpeed;
    private float maxBonus;
    private float remain;

    public static void Apply(GameObject player, float maxBonus, float duration)
    {
        AdrenalineStatus s = player.GetComponent<AdrenalineStatus>();
        if (s == null)
        {
            s = player.AddComponent<AdrenalineStatus>();
            s.pc = player.GetComponent<PlayerController>();
            s.ch = player.GetComponent<Character>();
            if (s.pc == null || s.ch == null) { Destroy(s); return; }
            s.originalSpeed = s.pc.speed;
        }
        s.maxBonus = maxBonus;
        s.remain = Mathf.Max(s.remain, duration);
    }

    void Update()
    {
        remain -= Time.deltaTime;
        if (remain <= 0f) { Restore(); Destroy(this); return; }

        float mult = 1f + (1f - ch.HpRatio) * maxBonus; // 저체력일수록 커짐
        pc.speed = originalSpeed * mult;
        pc.fireRateMultiplier = mult;
    }

    void Restore()
    {
        if (pc != null)
        {
            pc.speed = originalSpeed;
            pc.fireRateMultiplier = 1f;
        }
        pc = null;
    }

    void OnDisable() { Restore(); }
}
