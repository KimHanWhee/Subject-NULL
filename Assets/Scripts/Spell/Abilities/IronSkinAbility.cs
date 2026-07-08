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
        IronSkinStatus.Apply(ctx.caster, reduction, duration);
        if (effectPrefab != null)
        {
            GameObject fx = Object.Instantiate(effectPrefab, ctx.caster.transform.position, Quaternion.identity, ctx.caster.transform);
            Object.Destroy(fx, duration);
        }
        else SpellVfx.SpawnAura(ctx.caster.transform, vfxRadius, vfxColor, duration);
    }
}
