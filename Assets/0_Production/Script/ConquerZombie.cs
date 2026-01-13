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

        // ✅ 무조건 월드 루트
        fx.transform.SetParent(null, true);
        // 💥 폭발 이펙트
        // 🔊 사운드
        // 📈 Country 색 변화 step

        targetNode.OnConquerZombieExploded();

        Die();
    }
    public void Die()
    {
        SoundManager.Instance.PlaySFX("InfectExplode");
        Destroy(gameObject);
    }
}
