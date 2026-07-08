using UnityEngine;

// ♠ Piercing Shot 상태 — 지속시간 동안 발사하는 총알이 적을 관통(벽에는 소멸).
public class PiercingStatus : MonoBehaviour, IPlayerBulletModifier
{
    private float remain;

    public static void Apply(GameObject player, float duration)
    {
        PiercingStatus s = player.GetComponent<PiercingStatus>();
        if (s == null) s = player.AddComponent<PiercingStatus>();
        s.remain = Mathf.Max(s.remain, duration);
    }

    public void ModifyBullet(Bullet bullet) => bullet.pierce = true;

    void Update()
    {
        remain -= Time.deltaTime;
        if (remain <= 0f) Destroy(this);
    }
}
