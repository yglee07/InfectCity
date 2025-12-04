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
    private Dictionary<string, Transform> folderDict = new();

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
            // key 중복 방지
            if (prefabDict.ContainsKey(item.key))
            {
                Debug.LogWarning($"Duplicate pool key detected: {item.key}");
                continue;
            }

            // 폴더 생성
            Transform folder = new GameObject(item.key + "_Pool").transform;
            folder.SetParent(this.transform);
            folderDict[item.key] = folder;

            prefabDict[item.key] = item.prefab;
            pools[item.key] = new Queue<GameObject>();

            // preload
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

        // parent 폴더 지정
        obj.transform.SetParent(folderDict[key]);

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
            CreateInstance(key);

        GameObject obj = pools[key].Dequeue();

        obj.transform.position = pos;
        obj.transform.rotation = rot;

        obj.SetActive(true);

        // NavMeshAgent 처리
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
        obj.transform.SetParent(folderDict[key]); // Return 시에도 폴더로 정리
        pools[key].Enqueue(obj);
    }
}

[System.Serializable]
public class PoolItem
{
    public string key;
    public GameObject prefab;
    public int preloadCount = 10;
}
