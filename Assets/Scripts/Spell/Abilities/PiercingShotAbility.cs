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
        PiercingStatus.Apply(ctx.caster, duration, ctx.marble);
        Transform anchor = SpellVfx.VisualAnchor(ctx.caster); // 몸통 시각 중심(스프라이트 상단 여백 보정)
        if (effectPrefab != null)
        {
            GameObject fx = Object.Instantiate(effectPrefab, anchor.position, Quaternion.identity, anchor);
            Object.Destroy(fx, duration);
        }
        else
        {
            SpellVfx.SpawnAura(anchor, vfxRadius, vfxColor, duration);
            SpellParticleVfx.SpawnOrbit(anchor, vfxRadius, vfxColor, duration); // 관통 금빛 궤도 입자
        }
    }
}
