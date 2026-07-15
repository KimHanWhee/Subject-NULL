using UnityEngine;

// Design Ref: §2.1 — 적 진영 총알. 플레이어만 타격, 수명/벽/플레이어 충돌 시 풀 반환.
public class EnemyBullet : MonoBehaviour
{
    public float speed = 8f;
    public float damage = 1f;
    public float lifetime = 4f; // Plan SC: FR-06 — 수명 초과 시 반환 (풀 고갈 방지)

    private Vector2 direction;
    private float despawnTime;

    // ♦ SlowAura 국소 감속(1=정상). 매 프레임 SlowAura가 갱신, 범위 밖이면 1로 복귀.
    [System.NonSerialized] public float localTimeScale = 1f;

    public Vector2 Direction
    {
        get { return direction; }
        set { direction = value.normalized; }
    }

    void OnEnable()
    {
        despawnTime = Time.time + lifetime; // 활성화 시점 기준 수명 시작
        localTimeScale = 1f;
    }

    void Update()
    {
        transform.Translate(direction * (speed * Time.deltaTime * localTimeScale));

        if (Time.time >= despawnTime)
        {
            gameObject.SetActive(false); // 수명 초과 반환
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 대시 중(닷지롤 무적)인 플레이어는 총알이 통과 — 소멸/데미지 없음
        if (collision.tag == "Player")
        {
            PlayerController pc = collision.GetComponent<PlayerController>();
            if (pc != null && pc.IsDashActive) return; // 대시 무적 프레임 → 통과
        }

        // Plan SC: FR-04/FR-05 — 플레이어/벽에만 반응. 데미지는 PlayerController가 처리(TakeHit).
        if (collision.tag == "Player" || collision.tag == "Wall")
        {
            gameObject.SetActive(false); // 풀 반환
        }
    }
}
