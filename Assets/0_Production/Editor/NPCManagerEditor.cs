using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(NPCManager))]
public class NPCManagerEditor : Editor
{
    private NPCManager mgr;

    private bool foldCitizens = true;
    private bool foldGreen = true;
    private bool foldPurple = true;
    private bool foldYellow = true;
    private bool foldDoors = true;

    private string searchFilter = "";

    private enum SortMode { None, Name, ID, Latest }
    private SortMode sortMode = SortMode.None;

    private void OnEnable()
    {
        mgr = (NPCManager)target;
        SceneView.duringSceneGui += DrawSceneLabels;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= DrawSceneLabels;
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.LabelField("NPC Manager", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // --- Mutant chance slider 표시 ---
        EditorGUILayout.LabelField("Mutant Settings", EditorStyles.boldLabel);
        mgr.mutantChance = EditorGUILayout.Slider("Mutant Chance", mgr.mutantChance, 0f, 1f);
        EditorGUILayout.Space();
        // SEARCH
        EditorGUILayout.LabelField("Search");
        searchFilter = EditorGUILayout.TextField(searchFilter);
        EditorGUILayout.Space();

        // SORT
        sortMode = (SortMode)EditorGUILayout.EnumPopup("Sort", sortMode);
        EditorGUILayout.Space();

        // Citizens
        DrawNPCList("Citizens", mgr.Citizens, ref foldCitizens);

        EditorGUILayout.Space();

        // Green Zombies
        DrawNPCList("Green Zombies", mgr.GreenZombies, ref foldGreen);

        EditorGUILayout.Space();

        // Purple Zombies
        DrawNPCList("Purple Zombies", mgr.PurpleZombies, ref foldPurple);

        EditorGUILayout.Space();

        // Yellow Zombies
        DrawNPCList("Yellow Zombies", mgr.YellowZombies, ref foldYellow);

        EditorGUILayout.Space();

        // Doors
        DrawNPCList("Doors", mgr.Doors, ref foldDoors);


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
            SortMode.Latest => filtered.Reverse(),
            _ => filtered
        };

        foreach (var npc in filtered)
        {
            if (npc == null)
            {
                EditorGUILayout.LabelField("NULL (destroyed)");
                continue;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(npc.name);

            if (GUILayout.Button("Select", GUILayout.Width(60)))
                Selection.activeObject = npc;

            EditorGUILayout.EndHorizontal();
        }

        EditorGUI.indentLevel--;
    }

    // ===== Scene View Labels =====
    private void DrawSceneLabels(SceneView sceneView)
    {
        DrawLabels(mgr.Citizens, Color.cyan);
        DrawLabels(mgr.GreenZombies, Color.green);
        DrawLabels(mgr.PurpleZombies, Color.magenta);
        DrawLabels(mgr.YellowZombies, Color.yellow);
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
