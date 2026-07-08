using UnityEngine;

// ♥ Lifesteal 상태 — 지속시간 동안 총알이 적에게 명중할 때마다 체력 회복(기본 1).
// PlayerBulletEvents.EnemyHit 구독(구독 해제는 OnDisable/OnDestroy 책임).
public class LifestealStatus : MonoBehaviour
{
    private Character ch;
    private float healPerHit;
    private float remain;
    private bool subscribed;

    public static void Apply(GameObject player, float healPerHit, float duration)
    {
        LifestealStatus s = player.GetComponent<LifestealStatus>();
        if (s == null)
        {
            s = player.AddComponent<LifestealStatus>();
            s.ch = player.GetComponent<Character>();
        }
        s.healPerHit = healPerHit;
        s.remain = Mathf.Max(s.remain, duration);
        s.Subscribe();
    }

    void Subscribe()
    {
        if (subscribed) return;
        PlayerBulletEvents.EnemyHit += OnEnemyHit;
        subscribed = true;
    }

    void Unsubscribe()
    {
        if (!subscribed) return;
        PlayerBulletEvents.EnemyHit -= OnEnemyHit;
        subscribed = false;
    }

    void OnEnemyHit(GameObject enemy, float damage)
    {
        if (ch != null) ch.Heal(healPerHit);
    }

    void Update()
    {
        remain -= Time.deltaTime;
        if (remain <= 0f) Destroy(this);
    }

    void OnDisable() { Unsubscribe(); }
    void OnDestroy() { Unsubscribe(); }
}
