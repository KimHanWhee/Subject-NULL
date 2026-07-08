using UnityEngine;

// ♣ Decoy (Normal, SelfBuff 드롭) — 플레이어 위치에 분신 생성, 적들이 분신을 타겟팅.
[CreateAssetMenu(fileName = "Decoy", menuName = "Spell/Abilities/Decoy")]
public class DecoyAbility : SpellAbility
{
    public float duration = 5f;

    public override void Activate(SpellContext ctx)
    {
        if (ctx.caster == null) return;
        DecoyController.Spawn(ctx.caster, duration);
    }
}
