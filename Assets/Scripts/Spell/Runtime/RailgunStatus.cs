using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// ♠ Railgun 상태 — 다음 N번의 기본 공격이 "충전 후 발사" 레일건으로 대체된다.
// 클릭 → chargeTime 동안 기를 모으고(입자 수렴 연출) → 발사 시점 커서를 향해 관통 빔.
// 경로상 모든 적에게 강한 피해 + 넉백, 플레이어도 반동으로 미세 넉백.
public class RailgunStatus : MonoBehaviour, IPlayerShotOverride, IBuffDisplay
{
    const float ZoomMultiplier = 1.85f; // 시야 5 → 9.25

    private PlayerController pc;
    private int charges;
    private float damage;
    private float beamLength;
    private float beamWidth;
    private float knockbackDistance;
    private float playerRecoil;
    private float chargeTime;
    private bool charging;
    private SpellMarble marble;

    private static readonly Color beamColor = new Color(0.5f, 0.85f, 1f, 1f);

    public SpellMarble BuffMarble { get { return marble; } }
    public bool BuffTimed { get { return false; } }
    public float BuffRemaining { get { return 0f; } }
    public int BuffCharges { get { return charges; } }

    public static void Apply(GameObject player, int charges, float damage, float beamLength, float beamWidth,
                             float knockbackDistance, float playerRecoil, float chargeTime, SpellMarble marble = null)
    {
        RailgunStatus s = player.GetComponent<RailgunStatus>();
        if (s == null)
        {
            s = player.AddComponent<RailgunStatus>();
            s.pc = player.GetComponent<PlayerController>();
            CameraZoom.Request(s, ZoomMultiplier); // 맵 끝까지 뻗는 빔 — 조준선을 길게 볼 수 있게
        }
        s.charges = Mathf.Max(s.charges, 0) + charges; // 중복 발동 시 횟수 누적
        s.damage = damage;
        s.beamLength = beamLength;
        s.beamWidth = beamWidth;
        s.knockbackDistance = knockbackDistance;
        s.playerRecoil = playerRecoil;
        s.chargeTime = chargeTime;
        s.marble = marble;
    }

    private bool didFire;
    public bool DidFire { get { return didFire; } }

    public bool TryOverrideShot(Vector2 origin, Vector2 direction)
    {
        didFire = false;
        if (charging) return true;      // 충전 중 클릭은 소비만(기본탄도 안 나감)
        if (charges <= 0) return false;
        charges--;
        charging = true;
        didFire = true;
        StartCoroutine(ChargeAndFire());
        return true;
    }

    // 진행 중인 연사 코루틴 수 — 남은 횟수가 0이어도 이게 남아 있으면 컴포넌트를 지우면 안 된다
    // (지우면 코루틴이 같이 죽어서 쏘기로 한 발이 사라진다).
    private int pendingBursts;

    // ♠ Gatling 연사 — 충전 게이트를 건너뛰고 즉시 한 발 더 쏜다.
    // 횟수는 추가로 깎지 않는다: 게틀링과 함께 쓰면 "레일건 1회 소모 = 3발"이 되는
    // 조합 보너스다(3회 보유 시 총 9발). 소모는 TryOverrideShot에서 이미 1회 처리됐다.
    public void FireBurstShot(Vector2 origin, Vector2 direction)
    {
        pendingBursts++;
        StartCoroutine(BurstFire(direction));
    }

    IEnumerator BurstFire(Vector2 dir)
    {
        // 연사분은 충전을 짧게(원 충전의 절반) — 3연발의 리듬을 살린다.
        Transform anchor = SpellVfx.VisualAnchor(gameObject);
        float t = chargeTime * 0.5f;
        SpellParticleVfx.SpawnImplode(anchor.position, 1.2f, beamColor, t, 20, 0.3f, anchor);
        yield return new WaitForSeconds(t);
        if (pc == null) { pendingBursts--; yield break; }

        Vector2 origin = (Vector2)pc.transform.position + pc.muzzleOffset;
        Vector2 aim = dir;
        if (Camera.main != null && Mouse.current != null)
        {
            Vector3 c = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Vector2 to = (Vector2)c - origin;
            if (to.sqrMagnitude > 0.0001f) aim = to.normalized;
        }
        FireBeam(origin, aim);

        pendingBursts--;
        TryFinish();
    }

    // 남은 횟수도 없고 진행 중인 발사도 없을 때만 정리한다.
    void TryFinish()
    {
        if (charges <= 0 && !charging && pendingBursts <= 0) Destroy(this);
    }

    IEnumerator ChargeAndFire()
    {
        // 충전 연출 — 몸 주위로 에너지가 수렴(플레이어 추종)
        Transform anchor = SpellVfx.VisualAnchor(gameObject);
        SpellParticleVfx.SpawnImplode(anchor.position, 1.5f, beamColor, chargeTime, 30, 0.35f, anchor);
        SpellVfx.SpawnConverge(anchor.position, 1.3f, beamColor, chargeTime, false, anchor);

        yield return new WaitForSeconds(chargeTime);
        if (pc == null) { charging = false; yield break; }

        // 발사 시점 재조준 — 충전하는 동안 커서를 따라갈 수 있다
        Vector2 origin = (Vector2)pc.transform.position + pc.muzzleOffset;
        Vector2 dir = Vector2.right;
        if (Camera.main != null && Mouse.current != null)
        {
            Vector3 c = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Vector2 to = (Vector2)c - origin;
            if (to.sqrMagnitude > 0.0001f) dir = to.normalized;
        }

        FireBeam(origin, dir);
        charging = false;
        TryFinish(); // 연사분이 남아 있으면 그게 끝난 뒤에 정리된다
    }

    void FireBeam(Vector2 origin, Vector2 dir)
    {
        float range = BeamRange(origin, dir);
        RailBeamVfx.Spawn(origin, dir, range, beamWidth); // 번개 궤적(스프라이트+라인 합성)

        // 판정: 빔 중심선을 따라 박스(폭 = beamWidth) — 경로상 모든 적 관통 타격
        Vector2 center = origin + dir * (range * 0.5f);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, new Vector2(range, beamWidth), angle);
        for (int i = 0; i < hits.Length; i++)
        {
            if (!hits[i].CompareTag("Enemy")) continue;
            IDamageable dmg = hits[i].GetComponent<IDamageable>();
            if (dmg != null) dmg.ApplyHit(damage);
            else
            {
                Character ch = hits[i].GetComponent<Character>();
                if (ch != null && !ch.Hit(damage)) hits[i].gameObject.SetActive(false);
            }
            // 빔 진행 방향으로 밀려나도록 — 기준점을 적의 뒤(빔 반대쪽)에 둔다
            Vector2 from = (Vector2)hits[i].transform.position - dir;
            KnockbackStatus.Apply(hits[i].gameObject, from, knockbackDistance, 0.3f);
        }

        // 플레이어 반동(미세) — 발사 반대 방향
        if (pc != null) pc.transform.position -= (Vector3)(dir * playerRecoil);
    }

    // 빔 실제 길이 — 맵 끝(벽)까지 뻗는다. 벽이 없으면 beamLength(에셋 값)를 상한으로 사용.
    // LaserEnemyController.BeamRange()와 동일 규약.
    float BeamRange(Vector2 origin, Vector2 dir)
    {
        float max = Mathf.Max(beamLength, MaxRange);
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, dir, max);
        float best = max;
        for (int i = 0; i < hits.Length; i++)
            if (hits[i].collider != null && hits[i].collider.CompareTag("Wall") && hits[i].distance < best)
                best = hits[i].distance;
        return best;
    }

    // 어떤 맵에서도 반대편 벽에 닿고 남을 충분한 상한
    const float MaxRange = 60f;

    void OnDisable() { CameraZoom.Release(this); }
}
