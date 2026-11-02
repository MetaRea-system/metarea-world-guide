using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UdonSharpEditor;

// After shared-data preparation (-10000), before UdonSharp's scene serialization (0).
public sealed class MetaReaBuildAssets : IProcessSceneWithReport
{
    public int callbackOrder => -9000;
    public void OnProcessScene(Scene scene, BuildReport report)
    {
        // Never change the editing scene, or its previews when entering Play mode.
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Prepare(scene);
    }

    public static bool IsPreviewAsset(UnityEngine.Object asset)
    {
        string path = AssetDatabase.GetAssetPath(asset);
        const string root = "Packages/net.metarea.worldguide/Core/";
        if (path.StartsWith(root + "Pictures/", StringComparison.Ordinal) ||
            path.StartsWith(root + "Samples/SponsorAds/", StringComparison.Ordinal)) return true;
        if (!path.StartsWith(root + "UI/Placeholders/", StringComparison.Ordinal)) return false;
        string name = System.IO.Path.GetFileNameWithoutExtension(path);
        return name == "world" || name == "daily-1" || name == "daily-2" ||
            name == "many-daily-1" || name == "many-daily-2" || name == "many-latest" || name == "many-recommended";
    }

    // Only invoke on a build copy or isolated validation scene.
    public static void Prepare(Scene scene)
    {
        foreach (var settings in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MetaReaFavoritesSettings>(true)))
            MetaReaFavoritesStorage.ApplySaved(settings);
        foreach (var guide in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MetaReaWallGuide>(true)))
        {
            if (!guide.onlineMode) continue; // Offline boards really use their samples.
            guide.samplePictures = new Texture[0];
            guide.sampleAdBanner = null;
            guide.sampleAdDetail = null;
            guide.sampleFeed = null;
            foreach (var image in guide.GetComponentsInChildren<RawImage>(true))
            {
                if (!IsPreviewAsset(image.texture)) continue;
                image.texture = null;
                image.uvRect = new Rect(0, 0, 1, 1);
            }
            UdonSharpEditorUtility.CopyProxyToUdon(guide);
        }
    }
}
