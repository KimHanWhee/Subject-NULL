using UnityEngine;

// ♣ 감속 상태이상 — 적 speed를 일시 감속 후 원복. SlowFieldAbility가 부착.
// 풀링 안전: 비활성화(사망/반환) 시 speed 원복 + 컴포넌트 제거 → 재스폰 적이 느려진 채 나오지 않음.
// 중첩 시 새로 걸지 않고 지속만 갱신(가장 긴 쪽 유지). 이동 감속은 게임 시간(deltaTime) 기준.
public class SlowStatus : MonoBehaviour
{
    private EnemyController basic;
    private RangedEnemyController ranged;
    private float originalSpeed;
    private float remain;

    public static void Apply(GameObject enemy, float slowFactor, float duration)
    {
        SlowStatus s = enemy.GetComponent<SlowStatus>();
        if (s == null)
        {
            s = enemy.AddComponent<SlowStatus>();
            if (!s.Bind(slowFactor)) { Destroy(s); return; } // speed 필드가 없는 대상이면 무시
        }
        s.remain = Mathf.Max(s.remain, duration); // 재적용은 지속 갱신만
    }

    bool Bind(float slowFactor)
    {
        basic = GetComponent<EnemyController>();
        ranged = GetComponent<RangedEnemyController>();
        if (basic != null)
        {
            originalSpeed = basic.speed;
            basic.speed = originalSpeed * Mathf.Clamp01(slowFactor);
            return true;
        }
        if (ranged != null)
        {
            originalSpeed = ranged.speed;
            ranged.speed = originalSpeed * Mathf.Clamp01(slowFactor);
            return true;
        }
        return false;
    }

    void Update()
    {
        remain -= Time.deltaTime; // 게임 시간 기준(선택 슬로우 중엔 함께 느려짐 — 의도)
        if (remain <= 0f) Expire();
    }

    void Expire()
    {
        Restore();
        Destroy(this);
    }

    void Restore()
    {
        if (basic != null) basic.speed = originalSpeed;
        if (ranged != null) ranged.speed = originalSpeed;
    }

    // 풀 반환/사망 안전망: 감속 잔존 방지
    void OnDisable()
    {
        Restore();
        Destroy(this);
    }
}
