using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshObstacle))]
public class Breakable : MonoBehaviour
{
    // =========================
    // Base Settings
    // =========================
    [Header("Base Settings")]
    public int maxHP = 10;
    public int currentHP;

    public Vector3 size;
    private NavMeshObstacle obstacle;

    // =========================
    // Break Effect
    // =========================
    [Header("Break Effect Options")]
    public bool useBreakEffect = false;
    public GameObject wholeModel;
    public GameObject parts;
    public float removeAfter = 2f;

    // =========================
    // Hit Feedback (Visual Only)
    // =========================
    [Header("Hit Feedback")]
    public bool useHitScale = true;
    public float hitScaleUp = 1.1f;
    public float hitScaleDown = 0.95f;
    public float hitScaleDuration = 0.08f;

    // =========================
    // Visual Root (IMPORTANT)
    // =========================
    [Header("Visual")]
    public Transform visualRoot;   // ❗ NavMeshObstacle 없는 메쉬 루트

    // =========================
    // Internal
    // =========================
    private Vector3 originalScale;
    private Coroutine hitScaleRoutine;
    private bool isBroken = false;

    // =========================
    // Life Cycle
    // =========================
    void Awake()
    {
        currentHP = maxHP;

        obstacle = GetComponent<NavMeshObstacle>();
        obstacle.carving = true;

        // ❗ 스케일은 visualRoot 기준
        originalScale = visualRoot != null ? visualRoot.localScale : Vector3.one;

        var rend = GetComponentInChildren<Renderer>();
        size = rend != null ? rend.bounds.size : Vector3.one;

        if (parts != null)
            parts.SetActive(false);
    }

    // =========================
    // Damage
    // =========================
    public void TakeDamage(int dmg)
    {
        if (isBroken) return;

        currentHP -= dmg;

        if (useHitScale)
            PlayHitScale();

        if (currentHP <= 0)
            Break();
    }

    // =========================
    // Hit Scale (Visual Only)
    // =========================
    void PlayHitScale()
    {
        if (isBroken) return;
        if (visualRoot == null) return;
        if (!gameObject.activeInHierarchy) return;

        if (hitScaleRoutine != null)
            StopCoroutine(hitScaleRoutine);

        hitScaleRoutine = StartCoroutine(HitScaleRoutine());
    }

    IEnumerator HitScaleRoutine()
    {
        if (visualRoot == null)
            yield break;

        // 1️⃣ 살짝 커짐
        visualRoot.localScale = originalScale * hitScaleUp;
        yield return new WaitForSeconds(hitScaleDuration);

        // 2️⃣ 눌림
        visualRoot.localScale = originalScale * hitScaleDown;
        yield return new WaitForSeconds(hitScaleDuration);

        // 3️⃣ 복귀
        visualRoot.localScale = originalScale;
    }

    // =========================
    // Break
    // =========================
    private void Break()
    {
        if (isBroken) return;
        isBroken = true;

        if (obstacle != null)
            obstacle.enabled = false;   // 🔓 NavMesh 통과 허용

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

            // 거의 수평 방향으로만 파편 이동
            Vector3 dir = (rb.transform.position - transform.position).normalized;
            dir.y = Random.Range(0.0f, 0.05f);

            float power = Random.Range(0.5f, 1f);

            rb.mass = 1.5f;
            rb.linearDamping = 0.4f;
            rb.angularDamping = 0.4f;

            rb.AddForce(dir * power, ForceMode.Impulse);
            rb.AddTorque(
                Random.insideUnitSphere * Random.Range(0.05f, 0.2f),
                ForceMode.Impulse
            );
        }

        // 필요하면 일정 시간 후 제거
        // Destroy(gameObject, removeAfter);
    }

    // =========================
    // Debug
    // =========================
    public bool IsBroken => isBroken;
}
