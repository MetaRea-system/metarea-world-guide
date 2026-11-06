using TMPro;
using UnityEditor;
using UnityEngine;

// Scene-view icons are editor preferences, not prefab or runtime data.
[InitializeOnLoad]
public static class MetaReaTextIcons
{
    static string PreferenceKey => "MetaRea.WorldGuide.TextIconsInitialized." + Application.dataPath;
    static MetaReaTextIcons() { EditorApplication.delayCall += Initialize; EditorApplication.hierarchyChanged += Initialize; }
    public static void Initialize()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        { EditorApplication.delayCall += Initialize; return; }
        if (EditorPrefs.GetBool(PreferenceKey, false)) { EditorApplication.hierarchyChanged -= Initialize; return; }
        SetVisible(false);
    }
    [MenuItem("Tools/MetaRea World GUIDE/Scene表示/Tアイコンを非表示")]
    public static void Hide() { SetVisible(false); }
    [MenuItem("Tools/MetaRea World GUIDE/Scene表示/Tアイコンを表示")]
    public static void Show() { SetVisible(true); }
    static void SetVisible(bool visible)
    {
        if (!GizmoUtility.TryGetGizmoInfo(typeof(TextMeshProUGUI), out var info)) return;
        GizmoUtility.SetIconEnabled(typeof(TextMeshProUGUI), visible);
        EditorPrefs.SetBool(PreferenceKey, true);
        SceneView.RepaintAll();
    }
}

