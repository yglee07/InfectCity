using System.Collections;
using TMPro;
using UnityEngine;

public enum SpawnShape
{
    Grid,
    Line,
    Circle,
    Triangle
}

public class SpawnWave : MonoBehaviour
{
    [Header("Wave Trigger Condition")]
    public float requiredInfectionRatio = 0.6f;
    public bool triggered = false;

    [Header("Spawn Settings")]
    public string poolKey = "Citizen";     // 기본: 시민
    public int[] spawnGroups;              // 예: [2, 2, 5]
    public float spawnDelay = 0.05f;       // 그룹 내 개별 딜레이
    public float groupDelay = 0.5f;        // 그룹 간 딜레이

    [Header("Run Target")]
    public Transform runTarget;

    [Header("Pattern Settings")]
    public SpawnShape spawnShape = SpawnShape.Grid;
    public float spacing = 1.2f;

    void Update()
    {
        if (triggered) return;

        // 웨이브 발동 조건 검사
        float ratio = NPCManager.Instance.InfectionProgress;

        if (ratio >= requiredInfectionRatio)
        {
            triggered = true;
            StartCoroutine(SpawnWaveRoutine());
        }
    }

    IEnumerator SpawnWaveRoutine()
    {
        foreach (int count in spawnGroups)
        {
            yield return SpawnGroup(count);

            // 그룹 간 딜레이
            yield return new WaitForSeconds(groupDelay);
        }

        Debug.Log("[SpawnWave] All groups spawned.");
    }

    IEnumerator SpawnGroup(int count)
    {
        Vector3[] positions = GetSpawnPositions(count);

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = positions[i];

            GameObject obj = PoolManager.Instance.Spawn(poolKey, pos, Quaternion.identity);

            //// 시민 타입 설정
            //CitizenNavMesh citizen = obj.GetComponent<CitizenNavMesh>();
            //if (citizen != null)
            //    citizen.behaviorType = citizenType;

            // 개별 스폰 딜레이
            CitizenBase citizen = obj.GetComponent<CitizenBase>();
            if (citizen != null)
            {
                citizen.RunTo(runTarget.position);
            }

            if (spawnDelay > 0)
                yield return new WaitForSeconds(spawnDelay);
        }
    }

    // ================================================================
    //  패턴별 스폰 위치 계산
    // ================================================================
    Vector3[] GetSpawnPositions(int count)
    {
        switch (spawnShape)
        {
            case SpawnShape.Line: return GetLinePositions(count);
            case SpawnShape.Circle: return GetCirclePositions(count);
            case SpawnShape.Triangle: return GetTrianglePositions(count);
            default: return GetGridPositions(count);
        }
    }

    Vector3[] GetGridPositions(int count)
    {
        int grid = Mathf.CeilToInt(Mathf.Sqrt(count));
        Vector3[] pos = new Vector3[count];
        int index = 0;

        for (int y = 0; y < grid; y++)
        {
            for (int x = 0; x < grid; x++)
            {
                if (index >= count) break;

                float ox = (x - (grid - 1) / 2f) * spacing;
                float oz = (y - (grid - 1) / 2f) * spacing;

                pos[index++] = transform.position + new Vector3(ox, 0, oz);
            }
        }
        return pos;
    }

    Vector3[] GetLinePositions(int count)
    {
        Vector3[] pos = new Vector3[count];
        float start = -(count - 1) / 2f;

        for (int i = 0; i < count; i++)
        {
            float x = (start + i) * spacing;
            pos[i] = transform.position + new Vector3(x, 0, 0);
        }
        return pos;
    }

    Vector3[] GetCirclePositions(int count)
    {
        Vector3[] pos = new Vector3[count];
        float radius = spacing * Mathf.Sqrt(count);

        for (int i = 0; i < count; i++)
        {
            float angle = (360f / count) * i * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;

            pos[i] = transform.position + new Vector3(x, 0, z);
        }
        return pos;
    }

    Vector3[] GetTrianglePositions(int count)
    {
        Vector3[] pos = new Vector3[count];
        int row = 1;
        int index = 0;

        while (index < count)
        {
            int inRow = Mathf.Min(row, count - index);
            float start = -(inRow - 1) / 2f;

            for (int i = 0; i < inRow; i++)
            {
                float x = (start + i) * spacing;
                float z = -(row - 1) * spacing;

                pos[index++] = transform.position + new Vector3(x, 0, z);
            }
            row++;
        }
        return pos;
    }
}
