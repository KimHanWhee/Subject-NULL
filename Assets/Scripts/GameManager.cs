using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject player;
    public float spawnTerm = 5;
    public float fasterEnemySpawn = 0.05f;
    public float minSpawnTerm = 1;
    public TextMeshProUGUI scoreText;

    // ---- 스폰 가중치 방식 ----
    // 각 값은 "상대 가중치" — (자기 값 ÷ 전체 합)이 실제 등장 비율.
    // 예) 합이 1.65일 때 드론 0.1 → 6%. 순차 체인이 아니라 뒤에 있어도 확률이 깎이지 않는다.
    [Header("Melee (Slime)")]
    [Range(0f, 1f)] public float meleeWeight = 0.4f; // 기본 슬라임 가중치 (풀은 자기 자신의 ObjectPool)

    [Header("Ranged Enemy")] // Design Ref: §3.2 — 원거리 적 스폰
    public ObjectPool rangedPool;                   // 원거리 적 전용 풀 (자식 오브젝트의 ObjectPool)
    [Range(0f, 1f)] public float rangedSpawnChance = 0.4f; // 가중치

    [Header("Charger Enemy")] // 돌진형 적(개구리) 스폰
    public ObjectPool chargerPool;                  // 돌진형 적 전용 풀 (자식 오브젝트의 ObjectPool)
    [Range(0f, 1f)] public float chargerSpawnChance = 0.25f; // 가중치

    [Header("Shotgun Enemy")] // 산탄 박쥐(보라) 스폰 — RangedEnemyController 산탄 변형
    public ObjectPool shotgunPool;                  // 산탄 적 전용 풀 (자식 오브젝트의 ObjectPool)
    [Range(0f, 1f)] public float shotgunSpawnChance = 0.2f; // 가중치

    [Header("Laser Enemy")] // 레이저 박쥐(초록) 스폰
    public ObjectPool laserPool;                    // 레이저 적 전용 풀 (자식 오브젝트의 ObjectPool)
    [Range(0f, 1f)] public float laserSpawnChance = 0.15f; // 가중치

    [Header("Tank Enemy")] // 탱커 슬라임(초록·대형) — EnemyController 재사용, 프리팹 값만 다름(저속·고체력)
    public ObjectPool tankPool;                     // 탱커 전용 풀 (자식 오브젝트의 ObjectPool)
    [Range(0f, 1f)] public float tankSpawnChance = 0.15f; // 가중치

    [Header("Heal Drone")] // 힐 드론(서포트) — 주변 적을 주기 회복, 우선 처치 유도
    public ObjectPool dronePool;                    // 드론 전용 풀 (자식 오브젝트의 ObjectPool)
    [Range(0f, 1f)] public float droneSpawnChance = 0.1f; // 가중치

    private float score;

    private float timeAfterLastSpawn;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GetComponent<ObjectPool>().Initialize();    // 근접 풀(기존)
        if (rangedPool != null) rangedPool.Initialize(); // 원거리 풀(미할당 시 근접만)
        if (chargerPool != null) chargerPool.Initialize(); // 돌진형 풀(미할당 시 스킵)
        if (shotgunPool != null) shotgunPool.Initialize(); // 산탄 풀(미할당 시 스킵)
        if (laserPool != null) laserPool.Initialize();     // 레이저 풀(미할당 시 스킵)
        if (tankPool != null) tankPool.Initialize();       // 탱커 풀(미할당 시 스킵)
        if (dronePool != null) dronePool.Initialize();     // 드론 풀(미할당 시 스킵)
        timeAfterLastSpawn = 0;
        score = 0;
    }

    // Update is called once per frame
    void Update()
    {
        timeAfterLastSpawn += Time.deltaTime;
        score+= Time.deltaTime;

        if (timeAfterLastSpawn >= spawnTerm)
        {
            timeAfterLastSpawn -= spawnTerm;
            SpawnEnemy();
            spawnTerm -= fasterEnemySpawn;
            if (spawnTerm <= minSpawnTerm)
            {
                spawnTerm = minSpawnTerm;
            }
        }

        scoreText.text = ((int)score).ToString();
    }

    void SpawnEnemy()
    {
        float x = Random.Range(-9f, 9f);
        float y = Random.Range(-5f, 5f);

        GameObject obj = PickPool().Get();
        if (obj == null) return; // 풀 고갈 방어
        obj.transform.position = new Vector3(x, y, 0);

        // 타입 분기 불필요 — 모든 적은 EnemyBase.Spawn 공통(새 몬스터도 상속만으로 호환)
        obj.GetComponent<EnemyBase>().Spawn(player);
    }

    // Design Ref: §3.2 — 가중치 추첨. 풀 미할당 타입은 합계에서 자동 제외.
    // (기존 순차 체인은 뒤쪽 타입의 실효 확률이 복리로 깎여 드론이 ~2.6%까지 떨어지는 문제가 있었음)
    ObjectPool PickPool()
    {
        ObjectPool melee = GetComponent<ObjectPool>();
        ObjectPool[] pools = { rangedPool, chargerPool, shotgunPool, laserPool, tankPool, dronePool, melee };
        float[] weights = { rangedSpawnChance, chargerSpawnChance, shotgunSpawnChance,
                            laserSpawnChance, tankSpawnChance, droneSpawnChance, meleeWeight };

        float total = 0f;
        for (int i = 0; i < pools.Length; i++)
            if (pools[i] != null) total += weights[i];
        if (total <= 0f) return melee;

        float r = Random.value * total;
        for (int i = 0; i < pools.Length; i++)
        {
            if (pools[i] == null) continue;
            if (r < weights[i]) return pools[i];
            r -= weights[i];
        }
        return melee; // 부동소수점 잔여 방어
    }
}
