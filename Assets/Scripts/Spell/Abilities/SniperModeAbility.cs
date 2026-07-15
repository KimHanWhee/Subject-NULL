using UnityEngine;

// ♠ Sniper Mode (Gold, SelfBuff) — 3초간 총알 속도/데미지 3배, 대신 이동속도 감소.
[CreateAssetMenu(fileName = "SniperMode", menuName = "Spell/Abilities/SniperMode")]
public class SniperModeAbility : SpellAbility
{
    public float damageMult = 3f;
    public float bulletSpeedMult = 3f;
    [Range(0.1f, 1f)] public float moveMult = 0.5f; // 이동속도 배율(감소)
    public float duration = 3f;

    [Header("Code VFX (effectPrefab 미지정 시)")]
    public Color vfxColor = new Color(0.9f, 0.2f, 0.2f, 1f); // 조준 붉은빛
    public float vfxRadius = 0.7f;

    public override void Activate(SpellContext ctx)
    {
        if (ctx.caster == null) return;
        SniperStatus.Apply(ctx.caster, damageMult, bulletSpeedMult, moveMult, duration, ctx.marble);
        Transform anchor = SpellVfx.VisualAnchor(ctx.caster); // 몸통 시각 중심(스프라이트 상단 여백 보정)
        if (effectPrefab != null)
        {
            GameObject fx = Object.Instantiate(effectPrefab, anchor.position, Quaternion.identity, anchor);
            Object.Destroy(fx, duration);
        }
        else
        {
            SpellVfx.SpawnAura(anchor, vfxRadius, vfxColor, duration);
            SpellParticleVfx.SpawnOrbit(anchor, vfxRadius, vfxColor, duration); // 조준 붉은빛 궤도 입자
        }
    }
}
