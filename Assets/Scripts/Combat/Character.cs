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
            // 살아있음: 오브젝트가 유지되므로 자체 AudioSource로 재생 (없으면 폴백)
            if (hitSound != null)
            {
                if (audioSource != null)
                    audioSource.PlayOneShot(hitSound, soundVolume);
                else
                    AudioSource.PlayClipAtPoint(hitSound, transform.position, soundVolume);
            }
        }
        else
        {
            // 사망: 곧 비활성화/씬전환 → 끊기지 않도록 임시 오브젝트로 재생
            if (deadSound != null)
                AudioSource.PlayClipAtPoint(deadSound, transform.position, soundVolume);
        }

        return alive;
    }
}
