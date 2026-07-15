using System.Collections.Generic;
using UnityEngine;

// ♣ Time Stop 런타임 — 지속시간 동안 모든 적(신규 스폰 포함) + 적 총알을 완전 정지.
// 적: 주기 스캔으로 FreezeStatus 갱신(스캔 주기보다 긴 freeze로 틈 없음).
// 총알: EnemyBullet 컴포넌트 disable → Update(이동/수명) 정지, 종료 시 일괄 복구.
public class TimeStopField : MonoBehaviour
{
    private float endTime;
    private float nextScan;
    private const float scanInterval = 0.2f;
    private readonly List<EnemyBullet> stoppedBullets = new List<EnemyBullet>();

    // 플레이어 잔상(시간 정지 중 이동하면 청백 잔상)
    private SpriteRenderer playerSr;
    private Vector3 lastGhostPos;
    private float nextGhost;

    public static TimeStopField Spawn(float duration)
    {
        GameObject go = new GameObject("TimeStopField");
        TimeStopField f = go.AddComponent<TimeStopField>();
        f.endTime = Time.unscaledTime + duration; // 시전자 체감 기준(unscaled) — 선택 슬로우와 무관
        f.nextScan = 0f;
        return f;
    }

    // TimeStopWave가 파동 중 멈춘 총알들을 인계 — 필드 종료 시 함께 복구
    public void AdoptStoppedBullets(List<EnemyBullet> bullets)
    {
        if (bullets != null) stoppedBullets.AddRange(bullets);
    }

    void Update()
    {
        if (Time.unscaledTime >= endTime) { Destroy(gameObject); return; }

        // 플레이어 잔상 — 정지된 세상 속에서 움직이는 느낌(매 프레임 판정, 스캔과 무관)
        SpawnPlayerGhost();

        if (Time.unscaledTime < nextScan) return;
        nextScan = Time.unscaledTime + scanInterval;

        float remain = endTime - Time.unscaledTime;

        // 적: 남은 시간만큼 빙결(주기 갱신이라 신규 스폰도 다음 스캔에 정지)
        // 모든 적 타입 공통 — 새 몬스터는 EnemyBase 상속만으로 자동 호환
        foreach (EnemyBase e in Object.FindObjectsOfType<EnemyBase>())
            FreezeStatus.Apply(e.gameObject, remain + scanInterval);

        // 적 총알: 스크립트 정지(이동+수명 동결)
        foreach (EnemyBullet b in Object.FindObjectsOfType<EnemyBullet>())
        {
            if (!b.enabled) continue;
            b.enabled = false;
            stoppedBullets.Add(b);
        }
    }

    void SpawnPlayerGhost()
    {
        if (playerSr == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerSr = p.GetComponent<SpriteRenderer>();
            if (playerSr == null) return;
            lastGhostPos = playerSr.transform.position;
        }
        if (Time.unscaledTime < nextGhost) return;
        float moved = ((Vector2)playerSr.transform.position - (Vector2)lastGhostPos).magnitude;
        if (moved < 0.12f) return;
        DashGhost.Spawn(playerSr, new Color(0.55f, 0.85f, 1f, 0.5f), 0.45f); // 시간정지 청백 잔상
        lastGhostPos = playerSr.transform.position;
        nextGhost = Time.unscaledTime + 0.045f;
    }

    void OnDestroy()
    {
        // 총알 복구(비활성화(풀 반환)된 것은 건드리지 않음 — enabled는 남지만 재활성 시 OnEnable로 정상)
        foreach (EnemyBullet b in stoppedBullets)
            if (b != null) b.enabled = true;
        stoppedBullets.Clear();
    }
}
