using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    public GameObject prefab;
    public Transform parent;
    public int maxObject = 30;
    List<GameObject> pool;

    public void Initialize()
    {
        pool = new List<GameObject>();

        for (int i = 0; i < maxObject; i++)
        {
            GameObject obj = Instantiate(prefab, parent);
            obj.SetActive(false);
            pool.Add(obj);
        }
    }

    public GameObject Get()
    {
        foreach (GameObject obj in pool )
        {
            if (!obj.activeInHierarchy)
            {
                obj.SetActive(true);
                return obj;
            }
        }
        return null;
    }
    
}