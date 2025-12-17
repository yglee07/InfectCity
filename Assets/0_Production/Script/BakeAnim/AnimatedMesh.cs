using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class AnimatedMesh : MonoBehaviour
{
    [Header("Baked Animation")]
    [SerializeField] private AnimatedMeshScriptableObject animationSO;
    [SerializeField] private string defaultAnimation = "Idle";
    [SerializeField] private bool playOnAwake = true;

    [Header("Disable On Start (Inspector Assigned)")]
    [SerializeField] private Animator animatorToDisable;
    [SerializeField] private SkinnedMeshRenderer skinnedMeshToDisable;

    private MeshFilter meshFilter;
    private Dictionary<string, List<Mesh>> animationMap;
    private List<Mesh> currentFrames;

    private int frameIndex;
    private float frameInterval;
    private float timer;

    private void Awake()
    {
        // ✅ 인스펙터에 넣어준 것만 정확히 끈다
        if (animatorToDisable != null)
            animatorToDisable.enabled = false;

        if (skinnedMeshToDisable != null)
            skinnedMeshToDisable.enabled = false;

        meshFilter = GetComponent<MeshFilter>();

        animationMap = new Dictionary<string, List<Mesh>>();
        foreach (var anim in animationSO.Animations)
        {
            animationMap[anim.Name] = anim.Meshes;
        }

        frameInterval = 1f / animationSO.AnimationFPS;

        if (playOnAwake)
            Play(defaultAnimation);
    }

    public void Play(string animationName)
    {
        if (!animationMap.TryGetValue(animationName, out currentFrames))
        {
            Debug.LogError($"[AnimatedMesh] Animation '{animationName}' not found on {name}");
            return;
        }

        frameIndex = 0;
        timer = 0f;
        meshFilter.sharedMesh = currentFrames[0];
    }

    private void Update()
    {
        if (currentFrames == null || currentFrames.Count == 0)
            return;

        timer += Time.deltaTime;
        if (timer < frameInterval)
            return;

        timer -= frameInterval;
        frameIndex = (frameIndex + 1) % currentFrames.Count;
        meshFilter.sharedMesh = currentFrames[frameIndex];
    }
}
