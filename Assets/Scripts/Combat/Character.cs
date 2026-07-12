using UnityEngine;

public class Character : MonoBehaviour
{
    public float maxHp = 3;
    public HeartPoint heartPoint;

    [Header("Sound")]
    public AudioClip hitSound;   // 피격(살아있음)
    public AudioClip deadSound;  // 사망
    [Range(0f, 1f)] public float soundVolume = 0.6f;

    private float hp;
    private AudioSource audioSource;

    private float hpMaxWidth;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    public void Initialize()
    {
        hp = maxHp;
        if (heartPoint != null)
        {
            heartPoint.UpdateHeart(hp, maxHp);
        }
    }
    // 현재 HP 비율(0~1) — 스펠 마블 ♥ Adrenaline 등 저체력 조건용
    public float HpRatio => maxHp > 0f ? Mathf.Clamp01(hp / maxHp) : 0f;

    /**
     * 부활(사망 인터셉트 전용) — HP를 maxHp×ratio로 설정. 스펠 마블 ♥ Resurrection.
     */
    public void Revive(float ratio)
    {
        hp = Mathf.Clamp(maxHp * Mathf.Clamp01(ratio), 1f, maxHp);
        if (heartPoint != null) heartPoint.UpdateHeart(hp, maxHp);
    }

    /**
     * HP 회복(maxHp 초과 불가). 스펠 마블 ♥회복 등에서 사용.
     */
    public void Heal(float amount)
    {
        if (hp <= 0f || amount <= 0f) return; // 사망 상태는 회복 불가
        hp = Mathf.Min(hp + amount, maxHp);
        if (heartPoint != null)
        {
            heartPoint.UpdateHeart(hp, maxHp);
        }
    }

    /**
     * 살아있으면 true 리턴
     */
    public bool Hit(float damage)
    {
        hp -= damage;
        if (hp <= 0) hp = 0;

        if (heartPoint != null)
        {
            heartPoint.UpdateHeart(hp, maxHp);
        }

        bool alive = hp > 0;

        if (alive)
        {
            // 살아있음: 오브젝트가 유지되므로 자체 AudioSource로 재생 (없으면 2D 폴백)
            if (hitSound != null)
            {
                if (audioSource != null)
                    audioSource.PlayOneShot(hitSound, soundVolume);
                else
                    Sfx.Play2D(hitSound, soundVolume);
            }
        }
        else
        {
            // 사망: 곧 비활성화/씬전환 → 임시 오브젝트로 재생.
            // 2D 재생 — 3D(PlayClipAtPoint)는 거리 감쇠로 소리가 작아져 타격감이 죽는다.
            if (deadSound != null)
                Sfx.Play2D(deadSound, soundVolume);
        }

        return alive;
    }
}
