using UnityEngine;

// ♥ Lifesteal (Normal, SelfBuff) — 지속시간 동안 총알이 적에게 명중할 때마다 체력 0.5 회복.
[CreateAssetMenu(fileName = "Lifesteal", menuName = "Spell/Abilities/Lifesteal")]
public class LifestealAbility : SpellAbility
{
    [Tooltip("명중 1회당 회복량(고정). 기본 공격 피해 1의 절반")]
    public float healPerHit = 0.5f;
    public float duration = 5f;

    [Header("Code VFX (effectPrefab 미지정 시)")]
    public Color vfxColor = new Color(0.9f, 0.2f, 0.4f, 1f); // 진홍(흡혈)
    public float vfxRadius = 0.75f;

    public override void Activate(SpellContext ctx)
    {
        if (ctx.caster == null) return;
        LifestealStatus.Apply(ctx.caster, healPerHit, duration, ctx.marble);
        Transform anchor = SpellVfx.VisualAnchor(ctx.caster); // 몸통 시각 중심(스프라이트 상단 여백 보정)
        if (effectPrefab != null)
        {
            GameObject fx = Object.Instantiate(effectPrefab, anchor.position, Quaternion.identity, anchor);
            Object.Destroy(fx, duration);
        }
        else
        {
            SpellVfx.SpawnAura(anchor, vfxRadius, vfxColor, duration);
            SpellParticleVfx.SpawnOrbit(anchor, vfxRadius, vfxColor, duration); // 진홍 궤도 입자(버프 지속 표시)
        }
    }
}
