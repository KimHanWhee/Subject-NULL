using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject player;
    public float spawnTerm = 5;
    public float fasterEnemySpawn = 0.05f;
    public float minSpawnTerm = 1;
    public TextMeshProUGUI scoreText;

    [Header("Ranged Enemy")] // Design Ref: §3.2 — 원거리 적 스폰
    public ObjectPool rangedPool;                   // 원거리 적 전용 풀 (자식 오브젝트의 ObjectPool)
    [Range(0f, 1f)] public float rangedSpawnChance = 0.4f; // 원거리 적 비율

    [Header("Charger Enemy")] // 돌진형 적(개구리) 스폰
    public ObjectPool chargerPool;                  // 돌진형 적 전용 풀 (자식 오브젝트의 ObjectPool)
    [Range(0f, 1f)] public float chargerSpawnChance = 0.25f; // 돌진형 적 비율 (원거리 판정 후 잔여에서)

    [Header("Shotgun Enemy")] // 산탄 박쥐(보라) 스폰 — RangedEnemyController 산탄 변형
    public ObjectPool shotgunPool;                  // 산탄 적 전용 풀 (자식 오브젝트의 ObjectPool)
    [Range(0f, 1f)] public float shotgunSpawnChance = 0.2f; // 산탄 적 비율 (원거리·돌진 판정 후 잔여에서)

    [Header("Laser Enemy")] // 레이저 박쥐(초록) 스폰
    public ObjectPool laserPool;                    // 레이저 적 전용 풀 (자식 오브젝트의 ObjectPool)
    [Range(0f, 1f)] public float laserSpawnChance = 0.15f; // 레이저 적 비율 (앞선 판정 후 잔여에서)

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

        // Design Ref: §3.2 — 가중 랜덤으로 근접/원거리/돌진/산탄/레이저 선택 (풀 미할당 시 해당 타입 스킵)
        bool ranged = rangedPool != null && Random.value < rangedSpawnChance;
        bool charger = !ranged && chargerPool != null && Random.value < chargerSpawnChance;
        bool shotgun = !ranged && !charger && shotgunPool != null && Random.value < shotgunSpawnChance;
        bool laser = !ranged && !charger && !shotgun && laserPool != null && Random.value < laserSpawnChance;
        ObjectPool pool = ranged ? rangedPool : charger ? chargerPool : shotgun ? shotgunPool : laser ? laserPool : GetComponent<ObjectPool>();

        GameObject obj = pool.Get();
        if (obj == null) return; // 풀 고갈 방어
        obj.transform.position = new Vector3(x, y, 0);

        // 타입 분기 불필요 — 모든 적은 EnemyBase.Spawn 공통(새 몬스터도 상속만으로 호환)
        obj.GetComponent<EnemyBase>().Spawn(player);
    }
}
