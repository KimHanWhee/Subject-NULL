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
        CounterStatus.Apply(ctx.caster, multiplier, counterWindow, duration, ctx.marble);
        Transform anchor = SpellVfx.VisualAnchor(ctx.caster); // 몸통 시각 중심(스프라이트 상단 여백 보정)
        if (effectPrefab != null)
        {
            GameObject fx = Object.Instantiate(effectPrefab, anchor.position, Quaternion.identity, anchor);
            Object.Destroy(fx, duration);
        }
        else
        {
            SpellVfx.SpawnAura(anchor, vfxRadius, vfxColor, duration);
            SpellParticleVfx.SpawnOrbit(anchor, vfxRadius, vfxColor, duration); // 반격 주황 궤도 입자
        }
    }
}
