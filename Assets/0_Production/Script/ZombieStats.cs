using UnityEngine;

[CreateAssetMenu(fileName = "ZombieStats", menuName = "InfectCity/Zombie Stats")]
public class ZombieStats : ScriptableObject
{
    [Header("Infect")]
    public float infectDistance = 1.2f;

    [Header("Size")]
    public float sizeMultiplier = 1f;

    [Header("Movement")]
    public float walkSpeed = 1.2f;
    public float runSpeed = 4f;
    public float chaseDistance = 7f;

    [Header("Animatio\n애니메이션 스피드 조절은 더이상 지원을 하지 않습니다")]
    //public float animSpeed = 1f;

    [Header("Combat")]
    [Tooltip("좀비 기본 체력. 일반 좀비 = 1, Mutant = 2 추천")]
    public int maxHP = 1;
}
