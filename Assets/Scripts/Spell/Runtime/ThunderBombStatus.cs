using UnityEngine;
using UnityEngine.InputSystem;

// ♠ 비격진천뢰 상태 — 지속시간 동안 기본 공격이 폭탄 투하로 "대체"된다(총알 없음).
// 클릭 시 커서 위치에 폭탄 낙하 → 소규모 폭발. 발사 속도는 무기 fireRate를 그대로 따른다.
public class ThunderBombStatus : MonoBehaviour, IPlayerShotOverride, IBuffDisplay
{
    const float ZoomMultiplier = 1.75f; // 커서로 착탄점을 조준하므로 넓은 시야가 유리(시야 5 → 8.75)

    private float radius;
    private float damage;
    private float minInterval;
    private float remain;
    private float nextDrop;
    private GameObject explosionPrefab;
    private SpellMarble marble;

    public SpellMarble BuffMarble { get { return marble; } }
    public bool BuffTimed { get { return true; } }
    public float BuffRemaining { get { return remain; } }
    public int BuffCharges { get { return 0; } }

    public static void Apply(GameObject player, float radius, float damage, float minInterval,
                             float duration, GameObject explosionPrefab, SpellMarble marble = null)
    {
        ThunderBombStatus s = player.GetComponent<ThunderBombStatus>();
        if (s == null)
        {
            s = player.AddComponent<ThunderBombStatus>();
            CameraZoom.Request(s, ZoomMultiplier); // 투하 중 시야 확대
        }
        s.radius = radius;
        s.damage = damage;
        s.minInterval = minInterval;
        s.explosionPrefab = explosionPrefab;
        s.marble = marble;
        s.remain = Mathf.Max(s.remain, duration);
    }

    private bool didFire;
    public bool DidFire { get { return didFire; } }

    // 기본 발사 대체 — 버프 동안 총알은 나가지 않고 폭탄만 떨어진다
    public bool TryOverrideShot(Vector2 origin, Vector2 direction)
    {
        didFire = false;
        if (Time.time < nextDrop) return true; // 과열 구간에도 기본탄 억제(클릭 소비)
        nextDrop = Time.time + minInterval;
        didFire = true;

        ThunderBomb.Spawn(PickTarget(origin, direction), radius, damage, explosionPrefab);
        return true;
    }

    // ♠ Gatling 연사 — 투하 쿨다운을 건너뛰고 즉시 한 발 더 떨어뜨린다.
    public void FireBurstShot(Vector2 origin, Vector2 direction)
    {
        ThunderBomb.Spawn(PickTarget(origin, direction), radius, damage, explosionPrefab);
    }

    void Update()
    {
        remain -= Time.deltaTime;
        if (remain <= 0f) Destroy(this);
    }

    void OnDisable() { CameraZoom.Release(this); }

    // 착탄점 — 이 스펠만 유일하게 "방향"이 아니라 절대 좌표가 필요하다.
    //   마우스: 커서 위치
    //   터치  : 화면을 탭한 자리 (조준 스틱은 방향만 주므로 별도 경로)
    // 탭이 없으면 조준 방향 앞으로 떨어뜨린다.
    Vector2 PickTarget(Vector2 origin, Vector2 direction)
    {
        Vector2 tap;
        if (GameInput.TouchMode && GameInput.TryConsumeFieldTap(out tap)) return tap;
        Vector2 aim = GameInput.AimWorld;
        if (((Vector2)aim - origin).sqrMagnitude > 0.0001f) return aim;
        return origin + direction * 3f;
    }
}
