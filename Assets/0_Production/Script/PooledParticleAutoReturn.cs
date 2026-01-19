using UnityEngine;

public class PooledParticleAutoReturn : MonoBehaviour
{
    [SerializeField] string poolKey;
    ParticleSystem ps;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
    }

    void OnEnable()
    {
        if (ps != null)
            ps.Play();
    }

    void Update()
    {
        if (ps != null && !ps.IsAlive(true))
        {
            PoolManager.Instance.Despawn(poolKey, gameObject);
        }
    }
}
