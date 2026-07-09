using UnityEngine;

// ♦ Reflect (Normal, SelfBuff) — 3초간 받는 피해를 공격한 적에게 반사.
[CreateAssetMenu(fileName = "Reflect", menuName = "Spell/Abilities/Reflect")]
public class ReflectAbility : SpellAbility
{
    public float duration = 3f;

    [Header("Code VFX (effectPrefab 미지정 시)")]
    public Color vfxColor = new Color(1f, 0.9f, 0.3f, 1f); // 금빛(가시)
    public float vfxRadius = 0.8f;

    public override void Activate(SpellContext ctx)
    {
        if (ctx.caster == null) return;
        ReflectStatus.Apply(ctx.caster, duration);
        if (effectPrefab != null)
        {
            GameObject fx = Object.Instantiate(effectPrefab, ctx.caster.transform.position, Quaternion.identity, ctx.caster.transform);
            Object.Destroy(fx, duration);
        }
        else
        {
            SpellVfx.SpawnAura(ctx.caster.transform, vfxRadius, vfxColor, duration);
            SpellParticleVfx.SpawnOrbit(ctx.caster.transform, vfxRadius, vfxColor, duration); // 금빛 가시 궤도 입자
        }
    }
}
