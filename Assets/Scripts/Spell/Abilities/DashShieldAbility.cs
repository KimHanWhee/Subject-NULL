using UnityEngine;

// ♦ Dash Shield (Normal, SelfBuff) — 10초간 대시 무적 시간 2배.
[CreateAssetMenu(fileName = "DashShield", menuName = "Spell/Abilities/DashShield")]
public class DashShieldAbility : SpellAbility
{
    public float duration = 10f;

    [Header("Code VFX (effectPrefab 미지정 시)")]
    public Color vfxColor = new Color(0.4f, 0.8f, 1f, 1f); // 대시 하늘색
    public float vfxRadius = 0.7f;

    public override void Activate(SpellContext ctx)
    {
        if (ctx.caster == null) return;
        DashShieldStatus.Apply(ctx.caster, duration, ctx.marble);
        Transform anchor = SpellVfx.VisualAnchor(ctx.caster); // 몸통 시각 중심(스프라이트 상단 여백 보정)
        if (effectPrefab != null)
        {
            GameObject fx = Object.Instantiate(effectPrefab, anchor.position, Quaternion.identity, anchor);
            Object.Destroy(fx, duration);
        }
        else
        {
            SpellVfx.SpawnAura(anchor, vfxRadius, vfxColor, 0.8f); // 짧은 확인 오라
            SpellParticleVfx.SpawnOrbit(anchor, vfxRadius, vfxColor, duration); // 하늘색 궤도 입자(버프 지속 표시)
        }
    }
}
