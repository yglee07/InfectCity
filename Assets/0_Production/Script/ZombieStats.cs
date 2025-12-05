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

    [Header("Animation")]
    public float animSpeed = 1f;
}
