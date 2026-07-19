using UnityEngine;

// ♠ Black Hole (Diamond, Targeted) — 드롭 위치에 블랙홀 생성.
// duration 동안 적을 빨아들인 뒤 폭발해 반경 내 적을 소멸시킨다.
[CreateAssetMenu(fileName = "BlackHole", menuName = "Spell/Abilities/BlackHole")]
public class BlackHoleAbility : SpellAbility
{
    [Tooltip("흡입·폭발 반경")]
    public float radius = 4.5f;
    [Tooltip("중심으로 끌어당기는 속도(후반부에 최대 2.2배까지 가속)")]
    public float pullSpeed = 4.5f;
    [Tooltip("붕괴 시 반경 내 적에게 주는 피해. 사실상 소멸시키도록 크게 잡는다")]
    public float explosionDamage = 999f;
    [Tooltip("흡입 지속 시간 — 이 시간이 끝나면 폭발")]
    public float duration = 3f;

    public override void Activate(SpellContext ctx)
    {
        BlackHole.Spawn(ctx.targetPosition, radius, pullSpeed, explosionDamage, duration);
    }
}
