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
        if (Equal(settings.favoriteWorldIds, profile.favoriteWorldIds) && Equal(settings.favoriteWorldTitles, profile.favoriteWorldTitles) && Equal(settings.favoriteWorldAuthors, profile.favoriteWorldAuthors)) return;
        settings.favoriteWorldIds = Copy(profile.favoriteWorldIds);
        settings.favoriteWorldTitles = Copy(profile.favoriteWorldTitles);
        settings.favoriteWorldAuthors = Copy(profile.favoriteWorldAuthors);
        Persist(settings);
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
