using System;
using System.Collections.Generic;
using UnityEngine;

// Design Ref: §4.2 — 손패 5 관리 + 리필 + 발동 오케스트레이션.
// Plan SC: FR-05(손패5) / FR-06(리필) / FR-09(발동) / FR-10(실행 파이프라인)
public class SpellCaster : MonoBehaviour
{
    [Header("Deck / Hand")]
    public DeckData deck;              // 기본 덱(저장 덱이 없을 때 폴백)
    public SpellMarbleRegistry registry; // 저장 덱(marbleName JSON) 해석용 — 비우면 기본 덱만 사용
    public int handSize = 5;          // Plan SC: FR-05
    public float refillCooldown = 3f; // Plan SC: FR-06 — 사용 후 리필 대기(초)

    [Header("Refs")]
    public GameObject player;          // 시전자(비우면 "Player" 태그 탐색)
    public ObjectPool effectPool;      // 능력 이펙트용 풀(선택)

    [Header("Audio")]
    public AudioSource audioSource;    // 비우면 자동 생성. 능력 activationSound 재생(프로젝트 PlayOneShot 관례).

    [Header("Cast (시전 딜레이)")]
    public float castDelay = 0.5f;      // 드롭 후 능력 발동까지(초, unscaled). 그동안 등급색 시전 고리가 수렴.
    public float castRingRadius = 1.2f; // 시전 고리 시작 반경(중심으로 수렴)

    public event Action OnHandChanged;

    private SpellMarble[] slots;
    private float[] refillReadyTime;   // 각 슬롯 리필 가능 시각(unscaled). 채워지면 무의미.
    private int drawIndex;
    private List<SpellMarble> drawList; // 실제 뽑기 소스: 저장 덱(덱 편성 씬) 우선, 없으면 기본 DeckData

    public IReadOnlyList<SpellMarble> Slots => slots;

    void Awake()
    {
        slots = new SpellMarble[Mathf.Max(1, handSize)];
        refillReadyTime = new float[slots.Length];
        if (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) player = p;
        }
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    void Start()
    {
        drawList = BuildDrawList();
        for (int i = 0; i < slots.Length; i++) slots[i] = DrawNext();
        OnHandChanged?.Invoke();
    }

    // 저장 덱(DeckSaveService) 우선, 비었거나 registry 미연결이면 기본 DeckData 폴백
    List<SpellMarble> BuildDrawList()
    {
        if (registry != null && DeckSaveService.HasSave())
        {
            List<SpellMarble> saved = DeckSaveService.Load(registry);
            if (saved.Count > 0) return saved;
        }
        return (deck != null && deck.marbles != null) ? deck.marbles : null;
    }

    void Update()
    {
        // Plan SC: FR-06 — 빈 슬롯 리필. unscaled 기준이라 선택 슬로우 중에도 진행.
        bool changed = false;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null && Time.unscaledTime >= refillReadyTime[i])
            {
                slots[i] = DrawNext();
                if (slots[i] != null) changed = true;
            }
        }
        if (changed) OnHandChanged?.Invoke();
    }

    // Plan SC: FR-09/FR-10 — 발동. TargetMode에 따라 위치 결정 후 능력 실행·소비·리필 예약.
    public bool Activate(int slotIndex, Vector2 dropWorldPos)
    {
        if (slots == null || slotIndex < 0 || slotIndex >= slots.Length) return false;
        SpellMarble marble = slots[slotIndex];
        if (marble == null || marble.ability == null) return false;

        bool self = marble.ability.targetMode == TargetMode.SelfBuff && player != null;
        Vector2 pos = self ? (Vector2)player.transform.position : dropWorldPos;

        SpellContext ctx = new SpellContext
        {
            caster = player,
            targetPosition = pos,
            grade = marble.grade,
            effectPool = effectPool
        };

        // 시전 텔레그래프: 등급색 고리가 castDelay 동안 중심으로 수렴 → 이후 실제 발동.
        // Targeted=드롭 위치 고정, SelfBuff=플레이어 추종. 레전드는 무지개.
        Color ringColor = GradePalette.ColorOf(marble.grade);
        Transform follow = self ? SpellVfx.VisualAnchor(player) : null; // 몸통 시각 중심 추종(스프라이트 상단 여백 보정)
        SpellVfx.SpawnConverge(follow != null ? (Vector2)follow.position : pos, castRingRadius, ringColor, castDelay, marble.grade == Grade.Legend, follow);

        // castDelay 후 능력 발동 + 발동음(고리 애니메이션 뒤에 실제 효과). 사용음은 드롭 시(SpellDragHandler).
        StartCoroutine(CastAndActivate(marble.ability, ctx, castDelay));

        slots[slotIndex] = null;                                  // 소비(드롭 즉시)
        refillReadyTime[slotIndex] = Time.unscaledTime + refillCooldown;
        OnHandChanged?.Invoke();
        return true;
    }

    // castDelay(unscaled) 후 능력 발동 + 발동음. WaitForSecondsRealtime로 슬로우 무관 일정한 시전 시간.
    private System.Collections.IEnumerator CastAndActivate(SpellAbility ability, SpellContext ctx, float delay)
    {
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
        ability.Activate(ctx);
        if (audioSource != null && ability.activationSound != null)
            audioSource.PlayOneShot(ability.activationSound, ability.soundVolume);
    }

    SpellMarble DrawNext()
    {
        if (drawList == null || drawList.Count == 0) return null;
        SpellMarble m = drawList[drawIndex % drawList.Count];
        drawIndex++;
        return m;
    }

    // HUD 표시용: 빈 슬롯의 리필 진행도(0~1). 채워졌으면 1.
    public float RefillProgress(int slotIndex)
    {
        if (slots == null || slotIndex < 0 || slotIndex >= slots.Length) return 1f;
        if (slots[slotIndex] != null) return 1f;
        float remain = refillReadyTime[slotIndex] - Time.unscaledTime;
        if (remain <= 0f) return 1f;
        return Mathf.Clamp01(1f - remain / Mathf.Max(0.0001f, refillCooldown));
    }
}
