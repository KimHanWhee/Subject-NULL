using UnityEngine;

// 넉백 + 스턴 상태 — 발동 지점에서 멀어지도록 밀쳐내고, 그동안 적 컨트롤러를 꺼서 스턴.
// ConfusionStatus와 동일하게 EnemyBase.enabled를 꺼 이동/AI를 멈추고, transform으로 직접 밀어낸다.
// 풀링 안전: OnDisable 시 컨트롤러 복구.
public class KnockbackStatus : MonoBehaviour
{
    private EnemyBase enemy;
    private Vector2 dir;
    private float knockTime;     // 밀려나는 시간
    private float knockElapsed;
    private float knockSpeed;    // 초기 속도(선형 감쇠 → ease-out)
    private float stunRemain;    // 총 스턴(밀림 포함) 남은 시간

    public static void Apply(GameObject target, Vector2 fromPoint, float distance, float stunDuration, float knockDuration = 0.22f)
    {
        KnockbackStatus s = target.GetComponent<KnockbackStatus>();
        if (s == null)
        {
            s = target.AddComponent<KnockbackStatus>();
            if (!s.Bind()) { Destroy(s); return; } // EnemyBase가 없는 대상이면 무시
        }
        Vector2 d = (Vector2)target.transform.position - fromPoint;
        s.dir = d.sqrMagnitude > 0.0001f ? d.normalized : Random.insideUnitCircle.normalized;
        s.knockTime = Mathf.Max(0.05f, knockDuration);
        s.knockElapsed = 0f;
        s.knockSpeed = (distance / s.knockTime) * 2f; // 선형 감쇠 적분 = distance
        s.stunRemain = Mathf.Max(s.stunRemain, stunDuration);
    }

    bool Bind()
    {
        enemy = GetComponent<EnemyBase>();
        if (enemy == null) return false;
        enemy.enabled = false; // 스턴(컨트롤러 정지)
        return true;
    }

    void Update()
    {
        if (knockElapsed < knockTime)
        {
            float u = 1f - (knockElapsed / knockTime); // 1→0 (ease-out)
            transform.Translate(dir * (knockSpeed * u * Time.deltaTime));
            knockElapsed += Time.deltaTime;
        }
        stunRemain -= Time.deltaTime;
        if (stunRemain <= 0f) { Restore(); Destroy(this); }
    }

    void Restore()
    {
        if (enemy != null) enemy.enabled = true;
        enemy = null;
    }

    void OnDisable() { Restore(); }
}
