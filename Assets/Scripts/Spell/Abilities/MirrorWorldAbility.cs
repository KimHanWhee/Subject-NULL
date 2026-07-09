using UnityEngine;

// ♦ Mirror World (Legend, SelfBuff) — 5초간 받는 모든 피해가 주변 적에게 분산된다.
[CreateAssetMenu(fileName = "MirrorWorld", menuName = "Spell/Abilities/MirrorWorld")]
public class MirrorWorldAbility : SpellAbility
{
    public float radius = 4f;   // 분산 반경
    public float duration = 5f;

    [Header("Code VFX (effectPrefab 미지정 시)")]
    public Color vfxColor = new Color(0.8f, 0.5f, 1f, 1f); // 신비한 보라
    public float vfxRadius = 1f;

    public override void Activate(SpellContext ctx)
    {
        if (ctx.caster == null) return;
        MirrorWorldStatus.Apply(ctx.caster, radius, duration);
        if (effectPrefab != null)
        {
            GameObject fx = Object.Instantiate(effectPrefab, ctx.caster.transform.position, Quaternion.identity, ctx.caster.transform);
            Object.Destroy(fx, duration);
        }
        else
        {
            SpellVfx.SpawnAura(ctx.caster.transform, vfxRadius, vfxColor, duration);
            SpellParticleVfx.SpawnOrbit(ctx.caster.transform, vfxRadius, vfxColor, duration); // 신비한 보라 궤도 입자
        }
    }
}
