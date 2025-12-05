using System.Collections.Generic;
using UnityEngine;

public class NPCManager : MonoBehaviour
{
    public static NPCManager Instance;

    public List<CitizenNavMesh> Citizens = new List<CitizenNavMesh>();
    public List<ZombieNavMesh> Zombies = new List<ZombieNavMesh>();

    public GameObject zombiePrefab;

    [Header("Mutant Settings")]
    [Range(0f, 1f)]
    public float mutantChance = 0.1f; // 10% 확률

    void Awake()
    {
        Instance = this;
    }


    // ======================
    //   REGISTER / UNREGISTER
    // ======================
    public void RegisterCitizen(CitizenNavMesh citizen)
    {
        if (!Citizens.Contains(citizen))
            Citizens.Add(citizen);
    }

    public void UnregisterCitizen(CitizenNavMesh citizen)
    {
        if (Citizens.Contains(citizen))
            Citizens.Remove(citizen);
    }

    public void RegisterZombie(ZombieNavMesh zombie)
    {
        if (!Zombies.Contains(zombie))
            Zombies.Add(zombie);
    }

    public void UnregisterZombie(ZombieNavMesh zombie)
    {
        if (Zombies.Contains(zombie))
            Zombies.Remove(zombie);
    }



    public string GetZombiePoolKey()
    {
        return (Random.value < mutantChance) ? "Mutant" : "Zombie";
    }
    // ======================
    //     CLEAR / PROGRESS
    // ======================
    public bool IsStageClear()
    {
        return Citizens.Count == 0;
    }

    public float InfectionProgress
    {
        get
        {
            int total = Citizens.Count + Zombies.Count;
            return (total == 0) ? 0f : (float)Zombies.Count / total;
        }
    }

    public int CurrentCitizenCount => Citizens.Count;
    public int CurrentZombieCount => Zombies.Count;
}
