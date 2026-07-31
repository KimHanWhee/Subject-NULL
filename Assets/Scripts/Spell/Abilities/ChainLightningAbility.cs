using UnityEngine;

// ♠ Chain Lightning (Gold, SelfBuff) — 10초간 총알이 적에게 명중하면 주변 적으로 번개가 연쇄.
[CreateAssetMenu(fileName = "ChainLightning", menuName = "Spell/Abilities/ChainLightning")]
public class ChainLightningAbility : SpellAbility
{
    public float chainDamage = 1f;
    public float jumpRadius = 2.5f;
    public int maxJumps = 3;
    public float duration = 10f;

    [Header("Code VFX (effectPrefab 미지정 시)")]
    public Color vfxColor = new Color(0.6f, 0.85f, 1f, 1f); // 번개 청백
    public float vfxRadius = 0.7f;

    public override void Activate(SpellContext ctx)
    {
        if (ctx.caster == null) return;
        ChainLightningStatus.Apply(ctx.caster, chainDamage, jumpRadius, maxJumps, duration, ctx.marble);
        Transform anchor = SpellVfx.VisualAnchor(ctx.caster); // 몸통 시각 중심(스프라이트 상단 여백 보정)
        if (effectPrefab != null)
        {
            GameObject fx = Object.Instantiate(effectPrefab, anchor.position, Quaternion.identity, anchor);
            Object.Destroy(fx, duration);
        }
        else
        {
            SpellVfx.SpawnAura(anchor, vfxRadius, vfxColor, duration);
            SpellParticleVfx.SpawnOrbit(anchor, vfxRadius, vfxColor, duration); // 번개 청백 궤도 입자
        }
    }
}
