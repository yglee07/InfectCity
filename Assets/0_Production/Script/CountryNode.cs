using Linework.SurfaceFill;
using System.Collections;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class CountryNode : MonoBehaviour
{
    [Header("Identity")]
    public string countryId;
    public Transform center;
    public MeshRenderer countryMesh;

    [Header("Conquer Settings")]
    public int stagesToConquer = 3;
    
    [Header("UI")]
    public Sprite countrySprite; // 국기 or 국가 이미지

   
    

    int currentStep;
    int totalSteps;

    public System.Action<float> OnConquerProgressChanged;
    public System.Action OnConquerAnimationFinished;
    [HideInInspector]
    public Vector3 baseLocalPos;
    // ===============================
    // Init
    // ===============================
    void Awake()
    {
        Bind();
        //mat = countryMesh.material;
        //mat.color = baseColor;
        //ApplyWorldBoundsToFillMaterial();
        fillMPB = new MaterialPropertyBlock();

        Bounds b = countryMesh.bounds;
        fillMPB.SetFloat("_WorldMinX", b.min.x);
        fillMPB.SetFloat("_WorldMaxX", b.max.x);
        fillMPB.SetFloat("_Fill", 0f);

        countryMesh.SetPropertyBlock(fillMPB, fillMaterialIndex);

        baseLocalPos = transform.localPosition;
    }
    [ContextMenu("Bind CountryNode")]
    void Bind()
    {
        countryId = gameObject.name;


        center = gameObject.transform;
        countryMesh = GetComponent<MeshRenderer>();
        //Transform countryTf = transform.Find("Country");
        //if (countryTf == null)
        //{
        //    Debug.LogError($"[CountryNode] Country object missing on {name}");
        //    return;
        //}

        //countryMesh = countryTf.GetComponent<MeshRenderer>();
        //if (countryMesh == null)
        //    Debug.LogError($"[CountryNode] MeshRenderer missing on Country in {name}");
    }

    // ===============================
    // Camera Helper
    // ===============================
    public float GetSuggestedZoom(float padding = 1.2f)
    {
        if (countryMesh == null)
        {
            Debug.LogError("getSuggestedZoom: countryMesh is null");
            return 22f;
        }
         

        Bounds b = countryMesh.bounds;

        float halfHeight = b.size.y * 0.5f;
        float halfWidth = b.size.x * 0.5f;

        float aspect = (float)Screen.width / Screen.height;
        float sizeByWidth = halfWidth / aspect;

        float size = Mathf.Max(halfHeight, sizeByWidth);
        return size * padding;
    }

  

    
    // ===============================
    // 즉시 상태 반영 (로비 진입용)
    // ===============================
    public void ApplyInstantProgress(int cleared)
    {
        float t = Mathf.Clamp01((float)cleared / stagesToConquer);
        SetFill(t);
    }
    float startFill;
    float targetFill;
    public void PrepareConquerStepAnimation(int beforeCleared, int afterCleared, int stepCount)
    {
        totalSteps = Mathf.Max(1, stepCount);
        currentStep = 0;

        startFill = Mathf.Clamp01((float)beforeCleared / stagesToConquer);
        targetFill = Mathf.Clamp01((float)afterCleared / stagesToConquer);

        SetFill(startFill);
    }

    public void OnZombieExplode()
    {
        if (currentStep >= totalSteps)
            return;

        currentStep++;

        float stepT = (float)currentStep / totalSteps;
        float fill = Mathf.Lerp(startFill, targetFill, stepT);

        SetFill(fill);
   
        if (currentStep >= totalSteps)
            OnConquerAnimationFinished?.Invoke();
    }

 
  
    MaterialPropertyBlock fillMPB;

    [SerializeField]
    int fillMaterialIndex = 1; // CountryMaskFill
    //[ContextMenu("FillMaterial")]
    //void ApplyWorldBoundsToFillMaterial()
    //{
    //    if (countryMesh == null)
    //        return;

    //    var mats = countryMesh.materials;

    //    if (fillMaterialIndex < 0 || fillMaterialIndex >= mats.Length)
    //    {
    //        Debug.LogWarning(
    //            $"[CountryNode] Invalid material index {fillMaterialIndex} on {name}"
    //        );
    //        return;
    //    }

    //    Material mat = mats[fillMaterialIndex];
    //    Bounds b = countryMesh.bounds;

    //    mat.SetFloat("_WorldMinX", b.min.x);
    //    mat.SetFloat("_WorldMaxX", b.max.x);

    //    Debug.Log(
    //        $"[CountryNode] {countryId} Bounds applied " +
    //        $"MinX={b.min.x:F2}, MaxX={b.max.x:F2}"
    //    );
    //}
    void SetFill(float t)
    {
        t = Mathf.Clamp01(t);
        fillMPB.SetFloat("_Fill", t);
        countryMesh.SetPropertyBlock(fillMPB, fillMaterialIndex);

        OnConquerProgressChanged?.Invoke(t);
    }
}
