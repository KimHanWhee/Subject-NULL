using UnityEngine;

// ♣ Confusion (Normal, Targeted) — 드롭 위치에서 가장 가까운 적 하나가 5초간 랜덤 방향으로 이동.
[CreateAssetMenu(fileName = "Confusion", menuName = "Spell/Abilities/Confusion")]
public class ConfusionAbility : SpellAbility
{
    public float searchRadius = 3f; // 드롭 위치 기준 대상 탐색 반경
    public float duration = 5f;

    [Header("Code VFX")]
    public Color vfxColor = new Color(1f, 0.85f, 0.3f, 1f); // 혼란 노랑

    public override void Activate(SpellContext ctx)
    {
        Vector2 pos = ctx.targetPosition;
        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, searchRadius);
        GameObject best = null;
        float bestSq = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
        {
            if (!hits[i].CompareTag("Enemy")) continue;
            if (hits[i].GetComponent<EnemyController>() == null && hits[i].GetComponent<RangedEnemyController>() == null) continue;
            float sq = ((Vector2)hits[i].transform.position - pos).sqrMagnitude;
            if (sq < bestSq) { bestSq = sq; best = hits[i].gameObject; }
        }
        if (best == null) return; // 대상 없음 → 불발

        ConfusionStatus.Apply(best, duration);
        SpellVfx.SpawnAura(best.transform, 0.6f, vfxColor, duration); // 대상 머리 위 혼란 오라
        SpellParticleVfx.SpawnOrbit(best.transform, 0.6f, vfxColor, duration); // 대상 주위를 도는 혼란 입자
    }
}
