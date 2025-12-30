using System.Collections.Generic;
using UnityEngine;

public class Level : MonoBehaviour
{
    
    [Header("Difficulty")]
    public LevelDifficulty difficulty = LevelDifficulty.Normal;
    public List<Breakable> Breakables { get; private set; }

    
    [Header("Camera")]
    public Transform startCameraPoint;   // ⭐ 여기만 사용
    public Transform endCameraPoint;
    [Header("Camera Zoom")]
    public float startZoom = 15f;
    public float endZoom = 15f;

#if UNITY_EDITOR
    private void OnValidate()
    {
        Debug.Log($"[Level OnValidate] {name} startZoom={startZoom}, endZoom={endZoom}");
    }
#endif

  
    void Awake()
    {

        Debug.Log($"[Level Awake] {name} startZoom={startZoom}, endZoom={endZoom}");
        Breakables = new List<Breakable>(GetComponentsInChildren<Breakable>());
    }

    public Breakable FindBreakableNear(Vector3 point, float radius)
    {
        Breakable best = null;
        float bestDist = float.MaxValue;

        foreach (var b in Breakables)
        {
            float d = Vector3.Distance(point, b.transform.position);
            if (d < radius && d < bestDist)
            {
                bestDist = d;
                best = b;
            }
        }

        return best;
    }
}

public enum LevelDifficulty
{
    Normal,
    Hard,
    VeryHard
}