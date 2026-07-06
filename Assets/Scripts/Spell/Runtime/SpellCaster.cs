using System;
using System.Collections.Generic;
using UnityEngine;

// Design Ref: §4.2 — 손패 5 관리 + 리필 + 발동 오케스트레이션.
// Plan SC: FR-05(손패5) / FR-06(리필) / FR-09(발동) / FR-10(실행 파이프라인)
public class SpellCaster : MonoBehaviour
{
    [Header("Deck / Hand")]
    public DeckData deck;              // 테스트 덱 (P2에서 저장 덱으로 교체)
    public int handSize = 5;          // Plan SC: FR-05
    public float refillCooldown = 3f; // Plan SC: FR-06 — 사용 후 리필 대기(초)

    [Header("Refs")]
    public GameObject player;          // 시전자(비우면 "Player" 태그 탐색)
    public ObjectPool effectPool;      // 능력 이펙트용 풀(선택)

    public event Action OnHandChanged;

    private SpellMarble[] slots;
    private float[] refillReadyTime;   // 각 슬롯 리필 가능 시각(unscaled). 채워지면 무의미.
    private int drawIndex;

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
    }

    void Start()
    {
        for (int i = 0; i < slots.Length; i++) slots[i] = DrawNext();
        OnHandChanged?.Invoke();
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

        Vector2 pos = (marble.ability.targetMode == TargetMode.SelfBuff && player != null)
            ? (Vector2)player.transform.position
            : dropWorldPos;

        SpellContext ctx = new SpellContext
        {
            caster = player,
            targetPosition = pos,
            grade = marble.grade,
            effectPool = effectPool
        };
        marble.ability.Activate(ctx);

        slots[slotIndex] = null;                                  // 소비
        refillReadyTime[slotIndex] = Time.unscaledTime + refillCooldown;
        OnHandChanged?.Invoke();
        return true;
    }

    SpellMarble DrawNext()
    {
        if (deck == null || deck.marbles == null || deck.marbles.Count == 0) return null;
        SpellMarble m = deck.marbles[drawIndex % deck.marbles.Count];
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
