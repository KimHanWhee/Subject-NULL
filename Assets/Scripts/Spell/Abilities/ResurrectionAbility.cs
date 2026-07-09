using UnityEngine;

// ♥ Resurrection (Legend, SelfBuff) — 다음 사망 시 체력 50%로 부활(1회).
[CreateAssetMenu(fileName = "Resurrection", menuName = "Spell/Abilities/Resurrection")]
public class ResurrectionAbility : SpellAbility
{
    [Range(0.1f, 1f)] public float reviveRatio = 0.5f;

    [Header("Code VFX (effectPrefab 미지정 시)")]
    public Color vfxColor = new Color(1f, 0.95f, 0.5f, 1f); // 성스러운 금빛
    public float vfxRadius = 0.9f;
    public float vfxDuration = 1f;

    public override void Activate(SpellContext ctx)
    {
        if (ctx.caster == null) return;
        ResurrectionStatus.Apply(ctx.caster, reviveRatio);
        Transform anchor = SpellVfx.VisualAnchor(ctx.caster); // 몸통 시각 중심(스프라이트 상단 여백 보정)
        if (effectPrefab != null)
        {
            GameObject fx = Object.Instantiate(effectPrefab, anchor.position, Quaternion.identity, anchor);
            Object.Destroy(fx, vfxDuration);
        }
        else
        {
            SpellVfx.SpawnAura(anchor, vfxRadius, vfxColor, vfxDuration);
            SpellParticleVfx.SpawnRise(anchor, vfxColor, 1.2f); // 금빛 입자 상승(축복 부여감)
        }
    }
}
