using UnityEngine;

// ♠ Piercing Shot 상태 — 지속시간 동안 발사하는 총알이 적을 관통(벽에는 소멸).
public class PiercingStatus : MonoBehaviour, IPlayerBulletModifier, IBuffDisplay
{
    private float remain;
    private SpellMarble marble;

    public SpellMarble BuffMarble { get { return marble; } }
    public bool BuffTimed { get { return true; } }
    public float BuffRemaining { get { return remain; } }
    public int BuffCharges { get { return 0; } }

    public static void Apply(GameObject player, float duration, SpellMarble marble = null)
    {
        PiercingStatus s = player.GetComponent<PiercingStatus>();
        if (s == null) s = player.AddComponent<PiercingStatus>();
        s.marble = marble;
        s.remain = Mathf.Max(s.remain, duration);
    }

    public void ModifyBullet(Bullet bullet) => bullet.pierce = true;

    void Update()
    {
        remain -= Time.deltaTime;
        if (remain <= 0f) Destroy(this);
    }
}
