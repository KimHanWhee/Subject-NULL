using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// ♠ Railgun 상태 — 다음 N번의 기본 공격이 "충전 후 발사" 레일건으로 대체된다.
// 클릭 → chargeTime 동안 기를 모으고(입자 수렴 연출) → 발사 시점 커서를 향해 관통 빔.
// 경로상 모든 적에게 강한 피해 + 넉백, 플레이어도 반동으로 미세 넉백.
public class RailgunStatus : MonoBehaviour, IPlayerShotOverride, IBuffDisplay
{
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

    public bool TryOverrideShot(Vector2 origin, Vector2 direction)
    {
        if (charging) return true;      // 충전 중 클릭은 소비만(기본탄도 안 나감)
        if (charges <= 0) return false;
        charges--;
        charging = true;
        StartCoroutine(ChargeAndFire());
        return true;
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
        if (charges <= 0) Destroy(this);
    }

    void FireBeam(Vector2 origin, Vector2 dir)
    {
        RailBeamVfx.Spawn(origin, dir, beamLength, beamWidth); // 번개 궤적(스프라이트+라인 합성)

        // 판정: 빔 중심선을 따라 박스(폭 = beamWidth) — 경로상 모든 적 관통 타격
        Vector2 center = origin + dir * (beamLength * 0.5f);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, new Vector2(beamLength, beamWidth), angle);
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
}
