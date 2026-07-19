using UnityEngine;

// ♠ Gatling (Normal, SelfBuff) — 10초간 클릭 1회가 빠른 3연사가 된다.
[CreateAssetMenu(fileName = "Gatling", menuName = "Spell/Abilities/Gatling")]
public class GatlingAbility : SpellAbility
{
    [Tooltip("기본 1발에 이어 쏘는 추가 발 수(2 = 총 3연사)")]
    public int extraBullets = 2;
    [Tooltip("연사 간격(초)")]
    public float burstInterval = 0.08f;
    public float duration = 10f;

    [Header("Code VFX")]
    public Color vfxColor = new Color(1f, 0.75f, 0.3f, 1f); // 화약 주황

    public override void Activate(SpellContext ctx)
    {
        if (ctx.caster == null) return;
        GatlingStatus.Apply(ctx.caster, extraBullets, burstInterval, duration, ctx.marble);
        Transform anchor = SpellVfx.VisualAnchor(ctx.caster);
        SpellVfx.SpawnAura(anchor, 0.6f, vfxColor, 0.6f);
        SpellParticleVfx.SpawnBurst(anchor.position, 0.6f, vfxColor, 14, 0.3f);
    }
}
