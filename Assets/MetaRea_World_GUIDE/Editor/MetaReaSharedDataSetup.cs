using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UdonSharpEditor;
using VRC.SDKBase.Editor.BuildPipeline;

// The source lives outside the boards: disabling a display cannot stop downloads.
[InitializeOnLoad]
public static class MetaReaSharedDataSetup
{
    const string SourceName = "MetaRea World GUIDE Shared Data";
    static bool scheduled;
    static bool preparing;

    static MetaReaSharedDataSetup()
    {
        EditorApplication.hierarchyChanged += Schedule;
        EditorSceneManager.sceneOpened += (scene, mode) => Schedule();
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingEditMode) PrepareAll();
            if (state == PlayModeStateChange.EnteredEditMode) Schedule();
        };
        Schedule();
    }

    static void Schedule()
    {
        if (scheduled || preparing || EditorApplication.isPlayingOrWillChangePlaymode) return;
        scheduled = true;
        EditorApplication.delayCall += () => { scheduled = false; if (!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling) PrepareAll(); };
    }

    [MenuItem("MetaRea/複数ボードの共通データ接続を更新")]
    public static void PrepareAll()
    {
        if (EditorApplication.isPlaying || preparing) return;
        for (int i = 0; i < SceneManager.sceneCount; i++) Prepare(SceneManager.GetSceneAt(i));
    }

    public static MetaReaWallGuide Prepare(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded || EditorSceneManager.IsPreviewScene(scene) || preparing) return null;
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null && stage.scene == scene) return null;
        preparing = true;
        try
        {
            var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MetaReaWallGuide>(true)).ToArray();
            var displays = all.Where(g => !g.isSharedDataSource).OrderBy(g => HierarchyKey(g.transform), StringComparer.Ordinal).ToArray();
            var sources = all.Where(g => g.isSharedDataSource).ToArray();
            if (displays.Length == 0) return sources.FirstOrDefault();
            if (sources.Length > 1) throw new BuildFailedException("MetaRea: 共通データ管理オブジェクトが重複しています。1つにしてください。");
            var first = displays[0];
            foreach (var display in displays)
                if (display.onlineMode != first.onlineMode || MetaReaOnlineSetup.VersionedFeedUrl(display.feedUrl.Get()) != MetaReaOnlineSetup.VersionedFeedUrl(first.feedUrl.Get()) || display.recommendedDrawUrl.Get() != first.recommendedDrawUrl.Get() || display.randomDrawUrl.Get() != first.randomDrawUrl.Get())
                    throw new BuildFailedException("MetaRea: 同じシーンのボードには同じ配信設定を使用してください。");
            var source = sources.FirstOrDefault();
            bool changed = source == null;
            if (source == null)
            {
                var go = new GameObject(SourceName);
                SceneManager.MoveGameObjectToScene(go, scene);
                Undo.RegisterCreatedObjectUndo(go, "Create MetaRea shared data");
                source = UdonSharpUndo.AddComponent<MetaReaWallGuide>(go);
                source.isSharedDataSource = true;
                source.favoritesEnabled = false;
            }
            string before = EditorJsonUtility.ToJson(source);
            source.sharedDataSource = null;
            source.sharedDisplays = displays;
            source.onlineMode = first.onlineMode;
            source.feedUrl = new VRC.SDKBase.VRCUrl(MetaReaOnlineSetup.VersionedFeedUrl(first.feedUrl.Get()));
            source.anonymousStatistics = displays.All(g => g.anonymousStatistics);
            source.statisticsUrls = MetaReaOnlineSetup.StatisticsUrls(first.feedUrl.Get());
            source.detailTrackingUrls = source.anonymousStatistics ? MetaReaOnlineSetup.DetailTrackingUrls(first.feedUrl.Get()) : new VRC.SDKBase.VRCUrl[0];
            source.clickUrls = source.anonymousStatistics ? MetaReaOnlineSetup.ClickUrls(first.feedUrl.Get()) : new VRC.SDKBase.VRCUrl[0];
            source.sponsorStatusUrl = first.sponsorStatusUrl;
            source.recommendedDrawUrl = first.recommendedDrawUrl;
            source.randomDrawUrl = first.randomDrawUrl;
            source.atlasUrls = first.atlasUrls;
            source.sponsorAtlasUrls = first.sponsorAtlasUrls;
            source.sponsorExtraAtlasUrls = first.sponsorExtraAtlasUrls;
            source.sponsorDetailAtlasUrls = first.sponsorDetailAtlasUrls;
            source.sponsorTopAtlasUrls = first.sponsorTopAtlasUrls;
            MetaReaOnlineSetup.EnsureSponsorUrls(source, first.feedUrl.Get());
            source.drawAtlasUrls = first.drawAtlasUrls;
            source.sampleFeed = first.sampleFeed;
            source.sponsorRotationSeconds = first.sponsorRotationSeconds;
            source.sponsorFadeSeconds = first.sponsorFadeSeconds;
            if (before != EditorJsonUtility.ToJson(source) || changed)
            { UdonSharpEditorUtility.CopyProxyToUdon(source); EditorUtility.SetDirty(source); changed = true; }
            foreach (var display in displays)
            {
                if (display.sharedDataSource == source) continue;
                Undo.RecordObject(display, "Connect MetaRea shared data");
                display.sharedDataSource = source;
                UdonSharpEditorUtility.CopyProxyToUdon(display);
                EditorUtility.SetDirty(display);
                PrefabUtility.RecordPrefabInstancePropertyModifications(display);
                changed = true;
            }
            if (changed) EditorSceneManager.MarkSceneDirty(scene);
            return source;
        }
        finally { preparing = false; }
    }

    static string HierarchyKey(Transform t)
    {
        string key = t.GetSiblingIndex().ToString("D6");
        while (t.parent != null) { t = t.parent; key = t.GetSiblingIndex().ToString("D6") + "/" + key; }
        return key;
    }
}

public sealed class MetaReaSharedDataBuild : IVRCSDKBuildRequestedCallback, IProcessSceneWithReport
{
    public int callbackOrder => -10000;
    public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType)
    { MetaReaSharedDataSetup.PrepareAll(); return true; }
    public void OnProcessScene(Scene scene, BuildReport report)
    { MetaReaSharedDataSetup.Prepare(scene); }
}
