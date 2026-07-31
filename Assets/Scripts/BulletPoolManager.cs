using System.Collections.Generic;
using UnityEngine;

public class BulletPoolManager : MonoBehaviour
{
    // 총알 풀 하나당 크기.
    // ObjectPool의 기본값(30)으로는 ♣ Time Stop처럼 총알이 오래 머무는 상황에서 풀이 바닥난다.
    // 풀이 비면 Get()이 null을 반환하고 PlayerController가 발사를 건너뛰어 "쏘다 마는" 증상이 된다.
    // 여기서만 지정한다 — ObjectPool 기본값을 바꾸면 적·이펙트 풀까지 커진다.
    [Tooltip("총알 프리팹별 풀 크기")]
    public int bulletsPerPool = 100;

    private Dictionary<GameObject, ObjectPool> pools = new Dictionary<GameObject, ObjectPool>();

    public ObjectPool GetPool(GameObject bulletPrefab)
    {
        if (pools.ContainsKey(bulletPrefab))
        {
            return pools[bulletPrefab];
        }

        // 처음 쓰는 총알이면 풀을 동적으로 생성
        GameObject poolObj = new GameObject(bulletPrefab.name + "Pool");
        poolObj.transform.parent = this.transform; // 깔끔하게 이 매니저 밑으로 정리

        ObjectPool newPool = poolObj.AddComponent<ObjectPool>();
        newPool.prefab = bulletPrefab;
        newPool.parent = poolObj.transform;
        newPool.maxObject = Mathf.Max(1, bulletsPerPool); // Initialize 전에 지정해야 반영된다
        newPool.Initialize(); // 풀 채우는 로직을 Start()에서 빼서 직접 호출 (아래 설명)

        pools[bulletPrefab] = newPool;
        return newPool;
    }
}