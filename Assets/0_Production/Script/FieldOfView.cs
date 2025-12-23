using System.Collections.Generic;
using UnityEngine;
public class FieldOfView : MonoBehaviour
{
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    [Header("Colors")]
    public Color normalColor = new Color(0f, 0f, 0f, 0.25f); // 검정
    public Color alertColor  = new Color(1f, 0.3f, 0.4f, 0.35f);
bool isAlert = false;

    public void BuildMesh(float range, float angle)
    {
        Mesh mesh = new Mesh();

        int segments = Mathf.Max(6, Mathf.RoundToInt(angle / 5f));
        float halfAngle = angle * 0.5f;

        List<Vector3> verts = new();
        List<int> tris = new();

        verts.Add(Vector3.zero); // 중심

        for (int i = 0; i <= segments; i++)
        {
            float a = -halfAngle + (angle * i / segments);
            Vector3 dir = Quaternion.Euler(0, a, 0) * Vector3.forward;
            verts.Add(dir * range);
        }

        for (int i = 1; i < verts.Count - 1; i++)
        {
            tris.Add(0);
            tris.Add(i);
            tris.Add(i + 1);
        }

        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
    }
        public void SetAlert(bool alert)
    {
        if (isAlert == alert) return; // ⭐ 변화 없으면 무시
    isAlert = alert;
    
        if (meshRenderer == null) return;
        meshRenderer.material.color = alert ? alertColor : normalColor;
    }

}
