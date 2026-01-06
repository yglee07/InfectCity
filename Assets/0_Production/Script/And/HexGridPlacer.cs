using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class HexGridPlacer : MonoBehaviour
{
    [Header("Hex Prefab")]
    public GameObject hexPrefab;

    [Header("Grid Size")]
    public int rows = 5;
    public int columns = 5;

    [Header("Hex Size (Bounds 기준)")]
    public float hexWidth = 4.33f;
    public float hexHeight = 5f;

    [Header("Options")]
    public bool clearBeforePlace = true;

    public void PlaceHexGrid()
    {
        if (hexPrefab == null)
        {
            Debug.LogError("Hex Prefab이 지정되지 않았습니다.");
            return;
        }

        if (clearBeforePlace)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
#if UNITY_EDITOR
                DestroyImmediate(transform.GetChild(i).gameObject);
#else
                Destroy(transform.GetChild(i).gameObject);
#endif
            }
        }

        float zStep = hexHeight * 0.75f;
        float halfWidth = hexWidth * 0.5f;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                float xOffset = (row % 2 == 1) ? halfWidth : 0f;

                Vector3 pos = new Vector3(
                    col * hexWidth + xOffset,
                    0f,
                    row * zStep
                );

#if UNITY_EDITOR
                GameObject hex = (GameObject)PrefabUtility.InstantiatePrefab(hexPrefab);
                hex.transform.position = pos;
                hex.transform.SetParent(transform);
                Undo.RegisterCreatedObjectUndo(hex, "Create Hex Tile");
#else
                Instantiate(hexPrefab, pos, Quaternion.identity, transform);
#endif
            }
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(HexGridPlacer))]
public class HexGridPlacerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        HexGridPlacer placer = (HexGridPlacer)target;

        GUILayout.Space(10);

        if (GUILayout.Button("Place Hex Grid"))
        {
            placer.PlaceHexGrid();
        }
    }
}
#endif
