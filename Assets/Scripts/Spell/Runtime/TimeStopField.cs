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
        if (Time.unscaledTime < nextScan) return;
        nextScan = Time.unscaledTime + scanInterval;

        float remain = endTime - Time.unscaledTime;

        // 적: 남은 시간만큼 빙결(주기 갱신이라 신규 스폰도 다음 스캔에 정지)
        foreach (EnemyController ec in Object.FindObjectsOfType<EnemyController>())
            FreezeStatus.Apply(ec.gameObject, remain + scanInterval);
        foreach (RangedEnemyController rc in Object.FindObjectsOfType<RangedEnemyController>())
            FreezeStatus.Apply(rc.gameObject, remain + scanInterval);

        // 적 총알: 스크립트 정지(이동+수명 동결)
        foreach (EnemyBullet b in Object.FindObjectsOfType<EnemyBullet>())
        {
            if (!b.enabled) continue;
            b.enabled = false;
            stoppedBullets.Add(b);
        }
    }

    void OnDestroy()
    {
        // 총알 복구(비활성화(풀 반환)된 것은 건드리지 않음 — enabled는 남지만 재활성 시 OnEnable로 정상)
        foreach (EnemyBullet b in stoppedBullets)
            if (b != null) b.enabled = true;
        stoppedBullets.Clear();
    }
}
