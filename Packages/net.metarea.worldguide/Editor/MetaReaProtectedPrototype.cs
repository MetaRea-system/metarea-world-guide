using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEditor;



// Editor-only guard for the unified board.
// NotEditable is deliberately used without HideInHierarchy or DontSave flags.
[InitializeOnLoad]
public static class MetaReaProtectedPrototype
{
    public static bool IsProtectedPath(string path)
    {
        return path == "Packages/net.metarea.worldguide/Runtime/MetaRea World GUIDE.prefab";
    }
    static bool queued;

    static MetaReaProtectedPrototype()
    {
        EditorApplication.hierarchyChanged += Schedule;
        Undo.undoRedoPerformed += Schedule;
        EditorSceneManager.sceneOpened += (scene, mode) => Schedule();
        PrefabStage.prefabStageOpened += stage => Schedule();
        EditorApplication.playModeStateChanged += state => Schedule();
        Schedule();
    }
    static void Schedule()
    {
        if (queued) return;
        queued = true;
        EditorApplication.delayCall += Refresh;
    }
    public static void Refresh()
    {
        queued = false;
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        foreach (var guide in Resources.FindObjectsOfTypeAll<MetaReaWallGuide>())
        {
            if (EditorUtility.IsPersistent(guide) || !guide.gameObject.scene.IsValid()) continue;
            var source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(guide.gameObject);
            var stage = PrefabStageUtility.GetPrefabStage(guide.gameObject);
            if ((source != null && IsProtectedPath(AssetDatabase.GetAssetPath(source))) ||
                (stage != null && IsProtectedPath(stage.assetPath) && stage.prefabContentsRoot == guide.gameObject))
                Protect(guide);
        }
    }
    public static void Protect(MetaReaWallGuide guide)
    {
        foreach (var t in guide.GetComponentsInChildren<Transform>(true))
        {
            if (t == guide.transform || t == guide.portalAnchor ||
                (guide.portalAnchor != null && t.IsChildOf(guide.portalAnchor)) ||
                (guide.favoritesSettings != null && (t == guide.favoritesSettings.transform || t.IsChildOf(guide.favoritesSettings.transform)))) continue;
            if ((t.gameObject.hideFlags & HideFlags.NotEditable) == 0)
                t.gameObject.hideFlags |= HideFlags.NotEditable;
            foreach (var c in t.GetComponents<Component>())
                if (c != null && (c.hideFlags & HideFlags.NotEditable) == 0)
                    c.hideFlags |= HideFlags.NotEditable;
        }
    }
}
