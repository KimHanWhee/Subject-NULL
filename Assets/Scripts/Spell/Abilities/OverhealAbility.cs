using UnityEngine;

// ♥ Overheal (Gold, SelfBuff) — 최대 체력을 초과하는 임시 체력을 부여(소진까지 유지).
[CreateAssetMenu(fileName = "Overheal", menuName = "Spell/Abilities/Overheal")]
public class OverhealAbility : SpellAbility
{
    public float tempHp = 2f;

    [Header("Code VFX (effectPrefab 미지정 시)")]
    public Color vfxColor = new Color(1f, 0.85f, 0.35f, 1f); // 황금빛(초과 체력)
    public float vfxRadius = 0.8f;
    public float vfxDuration = 0.8f;

    public override void Activate(SpellContext ctx)
    {
        if (ctx.caster == null) return;
        OverhealStatus.Apply(ctx.caster, tempHp);
        if (effectPrefab != null)
        {
            GameObject fx = Object.Instantiate(effectPrefab, ctx.caster.transform.position, Quaternion.identity, ctx.caster.transform);
            Object.Destroy(fx, vfxDuration);
        }
        else
        {
            SpellVfx.SpawnAura(ctx.caster.transform, vfxRadius, vfxColor, vfxDuration);
            SpellParticleVfx.SpawnRise(ctx.caster.transform, vfxColor, 1f); // 황금 입자 상승(초과 체력 획득감)
        }
    }
}
