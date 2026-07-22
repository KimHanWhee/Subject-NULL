using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameObject player;
    public float spawnTerm = 5;         // 초기 스폰 간격(점수 0일 때)
    public float fasterEnemySpawn = 0.05f; // (레거시) 미사용 — 점수 기반 간격으로 대체
    public float minSpawnTerm = 1;      // 최소 스폰 간격(고점수)
    public TextMeshProUGUI scoreText;

    [Header("Difficulty (점수 기반)")]
    public float spawnRampScore = 600f; // 이 점수에서 스폰 간격이 최소치에 도달
    [Range(1, 8)] public int maxSimultaneous = 5;  // 한 번에 최대 동시 스폰 수
    public float simulPerScore = 180f;  // 이 점수마다 동시 스폰 상한 +1
    public float multiChanceScore = 800f; // 이 점수에서 추가 스폰 확률 최대(100%)

    [Header("Combo")]
    public float comboWindow = 2.5f;    // 콤보 유지 시간(초) — 이 안에 처치하면 연속
    public float comboBonus = 0.2f;     // 콤보 1단계당 배수 증가(x1.2, x1.4 ...)

    [Header("Time Score")]
    public float timeScoreRate = 1f;    // 초당 자동 획득 점수(생존 보너스, 원래 기믹 부활)

    [Header("Power (점수 기반 기본공격)")]
    // 적이 점점 강해지는 만큼 플레이어 기본공격도 같이 올라간다.
    // 연속 증가는 체감이 안 되므로 단계로 끊어서 "강해졌다"는 순간을 만든다.
    [Tooltip("점수로 얻는 최대 추가 피해 — 기본 무기(1)에 더해져 최종 3이 된다")]
    public float maxDamageBonus = 2f;
    [Tooltip("이 점수에서 추가 피해가 최대치에 도달")]
    public float damageRampScore = 800f;
    [Tooltip("추가 피해를 몇 단계로 나눠 올릴지 — 2면 400점에 +1, 800점에 +2")]
    [Range(1, 5)] public int damageSteps = 2;

    // ---- 스폰 가중치 방식 ----
    // 각 값은 "상대 가중치" — (자기 값 ÷ 전체 합)이 실제 등장 비율.
    [Header("Melee (Slime)")]
    [Range(0f, 1f)] public float meleeWeight = 0.4f;
    [Tooltip("슬라임이 차지할 최소 등장 비율 — 후반에도 기본 적으로 계속 나오게 하는 바닥값")]
    [Range(0f, 0.6f)] public float meleeMinShare = 0.25f;

    [Header("Ranged Enemy")]
    public ObjectPool rangedPool;
    [Range(0f, 1f)] public float rangedSpawnChance = 0.4f;

    [Header("Charger Enemy")]
    public ObjectPool chargerPool;
    [Range(0f, 1f)] public float chargerSpawnChance = 0.25f;

    [Header("Shotgun Enemy")]
    public ObjectPool shotgunPool;
    [Range(0f, 1f)] public float shotgunSpawnChance = 0.2f;

    [Header("Laser Enemy")]
    public ObjectPool laserPool;
    [Range(0f, 1f)] public float laserSpawnChance = 0.15f;

    [Header("Tank Enemy")]
    public ObjectPool tankPool;
    [Range(0f, 1f)] public float tankSpawnChance = 0.15f;

    [Header("Heal Drone")]
    public ObjectPool dronePool;
    [Range(0f, 1f)] public float droneSpawnChance = 0.1f;

    // 타입별 점수 해금 시점(이 점수 이상부터 등장) — pools 배열 순서와 정렬
    // 순서: ranged, charger, shotgun, laser, tank, drone, melee
    private static readonly float[] UnlockScore = { 0f, 40f, 120f, 250f, 200f, 400f, 0f };
    // 점수가 오를수록 강한 적의 실효 가중치를 더 키우는 편향(강할수록 크게)
    private static readonly float[] StrengthBias = { 0.3f, 0.5f, 0.8f, 1.2f, 1.0f, 0.7f, 0f };
    private const float UnlockRamp = 150f; // 해금 후 이 점수 동안 0→기본 가중치로 상승

    private int score;
    private int combo;
    private float comboTimer;
    private float baseSpawnTerm;
    private float timeAfterLastSpawn;
    private float timeScoreAcc;          // 시간 점수 누적(1 이상 쌓이면 정수 가산)
    private int powerStep;               // 현재 도달한 공격력 단계(0 ~ damageSteps)

    private TextMeshProUGUI comboText;

    public int Score { get { return score; } }

    // 기본공격에 더해지는 점수 보너스 피해 — PlayerController.Shoot()에서 무기 피해에 가산
    public float DamageBonus
    {
        get { return damageSteps <= 0 ? 0f : maxDamageBonus * powerStep / damageSteps; }
    }
    public int PowerStep { get { return powerStep; } }

    void Awake()
    {
        Instance = this;
        HardshipSystem.ResetAll(); // 고난은 판 단위 — 새 게임 시작 시 누적 초기화
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        GetComponent<ObjectPool>().Initialize();
        if (rangedPool != null) rangedPool.Initialize();
        if (chargerPool != null) chargerPool.Initialize();
        if (shotgunPool != null) shotgunPool.Initialize();
        if (laserPool != null) laserPool.Initialize();
        if (tankPool != null) tankPool.Initialize();
        if (dronePool != null) dronePool.Initialize();

        timeAfterLastSpawn = 0;
        baseSpawnTerm = spawnTerm;
        score = 0;
        combo = 0;
        comboTimer = 0f;

        BuildComboText();
    }

    // 점수가 구간을 넘으면 공격력 단계를 올리고 강화 연출을 띄운다(단계는 내려가지 않음).
    void UpdatePowerStep()
    {
        if (damageSteps <= 0) return;
        float t = Mathf.Clamp01(score / Mathf.Max(1f, damageRampScore));
        int step = Mathf.Clamp(Mathf.FloorToInt(t * damageSteps), 0, damageSteps);
        if (step <= powerStep) return;

        powerStep = step;
        if (player == null) return;
        Color c = new Color(1f, 0.6f, 0.35f);
        FloatingScore.SpawnText(player.transform.position, Loc.T("game.powerUp"), c);
        SpellVfx.SpawnRing(player.transform.position, 1.2f, c, 0.5f, 0.1f);
    }

    void Update()
    {
        // 콤보 만료
        if (comboTimer > 0f)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0f) combo = 0;
        }
        RefreshComboText();

        // 시간 점수(생존 보너스) — 초당 timeScoreRate 만큼 계속 증가
        timeScoreAcc += Time.deltaTime * timeScoreRate;
        if (timeScoreAcc >= 1f) { int add = (int)timeScoreAcc; score += add; timeScoreAcc -= add; }

        UpdatePowerStep(); // 점수 구간 도달 시 기본공격 강화

        // 점수 기반 스폰 간격
        float term = Mathf.Lerp(baseSpawnTerm, minSpawnTerm, Mathf.Clamp01(score / spawnRampScore));
        term *= HardshipSystem.SpawnIntervalMult; // 고난 "급속 배양"

        timeAfterLastSpawn += Time.deltaTime;
        if (timeAfterLastSpawn >= term)
        {
            timeAfterLastSpawn -= term;
            SpawnWave();
        }

        if (scoreText != null) scoreText.text = score.ToString();
    }

    // ♦ 처치 보고 — 콤보 배수 적용해 점수 가산 (EnemyBase.Die 공통 호출)
    public void ReportKill(int baseValue, Vector3 worldPos)
    {
        combo = comboTimer > 0f ? combo + 1 : 1;
        comboTimer = comboWindow;
        float mult = 1f + comboBonus * (combo - 1);
        int gained = Mathf.RoundToInt(baseValue * mult);
        score += gained;
        FloatingScore.Spawn(worldPos, gained, combo); // 처치 위치에 +점수 팝업
    }

    // 동시 스폰 — 점수가 오를수록 한 번에 여러 마리(최대 maxSimultaneous)
    void SpawnWave()
    {
        // 고난 "과밀 배양"은 점수 상한 자체를 끌어올린다(초반부터 체감되도록)
        int cap = maxSimultaneous + HardshipSystem.SpawnBonus;
        int maxSimul = Mathf.Clamp(1 + (int)(score / simulPerScore) + HardshipSystem.SpawnBonus, 1, cap);
        float p = Mathf.Clamp01(score / multiChanceScore);
        int count = 1;
        for (int k = 1; k < maxSimul; k++)
            if (Random.value < p) count++;

        for (int i = 0; i < count; i++) SpawnEnemy();
    }

    void SpawnEnemy()
    {
        float x = Random.Range(-19f, 19f);
        float y = Random.Range(-9f, 9f);

        ObjectPool pool = PickPool();
        if (pool == null) return;
        GameObject obj = pool.Get();
        if (obj == null) return; // 풀 고갈 방어
        obj.transform.position = new Vector3(x, y, 0);
        obj.GetComponent<EnemyBase>().Spawn(player);
    }

    // 점수 기반 가중치 추첨 — 해금 전 타입은 가중치 0, 이후 램프+강도 편향으로 상승
    ObjectPool PickPool()
    {
        ObjectPool melee = GetComponent<ObjectPool>();
        ObjectPool[] pools = { rangedPool, chargerPool, shotgunPool, laserPool, tankPool, dronePool, melee };
        float[] baseW = { rangedSpawnChance, chargerSpawnChance, shotgunSpawnChance,
                          laserSpawnChance, tankSpawnChance, droneSpawnChance, meleeWeight };

        float[] eff = new float[pools.Length];
        float total = 0f;
        for (int i = 0; i < pools.Length; i++)
        {
            if (pools[i] == null) { eff[i] = 0f; continue; }
            if (i == pools.Length - 1) // melee: 항상 등장하되 고점수에서 비중 완화(강한 적이 무대 차지)
            {
                eff[i] = baseW[i] * Mathf.Clamp(1f - score / 1500f, 0.25f, 1f);
            }
            else if (score < UnlockScore[i]) eff[i] = 0f;
            else
            {
                float ramp = Mathf.Clamp01((score - UnlockScore[i]) / UnlockRamp);
                eff[i] = baseW[i] * ramp * (1f + StrengthBias[i] * score / 600f);
            }
            total += eff[i];
        }

        // 슬라임 최소 비율 보장.
        // 강한 적의 가중치는 점수에 비례해 무한히 커지는 반면 근접은 0.25배까지 줄어들기만 해서,
        // 그대로 두면 후반에 슬라임이 사실상 사라진다(3000점 기준 약 2%).
        // 여기서 "전체의 meleeMinShare 이상"이 되도록 바닥을 깔아준다.
        int mi = pools.Length - 1;
        if (pools[mi] != null && meleeMinShare > 0f)
        {
            float others = total - eff[mi];
            float need = others * meleeMinShare / Mathf.Max(0.0001f, 1f - meleeMinShare);
            if (eff[mi] < need) { total += need - eff[mi]; eff[mi] = need; }
        }

        if (total <= 0f) return melee;

        float r = Random.value * total;
        for (int i = 0; i < pools.Length; i++)
        {
            if (eff[i] <= 0f) continue;
            if (r < eff[i]) return pools[i];
            r -= eff[i];
        }
        return melee;
    }

    // ---- 콤보 UI (scoreText와 같은 캔버스에 코드 생성) ----

    void BuildComboText()
    {
        if (scoreText == null) return;
        var go = new GameObject("ComboText", typeof(RectTransform));
        go.transform.SetParent(scoreText.transform.parent, false);
        comboText = go.AddComponent<TextMeshProUGUI>();
        comboText.font = scoreText.font;
        comboText.fontSize = scoreText.fontSize * 0.6f;
        comboText.alignment = TextAlignmentOptions.TopRight;
        comboText.raycastTarget = false;
        var srt = scoreText.rectTransform;
        var rt = comboText.rectTransform;
        rt.anchorMin = srt.anchorMin;
        rt.anchorMax = srt.anchorMax;
        rt.pivot = srt.pivot;
        rt.sizeDelta = srt.sizeDelta;
        // scoreText 바로 아래
        rt.anchoredPosition = srt.anchoredPosition + new Vector2(0f, -srt.sizeDelta.y * 0.5f - 34f);
        comboText.text = "";
    }

    void RefreshComboText()
    {
        if (comboText == null) return;
        if (combo >= 2)
        {
            float mult = 1f + comboBonus * (combo - 1);
            comboText.text = "COMBO x" + combo + "  (x" + mult.ToString("0.0") + ")";
            // 남은 시간에 따라 페이드(끊기기 직전 옅어짐)
            float a = Mathf.Clamp01(comboTimer / comboWindow);
            comboText.color = new Color(1f, 0.85f, 0.2f, 0.4f + 0.6f * a);
        }
        else comboText.text = "";
    }
}
