using UnityEngine;

// 돌진형 적(개구리) — 천천히 접근하다 사거리에 들면 조준(텔레그래프) 후 고속 돌진.
// 공통 규약(스폰/피격/사망)은 EnemyBase 소유.
public class ChargerEnemyController : EnemyBase
{
    enum State { Chasing, Windup, Charging, Recover }

    [Header("Charge")]
    public float chargeRange = 5f;       // 이 거리 안이면 돌진 시작
    public float windupTime = 0.6f;      // 조준(멈춤+점멸) 시간 — 플레이어가 피할 틈
    public float chargeSpeed = 9f;       // 돌진 속도
    public float chargeTime = 0.45f;     // 돌진 지속(≈4유닛 이동)
    public float recoverTime = 0.5f;     // 돌진 후 경직
    public float chargeCooldown = 1.5f;  // 경직 종료 후 재돌진까지 대기
    public Color windupTint = new Color(1f, 0.35f, 0.25f); // 조준 점멸 색

    private State state;
    private float stateTimer;     // 현재 상태 경과(고정 스텝 누적 → 빙결 시 자동 정지)
    private float cooldownTimer;  // 다음 돌진 가능까지 남은 시간
    private Vector2 chargeDir;    // 조준 종료 시점에 고정되는 돌진 방향

    public override void Spawn(GameObject target)
    {
        base.Spawn(target);
        if (sr != null) sr.color = Color.white; // 풀 재사용 시 조준 점멸 색 잔존 방지
        state = State.Chasing;
        stateTimer = 0f;
        cooldownTimer = 0.5f; // 스폰 직후 즉시 돌진 방지(짧은 유예)
        // 돌진은 velocity 이동(아래 Charging) — 고속에서도 벽을 못 뚫게 연속 충돌 검사
        if (rb != null) rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    protected override void Tick(Vector2 toTarget, float dt)
    {
        stateTimer += dt;
        if (cooldownTimer > 0f) cooldownTimer -= dt;

        switch (state)
        {
            case State.Chasing:
                transform.Translate(toTarget.normalized * (speed * dt));
                sr.flipX = toTarget.x < 0;
                if (toTarget.magnitude <= chargeRange && cooldownTimer <= 0f)
                    Enter(State.Windup);
                break;

            case State.Windup:
                // 정지한 채 조준 — 점멸로 위험 예고, 방향은 계속 갱신(발사 순간 고정)
                sr.flipX = toTarget.x < 0;
                sr.color = Color.Lerp(Color.white, windupTint, Mathf.PingPong(stateTimer * 6f, 1f));
                if (stateTimer >= windupTime)
                {
                    chargeDir = toTarget.normalized; // 이후 유도 없음 — 대시로 회피 가능
                    sr.color = Color.white;
                    Enter(State.Charging);
                }
                break;

            case State.Charging:
                // ⚠️ Translate 금지 — 물리 우회라 고속에서 벽 관통(스텝당 침투 > 엔진 보정).
                // EnemyBase.FixedUpdate가 매 스텝 velocity를 0으로 만든 뒤 Tick을 부르므로
                // 여기서 다시 세팅하면 "이번 스텝만 유효한" 돌진 속도가 되고, 벽은 엔진이 막는다.
                if (rb != null) rb.linearVelocity = chargeDir * (chargeSpeed * localTimeScale);
                else transform.Translate(chargeDir * (chargeSpeed * dt));
                if (stateTimer >= chargeTime) Enter(State.Recover);
                break;

            case State.Recover:
                if (stateTimer >= recoverTime)
                {
                    cooldownTimer = chargeCooldown;
                    Enter(State.Chasing);
                }
                break;
        }
    }

    void Enter(State next)
    {
        state = next;
        stateTimer = 0f;
    }

    protected override void Die()
    {
        sr.color = Color.white; // 조준 점멸 중 사망 시 색 원복
        base.Die();
    }

    // 빙결(FreezeStatus)이 컨트롤러를 끌 때 정리 — 색 원복 + 돌진 관성 제거
    // (컨트롤러가 꺼지면 EnemyBase의 velocity 초기화도 멈추므로 여기서 지워야 빙결 중 미끄러지지 않음)
    void OnDisable()
    {
        if (sr != null) sr.color = Color.white;
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }
}
