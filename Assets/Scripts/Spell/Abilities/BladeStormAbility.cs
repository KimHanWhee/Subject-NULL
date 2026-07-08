using UnityEngine;

// ♠ Blade Storm (Normal, SelfBuff) — 플레이어 주변을 도는 칼날이 닿는 적에게 피해.
[CreateAssetMenu(fileName = "BladeStorm", menuName = "Spell/Abilities/BladeStorm")]
public class BladeStormAbility : SpellAbility
{
    public int bladeCount = 3;
    public float orbitRadius = 1.2f;
    public float damagePerHit = 1f;
    public float duration = 5f;
    public float angularSpeed = 240f; // deg/sec

    public override void Activate(SpellContext ctx)
    {
        if (ctx.caster == null) return;
        BladeStorm.Spawn(ctx.caster.transform, bladeCount, orbitRadius, damagePerHit, duration, angularSpeed);
    }
}
