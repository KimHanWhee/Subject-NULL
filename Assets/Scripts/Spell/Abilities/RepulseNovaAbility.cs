using UnityEngine;

// ♦ Repulse Nova / 넉백 노바 (SelfBuff, 즉발) — 주변 적을 강하게 밀쳐내고 잠깐 스턴. 포위 탈출용.
[CreateAssetMenu(fileName = "RepulseNova", menuName = "Spell/Abilities/RepulseNova")]
public class RepulseNovaAbility : SpellAbility
{
    [Header("Effect")]
    public float radius = 4f;             // 넉백 범위
    public float knockbackDistance = 3.2f; // 밀려나는 거리
    public float stunDuration = 0.5f;      // 스턴 시간(밀림 포함)

    [Header("Code VFX (effectPrefab 미지정 시)")]
    public Color vfxColor = new Color(0.6f, 0.85f, 1f, 1f); // 충격파 하늘색

    public override void Activate(SpellContext ctx)
    {
        if (ctx.caster == null) return;
        Vector2 center = ctx.caster.transform.position;

        foreach (EnemyBase e in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
        {
            if (e == null || !e.gameObject.activeInHierarchy) continue;
            if (Vector2.Distance(e.transform.position, center) <= radius)
                KnockbackStatus.Apply(e.gameObject, center, knockbackDistance, stunDuration);
        }

        Transform anchor = SpellVfx.VisualAnchor(ctx.caster);
        if (effectPrefab != null)
        {
            GameObject fx = Object.Instantiate(effectPrefab, anchor.position, Quaternion.identity);
            Object.Destroy(fx, 1f);
        }
        else
        {
            SpellVfx.SpawnRing(center, radius, vfxColor, 0.4f); // 확산 충격파 링
            SpellParticleVfx.SpawnBurst(center, radius * 0.6f, vfxColor, 40, 0.5f);
        }
    }
}
