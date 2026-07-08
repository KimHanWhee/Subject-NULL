using UnityEngine;

// ♣ 감속 필드(Targeted) — 드롭 위치 반경 내 적들을 잠시 느리게. 등급이 높을수록 오래 지속.
// SlowStatus 컴포넌트를 적에게 부착하는 방식(적 컨트롤러 수정 불필요).
[CreateAssetMenu(fileName = "SlowField", menuName = "Spell/Abilities/SlowField")]
public class SlowFieldAbility : SpellAbility
{
    public float radius = 2f;
    [Range(0f, 1f)] public float slowFactor = 0.4f; // 감속 배율(0.4 = 원래 속도의 40%)
    public float baseDuration = 2f;

    [Header("Code VFX (effectPrefab 미지정 시)")]
    public Color vfxColor = new Color(0.5f, 1f, 0.5f, 1f); // 연두(클로버)
    public float vfxLifetime = 0.6f;

    public override void Activate(SpellContext ctx)
    {
        Vector2 pos = ctx.targetPosition;
        float duration = baseDuration * GradeMultiplier(ctx.grade); // 지속만 등급 스케일(이 능력의 선택)

        // 이펙트: 프리팹 있으면 그쪽, 없으면 코드 VFX(감속 반경과 동일 크기 링)
        if (effectPrefab != null)
        {
            GameObject fx = Object.Instantiate(effectPrefab, pos, Quaternion.identity);
            Object.Destroy(fx, vfxLifetime);
        }
        else
        {
            SpellVfx.SpawnRing(pos, radius, vfxColor, vfxLifetime);
        }

        // 반경 내 "Enemy"에게 감속 상태 부여(오사 방지: 태그 확인)
        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, radius);
        for (int i = 0; i < hits.Length; i++)
        {
            if (!hits[i].CompareTag("Enemy")) continue;
            SlowStatus.Apply(hits[i].gameObject, slowFactor, duration);
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
