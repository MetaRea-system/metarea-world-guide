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
    public const int DetailTrackingCapacity = 65535;
    // Tracking pixels are disabled, including on click and during prefetch.
    public static VRCUrl[] DetailTrackingUrls(string feed) => new VRCUrl[0];
    static VRCUrl[] cachedClickUrls;
    public static VRCUrl[] ClickUrls(string feed)
    {
        if (feed != FeedEndpoint && !IsVersionedOfficialFeed(feed)) return new VRCUrl[0];
        if (cachedClickUrls == null)
        {
            cachedClickUrls = new VRCUrl[8192];
            for (int slot = 0; slot < 4096; slot++)
            {
                cachedClickUrls[slot * 2] = new VRCUrl("https://metarea.net/api/udon/ad-click/" + slot + "/top");
                cachedClickUrls[slot * 2 + 1] = new VRCUrl("https://metarea.net/api/udon/ad-click/" + slot + "/banner");
            }
        }
        return cachedClickUrls;
    }
    public const string FeedEndpoint = "https://metarea.net/api/udon/feed-v2";
    public static string FeedUrl => VersionedFeedUrl(FeedEndpoint);

    // Only the official feed receives a package version. No player, device,
    // installation or session identifier is added, and no extra request is made.
    public static string VersionedFeedUrl(string url)
    {
        if (url != FeedEndpoint && !IsVersionedOfficialFeed(url)) return url;
        var package = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/net.metarea.worldguide");
        if (package == null || string.IsNullOrEmpty(package.version)) return url;
        return FeedEndpoint + "?version=" + Uri.EscapeDataString(package.version) + "&topImage=1&bannerLayout=3&detailLayout=2";
    }

    static bool IsVersionedOfficialFeed(string url)
    {
        string prefix = FeedEndpoint + "?";
        if (url == null || !url.StartsWith(prefix, StringComparison.Ordinal) || url.IndexOf('#') >= 0) return false;
        foreach (string part in url.Substring(prefix.Length).Split('&'))
            if (!part.StartsWith("version=", StringComparison.Ordinal) && part != "topImage=1" && part != "bannerLayout=3" && part != "detailLayout=2") return false;
        return true;
    }

    public static VRCUrl[] StatisticsUrls(string feed)
    {
        if (feed != FeedEndpoint && !IsVersionedOfficialFeed(feed)) return new VRCUrl[0];
        string versioned = VersionedFeedUrl(FeedEndpoint);
        int query = versioned.IndexOf('?');
        if (query < 0) return new VRCUrl[0];
        string version = versioned.Substring(query + 1).Split('&')[0];
        string[] events = { "feed_success", "feed_failure", "status_success", "status_failure", "board_open", "ad_detail", "portal_request" };
        var urls = new VRCUrl[events.Length];
        for (int i = 0; i < events.Length; i++)
            urls[i] = new VRCUrl("https://metarea.net/api/udon/telemetry?schema=1&event=" + events[i] + "&" + version);
        return urls;
    }
    public const string SponsorStatusUrl = "https://metarea.net/api/udon/status";
    public const string AtlasTemplate = "https://metarea.net/api/udon/atlas/{0}.jpg";
    public const string SponsorAtlasTemplate = "https://metarea.net/api/udon/sponsors/{0}.png";
    public const string SponsorExtraAtlasTemplate = "https://metarea.net/api/udon/sponsor-banners/{0}/1.png";
    public const string SponsorDetailAtlasTemplate = "https://metarea.net/api/udon/sponsor-details/{0}/{1}.png";
    public const string SponsorTopAtlasTemplate = "https://metarea.net/api/udon/sponsor-tops/{0}/{1}";
    public const string RecommendedDrawUrl = "https://metarea.net/api/udon/draw/recommended";
    public const string RandomDrawUrl = "https://metarea.net/api/udon/draw/random";
    public const string DrawAtlasTemplate = "https://metarea.net/api/udon/draw-atlas/{0}.jpg";

    static VRCUrl[] cachedDetailAtlases, cachedTopAtlases;
    public static void EnsureSponsorUrls(MetaReaWallGuide guide, string feed)
    {
        if (feed != FeedEndpoint && !IsVersionedOfficialFeed(feed)) return;
        if (cachedDetailAtlases == null)
        {
            cachedDetailAtlases = new VRCUrl[SnapshotCapacity * 4];
            cachedTopAtlases = new VRCUrl[SnapshotCapacity * 2];
            for (int snapshot = 0; snapshot < SnapshotCapacity; snapshot++)
            {
                for (int page = 0; page < 4; page++) cachedDetailAtlases[snapshot * 4 + page] = new VRCUrl(string.Format(SponsorDetailAtlasTemplate, snapshot, page));
                for (int page = 0; page < 2; page++) cachedTopAtlases[snapshot * 2 + page] = new VRCUrl(string.Format(SponsorTopAtlasTemplate, snapshot, page));
            }
        }
        guide.sponsorDetailAtlasUrls = cachedDetailAtlases;
        guide.sponsorTopAtlasUrls = cachedTopAtlases;
    }

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
        guide.feedUrl = new VRCUrl(VersionedFeedUrl(feed));
        guide.statisticsUrls = StatisticsUrls(feed);
        guide.sponsorStatusUrl = new VRCUrl(SponsorStatusUrl);
        guide.recommendedDrawUrl = new VRCUrl(RecommendedDrawUrl);
        guide.randomDrawUrl = new VRCUrl(RandomDrawUrl);
        guide.drawAtlasUrls = new VRCUrl[DrawAtlasCapacity];
        for (int i = 0; i < DrawAtlasCapacity; i++) guide.drawAtlasUrls[i] = new VRCUrl(string.Format(DrawAtlasTemplate, i));
        guide.atlasUrls = new VRCUrl[SnapshotCapacity];
        guide.sponsorAtlasUrls = new VRCUrl[SnapshotCapacity];
        guide.sponsorExtraAtlasUrls = new VRCUrl[SnapshotCapacity];
        guide.sponsorDetailAtlasUrls = new VRCUrl[SnapshotCapacity * 4];
        guide.sponsorTopAtlasUrls = new VRCUrl[SnapshotCapacity * 2];
        for (int i = 0; i < SnapshotCapacity; i++)
        {
            guide.atlasUrls[i] = new VRCUrl(string.Format(atlasTemplate, i)); guide.sponsorAtlasUrls[i] = new VRCUrl(string.Format(SponsorAtlasTemplate, i));
            guide.sponsorExtraAtlasUrls[i] = new VRCUrl(string.Format(SponsorExtraAtlasTemplate, i));
            for (int page = 0; page < 4; page++) guide.sponsorDetailAtlasUrls[i * 4 + page] = new VRCUrl(string.Format(SponsorDetailAtlasTemplate, i, page));
            for (int page = 0; page < 2; page++) guide.sponsorTopAtlasUrls[i * 2 + page] = new VRCUrl(string.Format(SponsorTopAtlasTemplate, i, page));
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
        if (guide.unifiedDisplay) MetaReaUnifiedLayout.Apply(guide);
        else if (guide.favoritesEnabled) MetaReaFavoritesLayout.Apply(guide);
        MetaReaPlaceholderLayout.Apply(guide);
        UdonSharpEditorUtility.CopyProxyToUdon(guide);
        EditorUtility.SetDirty(guide);
    }
}
