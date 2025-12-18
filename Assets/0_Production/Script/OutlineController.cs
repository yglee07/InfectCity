using UnityEngine;

public class OutlineController : MonoBehaviour
{

    [SerializeField] private Renderer targetRenderer; // Mesh or Skinned 공통
    [SerializeField]
    private Material outlineMat;

    [SerializeField]
    private Color normalColor; // 기본 머티리얼에서 자동 추출됨

    public Color highlightColor = Color.red;

    void Awake()
    {
      
        if (targetRenderer == null)
        {
            Debug.LogWarning($"[OutlineController] Renderer not found on {name}");
            enabled = false;
            return;
        }
        // Materials[1] = OutlineOnly Material
        outlineMat = targetRenderer.materials[1];

        // ★ 초기 OutlineColor → normalColor로 자동 저장
        normalColor = outlineMat.GetColor("_OutlineColor");
    }

    public void SetHighlight(bool enable)
    {
        outlineMat.SetColor("_OutlineColor", enable ? highlightColor : normalColor);
    }
}
