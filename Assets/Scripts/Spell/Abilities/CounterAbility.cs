using UnityEngine;

// ♦ Counter (Gold, SelfBuff) — 버프 지속 중 피격 직후 0.5초 내에 공격하면 데미지 2배.
[CreateAssetMenu(fileName = "Counter", menuName = "Spell/Abilities/Counter")]
public class CounterAbility : SpellAbility
{
    public float multiplier = 2f;
    public float counterWindow = 0.5f; // 피격 후 반격 인정 시간(초)
    public float duration = 10f;       // 버프 지속(초)

    [Header("Code VFX (effectPrefab 미지정 시)")]
    public Color vfxColor = new Color(1f, 0.4f, 0.2f, 1f); // 반격 주황
    public float vfxRadius = 0.75f;

    public override void Activate(SpellContext ctx)
    {
        if (ctx.caster == null) return;
        CounterStatus.Apply(ctx.caster, multiplier, counterWindow, duration);
        if (effectPrefab != null)
        {
            GameObject fx = Object.Instantiate(effectPrefab, ctx.caster.transform.position, Quaternion.identity, ctx.caster.transform);
            Object.Destroy(fx, duration);
        }
        else
        {
            SpellVfx.SpawnAura(ctx.caster.transform, vfxRadius, vfxColor, duration);
            SpellParticleVfx.SpawnOrbit(ctx.caster.transform, vfxRadius, vfxColor, duration); // 반격 주황 궤도 입자
        }
    }
}
