using UnityEngine;

public class Barricade : MonoBehaviour
{
    public Vector3 size = new Vector3(2, 1, 1); // 바리게이트 크기
    public int hp = 5;

    public void TakeDamage(int dmg)
    {
        hp -= dmg;
        if (hp <= 0)
            gameObject.SetActive(false);
    }
}