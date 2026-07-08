using UnityEngine;

// ♠ Sniper Mode 상태 — 지속시간 동안 총알 속도/데미지 배율(기본 3배), 대신 이동속도 감소.
public class SniperStatus : MonoBehaviour, IPlayerOutgoingModifier, IPlayerBulletModifier
{
    private PlayerController pc;
    private float originalSpeed;
    private float damageMult;
    private float bulletSpeedMult;
    private float remain;

    public static void Apply(GameObject player, float damageMult, float bulletSpeedMult, float moveMult, float duration)
    {
        SniperStatus s = player.GetComponent<SniperStatus>();
        if (s == null)
        {
            s = player.AddComponent<SniperStatus>();
            s.pc = player.GetComponent<PlayerController>();
            if (s.pc == null) { Destroy(s); return; }
            s.originalSpeed = s.pc.speed;
            s.pc.speed = s.originalSpeed * Mathf.Clamp01(moveMult); // 이동속도 감소
        }
        s.damageMult = damageMult;
        s.bulletSpeedMult = bulletSpeedMult;
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
        if (pc != null) pc.speed = originalSpeed;
        pc = null;
    }

    void OnDisable() { Restore(); }
}
