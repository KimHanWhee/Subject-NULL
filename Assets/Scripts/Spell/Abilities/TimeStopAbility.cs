using UnityEngine;

// ♣ Time Stop (Legend, SelfBuff 드롭) — 3초간 모든 적과 적 총알을 완전 정지.
[CreateAssetMenu(fileName = "TimeStop", menuName = "Spell/Abilities/TimeStop")]
public class TimeStopAbility : SpellAbility
{
    public float duration = 3f;

    [Header("Code VFX")]
    public Color vfxColor = new Color(0.8f, 0.95f, 1f, 1f); // 시간 정지 청백

    public override void Activate(SpellContext ctx)
    {
        Vector2 center = ctx.caster != null ? (Vector2)ctx.caster.transform.position : ctx.targetPosition;
        SpellVfx.SpawnRing(center, 10f, vfxColor, 0.8f); // 세계로 퍼지는 정지 파동
        TimeStopField.Spawn(duration);
    }
}
