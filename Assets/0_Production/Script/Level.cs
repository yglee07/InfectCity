using System.Collections.Generic;
using UnityEngine;

public class Level : MonoBehaviour
{
    
    [Header("Difficulty")]
    public LevelDifficulty difficulty = LevelDifficulty.Normal;
    public List<Breakable> Breakables { get; private set; }


    

    [Header("Camera Intro Zoom")]
    public Transform startCameraPoint;   // ⭐ 여기만 사용
    public Transform endCameraPoint;
    public float startZoom = 15f;
    public float endZoom = 15f;

    [Header("Camera Move Limit Settings")]
    public BoxCollider cameraBounds;

    [Header("Camera Zoom Limit")]
    public float minZoom = 5f;
    public float maxZoom = 40f;


    void Awake()
    {


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