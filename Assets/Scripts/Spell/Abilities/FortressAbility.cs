using UnityEngine;

// ♦ Fortress (Gold, SelfBuff) — 5초간 이동 불가 대신 받는 피해 완전 무효.
[CreateAssetMenu(fileName = "Fortress", menuName = "Spell/Abilities/Fortress")]
public class FortressAbility : SpellAbility
{
    public float duration = 5f;

    [Header("Code VFX (effectPrefab 미지정 시)")]
    public Color vfxColor = new Color(0.6f, 0.6f, 1f, 1f); // 요새 푸른빛
    public float vfxRadius = 0.9f;

    public override void Activate(SpellContext ctx)
    {
        if (ctx.caster == null) return;
        FortressStatus.Apply(ctx.caster, duration);
        if (effectPrefab != null)
        {
            GameObject fx = Object.Instantiate(effectPrefab, ctx.caster.transform.position, Quaternion.identity, ctx.caster.transform);
            Object.Destroy(fx, duration);
        }
        else SpellVfx.SpawnAura(ctx.caster.transform, vfxRadius, vfxColor, duration);
    }
}
