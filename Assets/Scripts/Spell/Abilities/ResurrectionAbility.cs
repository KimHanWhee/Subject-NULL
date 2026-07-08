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
        if (effectPrefab != null)
        {
            GameObject fx = Object.Instantiate(effectPrefab, ctx.caster.transform.position, Quaternion.identity, ctx.caster.transform);
            Object.Destroy(fx, vfxDuration);
        }
        else SpellVfx.SpawnAura(ctx.caster.transform, vfxRadius, vfxColor, vfxDuration);
    }
}
