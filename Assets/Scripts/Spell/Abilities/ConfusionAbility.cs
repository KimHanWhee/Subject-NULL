using UnityEngine;

// ♣ Confusion (Normal, Targeted) — 드롭 위치 반경 내 모든 적이 지속시간 동안 랜덤 방향으로 이동.
[CreateAssetMenu(fileName = "Confusion", menuName = "Spell/Abilities/Confusion")]
public class ConfusionAbility : SpellAbility
{
    public float searchRadius = 3f; // 드롭 위치 기준 적용 반경
    public float duration = 5f;

    [Header("Code VFX")]
    public Color vfxColor = new Color(1f, 0.85f, 0.3f, 1f); // 혼란 노랑

    public override void Activate(SpellContext ctx)
    {
        Vector2 pos = ctx.targetPosition;
        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, searchRadius);
        var applied = new System.Collections.Generic.HashSet<GameObject>(); // 콜라이더 중복 방지
        int count = 0;
        for (int i = 0; i < hits.Length; i++)
        {
            if (!hits[i].CompareTag("Enemy")) continue;
            if (hits[i].GetComponent<EnemyBase>() == null) continue; // 모든 적 타입 공통
            GameObject e = hits[i].gameObject;
            if (!applied.Add(e)) continue; // 이미 처리한 적이면 스킵

            ConfusionStatus.Apply(e, duration);
            SpellVfx.SpawnAura(e.transform, 0.6f, vfxColor, duration);       // 대상 머리 위 혼란 오라
            SpellParticleVfx.SpawnOrbit(e.transform, 0.6f, vfxColor, duration); // 대상 주위 혼란 입자
            count++;
        }
        if (count > 0) SpellVfx.SpawnRing(pos, searchRadius, vfxColor, 0.4f); // 적용 범위 표시
    }
}
