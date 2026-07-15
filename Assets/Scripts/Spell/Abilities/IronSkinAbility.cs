using UnityEngine;

// ♦ Iron Skin (Normal, SelfBuff) — 10초간 받는 피해 50% 감소.
[CreateAssetMenu(fileName = "IronSkin", menuName = "Spell/Abilities/IronSkin")]
public class IronSkinAbility : SpellAbility
{
    [Range(0f, 1f)] public float reduction = 0.5f;
    public float duration = 10f;

    [Header("Code VFX (effectPrefab 미지정 시)")]
    public Color vfxColor = new Color(0.75f, 0.75f, 0.8f, 1f); // 강철빛
    public float vfxRadius = 0.75f;

    public override void Activate(SpellContext ctx)
    {
        if (ctx.caster == null) return;
        IronSkinStatus.Apply(ctx.caster, reduction, duration, ctx.marble);
        Transform anchor = SpellVfx.VisualAnchor(ctx.caster); // 몸통 시각 중심(스프라이트 상단 여백 보정)
        if (effectPrefab != null)
        {
            GameObject fx = Object.Instantiate(effectPrefab, anchor.position, Quaternion.identity, anchor);
            Object.Destroy(fx, duration);
        }
        else
        {
            SpellVfx.SpawnAura(anchor, vfxRadius, vfxColor, duration);
            SpellParticleVfx.SpawnOrbit(anchor, vfxRadius, vfxColor, duration); // 강철빛 궤도 입자(버프 지속 표시)
        }
    }
}
