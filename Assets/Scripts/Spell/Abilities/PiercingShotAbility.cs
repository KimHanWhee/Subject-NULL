using UnityEngine;

// ♠ Piercing Shot (Normal, SelfBuff) — 5초간 발사하는 총알이 적을 관통.
[CreateAssetMenu(fileName = "PiercingShot", menuName = "Spell/Abilities/PiercingShot")]
public class PiercingShotAbility : SpellAbility
{
    public float duration = 5f;

    [Header("Code VFX (effectPrefab 미지정 시)")]
    public Color vfxColor = new Color(1f, 0.95f, 0.6f, 1f); // 관통 금빛
    public float vfxRadius = 0.7f;

    public override void Activate(SpellContext ctx)
    {
        if (ctx.caster == null) return;
        PiercingStatus.Apply(ctx.caster, duration);
        if (effectPrefab != null)
        {
            GameObject fx = Object.Instantiate(effectPrefab, ctx.caster.transform.position, Quaternion.identity, ctx.caster.transform);
            Object.Destroy(fx, duration);
        }
        else
        {
            SpellVfx.SpawnAura(ctx.caster.transform, vfxRadius, vfxColor, duration);
            SpellParticleVfx.SpawnOrbit(ctx.caster.transform, vfxRadius, vfxColor, duration); // 관통 금빛 궤도 입자
        }
    }
}
