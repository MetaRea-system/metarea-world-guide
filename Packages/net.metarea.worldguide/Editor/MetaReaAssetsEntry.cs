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
    public const string Folder = "Assets/MetaRea_World_GUIDE";
    const string LegacyFolder = "Assets/MetaRea World GUIDE";
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

    static void MoveAsset(string from, string to)
    {
        string error = AssetDatabase.MoveAsset(from, to);
        if (!string.IsNullOrEmpty(error)) throw new IOException(error);
    }

    static void MigrateEntry()
    {
        if (!AssetDatabase.IsValidFolder(LegacyFolder)) return;
        if (!AssetDatabase.IsValidFolder(Folder))
        {
            MoveAsset(LegacyFolder, Folder);
            return;
        }
        // Move instead of copying, preserving the GUID and all user overrides.
        // Other user files are retained in the same unified folder.
        foreach (string path in Directory.GetFileSystemEntries(LegacyFolder))
        {
            string from = path.Replace((char)92, (char)47);
            if (from.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) continue;
            string target = Folder + "/" + Path.GetFileName(from);
            if (File.Exists(target) || Directory.Exists(target))
                target = AssetDatabase.GenerateUniqueAssetPath(target);
            MoveAsset(from, target);
        }
        if (Directory.GetFileSystemEntries(LegacyFolder).Length == 0)
            AssetDatabase.DeleteAsset(LegacyFolder);
    }

    static void EnsureDocumentation()
    {
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets", "MetaRea_World_GUIDE");
        string docs = Folder + "/Documentation";
        if (!AssetDatabase.IsValidFolder(docs))
            AssetDatabase.CreateFolder(Folder, "Documentation");
        CopyDocument("Packages/net.metarea.worldguide/Runtime/README.txt", Folder + "/README.txt");
        CopyDocument("Packages/net.metarea.worldguide/Runtime/Documentation/スポンサー広告サンプル.txt",
            docs + "/スポンサー広告サンプル.txt");
    }

    static void CopyDocument(string source, string destination)
    {
        // Keep existing user edits. No .meta is copied from the package.
        if (File.Exists(destination) || !File.Exists(source)) return;
        File.Copy(source, destination);
        AssetDatabase.ImportAsset(destination);
    }
    public static bool EnsureEntry(bool revealCreated)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return false;
        MigrateEntry();
        EnsureDocumentation();
        // Never overwrite a user's existing prefab, including its GUID/overrides.
        if (File.Exists(PrefabPath)) return false;
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath);
        if (source == null) return false;
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "MetaRea_World_GUIDE");
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
