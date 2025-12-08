using UnityEngine;

public class OutlineController : MonoBehaviour
{
    [SerializeField]
    private SkinnedMeshRenderer rend;
    [SerializeField]
    private Material outlineMat;

    [SerializeField]
    private Color normalColor; // 기본 머티리얼에서 자동 추출됨

    public Color highlightColor = Color.red;

    void Awake()
    {
        rend = GetComponentInChildren<SkinnedMeshRenderer>();

        // Materials[1] = OutlineOnly Material
        outlineMat = rend.materials[1];

        // ★ 초기 OutlineColor → normalColor로 자동 저장
        normalColor = outlineMat.GetColor("_OutlineColor");
    }

    public void SetHighlight(bool enable)
    {
        outlineMat.SetColor("_OutlineColor", enable ? highlightColor : normalColor);
    }
}
