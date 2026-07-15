using UnityEngine;

// ♥ Adrenaline (Gold, SelfBuff) — 10초간 체력이 낮을수록 이동속도/공격속도 증가.
[CreateAssetMenu(fileName = "Adrenaline", menuName = "Spell/Abilities/Adrenaline")]
public class AdrenalineAbility : SpellAbility
{
    [Tooltip("빈사(HP 0%) 시 최대 보너스 배율(0.5 = +50%)")]
    public float maxBonus = 0.5f;
    public float duration = 10f;

    [Header("Code VFX (effectPrefab 미지정 시)")]
    public Color vfxColor = new Color(1f, 0.55f, 0.15f, 1f); // 아드레날린 주황
    public float vfxRadius = 0.75f;

    public override void Activate(SpellContext ctx)
    {
        if (ctx.caster == null) return;
        AdrenalineStatus.Apply(ctx.caster, maxBonus, duration, ctx.marble);
        Transform anchor = SpellVfx.VisualAnchor(ctx.caster); // 몸통 시각 중심(스프라이트 상단 여백 보정)
        if (effectPrefab != null)
        {
            GameObject fx = Object.Instantiate(effectPrefab, anchor.position, Quaternion.identity, anchor);
            Object.Destroy(fx, duration);
        }
        else
        {
            SpellVfx.SpawnAura(anchor, vfxRadius, vfxColor, duration);
            SpellParticleVfx.SpawnOrbit(anchor, vfxRadius, vfxColor, duration); // 주황 궤도 입자(버프 지속 표시)
        }
    }
}
