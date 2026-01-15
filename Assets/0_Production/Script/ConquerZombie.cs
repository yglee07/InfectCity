using System.Collections;
using UnityEngine;

public class ConquerZombie : MonoBehaviour
{
    CountryNode targetNode;
    float explodeDelay;

    public void Init(CountryNode node, float delay)
    {
        targetNode = node;
        explodeDelay = delay;
        StartCoroutine(ExplodeRoutine());
    }

    IEnumerator ExplodeRoutine()
    {
        yield return new WaitForSecondsRealtime(explodeDelay);

        GameObject fx = PoolManager.Instance.Spawn(
            "SpikyExplosionGreen",
            transform.position,
            Quaternion.identity
        );

        // 월드 루트에 유지
        fx.transform.SetParent(null, true);

        // 🔥 핵심: Fill 스텝 1회 진행
        if (targetNode != null)
        {
            targetNode.OnZombieExplode();
        }

        Die();
    }

    void Die()
    {
        SoundManager.Instance.PlaySFX("InfectExplode");
        Destroy(gameObject);
    }
}
