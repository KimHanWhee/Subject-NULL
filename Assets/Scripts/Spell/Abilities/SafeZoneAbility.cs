using UnityEngine;

// ♦ 안전지대 (Diamond, Targeted) — 드롭한 위치에 보호 구역 생성.
// 구역 안 플레이어는 받는 피해 무효, 적은 경계 밖으로 밀려나 진입 불가.
[CreateAssetMenu(fileName = "SafeZone", menuName = "Spell/Abilities/SafeZone")]
public class SafeZoneAbility : SpellAbility
{
    public float radius = 3f;
    public float duration = 6f;

    public override void Activate(SpellContext ctx)
    {
        SafeZoneField.Spawn(ctx.targetPosition, radius, duration);
    }
}
