using UnityEngine;

// Design Ref: §8.5 참조 능력(Targeted, ♠ 공격) — 프레임워크 검증용. 실제 콘텐츠는 후속 데이터로 교체.
// 드롭 위치에 이펙트 + 범위 데미지. 공개 Character.Hit API만 사용(적 컨트롤러 수정 불필요).
[CreateAssetMenu(fileName = "TargetedBurst", menuName = "Spell/Abilities/TargetedBurst")]
public class TargetedBurstAbility : SpellAbility
{
    public float radius = 1.5f;
    public float baseDamage = 2f;
    public float effectLifetime = 0.5f;

    [Header("Code VFX (effectPrefab 미지정 시)")]
    public Color vfxColor = new Color(1f, 0.5f, 0.1f, 1f); // 주황(폭발)
    public float vfxWidth = 0.12f;

    public override void Activate(SpellContext ctx)
    {
        Vector2 pos = ctx.targetPosition;
        float damage = baseDamage * GradeMultiplier(ctx.grade); // 위력만 등급 스케일(이 능력의 선택)

        // 이펙트: 프리팹 있으면 그쪽(풀 우선), 없으면 코드 VFX(확장 링)
        if (effectPrefab != null)
        {
            GameObject fx = ctx.effectPool != null ? ctx.effectPool.Get() : null;
            if (fx == null) fx = Object.Instantiate(effectPrefab);
            if (fx != null)
            {
                fx.transform.position = pos;
                fx.SetActive(true);
                if (ctx.effectPool == null) Object.Destroy(fx, effectLifetime); // 풀 오브젝트는 자체 반환 가정
            }
        }
        else
        {
            SpellVfx.SpawnRing(pos, radius, vfxColor, effectLifetime, vfxWidth); // 데미지 반경과 동일 크기
        }

        // 범위 내 "Enemy"만 타격(오사 방지). 적 컨트롤러의 ApplyHit을 태워 사망 애니메이션 보존.
        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, radius);
        for (int i = 0; i < hits.Length; i++)
        {
            if (!hits[i].CompareTag("Enemy")) continue;
            IDamageable dmg = hits[i].GetComponent<IDamageable>();
            if (dmg != null)
            {
                dmg.ApplyHit(damage); // Flash/Die는 적 컨트롤러가 처리
            }
            else
            {
                // 폴백: IDamageable 없는 적은 기존 방식(즉시 비활성)
                Character ch = hits[i].GetComponent<Character>();
                if (ch != null && !ch.Hit(damage)) hits[i].gameObject.SetActive(false);
            }
        }
    }

    static float GradeMultiplier(Grade g)
    {
        switch (g)
        {
            case Grade.Gold:    return 1.5f;
            case Grade.Diamond: return 2f;
            case Grade.Legend:  return 3f;
            default:            return 1f;
        }
    }
}
