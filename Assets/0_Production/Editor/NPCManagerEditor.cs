using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(NPCManager))]
public class NPCManagerEditor : Editor
{
    private NPCManager mgr;

    private bool foldCitizens = true;
    private bool foldZombies = true;

    private string searchFilter = "";

    private enum SortMode { None, Name, ID, Latest }
    private SortMode sortMode = SortMode.None;

    private void OnEnable()
    {
        mgr = (NPCManager)target;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.LabelField("NPC Manager", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // ---------------- SEARCH ----------------
        EditorGUILayout.LabelField("Search");
        searchFilter = EditorGUILayout.TextField(searchFilter);
        EditorGUILayout.Space();

        // ---------------- SORT OPTIONS ----------------
        sortMode = (SortMode)EditorGUILayout.EnumPopup("Sort", sortMode);
        EditorGUILayout.Space();

        // ---------------- CITIZENS ----------------
        DrawNPCList("Citizens", mgr.Citizens, ref foldCitizens);

        EditorGUILayout.Space();

        // ---------------- ZOMBIES ----------------
        DrawNPCList("Zombies", mgr.Zombies, ref foldZombies);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawNPCList<T>(string label, List<T> list, ref bool foldout) where T : Object
    {
        foldout = EditorGUILayout.Foldout(foldout, $"{label} ({list.Count})", true);
        if (!foldout) return;

        EditorGUI.indentLevel++;

        IEnumerable<T> filtered = list;

        // Filter
        if (!string.IsNullOrEmpty(searchFilter))
        {
            string lower = searchFilter.ToLower();
            filtered = filtered.Where(x => x != null && x.name.ToLower().Contains(lower));
        }

        // Sort
        filtered = sortMode switch
        {
            SortMode.Name => filtered.OrderBy(x => x.name),
            SortMode.ID => filtered.OrderBy(x => x.GetInstanceID()),
            SortMode.Latest => filtered.Reverse(),  // 최신 생성된 오브젝트가 리스트 마지막이라고 가정
            _ => filtered
        };

        foreach (var npc in filtered)
        {
            if (npc == null)
            {
                EditorGUILayout.LabelField("NULL (destroyed)");
                continue;
            }

            var comp = npc as Component;

            EditorGUILayout.BeginHorizontal();

            GUIStyle style = new GUIStyle(EditorStyles.label);
   

            EditorGUILayout.LabelField(npc.name, style);

            if (GUILayout.Button("Select", GUILayout.Width(60)))
                Selection.activeObject = npc;

            EditorGUILayout.EndHorizontal();
        }

        EditorGUI.indentLevel--;
    }

    // ---------------- SCENE VIEW LABELS (ID 표시) ----------------
    private void OnSceneGUI(SceneView view)
    {
        DrawLabels(mgr.Citizens, Color.green);
        DrawLabels(mgr.Zombies, Color.red);
    }

    private void DrawLabels<T>(List<T> list, Color color) where T : Object
    {
        GUIStyle style = new GUIStyle();
        style.normal.textColor = color;
        style.fontSize = 12;
        style.alignment = TextAnchor.MiddleCenter;

        foreach (var npc in list)
        {
            if (npc == null) continue;

            Component comp = npc as Component;
            if (comp == null) continue;

            Vector3 pos = comp.transform.position + Vector3.up * 1.5f;
            Handles.Label(pos, comp.name, style);
        }
    }
}
