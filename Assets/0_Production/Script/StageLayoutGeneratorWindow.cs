#if UNITY_EDITOR
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
        Debug.Log("===== STAGE LAYOUT GENERATE START =====");

        if (string.IsNullOrEmpty(jsonText))
        {
            Debug.LogError("❌ JSON is empty");
            return;
        }

        Debug.Log("📄 Raw JSON:");
        Debug.Log(jsonText);

        StageLayoutData data = null;

        try
        {
            data = JsonUtility.FromJson<StageLayoutData>(jsonText);
        }
        catch (System.Exception e)
        {
            Debug.LogError("❌ JSON parse exception");
            Debug.LogException(e);
            return;
        }

        if (data == null)
        {
            Debug.LogError("❌ Parsed data is NULL (JsonUtility silently failed)");
            return;
        }

        Debug.Log("✅ JSON parsed successfully");

        // origin / unit
        Debug.Log($"origin = {data.origin?.ToVector3()}");
        Debug.Log($"unitSize = {data.unitSize}");

        if (data.groups == null)
        {
            Debug.LogError("❌ data.groups is NULL (field name mismatch?)");
            return;
        }

        Debug.Log($"groups count = {data.groups.Count}");

        if (data.groups.Count == 0)
        {
            Debug.LogWarning("⚠️ groups is EMPTY (nothing to generate)");
            return;
        }

        Debug.Log(parentRoot == null
            ? "⚠️ parentRoot is NULL (objects will spawn at scene root)"
            : $"✅ parentRoot = {parentRoot.name}");

        Vector3 origin =
      parentRoot != null
      ? parentRoot.position
      : data.origin.ToVector3();
        float unit = data.unitSize;

        int spawnCount = 0;

        foreach (var group in data.groups)
        {
            if (group == null)
            {
                Debug.LogError("❌ group is NULL");
                continue;
            }

            Debug.Log($"--- GROUP START ---");
            Debug.Log($"type = {group.type}, pattern = {group.pattern}");

            GameObject prefab = FindPrefab(group.type);
            if (prefab == null)
            {
                Debug.LogError($"❌ Prefab NOT FOUND for type: {group.type}");
                continue;
            }

            Debug.Log($"✅ Prefab found: {prefab.name}");

            if (group.pattern == "Rect")
            {
                Debug.Log($"Rect: rows={group.rows}, cols={group.cols}, spacing={group.spacing}");
                Debug.Log($"start = {group.start?.ToVector3()}");

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
                        spawnCount++;
                    }
                }
            }
            else if (group.pattern == "Line")
            {
                Vector3 dir = group.dir == "Z" ? Vector3.forward : Vector3.right;
                Debug.Log($"Line: count={group.count}, spacing={group.spacing}, dir={group.dir}");

                for (int i = 0; i < group.count; i++)
                {
                    Vector3 pos = origin +
                        new Vector3(group.start.x, 0, group.start.z) +
                        dir * group.spacing * i;

                    Spawn(prefab, pos);
                    spawnCount++;
                }
            }
            else if (group.pattern == "Single")
            {
                Vector3 pos = origin + group.pos.ToVector3();
                Debug.Log($"Single at {pos}");

                Spawn(prefab, pos);
                spawnCount++;
            }
            else
            {
                Debug.LogError($"❌ Unknown pattern: {group.pattern}");
            }

            Debug.Log($"--- GROUP END ---");
        }

        Debug.Log($"===== GENERATE END | spawned {spawnCount} objects =====");
    }

    void Spawn(GameObject prefab, Vector3 pos)
    {
        if (prefab == null)
        {
            Debug.LogError("❌ Spawn called with NULL prefab");
            return;
        }

        GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        obj.transform.position = pos;

        if (parentRoot != null)
            obj.transform.SetParent(parentRoot);

        Undo.RegisterCreatedObjectUndo(obj, "Spawn Stage Unit");

        Debug.Log($"🟢 Spawned {prefab.name} at {pos}");
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
#endif