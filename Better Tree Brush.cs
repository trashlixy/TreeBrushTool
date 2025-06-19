using UnityEngine;
using UnityEditor;

public class TreeBrushTool : EditorWindow
{
    public Terrain terrain;
    public GameObject[] treePrefabs;

    public float brushSize = 10f;
    public float density = 0.05f;
    public Vector2 randomScale = new Vector2(0.8f, 1.2f);

    bool painting = false;
    bool isMouseDown = false;

    [MenuItem("Tools/Tree Brush Tool")]
    public static void ShowWindow()
    {
        GetWindow<TreeBrushTool>("Tree Brush Tool");
    }

    private void OnGUI()
    {
        GUILayout.Label("Tree Brush Settings", EditorStyles.boldLabel);

        terrain = (Terrain)EditorGUILayout.ObjectField("Terrain", terrain, typeof(Terrain), true);

        SerializedObject so = new SerializedObject(this);
        SerializedProperty treePrefabsProp = so.FindProperty("treePrefabs");
        EditorGUILayout.PropertyField(treePrefabsProp, true);
        so.ApplyModifiedProperties();

        brushSize = EditorGUILayout.Slider("Brush Size", brushSize, 1f, 100f);
        density = EditorGUILayout.Slider("Density (trees/m²)", density, 0.001f, 1f);
        randomScale = EditorGUILayout.Vector2Field("Random Scale", randomScale);

        GUILayout.Space(10);

        if (!painting)
        {
            if (GUILayout.Button("Start Painting"))
            {
                SceneView.duringSceneGui += OnSceneGUI;
                painting = true;
            }
        }
        else
        {
            if (GUILayout.Button("Stop Painting"))
            {
                SceneView.duringSceneGui -= OnSceneGUI;
                painting = false;
            }
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        Event e = Event.current;
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.GetComponent<Terrain>() == terrain)
            {
                Handles.color = e.control ? new Color(1, 0, 0, 0.3f) : new Color(0, 1, 0, 0.3f);
                Handles.DrawSolidDisc(hit.point, Vector3.up, brushSize);

                if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
                {
                    isMouseDown = true;
                    ApplyBrush(hit.point, e.control);
                    e.Use();
                }
                else if (e.type == EventType.MouseDrag && isMouseDown && e.button == 0 && !e.alt)
                {
                    ApplyBrush(hit.point, e.control);
                    e.Use();
                }
                else if (e.type == EventType.MouseUp && e.button == 0)
                {
                    isMouseDown = false;
                }
            }
        }
        sceneView.Repaint();
    }

    private void ApplyBrush(Vector3 center, bool isErasing)
    {
        if (isErasing)
        {
            EraseTrees(center);
        }
        else
        {
            PlaceTrees(center);
        }
    }

    private void PlaceTrees(Vector3 center)
    {
        if (treePrefabs.Length == 0)
        {
            Debug.LogWarning("Keine Tree Prefabs zugewiesen.");
            return;
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Tree Brush Placement");
        int undoGroup = Undo.GetCurrentGroup();

        float area = Mathf.PI * brushSize * brushSize;
        int totalTrees = Mathf.RoundToInt(area * density);

        for (int i = 0; i < totalTrees; i++)
        {
            Vector2 randPos = Random.insideUnitCircle * brushSize;
            Vector3 pos = new Vector3(center.x + randPos.x, 0, center.z + randPos.y);
            pos.y = terrain.SampleHeight(pos) + terrain.GetPosition().y;

            int prefabIndex = Random.Range(0, treePrefabs.Length);
            GameObject prefab = treePrefabs[prefabIndex];
            GameObject tree = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

            Undo.RegisterCreatedObjectUndo(tree, "Tree Brush Placement");
            tree.transform.position = pos;

            float scale = Random.Range(randomScale.x, randomScale.y);
            tree.transform.localScale = Vector3.one * scale;
            tree.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
        }

        Undo.CollapseUndoOperations(undoGroup);
    }

    private void EraseTrees(Vector3 center)
    {
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Tree Brush Erase");
        int undoGroup = Undo.GetCurrentGroup();

        GameObject[] allTrees = GameObject.FindObjectsOfType<GameObject>();
        foreach (var obj in allTrees)
        {
            if (obj == null || obj.transform.parent != null) continue;
            if (treePrefabs != null && System.Array.Exists(treePrefabs, prefab => prefab.name == obj.name))
            {
                float distance = Vector3.Distance(center, obj.transform.position);
                if (distance <= brushSize)
                {
                    Undo.DestroyObjectImmediate(obj);
                }
            }
        }

        Undo.CollapseUndoOperations(undoGroup);
    }
}
