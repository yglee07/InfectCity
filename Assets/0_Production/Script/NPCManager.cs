using System.Collections.Generic;
using UnityEngine;

public class NPCManager : MonoBehaviour
{
    public static NPCManager Instance;

    [Header("Active Citizens")]
    public List<CitizenNavMesh> Citizens = new List<CitizenNavMesh>();

    [Header("Active Zombies")]
    public List<ZombieNavMesh> Zombies = new List<ZombieNavMesh>();

    void Awake()
    {
        Instance = this;
    }

    public void RegisterCitizen(CitizenNavMesh c)
    {
        if (!Citizens.Contains(c))
            Citizens.Add(c);
    }

    public void UnregisterCitizen(CitizenNavMesh c)
    {
        Citizens.Remove(c);
    }

    public void RegisterZombie(ZombieNavMesh z)
    {
        if (!Zombies.Contains(z))
            Zombies.Add(z);
    }

    public void UnregisterZombie(ZombieNavMesh z)
    {
        Zombies.Remove(z);
    }
}
