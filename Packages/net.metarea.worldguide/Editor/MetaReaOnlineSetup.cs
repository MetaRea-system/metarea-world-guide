using System;
using UnityEngine;
using UnityEditor;
using UdonSharpEditor;
using VRC.SDKBase;

// URL instances are baked in the editor because Udon cannot construct VRCUrl
// from a downloaded string. Server snapshot numbers must NEVER be recycled.
public static class MetaReaOnlineSetup
{
    public const int SnapshotCapacity = 4096;
    public const int DrawAtlasCapacity = 1024;
    public const string FeedUrl = "https://metarea.net/api/udon/feed-v2";
    public const string SponsorStatusUrl = "https://metarea.net/api/udon/status";
    public const string AtlasTemplate = "https://metarea.net/api/udon/atlas/{0}.jpg";
    public const string SponsorAtlasTemplate = "https://metarea.net/api/udon/sponsors/{0}.png";
    public const string SponsorExtraAtlasTemplate = "https://metarea.net/api/udon/sponsor-banners/{0}/1.png";
    public const string SponsorDetailAtlasTemplate = "https://metarea.net/api/udon/sponsor-details/{0}/{1}.png";
    public const string RecommendedDrawUrl = "https://metarea.net/api/udon/draw/recommended";
    public const string RandomDrawUrl = "https://metarea.net/api/udon/draw/random";
    public const string DrawAtlasTemplate = "https://metarea.net/api/udon/draw-atlas/{0}.jpg";

    [MenuItem("MetaRea/選択した壁新聞をオンライン配信に設定")]
    public static void ConfigureSelected()
    {
        var guide = Selection.activeGameObject == null ? null : Selection.activeGameObject.GetComponentInParent<MetaReaWallGuide>();
        if (guide == null) throw new InvalidOperationException("HierarchyでMetaRea Wall Guideを選択してください。");
        MetaReaSponsorLayout.Apply(guide);
        MetaReaPresentationLayout.Apply(guide);
        Configure(guide, FeedUrl, AtlasTemplate);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(guide.gameObject.scene);
    }

    public static void Configure(MetaReaWallGuide guide, string feed, string atlasTemplate)
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
        if (guide == null || !feed.StartsWith("https://") || !atlasTemplate.StartsWith("https://") || !atlasTemplate.Contains("{0}"))
            throw new ArgumentException("HTTPS feed and atlas template with {0} are required.");
        Undo.RecordObject(guide, "Configure MetaRea online feed");
        guide.feedUrl = new VRCUrl(feed);
        guide.sponsorStatusUrl = new VRCUrl(SponsorStatusUrl);
        guide.recommendedDrawUrl = new VRCUrl(RecommendedDrawUrl);
        guide.randomDrawUrl = new VRCUrl(RandomDrawUrl);
        guide.drawAtlasUrls = new VRCUrl[DrawAtlasCapacity];
        for (int i = 0; i < DrawAtlasCapacity; i++) guide.drawAtlasUrls[i] = new VRCUrl(string.Format(DrawAtlasTemplate, i));
        guide.atlasUrls = new VRCUrl[SnapshotCapacity];
        guide.sponsorAtlasUrls = new VRCUrl[SnapshotCapacity];
        guide.sponsorExtraAtlasUrls = new VRCUrl[SnapshotCapacity];
        guide.sponsorDetailAtlasUrls = new VRCUrl[SnapshotCapacity * 3];
        for (int i = 0; i < SnapshotCapacity; i++)
        {
            guide.atlasUrls[i] = new VRCUrl(string.Format(atlasTemplate, i)); guide.sponsorAtlasUrls[i] = new VRCUrl(string.Format(SponsorAtlasTemplate, i));
            guide.sponsorExtraAtlasUrls[i] = new VRCUrl(string.Format(SponsorExtraAtlasTemplate, i));
            for (int page = 0; page < 3; page++) guide.sponsorDetailAtlasUrls[i * 3 + page] = new VRCUrl(string.Format(SponsorDetailAtlasTemplate, i, page));
        }
        guide.sampleAdBanner = AssetDatabase.LoadAssetAtPath<Texture2D>(MetaReaPrototypeBuilder.Root + "/Samples/SponsorAds/Navy/banner-1536x256.png");
        guide.sampleAdDetail = AssetDatabase.LoadAssetAtPath<Texture2D>(MetaReaPrototypeBuilder.Root + "/Samples/SponsorAds/Navy/detail.png");
        guide.onlineMode = true;
        // Downloaded text is content, not Unity rich-text markup.
        foreach (var text in guide.GetComponentsInChildren<UnityEngine.UI.Text>(true))
        { text.supportRichText = false; EditorUtility.SetDirty(text); }
        guide.pinnedSnapshot = ""; guide.sharedPortalWorldId = ""; guide.nextPortalTime = 0; guide.portalCooldownStarted = false;
        guide.sharedPortalFromDraw = false; guide.sharedPortalDrawTitle = "";
        MetaReaClientCompatibility.Apply(guide);
        MetaReaSocialLayout.Apply(guide);
        if (guide.favoritesEnabled) MetaReaFavoritesLayout.Apply(guide);
        UdonSharpEditorUtility.CopyProxyToUdon(guide);
        EditorUtility.SetDirty(guide);
    }
}
