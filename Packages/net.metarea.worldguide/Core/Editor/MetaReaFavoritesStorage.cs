using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UdonSharpEditor;

[InitializeOnLoad]
public static class MetaReaFavoritesStorage
{
    public const string Folder = "Assets/MetaRea_World_GUIDE/UserContent/Favorites";
    static bool scheduled;
    static MetaReaFavoritesStorage()
    {
        EditorApplication.hierarchyChanged += Schedule;
        EditorSceneManager.sceneOpened += (scene, mode) => Schedule();
        Undo.undoRedoPerformed += Schedule;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingEditMode) PrepareAll();
        };
        Schedule();
    }
    static void Schedule()
    {
        if (scheduled) return;
        scheduled = true;
        EditorApplication.delayCall += () =>
        {
            scheduled = false;
            if (!EditorApplication.isCompiling && !EditorApplication.isPlayingOrWillChangePlaymode && !BuildPipeline.isBuildingPlayer) PrepareAll();
        };
    }
    public static bool CanStore(MetaReaFavoritesSettings settings)
    {
        return !EditorUtility.IsPersistent(settings) && settings.gameObject.scene.IsValid() &&
            !EditorSceneManager.IsPreviewScene(settings.gameObject.scene) && PrefabStageUtility.GetPrefabStage(settings.gameObject) == null;
    }
    public static MetaReaFavoritesProfile Load(MetaReaFavoritesSettings settings)
    {
        return string.IsNullOrEmpty(settings.favoritesProfileGuid) ? null :
            AssetDatabase.LoadAssetAtPath<MetaReaFavoritesProfile>(AssetDatabase.GUIDToAssetPath(settings.favoritesProfileGuid));
    }
    public static MetaReaFavoritesProfile Ensure(MetaReaFavoritesSettings settings)
    {
        var existing = Load(settings);
        if (existing != null) Migrate(existing);
        if (existing != null || !CanStore(settings) || !string.IsNullOrEmpty(settings.favoritesProfileGuid)) return existing;
        System.IO.Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();
        var profile = ScriptableObject.CreateInstance<MetaReaFavoritesProfile>();
        profile.favoriteWorldIds = Copy(settings.favoriteWorldIds);
        profile.favoriteWorldTitles = Copy(settings.favoriteWorldTitles);
        profile.favoriteWorldAuthors = Copy(settings.favoriteWorldAuthors);
        // Preserve pre-settings registrations when upgrading an older board.
        var guide = settings.GetComponentInParent<MetaReaWallGuide>();
        if (profile.favoriteWorldIds.Length == 0 && guide != null && guide.favoriteWorldIds != null && guide.favoriteWorldIds.Length > 0)
        {
            profile.favoriteWorldIds = Copy(guide.favoriteWorldIds);
            profile.favoriteWorldTitles = Copy(guide.favoriteWorldTitles);
            profile.favoriteWorldAuthors = Copy(guide.favoriteWorldAuthors);
        }
        AssetDatabase.CreateAsset(profile, AssetDatabase.GenerateUniqueAssetPath(Folder + "/Favorite Worlds.asset"));
        Bind(settings, profile);
        AssetDatabase.SaveAssetIfDirty(profile);
        return profile;
    }
    static string[] Copy(string[] values) => values == null ? new string[0] : (string[])values.Clone();
    static bool Equal(string[] a, string[] b) => (a ?? new string[0]).SequenceEqual(b ?? new string[0]);
    public static void Bind(MetaReaFavoritesSettings settings, MetaReaFavoritesProfile profile)
    {
        string path = AssetDatabase.GetAssetPath(profile);
        if (!path.StartsWith("Assets/", StringComparison.Ordinal)) throw new InvalidOperationException("お気に入りの保存先はAssets内にしてください。");
        Undo.RecordObject(settings, "Select favorites profile");
        settings.favoritesProfileGuid = AssetDatabase.AssetPathToGUID(path);
        Apply(settings, profile);
        Persist(settings);
    }
    public static void ApplySaved(MetaReaFavoritesSettings settings)
    {
        if (string.IsNullOrEmpty(settings.favoritesProfileGuid)) return;
        var profile = Load(settings);
        if (profile == null) throw new BuildFailedException("MetaRea: お気に入り保存データが見つかりません。「お気に入りワールド設定」で保存データを選び直してください。登録を空にせずビルドを停止しました。");
        Apply(settings, profile);
    }
    public static void Apply(MetaReaFavoritesSettings settings, MetaReaFavoritesProfile profile)
    {
        Migrate(profile);
        var ids = new System.Collections.Generic.List<string>();
        var titles = new System.Collections.Generic.List<string>();
        var authors = new System.Collections.Generic.List<string>();
        var indices = new System.Collections.Generic.List<int>();
        var folders = profile.folders ?? new MetaReaFavoriteFolder[0];
        var names = folders.Select(f => string.IsNullOrWhiteSpace(f.name) ? "名前未設定" : f.name).ToArray();
        for (int f = 0; f < folders.Length; f++)
        {
            var folder = folders[f];
            for (int i = 0; i < (folder.favoriteWorldIds?.Length ?? 0); i++)
            {
                ids.Add(folder.favoriteWorldIds[i]);
                titles.Add(i < (folder.favoriteWorldTitles?.Length ?? 0) ? folder.favoriteWorldTitles[i] : "");
                authors.Add(i < (folder.favoriteWorldAuthors?.Length ?? 0) ? folder.favoriteWorldAuthors[i] : "");
                indices.Add(f);
            }
        }
        if (Equal(settings.favoriteWorldIds, ids.ToArray()) && Equal(settings.favoriteWorldTitles, titles.ToArray()) && Equal(settings.favoriteWorldAuthors, authors.ToArray()) && Equal(settings.favoriteFolderNames, names) && (settings.favoriteFolderIndices ?? new int[0]).SequenceEqual(indices)) return;
        settings.favoriteWorldIds = ids.ToArray();
        settings.favoriteWorldTitles = titles.ToArray();
        settings.favoriteWorldAuthors = authors.ToArray();
        settings.favoriteFolderNames = names;
        settings.favoriteFolderIndices = indices.ToArray();
        Persist(settings);
    }
    public static void Migrate(MetaReaFavoritesProfile profile)
    {
        if (profile.folderSchemaVersion >= 1) return;
        string path = AssetDatabase.GetAssetPath(profile);
        if (!string.IsNullOrEmpty(path))
        {
            // Keep an exact serialized snapshot outside the package before any migration.
            string backup = "MetaReaWorldGuideBackups/Favorites/" + AssetDatabase.AssetPathToGUID(path) + "-before-folders.json";
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(backup));
            if (!System.IO.File.Exists(backup))
                System.IO.File.WriteAllText(backup, EditorJsonUtility.ToJson(profile, true), new System.Text.UTF8Encoding(false));
        }
        profile.folders = new[] { new MetaReaFavoriteFolder { name = "フォルダ1", favoriteWorldIds = Copy(profile.favoriteWorldIds), favoriteWorldTitles = Copy(profile.favoriteWorldTitles), favoriteWorldAuthors = Copy(profile.favoriteWorldAuthors) } };
        profile.folderSchemaVersion = 1;
        EditorUtility.SetDirty(profile);
        if (!string.IsNullOrEmpty(path)) AssetDatabase.SaveAssetIfDirty(profile);
    }
    static void Persist(MetaReaFavoritesSettings settings)
    {
        UdonSharpEditorUtility.CopyProxyToUdon(settings);
        EditorUtility.SetDirty(settings);
        PrefabUtility.RecordPrefabInstancePropertyModifications(settings);
        var backing = UdonSharpEditorUtility.GetBackingUdonBehaviour(settings);
        if (backing != null) { EditorUtility.SetDirty(backing); PrefabUtility.RecordPrefabInstancePropertyModifications(backing); }
        if (settings.gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(settings.gameObject.scene);
    }
    public static void PrepareAll()
    {
        if (EditorApplication.isPlaying) return;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded || EditorSceneManager.IsPreviewScene(scene)) continue;
            foreach (var settings in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MetaReaFavoritesSettings>(true)))
            {
                if (!CanStore(settings)) continue;
                var profile = Ensure(settings);
                if (profile != null) Apply(settings, profile);
            }
        }
    }
}
