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

    [Header("Legend Cast (레전드 차징)")]
    public float legendCastDelay = 3f;        // 레전드 등급: 강대한 힘을 모으는 긴 시전
    public float legendCastRingRadius = 2.6f; // 더 크고 넓게 수렴하는 고리

    [Header("Cast Slow (시전 중 슬로우)")]
    [Range(0.01f, 1f)] public float castSlowScale = 0.01f; // 시전 동안 게임 속도 — Ctrl 선택 슬로우(0.01)와 동일 체감

    public event Action OnHandChanged;

    private SpellMarble[] slots;
    private float[] refillReadyTime;   // 각 슬롯 리필 가능 시각(scaled Time.time). 채워지면 무의미.
                                       // 게임시간 기준이라 Ctrl 선택/시전 슬로우 중엔 리필도 함께 느려짐.
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

    async void Start()
    {
        // 복사본을 셔플 — 원본(DeckData 에셋/저장 덱 리스트)의 순서를 건드리지 않기 위함
        // 우선 로컬 미러(마지막 서버 소유)로 즉시 시작 → 게임 시작 지연 없음.
        List<SpellMarble> source = BuildDrawList();
        drawList = source != null ? new List<SpellMarble>(source) : null;
        Shuffle(drawList);
        for (int i = 0; i < slots.Length; i++) slots[i] = DrawNext();
        OnHandChanged?.Invoke();

        // 서버 소유 최신화 → 이후 리필 드로우풀만 보정(현재 손패는 유지). 씬 이탈 시 가드.
        for (int i = 0; i < 50 && !ServicesBootstrap.IsSignedIn; i++)
            await System.Threading.Tasks.Task.Delay(100);
        if (this == null) return;
        if (await PlayerProfileService.RefreshAsync())
        {
            if (this == null) return;
            List<SpellMarble> refreshed = BuildDrawList();
            if (refreshed != null) { drawList = new List<SpellMarble>(refreshed); Shuffle(drawList); }
        }
    }

    // 저장 덱(DeckSaveService) 우선, 비었거나 registry 미연결이면 기본 DeckData 폴백.
    // 소유(OwnedMarblesService)한 마블만 사용 — 미보유 Gold+(기본덱 포함)는 제외.
    List<SpellMarble> BuildDrawList()
    {
        List<SpellMarble> src = null;
        if (registry != null && DeckSaveService.HasSave())
        {
            List<SpellMarble> saved = DeckSaveService.Load(registry);
            if (saved.Count > 0) src = saved;
        }
        if (src == null) src = (deck != null && deck.marbles != null) ? deck.marbles : null;
        if (src == null) return null;

        List<SpellMarble> owned = new List<SpellMarble>(); // 원본(에셋 리스트) 훼손 방지 위해 새 리스트
        foreach (SpellMarble m in src)
            if (m != null && OwnedMarblesService.IsOwned(m)) owned.Add(m);
        return owned;
    }

    void Update()
    {
        // Plan SC: FR-06 — 빈 슬롯 리필. scaled(Time.time) 기준 — Ctrl 선택/시전 슬로우 중엔 리필도 느려짐.
        bool changed = false;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null && Time.time >= refillReadyTime[i])
            {
                slots[i] = DrawNext();
                if (slots[i] != null) changed = true;
            }
        }
        if (changed) OnHandChanged?.Invoke();

        CheckOverload();
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

        // 시전 텔레그래프: 등급색 고리가 시전 시간 동안 중심으로 수렴 → 이후 실제 발동.
        // Targeted=드롭 위치 고정, SelfBuff=플레이어 추종. 레전드는 무지개.
        // 레전드는 긴 차징(강대한 힘 연출): 큰 이중 수렴 링 + 지속 흡입 입자.
        bool legend = marble.grade == Grade.Legend;
        float delay = legend ? legendCastDelay : castDelay;
        float ringRadius = legend ? legendCastRingRadius : castRingRadius;
        Color ringColor = GradePalette.ColorOf(marble.grade);
        Transform follow = self ? SpellVfx.VisualAnchor(player) : null; // 몸통 시각 중심 추종(스프라이트 상단 여백 보정)
        Vector2 telegraphPos = follow != null ? (Vector2)follow.position : pos;
        SpellVfx.SpawnConverge(telegraphPos, ringRadius, ringColor, delay, legend, follow);
        if (legend)
        {
            // 안쪽에서 더 빨리 수렴하는 보조 링(겹겹이 모이는 느낌) + 힘이 빨려드는 입자
            SpellVfx.SpawnConverge(telegraphPos, ringRadius * 0.6f, ringColor, delay * 0.55f, true, follow);
            SpellParticleVfx.SpawnImplode(telegraphPos, ringRadius * 1.8f, ringColor, delay, 48, 0.7f, follow);
        }

        // 시전 중 게임 슬로우: Ctrl을 떼도 시전이 끝날 때까지 유지(펄스 — unscaled 기준 자동 만료라 누수 없음)
        if (TimeController.Instance != null)
            TimeController.Instance.Pulse(castSlowScale, delay);

        // 시전 시간 후 능력 발동 + 발동음(고리 애니메이션 뒤에 실제 효과). 사용음은 드롭 시(SpellDragHandler).
        StartCoroutine(CastAndActivate(marble.ability, ctx, delay));

        slots[slotIndex] = null;                                  // 소비(드롭 즉시)
        refillReadyTime[slotIndex] = Time.time + refillCooldown;  // scaled — 슬로우 중엔 리필 대기도 늘어남
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

    // 셔플백 뽑기 — 덱은 유한. 남은 덱이 없으면 null(빈 슬롯 유지, 자동 재셔플 없음).
    // 덱 소진 + 손패 전부 소모 = 과부하(조커) → OnJokerFired에서 재셔플·리필된다.
    SpellMarble DrawNext()
    {
        if (drawList == null || drawIndex >= drawList.Count) return null;
        SpellMarble m = drawList[drawIndex];
        drawIndex++;
        return m;
    }

    // ---- 과부하(조커) ----
    // 덱을 전부 뽑았고 손패까지 모두 소모되면 벨트 과부하 → 3초 경고 후 조커 기믹 발동.
    [Header("Overload (Joker)")]
    public AudioClip overloadWarningSound; // 경고 사이렌(점멸 3초 동안 루프)
    [Header("Overload (Joker) 기믹 사운드")]
    public AudioClip jokerThunderSound;    // 번개 폭풍 — 낙뢰음(타격마다)
    public AudioClip jokerPurgeSound;      // 대숙청 — 폭발음
    public AudioClip jokerConfusionSound;  // 집단 혼란 — 발동음
    // 조커 최소 간격 — 마블 속사 사이클링(고의 낭비→조커 즉시 반복) 방지.
    // 반드시 unscaled 기준: 스케일드(Time.time)로 재면 Ctrl/시전 슬로우(0.01배) 동안 거의 안 흘러
    // "조커가 게임당 한 번만 터진다"급으로 길어진다. 정상 덱 한 바퀴(수십 초)보다 짧게 유지할 것.
    public float jokerCooldown = 10f;

    public bool IsOverloaded { get; private set; }
    private float lastJokerTime = float.NegativeInfinity;

    void CheckOverload()
    {
        if (IsOverloaded || drawList == null || drawList.Count == 0) return;
        if (drawIndex < drawList.Count) return;                    // 아직 덱이 남음
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] != null) return;                          // 손패가 남음
        // 쿨다운 중이면 발동 보류 — 벨트는 빈 채로 대기하다 쿨다운이 끝나면 자동 발동(소프트락 없음)
        if (Time.unscaledTime < lastJokerTime + jokerCooldown) return;
        IsOverloaded = true;
        lastJokerTime = Time.unscaledTime;
        JokerSpell.Trigger(player, -1, OnJokerFired, overloadWarningSound,
            jokerThunderSound, jokerPurgeSound, jokerConfusionSound); // 3초 경고 → 기믹 → 콜백
    }

    // 기믹 발동 순간: 덱 재셔플 + 손패 즉시 풀 리필(과부하 해소 보상) + Ctrl 잠금 해제
    void OnJokerFired()
    {
        Shuffle(drawList);
        drawIndex = 0;
        for (int i = 0; i < slots.Length; i++) slots[i] = DrawNext();
        IsOverloaded = false;
        OnHandChanged?.Invoke();
    }

    static void Shuffle(List<SpellMarble> list) // Fisher-Yates
    {
        if (list == null) return;
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    // HUD 표시용: 빈 슬롯의 리필 진행도(0~1). 채워졌으면 1.
    public float RefillProgress(int slotIndex)
    {
        if (slots == null || slotIndex < 0 || slotIndex >= slots.Length) return 1f;
        if (slots[slotIndex] != null) return 1f;
        float remain = refillReadyTime[slotIndex] - Time.time;
        if (remain <= 0f) return 1f;
        return Mathf.Clamp01(1f - remain / Mathf.Max(0.0001f, refillCooldown));
    }
}
