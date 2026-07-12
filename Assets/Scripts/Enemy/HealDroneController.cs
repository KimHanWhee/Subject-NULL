using UnityEngine;

// 힐 드론(서포트 적) — 플레이어에게서 도망 다니며 일정 주기로 주변 아군 적을 회복.
// 공통 규약(스폰/피격/사망)은 EnemyBase 소유. 우선 처치를 유도하는 교란형 유닛.
public class HealDroneController : EnemyBase
{
    [Header("Support Move")]
    public float fleeRange = 4.5f;   // 플레이어가 이 안이면 반대 방향으로 도망
    public float followRange = 2.5f; // 가장 가까운 아군과 유지하려는 거리(이보다 멀면 접근)

    [Header("Heal")]
    public float healInterval = 3f;  // 힐 주기(N초)
    public float healRadius = 3.5f;  // 힐 범위
    public float healAmount = 1f;    // 회복량(부상당한 아군만, maxHp 초과 불가)
    public float chargeTime = 0.8f;  // 방출 전 에너지 수집 연출 시간(healInterval에 포함)
    public Color healColor = new Color(0.45f, 1f, 0.6f, 1f); // 힐 파동 색
    public AudioClip healSound;      // 충전 시작 시 재생(수집→방출 타이밍과 동기화)
    [Range(0f, 1f)] public float healSoundVolume = 0.7f;

    private float healTimer;
    private bool charging; // 수집 연출 중(방출 대기)

    public override void Spawn(GameObject target)
    {
        base.Spawn(target);
        healTimer = 0f;   // 스폰 직후 즉시 힐 방지
        charging = false; // 풀 재사용 시 상태 초기화
    }

    protected override void Tick(Vector2 toTarget, float dt)
    {
        // ---- 이동: 플레이어 회피 우선, 아니면 아군 곁으로 ----
        float playerDist = toTarget.magnitude;
        if (playerDist < fleeRange)
        {
            transform.Translate(-toTarget.normalized * (speed * dt)); // 도망
        }
        else
        {
            EnemyBase ally = NearestAlly();
            if (ally != null)
            {
                Vector2 toAlly = ally.transform.position - transform.position;
                if (toAlly.magnitude > followRange)
                    transform.Translate(toAlly.normalized * (speed * dt)); // 아군 곁으로
            }
        }
        sr.flipX = toTarget.x < 0;

        // ---- 힐 펄스: 방출 chargeTime 전부터 초록 에너지를 드론으로 수집 → 주기 도달 시 방출 ----
        healTimer += dt;
        if (!charging && healTimer >= healInterval - chargeTime)
        {
            // 회복 대상이 있을 때만 충전 시작(빈 연출 방지). 없으면 다음 프레임 재확인.
            if (AnyWoundedInRange())
            {
                charging = true;
                anim.SetTrigger("Heal"); // 눈 발광(HealDroneHeal 프레임) — 충전과 동기화
                // 초록 입자가 사방에서 드론으로 모여듦(드론 이동 추종)
                SpellParticleVfx.SpawnImplode(transform.position, healRadius, healColor, chargeTime, 30, 0.55f, transform);
                Sfx.Play2D(healSound, healSoundVolume); // 2D — 드론이 화면 어디 있든 또렷하게
            }
        }
        if (healTimer >= healInterval)
        {
            healTimer = 0f;
            if (charging) { charging = false; ReleasePulse(); }
        }
    }

    bool AnyWoundedInRange()
    {
        foreach (EnemyBase e in Object.FindObjectsOfType<EnemyBase>())
        {
            if (e == this || !e.gameObject.activeInHierarchy) continue;
            if (Vector2.Distance(e.transform.position, transform.position) > healRadius) continue;
            Character ch = e.GetComponent<Character>();
            if (ch != null && ch.HpRatio < 1f && ch.HpRatio > 0f) return true;
        }
        return false;
    }

    // 수집한 에너지 방출 — 파동 링 + 범위 내 부상 아군 회복
    void ReleasePulse()
    {
        SpellVfx.SpawnRing(transform.position, healRadius, healColor, 0.5f);
        SpellParticleVfx.SpawnBurst(transform.position, 1.2f, healColor, 18, 0.4f); // 방출 순간 터짐

        foreach (EnemyBase e in Object.FindObjectsOfType<EnemyBase>())
        {
            if (e == this || !e.gameObject.activeInHierarchy) continue;
            if (Vector2.Distance(e.transform.position, transform.position) > healRadius) continue;
            Character ch = e.GetComponent<Character>();
            if (ch == null || ch.HpRatio >= 1f || ch.HpRatio <= 0f) continue; // 무손상/사망 제외
            ch.Heal(healAmount);
            SpellParticleVfx.SpawnBurst(e.transform.position, 0.5f, healColor, 8, 0.35f); // 회복 피드백
        }
    }

    EnemyBase NearestAlly()
    {
        EnemyBase best = null;
        float bestSq = float.MaxValue;
        foreach (EnemyBase e in Object.FindObjectsOfType<EnemyBase>())
        {
            if (e == this || !e.gameObject.activeInHierarchy) continue;
            if (e is HealDroneController) continue; // 드론끼리 뭉치는 것 방지
            float sq = (e.transform.position - transform.position).sqrMagnitude;
            if (sq < bestSq) { bestSq = sq; best = e; }
        }
        return best;
    }
}
