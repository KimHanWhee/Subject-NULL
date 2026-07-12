using UnityEngine;

// 레이저 적(초록 박쥐) — 사거리 유지하다 주기적으로 조준선을 그리며 조준,
// 발사 직전 방향 고정(회피 틈) 후 즉발 빔으로 히트스캔 데미지.
// 공통 규약(스폰/피격/사망)은 EnemyBase 소유.
public class LaserEnemyController : EnemyBase
{
    enum State { Moving, Aiming, Firing }

    [Header("Range Band")] // 원거리 적과 동일한 히스테리시스
    public float farBand = 8f;   // 이보다 멀면 접근
    public float nearBand = 5f;  // 이보다 가까우면 후퇴

    [Header("Laser")]
    public float fireInterval = 3f;   // 조준 시작 주기
    public float aimTime = 1.1f;      // 조준선 표시 총 시간
    public float lockTime = 0.35f;    // 발사 전 방향 고정 시간(aimTime에 포함) — 대시로 회피 가능
    public float beamTime = 0.18f;    // 빔 표시 시간
    public float laserRange = 12f;    // 빔 길이
    public float beamWidth = 0.18f;   // 빔 두께(판정 폭도 겸용)
    public float damage = 1f;
    public Color aimColor = new Color(0.2f, 1f, 0.45f, 0.4f);  // 조준선(반투명 초록)
    public Color beamColor = new Color(0.55f, 1f, 0.6f, 1f);   // 빔(밝은 초록)
    public AudioClip fireSound;

    private State state;
    private float stateTimer;    // 현재 상태 경과(고정 스텝 누적 → 빙결 시 자동 정지)
    private float cooldownTimer; // 다음 조준까지 남은 시간
    private Vector2 laserDir;    // 고정 시점 이후의 빔 방향
    private bool damageDealt;    // 빔 1회당 데미지 1회
    private LineRenderer line;

    protected override void Awake()
    {
        base.Awake();

        // 조준선/빔 겸용 라인 — 자식 오브젝트에 런타임 생성(프리팹 편집 불필요)
        GameObject go = new GameObject("LaserLine");
        go.transform.SetParent(transform, false);
        line = go.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.material = new Material(Shader.Find("Sprites/Default"));
        // 월드 스프라이트가 Default보다 위 레이어라 최상단 레이어로 강제(VFX 규약)
        SortingLayer[] layers = SortingLayer.layers;
        if (layers != null && layers.Length > 0) line.sortingLayerID = layers[layers.Length - 1].id;
        line.sortingOrder = 29000; // 테마 오버레이(30000)보다 아래, 월드보다 위
        line.enabled = false;
    }

    public override void Spawn(GameObject target)
    {
        base.Spawn(target);
        line.enabled = false;
        state = State.Moving;
        stateTimer = 0f;
        cooldownTimer = fireInterval; // 스폰 직후 즉발 방지
    }

    protected override void Tick(Vector2 toTarget, float dt)
    {
        stateTimer += dt;
        if (cooldownTimer > 0f) cooldownTimer -= dt;

        float dist = toTarget.magnitude;

        switch (state)
        {
            case State.Moving:
                // 사거리 유지(원거리 적과 동일 밴드 로직)
                if (dist > farBand)
                    transform.Translate(toTarget.normalized * (speed * dt));
                else if (dist < nearBand)
                    transform.Translate(-toTarget.normalized * (speed * dt));
                sr.flipX = toTarget.x < 0;

                if (dist <= farBand && cooldownTimer <= 0f)
                {
                    Enter(State.Aiming);
                    laserDir = toTarget.normalized;
                    line.enabled = true;
                }
                break;

            case State.Aiming:
                // 고정 전엔 플레이어 추적, 마지막 lockTime 동안 방향 고정(회피 틈)
                if (stateTimer < aimTime - lockTime)
                    laserDir = toTarget.normalized;
                sr.flipX = laserDir.x < 0;

                // 고정 구간엔 조준선이 빠르게 점멸(발사 임박 예고)
                bool locked = stateTimer >= aimTime - lockTime;
                Color c = aimColor;
                if (locked) c.a = Mathf.PingPong(stateTimer * 10f, 1f) * 0.6f + 0.2f;
                DrawLine(0.045f, c);

                if (stateTimer >= aimTime)
                {
                    Enter(State.Firing);
                    damageDealt = false;
                    if (fireSound != null)
                    {
                        AudioSource a = GetComponent<AudioSource>();
                        if (a != null) a.PlayOneShot(fireSound);
                    }
                }
                break;

            case State.Firing:
                DrawLine(beamWidth, beamColor);
                if (!damageDealt) TryHitPlayer();
                if (stateTimer >= beamTime)
                {
                    line.enabled = false;
                    cooldownTimer = fireInterval;
                    Enter(State.Moving);
                }
                break;
        }
    }

    void Enter(State next)
    {
        state = next;
        stateTimer = 0f;
    }

    void DrawLine(float width, Color color)
    {
        line.enabled = true; // 빙결 해제 등으로 꺼졌던 경우 복구
        line.startWidth = width;
        line.endWidth = width;
        line.startColor = color;
        line.endColor = color;
        line.SetPosition(0, transform.position);
        line.SetPosition(1, (Vector2)transform.position + laserDir * laserRange);
    }

    // 빔 선분과 플레이어 거리로 히트스캔(빔 두께 절반 + 플레이어 반경 여유)
    void TryHitPlayer()
    {
        if (target == null) return;
        PlayerController pc = target.GetComponent<PlayerController>();
        if (pc == null) return; // Decoy 등 플레이어가 아니면 데미지 없음

        Vector2 origin = transform.position;
        Vector2 p = target.transform.position;
        float t = Mathf.Clamp(Vector2.Dot(p - origin, laserDir), 0f, laserRange);
        float distToBeam = Vector2.Distance(p, origin + laserDir * t);
        if (distToBeam <= beamWidth * 0.5f + 0.35f)
        {
            damageDealt = true;
            pc.ApplyRangedHit(damage, gameObject); // 총알처럼 대시 무적 적용
        }
    }

    protected override void Die()
    {
        line.enabled = false;
        base.Die();
    }

    // 빙결(FreezeStatus)이 컨트롤러를 끄면 조준선/빔도 함께 숨김
    void OnDisable()
    {
        if (line != null) line.enabled = false;
    }
}
