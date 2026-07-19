using UnityEngine;

// ♦ 전자기장 (Diamond, SelfBuff) — 지속시간 동안 주변 반경의 적 원거리 공격을 무력화.
// 총알은 스파크와 함께 소멸, 레이저는 장막 경계에서 차단된다.
[CreateAssetMenu(fileName = "EMField", menuName = "Spell/Abilities/EMField")]
public class EMFieldAbility : SpellAbility
{
    public float radius = 3f;
    public float duration = 6f;

    public override void Activate(SpellContext ctx)
    {
        if (ctx.caster == null) return;
        EMFieldStatus.Apply(ctx.caster, radius, duration, ctx.marble);
    }
}
