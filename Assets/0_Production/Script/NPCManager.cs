using System.Collections.Generic;
using UnityEngine;

public class NPCManager : MonoBehaviour
{
    public static NPCManager Instance;

    public List<CitizenBase> Citizens = new List<CitizenBase>();
    public List<ZombieNavMesh> Zombies = new List<ZombieNavMesh>();

    // 새로 추가: 초록/보라/노랑 각각의 리스트 (선택, 없어도 되지만 디버깅 편함)
    public List<ZombieNavMesh> GreenZombies = new List<ZombieNavMesh>();
    public List<ZombieNavMesh> PurpleZombies = new List<ZombieNavMesh>();
    public List<ZombieNavMesh> YellowZombies = new List<ZombieNavMesh>();
    public List<Door> Doors = new();
    // ★ 초록/보라/노랑 감염 수 카운트
    public int greenInfectCount = 0;
    public int purpleInfectCount = 0;
    public int yellowInfectCount = 0;
    // ★ 초록/보라/노랑 스폰 풀 이름
    public string greenZombiePool = "Zombie";         // 기존 초록 좀비 풀 키
    public string purpleZombiePool = "ZombiePurple";  // 보라 좀비 풀 키 (PoolManager에 반드시
    public string yellowZombiePool = "ZombieYellow";  // 노랑 좀비 풀 키 
    public GameObject zombiePrefab;

    [Header("Mutant Settings")]
    [Range(0f, 1f)]
    public float mutantChance = 0.1f; // 10% 확률

    public bool combatMode = false;
    void Awake()
    {
        Instance = this;
    }


    // ======================
    //   REGISTER / UNREGISTER
    // ======================
     public void RegisterDoor(Door door)
    {
        if (!Doors.Contains(door))
            Doors.Add(door);
    }

    public void UnregisterDoor(Door door)
    {
        Doors.Remove(door);
    }
    public void RegisterCitizen(CitizenBase citizen)
    {
        if (!Citizens.Contains(citizen))
            Citizens.Add(citizen);
    }

    public void UnregisterCitizen(CitizenBase citizen)
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
        else if (zombie.faction == Faction.Purple)
        {
            if (!PurpleZombies.Contains(zombie))
                PurpleZombies.Add(zombie);
        }
        else if (zombie.faction == Faction.Yellow)
        {
            if (!YellowZombies.Contains(zombie))
                YellowZombies.Add(zombie);
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

        if (YellowZombies.Contains(zombie))
            YellowZombies.Remove(zombie);
    }
    public void AddInfectCount(Faction faction)
    {
        if (faction == Faction.Green)
            greenInfectCount++;
        else if (faction == Faction.Purple)
            purpleInfectCount++;
        else if (faction == Faction.Yellow)
            yellowInfectCount++;
    }

    public string GetZombiePoolKey()
    {
        return (Random.value < mutantChance) ? "Mutant" : "Zombie";
    }
    // ======================
    //     CLEAR / PROGRESS
    // ======================
    //public bool IsStageClear()
    //{
    //    return Citizens.Count == 0 && PurpleZombies.Count==0;
    //}

    public float InfectionProgress
    {
        get
        {
            int total = Citizens.Count + Zombies.Count;
            return (total == 0) ? 0f : (float)Zombies.Count / total;
        }
    }
    public ZombieNavMesh FindClosestZombie(Vector3 pos)
    {
        ZombieNavMesh nearest = null;
        float min = float.MaxValue;

        foreach (var z in Zombies) // ← PurpleZombies, GreenZombies 쓰고 싶으면 합쳐도 됨
        {
            if (!z || !z.gameObject.activeInHierarchy) continue;

            float sqr = (z.transform.position - pos).sqrMagnitude;
            if (sqr < min)
            {
                min = sqr;
                nearest = z;
            }
        }

        return nearest;
    }

    public int CurrentCitizenCount => Citizens.Count;
    public int CurrentZombieCount => Zombies.Count;
}
