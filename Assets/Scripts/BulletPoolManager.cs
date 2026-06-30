using System.Collections.Generic;
using UnityEngine;

public class BulletPoolManager : MonoBehaviour
{
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
        newPool.Initialize(); // 풀 채우는 로직을 Start()에서 빼서 직접 호출 (아래 설명)

        pools[bulletPrefab] = newPool;
        return newPool;
    }
}