using UnityEngine;

// Design Ref: §2.2 — 원거리 적(박쥐). 사거리 유지(히스테리시스) + 주기 사격.
// 산탄 변형(보라 박쥐)은 bulletsPerShot/spreadAngle만 다른 프리팹으로 공유.
public class RangedEnemyController : EnemyBase
{
    [Header("Range Band")] // Plan SC: FR-02 — 히스테리시스로 경계 떨림 방지
    public float farBand = 7f;   // 이보다 멀면 접근
    public float nearBand = 4f;  // 이보다 가까우면 후퇴
    // nearBand ~ farBand 사이 = 정지(사격 밴드)

    [Header("Fire")] // Plan SC: FR-01/FR-03/FR-07
    public GameObject bulletPrefab;          // 적 총알 prefab ("EnemyBullet" 태그)
    public BulletPoolManager bulletPoolManager;
    public float fireInterval = 1.5f;        // 발사 간격(초)
    public float bulletSpeed = 8f;
    public float bulletDamage = 1f;
    public AudioClip fireSound;

    [Header("Spread")] // 산탄 변형(보라 박쥐) — 1이면 기존 단발과 동일
    public int bulletsPerShot = 1;   // 동시 발사 수
    public float spreadAngle = 30f;  // 부채꼴 전체 각도(bulletsPerShot > 1일 때만 사용)
    public Color bulletColor = Color.red; // 총알 틴트 — 캐릭터 색에 맞춤(풀 공유라 발사마다 지정)

    private float nextFireTime;

    protected override void Awake()
    {
        base.Awake();
        // 프리팹은 씬 오브젝트(BulletPoolManager)를 참조로 담을 수 없어 런타임에 탐색
        if (bulletPoolManager == null)
            bulletPoolManager = FindObjectOfType<BulletPoolManager>();
    }

    public override void Spawn(GameObject target)
    {
        base.Spawn(target);
        nextFireTime = Time.time + fireInterval; // Design Ref: §6 E5 — 스폰 직후 즉발 방지
    }

    protected override void Tick(Vector2 toTarget, float dt)
    {
        float dist = toTarget.magnitude;
        Vector2 dir = toTarget.normalized;

        // Plan SC: FR-02 — 사거리 유지 (밴드 밖이면 접근/후퇴, 안이면 정지)
        if (dist > farBand)
            transform.Translate(dir * (speed * dt));
        else if (dist < nearBand)
            transform.Translate(-dir * (speed * dt));
        // else: 정지

        sr.flipX = dir.x < 0;

        // Plan SC: FR-01 — 사격 밴드 진입 + 쿨다운
        if (dist <= farBand && Time.time >= nextFireTime)
        {
            Fire(dir);
            nextFireTime = Time.time + fireInterval;
        }
    }

    // Plan SC: FR-03 — 발사 순간 플레이어 방향으로 직진(유도 없음). 산탄이면 부채꼴 분산.
    void Fire(Vector2 dir)
    {
        if (bulletPrefab == null || bulletPoolManager == null) return;

        ObjectPool pool = bulletPoolManager.GetPool(bulletPrefab);
        int n = Mathf.Max(1, bulletsPerShot);
        for (int i = 0; i < n; i++)
        {
            GameObject b = pool.Get();
            if (b == null) break; // Design Ref: §6 E1 — 풀 고갈 시 남은 발사 스킵

            // n발을 spreadAngle 부채꼴에 균등 배치(단발이면 정방향 그대로)
            float offset = n > 1 ? spreadAngle * ((float)i / (n - 1) - 0.5f) : 0f;
            Vector2 shotDir = Quaternion.Euler(0f, 0f, offset) * dir;

            b.transform.position = transform.position;
            EnemyBullet eb = b.GetComponent<EnemyBullet>();
            eb.Direction = shotDir;
            eb.speed = bulletSpeed * HardshipSystem.EnemyBulletSpeedMult; // 고난 "탄속 개선"
            eb.damage = bulletDamage * HardshipSystem.EnemyDamageMult;    // 고난 "공격 본능"

            // 총알 풀은 박쥐 종류 간 공유 → 매 발사 시 자기 색으로 칠함(잔존 색 방지)
            SpriteRenderer bsr = b.GetComponent<SpriteRenderer>();
            if (bsr != null) bsr.color = bulletColor;
        }

        if (fireSound != null)
        {
            AudioSource a = GetComponent<AudioSource>();
            if (a != null) a.PlayOneShot(fireSound);
        }
    }
}
