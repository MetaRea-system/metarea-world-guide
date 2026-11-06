using System;
using System.Linq;
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
    static string cachedClickEnvironment;
    public static VRCUrl[] ClickUrls(string feed, string environment = "production")
    {
        if (feed != FeedEndpoint && !IsVersionedOfficialFeed(feed)) return new VRCUrl[0];
        environment = StatisticsEnvironment(environment);
        if (cachedClickUrls == null || cachedClickEnvironment != environment)
        {
            cachedClickEnvironment = environment;
            string context = "?version=" + GuideVersion + "&environment=" + environment;
            cachedClickUrls = new VRCUrl[8192];
            for (int slot = 0; slot < 4096; slot++)
            {
                cachedClickUrls[slot * 2] = new VRCUrl("https://metarea.net/api/udon/ad-click/" + slot + "/top" + context);
                cachedClickUrls[slot * 2 + 1] = new VRCUrl("https://metarea.net/api/udon/ad-click/" + slot + "/banner" + context);
            }
        }
        return cachedClickUrls;
    }
    static VRCUrl[] cachedPortalClickUrls;
    static string cachedPortalEnvironment;
    public static VRCUrl[] PortalClickUrls(string feed, string environment = "production")
    {
        if (feed != FeedEndpoint && !IsVersionedOfficialFeed(feed)) return new VRCUrl[0];
        environment = StatisticsEnvironment(environment);
        if (cachedPortalClickUrls == null || cachedPortalEnvironment != environment)
        {
            cachedPortalEnvironment = environment;
            cachedPortalClickUrls = new VRCUrl[4096];
            for (int slot = 0; slot < cachedPortalClickUrls.Length; slot++)
                cachedPortalClickUrls[slot] = new VRCUrl("https://metarea.net/api/udon/ad-portal/" + slot + "/top?version=" + GuideVersion + "&environment=" + environment);
        }
        return cachedPortalClickUrls;
    }
    public const string FeedEndpoint = "https://metarea.net/api/udon/guide-v3/current.json";
    public const string GuideVersion = "0.17.5";
    public static string FeedUrl => VersionedFeedUrl(FeedEndpoint);

    // Only the official feed receives a package version. No player, device,
    // installation or session identifier is added, and no extra request is made.
    public static string VersionedFeedUrl(string url)
    {
        if (url != FeedEndpoint && !IsVersionedOfficialFeed(url)) return url;
        return FeedEndpoint + "?version=" + GuideVersion;
    }

    static bool IsVersionedOfficialFeed(string url)
    {
        if (url == "https://metarea.net/api/udon/feed-v2" || (url != null && url.StartsWith("https://metarea.net/api/udon/feed-v2?", StringComparison.Ordinal))) return true;
        string prefix = FeedEndpoint + "?";
        if (url == null || !url.StartsWith(prefix, StringComparison.Ordinal) || url.IndexOf('#') >= 0) return false;
        foreach (string part in url.Substring(prefix.Length).Split('&'))
            if (!part.StartsWith("version=", StringComparison.Ordinal) && part != "topImage=1" && part != "bannerLayout=3" && part != "detailLayout=2") return false;
        return true;
    }

    public static string StatisticsSite(string site)
    {
        if (string.IsNullOrWhiteSpace(site)) return "unknown";
        if (site.Length > 64 || site.Any(c => !((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_' || c == '-')))
            throw new ArgumentException("MetaRea: 統計の設置先コードは英数字・_・-の1～64文字で指定してください。");
        return site;
    }
    public static string StatisticsEnvironment(string environment)
    {
        if (string.IsNullOrEmpty(environment)) return "unknown";
        if (environment != "production" && environment != "test" && environment != "unknown")
            throw new ArgumentException("MetaRea: 統計の環境はproduction / test / unknownで指定してください。");
        return environment;
    }
    public static VRCUrl[] StatisticsUrls(string feed, string site = "unknown", string environment = "production")
    {
        if (feed != FeedEndpoint && !IsVersionedOfficialFeed(feed)) return new VRCUrl[0];
        site = StatisticsSite(site); environment = StatisticsEnvironment(environment);
        string[] events = { "board_ready", "portal_request_accepted", "portal_owner_accepted" };
        string[] protocols = { "v2", "v3", "v4", "unknown" };
        var urls = new VRCUrl[12];
        for (int i = 0; i < events.Length; i++)
            for (int p = 0; p < protocols.Length; p++)
                urls[i * 4 + p] = new VRCUrl("https://metarea.net/api/udon/telemetry?schema=2&version=" + GuideVersion + "&event=" + events[i] + "&site=" + site + "&environment=" + environment + "&protocol=" + protocols[p]);
        return urls;
    }
    public const string SponsorStatusUrl = "https://metarea.net/api/udon/guide-v3/status.json";
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
        ConfigureV3Urls(guide);
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
        guide.statisticsUrls = StatisticsUrls(feed, guide.statisticsSite, guide.statisticsEnvironment);
        guide.sponsorStatusUrl = new VRCUrl(SponsorStatusUrl);
        ConfigureV3Urls(guide);
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
        MetaReaMaintenanceLayout.Apply(guide);
        MetaReaAdvertisingLayout.Apply(guide);
        UdonSharpEditorUtility.CopyProxyToUdon(guide);
        EditorUtility.SetDirty(guide);
    }

    public static void ConfigureV3Urls(MetaReaWallGuide guide)
    {
        guide.guideV3 = true;
        guide.preferGuideV4 = true;
        guide.v4FeedUrl = new VRCUrl("https://metarea.net/api/udon/guide-v4/current.json?version=" + GuideVersion);
        guide.v4StatusUrl = new VRCUrl("https://metarea.net/api/udon/guide-v4/status.json");
        guide.v4ManifestUrls = new VRCUrl[32]; guide.v4LatestUrls = new VRCUrl[32];
        guide.v4SetUrls = new VRCUrl[768]; guide.v4ImageUrls = new VRCUrl[1024];
        const string root = "https://metarea.net/api/udon/guide-v3/";
        guide.v3ManifestUrls = new VRCUrl[32];
        guide.v3LatestUrls = new VRCUrl[32];
        guide.v3SetUrls = new VRCUrl[32*24];
        guide.v3ImageUrls = new VRCUrl[32*32];
        for (int bank=0; bank<32; bank++)
        {
            string prefix=root+"banks/"+bank+"/";
            string v4 = "https://metarea.net/api/udon/guide-v4/banks/"+bank+"/";
            guide.v4ManifestUrls[bank] = new VRCUrl(v4+"manifest.json");
            guide.v4LatestUrls[bank] = new VRCUrl(v4+"sets/latest/0.json");
            for(int set=0;set<24;set++) guide.v4SetUrls[bank*24+set]=new VRCUrl(v4+"sets/"+(set<8?"recommended/"+set:"random/"+(set-8))+".json");
            for(int image=0;image<32;image++) guide.v4ImageUrls[bank*32+image]=new VRCUrl(v4+"images/"+image+(image==0||(image>=26&&image<=30)?".png":".jpg"));
            guide.v3ManifestUrls[bank]=new VRCUrl(prefix+"manifest.json");
            guide.v3LatestUrls[bank]=new VRCUrl(prefix+"sets/latest/0.json");
            for(int set=0;set<24;set++) guide.v3SetUrls[bank*24+set]=new VRCUrl(prefix+"sets/"+(set<8?"recommended/"+set:"random/"+(set-8))+".json");
            for(int image=0;image<32;image++)guide.v3ImageUrls[bank*32+image]=new VRCUrl(prefix+"images/"+image+(image==0||(image>=26&&image<=30)?".png":".jpg"));
        }
        guide.atlasUrls = new VRCUrl[0]; guide.sponsorAtlasUrls = new VRCUrl[0]; guide.sponsorExtraAtlasUrls = new VRCUrl[0];
        guide.sponsorDetailAtlasUrls = new VRCUrl[0]; guide.sponsorTopAtlasUrls = new VRCUrl[0]; guide.drawAtlasUrls = new VRCUrl[0];
        guide.recommendedDrawUrl = new VRCUrl(root+"current.json"); guide.randomDrawUrl = new VRCUrl(root+"current.json");
    }

}
