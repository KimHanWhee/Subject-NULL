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

    private float score;

    private float timeAfterLastSpawn;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GetComponent<ObjectPool>().Initialize();    // 근접 풀(기존)
        if (rangedPool != null) rangedPool.Initialize(); // 원거리 풀(미할당 시 근접만)
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

        // Design Ref: §3.2 — 가중 랜덤으로 근접/원거리 선택 (rangedPool 미할당 시 근접만)
        bool ranged = rangedPool != null && Random.value < rangedSpawnChance;
        ObjectPool pool = ranged ? rangedPool : GetComponent<ObjectPool>();

        GameObject obj = pool.Get();
        if (obj == null) return; // 풀 고갈 방어
        obj.transform.position = new Vector3(x, y, 0);

        if (ranged)
            obj.GetComponent<RangedEnemyController>().Spawn(player);
        else
            obj.GetComponent<EnemyController>().Spawn(player);
    }
}
