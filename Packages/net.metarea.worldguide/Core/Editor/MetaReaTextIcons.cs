using TMPro;
using UnityEditor;
using UnityEngine;

// Wait for an actual Scene GUI pass: annotations may not exist at domain load.
[InitializeOnLoad]
public static class MetaReaTextIcons
{
    static string VisibilityKey => "MetaRea.WorldGuide.TextIconsVisible." + Application.dataPath;
    static bool sceneReady;
    static int confirmed;
    static double nextCheck;
    static MetaReaTextIcons() { Initialize(); }
    public static void Initialize()
    {
        sceneReady = false;
        confirmed = 0;
        nextCheck = 0;
        EditorApplication.update -= ApplyWhenReady;
        EditorApplication.update += ApplyWhenReady;
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;
        SceneView.RepaintAll();
    }
    static void OnSceneGUI(SceneView view)
    {
        if (Event.current.type == EventType.Repaint) sceneReady = true;
    }
    static void ApplyWhenReady()
    {
        if (!sceneReady || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (EditorApplication.timeSinceStartup < nextCheck) return;
        nextCheck = EditorApplication.timeSinceStartup + 0.25;
        if (!GizmoUtility.TryGetGizmoInfo(typeof(TextMeshProUGUI), out var info)) return;
        bool visible = EditorPrefs.GetBool(VisibilityKey, false);
        if (info.iconEnabled != visible)
        {
            confirmed = 0;
            GizmoUtility.SetIconEnabled(typeof(TextMeshProUGUI), visible);
            SceneView.RepaintAll();
            return;
        }
        // Verify on later editor ticks rather than recording success at dispatch.
        if (++confirmed < 2) return;
        EditorApplication.update -= ApplyWhenReady;
        SceneView.duringSceneGui -= OnSceneGUI;
    }
    [MenuItem("Tools/MetaRea World GUIDE/Scene表示/Tアイコンを非表示")]
    public static void Hide() { SetVisible(false); }
    [MenuItem("Tools/MetaRea World GUIDE/Scene表示/Tアイコンを表示")]
    public static void Show() { SetVisible(true); }
    static void SetVisible(bool visible)
    {
        EditorPrefs.SetBool(VisibilityKey, visible);
        if (GizmoUtility.TryGetGizmoInfo(typeof(TextMeshProUGUI), out var info))
            GizmoUtility.SetIconEnabled(typeof(TextMeshProUGUI), visible);
        Initialize();
    }
}
