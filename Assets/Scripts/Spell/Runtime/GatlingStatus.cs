using System.Collections;
using UnityEngine;

// ♠ Gatling 상태 — 지속시간 동안 클릭 1회가 "빠른 3연사"가 된다.
// 기본 1발은 PlayerController가 쏘고, 이 상태가 짧은 간격으로 추가 2발을 이어 쏜다(연사 재조준).
public class GatlingStatus : MonoBehaviour, IBuffDisplay
{
    private PlayerController pc;
    private int extraBullets;
    private float burstInterval;
    private float remain;
    private SpellMarble marble;

    public SpellMarble BuffMarble { get { return marble; } }
    public bool BuffTimed { get { return true; } }
    public float BuffRemaining { get { return remain; } }
    public int BuffCharges { get { return 0; } }

    // 대체 발사(레일건/비격진천뢰)도 연사시키기 위한 정보.
    // 게틀링은 "클릭 1회 = 3회 발사"이므로, 대체 발사 역시 3번 반복해야 일관된다.
    public int ExtraShots { get { return extraBullets; } }
    public float BurstInterval { get { return burstInterval; } }

    public static void Apply(GameObject player, int extraBullets, float burstInterval, float duration, SpellMarble marble = null)
    {
        GatlingStatus s = player.GetComponent<GatlingStatus>();
        if (s == null)
        {
            s = player.AddComponent<GatlingStatus>();
            s.pc = player.GetComponent<PlayerController>();
            PlayerBulletEvents.PlayerShot += s.OnShot;
        }
        s.extraBullets = extraBullets;
        s.burstInterval = burstInterval;
        s.marble = marble;
        s.remain = Mathf.Max(s.remain, duration);
    }

    void OnShot(Vector2 cursorWorld)
    {
        StartCoroutine(Burst());
    }

    IEnumerator Burst()
    {
        for (int i = 0; i < extraBullets; i++)
        {
            yield return new WaitForSeconds(burstInterval);
            if (pc == null) yield break;
            pc.FireExtraShot(); // 발사 시점의 커서를 다시 조준 — 연사 손맛
        }
    }

    void Update()
    {
        remain -= Time.deltaTime;
        if (remain <= 0f) Destroy(this);
    }

    void OnDisable()
    {
        PlayerBulletEvents.PlayerShot -= OnShot; // static 이벤트 구독 해제(상태이상 규약)
    }
}
