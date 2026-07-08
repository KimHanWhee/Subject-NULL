using UnityEngine;

// ♠ Flame Field (Normal, Targeted) — 드롭 위치에 불꽃 장판 생성, 범위 내 적 지속 피해.
[CreateAssetMenu(fileName = "FlameField", menuName = "Spell/Abilities/FlameField")]
public class FlameFieldAbility : SpellAbility
{
    public float radius = 1.5f;
    public float dps = 1f;             // 초당 피해
    public float tickInterval = 0.5f;  // 피해 주기(초)
    public float duration = 4f;        // 장판 지속(초)

    public override void Activate(SpellContext ctx)
    {
        FlameField.Spawn(ctx.targetPosition, radius, dps, tickInterval, duration);
    }
}
