using UnityEngine;

// ♥ Speed Up (Normal, SelfBuff) — 잠시 이동속도 증가.
[CreateAssetMenu(fileName = "SpeedUp", menuName = "Spell/Abilities/SpeedUp")]
public class SpeedUpAbility : SpellAbility
{
    public float multiplier = 1.5f;
    public float duration = 5f;

    [Header("Code VFX (effectPrefab 미지정 시)")]
    public Color vfxColor = new Color(0.4f, 1f, 0.9f, 1f); // 청록(가속)
    public float vfxRadius = 0.7f;

    public override void Activate(SpellContext ctx)
    {
        if (ctx.caster == null) return;
        SpeedBoostStatus.Apply(ctx.caster, multiplier, duration);

        if (effectPrefab != null)
        {
            GameObject fx = Object.Instantiate(effectPrefab, ctx.caster.transform.position, Quaternion.identity, ctx.caster.transform);
            Object.Destroy(fx, duration);
        }
        else
        {
            SpellVfx.SpawnAura(ctx.caster.transform, vfxRadius, vfxColor, duration);
            SpellParticleVfx.SpawnRise(ctx.caster.transform, vfxColor, duration, 0.35f); // World 시뮬 → 이동 시 청록 궤적
        }
    }
}
