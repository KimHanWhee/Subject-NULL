using UnityEngine;

// Design Ref: §2.3 — 플레이어 자식 트리거. EnemyBullet 스침 + 대시면 슬로우 발동.
// 이 오브젝트(NearMissZone)는 "Player" 태그가 아니므로 총알이 스쳐도 소멸하지 않는다(감지≠피격).
public class NearMissDetector : MonoBehaviour
{
    public PlayerController player;
    public SlowMotion slowMotion;

    void Awake()
    {
        // 프리팹↔씬 참조 제약 회피: 미배선 시 런타임 탐색
        if (player == null) player = GetComponentInParent<PlayerController>();
        if (slowMotion == null) slowMotion = FindObjectOfType<SlowMotion>();
    }

    // Plan SC: FR-01/FR-04 — 감지 존은 "Player" 아님 → 총알 소멸 없이 감지만
    // ⚠️ Enter만 쓰면 안 됨: 총알이 존에 "먼저" 들어온 뒤 대시하는 게 일반적인 플레이 흐름이라
    //    Enter 시점엔 IsDashActive=false로 불발 → 이후 재진입이 없어 영영 안 터진다.
    //    Stay로 "대시 중 + 존 안에 총알 존재"를 매 스텝 검사(중복 발동은 SlowMotion 쿨다운이 방어).
    private void OnTriggerEnter2D(Collider2D collision) => TryTrigger(collision);
    private void OnTriggerStay2D(Collider2D collision) => TryTrigger(collision);

    void TryTrigger(Collider2D collision)
    {
        if (collision.tag == "EnemyBullet"
            && player != null && player.IsDashActive
            && slowMotion != null)
        {
            slowMotion.Trigger();
        }
    }
}
