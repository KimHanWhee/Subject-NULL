using UnityEngine;

// ♠ Sniper Mode 상태 — 지속시간 동안 총알 속도/데미지 배율(기본 3배), 대신 이동속도 감소.
public class SniperStatus : MonoBehaviour, IPlayerOutgoingModifier, IPlayerBulletModifier, IBuffDisplay
{
    const float ZoomMultiplier = 1.95f; // 사거리/탄속이 길어지는 만큼 가장 넓게(시야 5 → 9.75)

    private PlayerController pc;
    private float damageMult;
    private float bulletSpeedMult;
    private float remain;
    private SpellMarble marble;

    public SpellMarble BuffMarble { get { return marble; } }
    public bool BuffTimed { get { return true; } }
    public float BuffRemaining { get { return remain; } }
    public int BuffCharges { get { return 0; } }

    public static void Apply(GameObject player, float damageMult, float bulletSpeedMult, float moveMult, float duration, SpellMarble marble = null)
    {
        SniperStatus s = player.GetComponent<SniperStatus>();
        if (s == null)
        {
            s = player.AddComponent<SniperStatus>();
            s.pc = player.GetComponent<PlayerController>();
            if (s.pc == null) { Destroy(s); return; }
            CameraZoom.Request(s, ZoomMultiplier);                  // 저격 중 시야 확대
        }
        // 이동속도 감소는 배율로 등록 — 다른 속도 버프와 겹쳐도 서로를 덮어쓰지 않는다.
        PlayerSpeedModifiers.Set(s, Mathf.Clamp01(moveMult));
        s.damageMult = damageMult;
        s.bulletSpeedMult = bulletSpeedMult;
        s.marble = marble;
        s.remain = Mathf.Max(s.remain, duration);
    }

    public float ModifyOutgoingDamage(float damage) => damage * damageMult;

    public void ModifyBullet(Bullet bullet) => bullet.speed *= bulletSpeedMult;

    void Update()
    {
        remain -= Time.deltaTime;
        if (remain <= 0f) { Restore(); Destroy(this); }
    }

    void Restore()
    {
        PlayerSpeedModifiers.Clear(this);
        CameraZoom.Release(this);
        pc = null;
    }

    void OnDisable() { Restore(); }
}
