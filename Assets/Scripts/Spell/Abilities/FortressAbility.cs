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
        Transform anchor = SpellVfx.VisualAnchor(ctx.caster); // 몸통 시각 중심(스프라이트 상단 여백 보정)
        if (effectPrefab != null)
        {
            GameObject fx = Object.Instantiate(effectPrefab, anchor.position, Quaternion.identity, anchor);
            Object.Destroy(fx, duration);
        }
        else
        {
            SpellVfx.SpawnAura(anchor, vfxRadius, vfxColor, duration);
            SpellParticleVfx.SpawnOrbit(anchor, vfxRadius, vfxColor, duration); // 요새 푸른빛 궤도 입자
        }
    }
}
