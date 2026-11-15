using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// The root variant is the user-facing entry point. Its source and every
// dependencies live in the VCC package; user overrides remain in Assets.
[InitializeOnLoad]
public static class MetaReaAssetsEntry
{
    public const string Folder = "Assets/MetaRea_World_GUIDE";
    const string LegacyFolder = "Assets/MetaRea World GUIDE";
    public const string PrefabPath = Folder + "/MetaRea World GUIDE.prefab";
    public const string SourcePath = "Packages/net.metarea.worldguide/Core/Internal/MetaRea World GUIDE.prefab";
    static bool queued;

    static MetaReaAssetsEntry()
    {
        Schedule();
        ObjectChangeEvents.changesPublished += NewObjectsCreated;
    }

    // Only undoable new scene objects (e.g. dragging a prefab) are initialized.
    // Scene loading, package updates, moving existing boards and builds do not
    // run placement. No terrain lookup or runtime component is involved.
    static void NewObjectsCreated(ref ObjectChangeEventStream stream)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer) return;
        for (int i = 0; i < stream.length; i++)
        {
            if (stream.GetEventType(i) != ObjectChangeKind.CreateGameObjectHierarchy) continue;
            stream.GetCreateGameObjectHierarchyEvent(i, out var created);
            var root = EditorUtility.InstanceIDToObject(created.instanceId) as GameObject;
            if (root == null) continue;
            foreach (var guide in root.GetComponentsInChildren<MetaReaWallGuide>(true))
                InitializePlacement(guide);
        }
    }

    static void InitializePlacement(MetaReaWallGuide guide)
    {
        if (guide == null || guide.isSharedDataSource || EditorUtility.IsPersistent(guide)) return;
        var scene = guide.gameObject.scene;
        if (!scene.IsValid() || !scene.isLoaded || EditorSceneManager.IsPreviewScene(scene) ||
            PrefabStageUtility.GetPrefabStage(guide.gameObject) != null) return;
        if (!PrefabUtility.IsPartOfPrefabInstance(guide)) return;
        var original = PrefabUtility.GetCorrespondingObjectFromOriginalSource(guide.gameObject);
        if (original == null || AssetDatabase.GetAssetPath(original) != SourcePath) return;
        var frame = guide.transform.Find("Thin frame");
        var canvas = guide.transform.Find("Wall newspaper");
        if (frame == null || canvas == null) return;
        // Preserve deliberate placement overrides in custom variants/copies.
        var sourceFrame = original.transform.Find("Thin frame");
        if (sourceFrame == null || (frame.localPosition-sourceFrame.localPosition).sqrMagnitude > .000001f) return;
        float bottom = frame.localPosition.y - frame.localScale.y * .5f;
        float lift = .5f - bottom;
        if (Mathf.Abs(lift) < .00001f) return;
        foreach (string name in new[] { "Thin frame", "Top cyan trim", "Bottom cyan trim", "Wall newspaper" })
        {
            var part = guide.transform.Find(name);
            if (part == null) continue;
            Undo.RecordObject(part, "Place MetaRea board 0.5m above origin");
            part.localPosition += Vector3.up * lift;
            PrefabUtility.RecordPrefabInstancePropertyModifications(part);
        }
    }

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
        catch (Exception error) { Debug.LogError("MetaRea World GUIDE: 導入用プレハブを作成できませんでした。Consoleのエラーを確認してください。\n" + error.Message); }
    }

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
        string docs = Folder + "/UserContent";
        if (!AssetDatabase.IsValidFolder(docs))
            AssetDatabase.CreateFolder(Folder, "UserContent");
    }
    // VCC replaces package bytes outside AssetDatabase. Refresh the imported
    // source once per content revision/session, including existing variants.
    static void RefreshSource()
    {
        if (!File.Exists(SourcePath)) return;
        string revision;
        using (var hash = SHA256.Create())
            revision = Convert.ToBase64String(hash.ComputeHash(File.ReadAllBytes(SourcePath)));
        const string key = "MetaRea.WorldGuide.ImportedSourceRevision";
        if (SessionState.GetString(key, "") == revision) return;
        AssetDatabase.ImportAsset(SourcePath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        SessionState.SetString(key, revision);
    }
    public static bool EnsureEntry(bool revealCreated)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return false;
        MetaReaTextResources.EnsureSettings();
        MigrateEntry();
        EnsureDocumentation();
        RefreshSource();
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
