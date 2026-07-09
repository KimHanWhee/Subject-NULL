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

        Transform anchor = SpellVfx.VisualAnchor(ctx.caster); // 몸통 시각 중심(스프라이트 상단 여백 보정)
        if (effectPrefab != null)
        {
            GameObject fx = Object.Instantiate(effectPrefab, anchor.position, Quaternion.identity, anchor);
            Object.Destroy(fx, duration);
        }
        else
        {
            SpellVfx.SpawnAura(anchor, vfxRadius, vfxColor, duration);
            SpellParticleVfx.SpawnRise(anchor, vfxColor, duration, 0.35f); // World 시뮬 → 이동 시 청록 궤적
        }
    }
}
