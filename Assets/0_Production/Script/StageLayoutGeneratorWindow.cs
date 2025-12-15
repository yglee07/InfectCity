using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class StageLayoutGeneratorWindow : EditorWindow
{
    [TextArea(10, 30)]
    public string jsonText;

    [System.Serializable]
    public class PrefabMap
    {
        public string type;
        public GameObject prefab;
    }

    public List<PrefabMap> prefabMaps = new List<PrefabMap>();
    public Transform parentRoot;

    [MenuItem("Tools/Stage Layout Generator")]
    static void Open()
    {
        GetWindow<StageLayoutGeneratorWindow>("Stage Layout Generator");
    }

    void OnGUI()
    {
        GUILayout.Label("Stage Layout JSON", EditorStyles.boldLabel);
        jsonText = EditorGUILayout.TextArea(jsonText, GUILayout.Height(200));

        GUILayout.Space(10);
        GUILayout.Label("Prefab Mapping", EditorStyles.boldLabel);

        int removeIndex = -1;
        for (int i = 0; i < prefabMaps.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            prefabMaps[i].type = EditorGUILayout.TextField(prefabMaps[i].type, GUILayout.Width(120));
            prefabMaps[i].prefab = (GameObject)EditorGUILayout.ObjectField(prefabMaps[i].prefab, typeof(GameObject), false);
            if (GUILayout.Button("X", GUILayout.Width(20)))
                removeIndex = i;
            EditorGUILayout.EndHorizontal();
        }

        if (removeIndex >= 0)
            prefabMaps.RemoveAt(removeIndex);

        if (GUILayout.Button("+ Add Prefab Map"))
            prefabMaps.Add(new PrefabMap());

        GUILayout.Space(10);
        parentRoot = (Transform)EditorGUILayout.ObjectField("Parent Root", parentRoot, typeof(Transform), true);

        GUILayout.Space(20);
        if (GUILayout.Button("Generate Stage"))
        {
            Generate();
        }
    }

    void Generate()
    {
        if (string.IsNullOrEmpty(jsonText))
        {
            Debug.LogError("JSON is empty");
            return;
        }

        StageLayoutData data = JsonUtility.FromJson<StageLayoutData>(jsonText);
        if (data == null)
        {
            Debug.LogError("Failed to parse JSON");
            return;
        }

        Vector3 origin = data.origin.ToVector3();
        float unit = data.unitSize;

        foreach (var group in data.groups)
        {
            GameObject prefab = FindPrefab(group.type);
            if (prefab == null)
            {
                Debug.LogError($"Prefab not found for type: {group.type}");
                continue;
            }

            if (group.pattern == "Rect")
            {
                for (int r = 0; r < group.rows; r++)
                {
                    for (int c = 0; c < group.cols; c++)
                    {
                        Vector3 pos = origin +
                            new Vector3(
                                (group.start.x + c * group.spacing) * unit,
                                0,
                                (group.start.z + r * group.spacing) * unit
                            );

                        Spawn(prefab, pos);
                    }
                }
            }
            else if (group.pattern == "Line")
            {
                Vector3 dir = group.dir == "Z" ? Vector3.forward : Vector3.right;

                for (int i = 0; i < group.count; i++)
                {
                    Vector3 pos = origin +
                        new Vector3(group.start.x, 0, group.start.z) +
                        dir * group.spacing * i;

                    Spawn(prefab, pos);
                }
            }
            else if (group.pattern == "Single")
            {
                Vector3 pos = origin + group.pos.ToVector3();
                Spawn(prefab, pos);
            }
        }
    }

    void Spawn(GameObject prefab, Vector3 pos)
    {
        GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        obj.transform.position = pos;
        if (parentRoot != null)
            obj.transform.SetParent(parentRoot);
        Undo.RegisterCreatedObjectUndo(obj, "Spawn Stage Unit");
    }

    GameObject FindPrefab(string type)
    {
        foreach (var m in prefabMaps)
        {
            if (m.type == type)
                return m.prefab;
        }
        return null;
    }
}
