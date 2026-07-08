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
        SniperStatus.Apply(ctx.caster, damageMult, bulletSpeedMult, moveMult, duration);
        if (effectPrefab != null)
        {
            GameObject fx = Object.Instantiate(effectPrefab, ctx.caster.transform.position, Quaternion.identity, ctx.caster.transform);
            Object.Destroy(fx, duration);
        }
        else SpellVfx.SpawnAura(ctx.caster.transform, vfxRadius, vfxColor, duration);
    }
}
