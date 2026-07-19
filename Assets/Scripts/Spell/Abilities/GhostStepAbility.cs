using UnityEngine;

// ♦ Ghost Step (Gold, SelfBuff) — 3초간 이동속도 증가 + 모든 피해 무효, 대신 공격 불가.
[CreateAssetMenu(fileName = "GhostStep", menuName = "Spell/Abilities/GhostStep")]
public class GhostStepAbility : SpellAbility
{
    public float speedMult = 1.5f;
    public float duration = 3f;

    [Header("Code VFX")]
    public Color vfxColor = new Color(0.75f, 0.7f, 1f, 1f); // 창백한 보라

    public override void Activate(SpellContext ctx)
    {
        if (ctx.caster == null) return;
        GhostStepStatus.Apply(ctx.caster, speedMult, duration, ctx.marble);
        Transform anchor = SpellVfx.VisualAnchor(ctx.caster);
        SpellParticleVfx.SpawnBurst(anchor.position, 0.7f, vfxColor, 16, 0.35f); // 유령화 순간
    }
}
