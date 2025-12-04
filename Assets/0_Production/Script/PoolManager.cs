using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance;

    [Header("Pool Items (Inspector 등록)")]
    public List<PoolItem> poolItems = new List<PoolItem>();

    private Dictionary<string, Queue<GameObject>> pools = new();
    private Dictionary<string, GameObject> prefabDict = new();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        InitializePools();
    }

    // --------------- 초기 풀 생성 -------------------
    void InitializePools()
    {
        foreach (var item in poolItems)
        {
            if (prefabDict.ContainsKey(item.key))
            {
                Debug.LogWarning($"Duplicate pool key detected: {item.key}");
                continue;
            }

            prefabDict[item.key] = item.prefab;
            pools[item.key] = new Queue<GameObject>();

            for (int i = 0; i < item.preloadCount; i++)
                CreateInstance(item.key);
        }
    }

    // --------------- 인스턴스 생성 -------------------
    private GameObject CreateInstance(string key)
    {
        if (!prefabDict.ContainsKey(key))
        {
            Debug.LogError($"PoolManager: No prefab registered for key '{key}'");
            return null;
        }

        GameObject obj = Instantiate(prefabDict[key]);
        obj.SetActive(false);
        pools[key].Enqueue(obj);
        return obj;
    }

    // --------------- Spawn -------------------
    public GameObject Spawn(string key, Vector3 pos, Quaternion rot)
    {
        if (!pools.ContainsKey(key))
        {
            Debug.LogError($"PoolManager: No pool found for key '{key}'");
            return null;
        }

        if (pools[key].Count == 0)
        {
            //  자동 확장
            CreateInstance(key);
        }

        GameObject obj = pools[key].Dequeue();

        obj.transform.position = pos;
        obj.transform.rotation = rot;

        obj.SetActive(true);

        // NavMeshAgent 초기화가 필요한 오브젝트라면?
        if (obj.TryGetComponent<NavMeshAgent>(out var agent))
        {
            agent.enabled = false;
            agent.enabled = true;
        }

        return obj;
    }

    // --------------- Return -------------------
    public void Despawn(string key, GameObject obj)
    {
        obj.SetActive(false);
        pools[key].Enqueue(obj);
    }
}
[System.Serializable]
public class PoolItem
{
    public string key;                  // "Zombie" 같은 이름
    public GameObject prefab;           // 풀링할 프리팹
    public int preloadCount = 10;       // 초기 생성할 갯수
}
