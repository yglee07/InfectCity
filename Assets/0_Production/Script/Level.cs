using System.Collections.Generic;
using UnityEngine;

public class Level : MonoBehaviour
{
    public List<Breakable> Breakables { get; private set; }

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
