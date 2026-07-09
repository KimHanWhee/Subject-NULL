using UnityEngine;

// ♥ Resurrection 상태 — 다음 사망 시 체력 50%로 부활(1회 소비). 부활 직후 짧은 무적.
public class ResurrectionStatus : MonoBehaviour, IPlayerDeathInterceptor
{
    private float reviveRatio = 0.5f;
    private float postShield = 1.5f; // 부활 직후 무적(초)
    private bool consumed;           // Destroy는 프레임 끝 실행 → 같은 프레임 중복 인터셉트 방지

    public static void Apply(GameObject player, float reviveRatio)
    {
        ResurrectionStatus s = player.GetComponent<ResurrectionStatus>();
        if (s == null) s = player.AddComponent<ResurrectionStatus>();
        s.reviveRatio = reviveRatio;
        s.consumed = false; // 재사용(새 마블 사용) 시 리셋
    }

    public bool TryInterceptDeath()
    {
        if (consumed) return false;
        Character ch = GetComponent<Character>();
        if (ch == null) return false;
        consumed = true;
        ch.Revive(reviveRatio);
        PlayerController pc = GetComponent<PlayerController>();
        if (pc != null) pc.GrantShield(postShield); // 연속 피격 즉사 방지
        Color gold = new Color(1f, 0.95f, 0.5f, 1f);
        Transform anchor = SpellVfx.VisualAnchor(gameObject); // 몸통 시각 중심(스프라이트 상단 여백 보정)
        SpellVfx.SpawnRing(anchor.position, 1.5f, gold, 0.8f);                // 부활 섬광
        SpellParticleVfx.SpawnBurst(anchor.position, 1.5f, gold, 36, 0.6f);   // 금빛 파편 폭발
        SpellParticleVfx.SpawnRise(anchor, gold, 1.2f);                       // 승천하는 금빛 입자
        Destroy(this); // 1회 소비
        return true;
    }
}
