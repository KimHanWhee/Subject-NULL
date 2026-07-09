using UnityEngine;

// ♣ Decoy 런타임 — 플레이어 위치에 분신 생성, 모든 활성 적의 타겟을 분신으로 변경.
// 종료 시 타겟을 플레이어로 복구 + 분신 제거. 지속 중 새로 스폰된 적도 주기 스캔으로 유혹.
public class DecoyController : MonoBehaviour
{
    private GameObject player;
    private float endTime;
    private float nextScan;
    private const float scanInterval = 0.5f;

    public static DecoyController Spawn(GameObject player, float duration)
    {
        GameObject go = new GameObject("Decoy");
        go.transform.position = player.transform.position;

        // 외형: 플레이어 스프라이트 복사(반투명 청록 틴트로 분신 느낌)
        SpriteRenderer src = player.GetComponent<SpriteRenderer>();
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        if (src != null)
        {
            sr.sprite = src.sprite;
            sr.flipX = src.flipX;
            sr.sortingLayerID = src.sortingLayerID;
            sr.sortingOrder = src.sortingOrder;
        }
        sr.color = new Color(0.5f, 1f, 0.95f, 0.75f);

        DecoyController d = go.AddComponent<DecoyController>();
        d.player = player;
        d.endTime = Time.time + duration;
        d.nextScan = 0f;
        SpellVfx.SpawnRing(go.transform.position, 0.9f, new Color(0.5f, 1f, 0.95f, 1f), 0.5f);
        SpellParticleVfx.SpawnBurst(go.transform.position, 0.9f, new Color(0.5f, 1f, 0.95f, 1f), 20, 0.35f); // 분신 생성 파열
        return d;
    }

    void Update()
    {
        if (Time.time >= endTime) { Destroy(gameObject); return; }
        if (Time.time < nextScan) return;
        nextScan = Time.time + scanInterval;
        RetargetAll(gameObject); // 지속 중 스폰된 적 포함 분신으로 유혹
    }

    void RetargetAll(GameObject target)
    {
        foreach (EnemyController ec in Object.FindObjectsOfType<EnemyController>())
            ec.SetTarget(target);
        foreach (RangedEnemyController rc in Object.FindObjectsOfType<RangedEnemyController>())
            rc.SetTarget(target);
    }

    void OnDestroy()
    {
        if (player != null) RetargetAll(player); // 어그로 복구
    }
}
