using UnityEngine;

// ♥ Lifesteal (Normal, SelfBuff) — 10초간 적에게 총알이 명중할 때마다 체력 1 회복.
[CreateAssetMenu(fileName = "Lifesteal", menuName = "Spell/Abilities/Lifesteal")]
public class LifestealAbility : SpellAbility
{
    public float healPerHit = 1f;
    public float duration = 10f;

    [Header("Code VFX (effectPrefab 미지정 시)")]
    public Color vfxColor = new Color(0.9f, 0.2f, 0.4f, 1f); // 진홍(흡혈)
    public float vfxRadius = 0.75f;

    public override void Activate(SpellContext ctx)
    {
        if (ctx.caster == null) return;
        LifestealStatus.Apply(ctx.caster, healPerHit, duration);
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
