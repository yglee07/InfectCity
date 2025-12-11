using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshObstacle))]
public class Breakable : MonoBehaviour
{
    [Header("Base Settings")]
    public int maxHP = 10;
    public int currentHP;

    public Vector3 size;
    private NavMeshObstacle obstacle;

    [Header("Break Effect Options")]
    public bool useBreakEffect = false;
    public GameObject wholeModel;
    public GameObject parts;
    public float removeAfter = 2f; // 파편 제거 시간

    void Awake()
    {
        currentHP = maxHP;

        obstacle = GetComponent<NavMeshObstacle>();
        obstacle.carving = true;

        // 기존 size 계산 유지
        var rend = GetComponentInChildren<Renderer>();
        size = rend != null ? rend.bounds.size : new Vector3(1, 1, 1);

        if (parts != null)
            parts.SetActive(false);
    }

    public void TakeDamage(int dmg)
    {
        currentHP -= dmg;

        if (currentHP <= 0)
            Break();
    }

    private void Break()
    {
        if (obstacle != null)
            obstacle.enabled = false;

        if (!useBreakEffect)
        {
            gameObject.SetActive(false);
            return;
        }

        PlayBreakEffect();
    }

    private void PlayBreakEffect()
    {
        if (wholeModel != null)
            wholeModel.SetActive(false);

        if (parts == null)
        {
            gameObject.SetActive(false);
            return;
        }

        parts.SetActive(true);

        foreach (Rigidbody rb in parts.GetComponentsInChildren<Rigidbody>())
        {
            if (rb == null) continue;

            // ★ 방향 계산 (거의 수평으로만 밀림)
            Vector3 dir = (rb.transform.position - transform.position).normalized;

            // y 방향 거의 없게 → 하늘로 안 뜸
            dir.y = Random.Range(0.0f, 0.05f);

            // ★ 힘 매우 약하게 (박스 무겁다는 느낌)
            float power = Random.Range(0.5f, 1f);

            // 힘 적용
            rb.AddForce(dir * power, ForceMode.Impulse);

            // ★ 회전도 매우 약함
            rb.AddTorque(
                Random.insideUnitSphere * Random.Range(0.05f, 0.2f),
                ForceMode.Impulse
            );

            // ★ 무게감 부여
            rb.mass = 1.5f;         // 무겁게
            rb.linearDamping = 0.4f;         // 옆으로 잘 안 날아감
            rb.angularDamping = 0.4f;  // 너무 휙휙 돌지 않음
        }
        //// 일정 시간 후 Breakable 삭제
        //Destroy(gameObject, removeAfter);
    }
}
