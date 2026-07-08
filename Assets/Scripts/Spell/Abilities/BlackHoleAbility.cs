using UnityEngine;

// ♠ Black Hole (Diamond, Targeted) — 드롭 위치에 블랙홀 생성, 적을 끌어당기며 지속 피해.
[CreateAssetMenu(fileName = "BlackHole", menuName = "Spell/Abilities/BlackHole")]
public class BlackHoleAbility : SpellAbility
{
    public float radius = 2.5f;
    public float pullSpeed = 3f;
    public float dps = 1.5f;
    public float tickInterval = 0.5f;
    public float duration = 3f;

    public override void Activate(SpellContext ctx)
    {
        BlackHole.Spawn(ctx.targetPosition, radius, pullSpeed, dps, tickInterval, duration);
    }
}
