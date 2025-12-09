using System.Collections.Generic;
using UnityEngine;

public class NPCManager : MonoBehaviour
{
    public static NPCManager Instance;

    public List<CitizenNavMesh> Citizens = new List<CitizenNavMesh>();
    public List<ZombieNavMesh> Zombies = new List<ZombieNavMesh>();

    // 새로 추가: 초록/보라 각각의 리스트 (선택, 없어도 되지만 디버깅 편함)
    public List<ZombieNavMesh> GreenZombies = new List<ZombieNavMesh>();
    public List<ZombieNavMesh> PurpleZombies = new List<ZombieNavMesh>();

    // ★ 초록/보라 감염 수 카운트
    public int greenInfectCount = 0;
    public int purpleInfectCount = 0;
    // ★ 초록/보라 스폰 풀 이름
    public string greenZombiePool = "Zombie";         // 기존 초록 좀비 풀 키
    public string purpleZombiePool = "ZombiePurple";  // 보라 좀비 풀 키 (PoolManager에 반드시 
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

        // ★ 진영별로 등록
        if (zombie.faction == Faction.Green)
        {
            if (!GreenZombies.Contains(zombie))
                GreenZombies.Add(zombie);
        }
        else
        {
            if (!PurpleZombies.Contains(zombie))
                PurpleZombies.Add(zombie);
        }
    }

    public void UnregisterZombie(ZombieNavMesh zombie)
    {
        if (Zombies.Contains(zombie))
            Zombies.Remove(zombie);

        if (GreenZombies.Contains(zombie))
            GreenZombies.Remove(zombie);

        if (PurpleZombies.Contains(zombie))
            PurpleZombies.Remove(zombie);
    }
    public void AddInfectCount(Faction faction)
    {
        if (faction == Faction.Green)
            greenInfectCount++;
        else
            purpleInfectCount++;
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
