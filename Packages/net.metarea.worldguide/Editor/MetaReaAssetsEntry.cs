using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// A small variant is the user-facing entry point. Its source and dependencies
// remain in the VCC package so upgrades do not duplicate scripts or reset edits.
[InitializeOnLoad]
public static class MetaReaAssetsEntry
{
    public const string Folder = "Assets/MetaRea World GUIDE";
    public const string PrefabPath = Folder + "/MetaRea World GUIDE.prefab";
    public const string SourcePath = "Packages/net.metarea.worldguide/Runtime/MetaRea World GUIDE.prefab";
    static bool queued;

    static MetaReaAssetsEntry() { Schedule(); }

    static void Schedule()
    {
        if (queued) return;
        queued = true;
        EditorApplication.delayCall += InstallWhenReady;
    }

    static void InstallWhenReady()
    {
        queued = false;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        { Schedule(); return; }
        try { EnsureEntry(true); }
        catch (Exception error) { Debug.LogError("MetaRea World GUIDE: 導入用プレハブを作成できませんでした。MetaReaメニューから再試行してください。\n" + error.Message); }
    }

    [MenuItem("MetaRea/導入用プレハブを表示")]
    public static void ShowEntry()
    {
        EnsureEntry(false);
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (asset == null) return;
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);
    }

    public static bool EnsureEntry(bool revealCreated)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return false;
        // Never overwrite a user's existing prefab, including its GUID/overrides.
        if (File.Exists(PrefabPath)) return false;
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath);
        if (source == null) return false;
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "MetaRea World GUIDE");
        var preview = EditorSceneManager.NewPreviewScene();
        try
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source, preview);
            instance.name = "MetaRea World GUIDE";
            bool success;
            PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath, out success);
            if (!success) throw new IOException("プレハブの保存に失敗しました: " + PrefabPath);
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
        if (revealCreated && !Application.isBatchMode)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }
        return true;
    }
}
