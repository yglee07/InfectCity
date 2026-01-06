using UnityEngine;

public class CountryNode : MonoBehaviour
{
    public string countryId;
    public Transform center;
    public MeshRenderer countryMesh;
    public enum CountryState
    {
        Normal,
        Selected,
        Conquered
    }

    [SerializeField]
    private CountryState state;

  
    // 🔥 버튼 눌렀을 때만 호출
    [ContextMenu("Bind CountryNode")]
    void BindFromMenu()
    {
        Bind();
    }
    public void Bind()
    {
        // 1️⃣ countryId = GameObject 이름
        countryId = gameObject.name;

        // 2️⃣ Center 찾기
        center = transform.Find("Center");
        if (center == null)
        {
            Debug.LogError($"[CountryNode] Center not found in {name}");
        }

        // 3️⃣ Country Mesh 찾기
        Transform countryTf = transform.Find("Country");
        if (countryTf == null)
        {
            Debug.LogError($"[CountryNode] Country object not found in {name}");
        }
        else
        {
            countryMesh = countryTf.GetComponent<MeshRenderer>();
            if (countryMesh == null)
            {
                Debug.LogError($"[CountryNode] MeshRenderer missing on Country in {name}");
            }
        }
    }
    public float GetSuggestedZoom(float padding = 1.2f)
    {
        if (countryMesh == null)
            return 22f; // fallback

        Bounds b = countryMesh.bounds;

        // OrthographicSize는 "세로 반높이"
        float halfHeight = b.size.y * 0.5f;
        float halfWidth = b.size.x * 0.5f;

        // 화면 비율 고려해서 더 큰 쪽 기준
        float aspect = (float)Screen.width / Screen.height;
        float sizeByWidth = halfWidth / aspect;

        float targetSize = Mathf.Max(halfHeight, sizeByWidth);

        return targetSize * padding;
    }
        public void SetState(
        CountryState newState,
        Material normalMat,
        Material selectedMat,
        Material conqueredMat
    )
        {
            state = newState;

            if (countryMesh == null) return;

            switch (state)
            {
                case CountryState.Conquered:
                    countryMesh.material = conqueredMat;
                    break;

                case CountryState.Selected:
                    countryMesh.material = selectedMat;
                    break;

                default:
                    countryMesh.material = normalMat;
                    break;
            }
        }

}
