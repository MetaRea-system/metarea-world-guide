using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.SDK3.Components;
using VRC.SDK3.Data;
using VRC.SDK3.StringLoading;
using VRC.SDK3.Image;
using VRC.SDK3.Rendering;
using VRC.SDK3.UdonNetworkCalling;
using VRC.Udon.Common.Interfaces;

// All underscore UI entry points are local-only. Only explicitly
// NetworkCallable requests can change shared state; the owner validates them.
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class MetaReaWallGuide : UdonSharpBehaviour
{
    [Header("設置者の設定")]
    [Tooltip("OFF: 通常ランダム①＋② / ON: 通常①＋チル")]
    public bool secondSlotIsChill;
    [HideInInspector] public bool unifiedDisplay;
    [HideInInspector] public int initialDisplayMode = 1;
    [Min(1f)] public float portalCooldownSeconds = 5f;
    public Transform portalAnchor;
    public VRCPortalMarker portalMarker;
    public GameObject editorPortalPreview;
    public Text portalDestinationLabel;

    [Header("外部データ配信")]
    public bool onlineMode;
    public VRCUrl feedUrl = new VRCUrl("");
    // A separate instance is required: sharing VRCUrl.Empty aliases the Udon heap slots.
    public VRCUrl sponsorStatusUrl = new VRCUrl("https://metarea.net/api/udon/status");
    [Tooltip("Editorで生成する不変画像URL。番号は再利用しません。")]
    public VRCUrl[] atlasUrls;
    public VRCUrl[] sponsorAtlasUrls;
    public VRCUrl[] sponsorExtraAtlasUrls;
    public VRCUrl[] sponsorDetailAtlasUrls;
    public VRCUrl[] sponsorTopAtlasUrls;
    public Image[] sponsorGoldFrames;
    [Header("サイト全件からの抽選")]
    public VRCUrl recommendedDrawUrl = new VRCUrl("https://metarea.net/api/udon/draw/recommended");
    public VRCUrl randomDrawUrl = new VRCUrl("https://metarea.net/api/udon/draw/random");
    public VRCUrl[] drawAtlasUrls;
    [Header("公式X")]
    public string xProfileUrl = "https://x.com/metametarea";
    public GameObject xUrlPanel;
    public InputField xUrlField;
    [Header("BOOTH")]
    public string boothUrl = "https://metarea.booth.pm/";
    public GameObject boothUrlPanel;
    public InputField boothUrlField;
    [Min(5f)] public float sponsorRotationSeconds = 10f;
    [Range(.2f, 2f)] public float sponsorFadeSeconds = .8f;
    public TextAsset sampleFeed;
    [HideInInspector] public bool favoritesEnabled;
    [HideInInspector] public MetaReaFavoritesSettings favoritesSettings;
    [HideInInspector] public string[] favoriteWorldIds = new string[0];
    [HideInInspector] public string[] favoriteWorldTitles = new string[0];
    [HideInInspector] public string[] favoriteWorldAuthors = new string[0];
    [HideInInspector] public GameObject favoritesPanel;
    [HideInInspector] public Button[] favoriteButtons;
    [HideInInspector] public Text[] favoriteTitles;
    [HideInInspector] public Text[] favoriteAuthors;
    [HideInInspector] public Text favoritePageLabel;
    [HideInInspector] public Text favoriteHelp;
    [HideInInspector] public Button favoritePrevious;
    [HideInInspector] public Button favoriteNext;
    private bool favoriteTabSelected;
    private int favoritePage;
    private int[] favoriteOrder;
    private int favoriteCount;
    [HideInInspector] public bool favoriteFlatList;
    [HideInInspector] public string[] favoriteFolderDescriptions = new string[0];
    [HideInInspector] public string[] favoriteFolderNames = new string[0];
    [HideInInspector] public int[] favoriteFolderIndices = new int[0];
    [HideInInspector] public Button[] favoriteFolderButtons;
    [HideInInspector] public Text[] favoriteFolderTitles;
    [HideInInspector] public Text[] favoriteFolderCounts;
    [HideInInspector] public Button favoriteBack;
    [HideInInspector] public Text favoriteSection;
    private int favoriteFolder = -1;
    private int favoriteFolderPage;
    private int[] favoriteFolderCountsCache;

    public Texture[] samplePictures;
    [HideInInspector] public Texture loadingPicture;
    [HideInInspector] public Texture unavailablePicture;
    [HideInInspector] public Texture failedPicture;
    [HideInInspector] public RawImage[] loadingTargets;
    [HideInInspector] public Text[] loadingIndicators;
    public Texture sampleAdBanner;
    public Texture sampleAdDetail;
    public string[] pictureWorldIds;


    public Color headingColor = new Color(.88f,.94f,1f);
    public Color bodyColor = new Color(.72f,.82f,.89f);
    public Color secondaryColor = new Color(.64f,.77f,.86f);
    public Color accentColor = new Color(.18f,.73f,.95f);
    public Color buttonTextColor = new Color(.88f,.94f,1f);
    public Color backgroundColor = new Color(.016f,.036f,.068f);
    public Color cardColor = new Color(.022f,.064f,.11f);
    public Color selectedTabColor = new Color(.03f,.30f,.47f);
    public Color portalColor = new Color(0,.43f,.66f);
    public Color borderColor = new Color(.11f,.28f,.4f);
    public Text[] headingTexts;
    public Text[] bodyTexts;
    public Text[] secondaryTexts;
    public Text[] accentTexts;
    public Text[] buttonTexts;
    public Image backgroundImage;
    public Image[] cardImages;
    public Image[] portalImages;
    public Outline[] borderOutlines;
    public Image[] dividerImages;

    [Header("UI参照：自動組み立てで設定")]
    public GameObject topPanel;
    public GameObject listPanel;
    public GameObject detailPanel;
    public GameObject adPanel;
    public Text[] topTitles;
    public Text[] topAuthors;
    public Text[] topSummaries;
    public Text[] topCaptions;
    public RawImage[] topPictures;
    public Button[] topButtons;
    public GameObject[] rows;
    public Text[] rowTitles;
    public Text[] rowSummaries;
    public RawImage[] rowPictures;
    public Text sectionLabel;
    public Text pageLabel;
    public Button previousPage;
    public Button nextPage;
    public Button reshuffleButton;
    public Text detailTitle;
    public Text detailAuthor;
    public Text detailDescription;
    public Text platformNotice;
    public RawImage detailPicture;
    public Button previousWorld;
    public Button nextWorld;
    public Text returnLabel;
    public Button portalButton;
    public Text portalButtonLabel;
    public Text statusLabel;
    public Button retryButton;
    public Image[] navImages;
    public Text adTitle;
    public Text adDescription;
    public Text adSponsor;
    public RawImage adPicture;
    public Text adDownloadStatus;
    public Text adQrStatus;
    public GameObject initialLoadingPanel;
    public Text initialLoadingLabel;
    public CanvasGroup initialTopGroup;
    public Text bannerTitle;
    public RawImage bannerPicture;
    public Button adPortalButton;
    public Text adPortalLabel;
    public GameObject sponsoredWorldPanel;
    public Button[] sponsoredWorldButtons;
    public RawImage[] sponsoredWorldPictures;
    public Text[] sponsoredWorldTitles;
    public RawImage sponsorQrPicture;
    public InputField sponsorUrlField;
    public Button sponsorBannerButton;
    public Text sponsorLinkHelp;
    public GameObject sponsorDisclosure;
    public CanvasGroup sponsorBannerFade;
    public CanvasGroup[] sponsorWorldFades;

    [UdonSynced] public string pinnedSnapshot = "";
    [UdonSynced] public string sharedPortalWorldId = "";
    [UdonSynced] public double nextPortalTime;
    [UdonSynced] public bool portalCooldownStarted;
    [UdonSynced] public double portalExpiresAt;
    [UdonSynced] public bool sharedPortalFromDraw;
    [UdonSynced] public string sharedPortalDrawTitle = "";

    // One independent, editor-wired source per scene owns transport and cached textures.
    [HideInInspector] public bool isSharedDataSource;
    [HideInInspector] public MetaReaWallGuide sharedDataSource;
    [HideInInspector] public MetaReaWallGuide[] sharedDisplays;
    [UdonSynced] public string sharedRecommendedDraw = "";
    [UdonSynced] public string sharedRandomDraw = "";
    [UdonSynced] public double sharedRecommendedExpiry;
    [UdonSynced] public double sharedRandomExpiry;
    [UdonSynced] public double sharedDrawCooldownEnd;
    [UdonSynced] public double sharedDrawBusyEnd;
    [UdonSynced] public int sharedDrawBusyTab = -1;
    private string[] committedDrawJson = new string[2];
    private string[] appliedSharedDrawJson = new string[2];
    private string[] attemptedSharedDrawJson = new string[2];
    private bool receivingSharedDraw;
    private bool sharedImageUnavailable;
    private string sponsorStatusJson = "";
    private int sponsorStatusVersion;
    private int appliedSponsorStatusVersion = -1;
    private IVRCImageDownload[] retiredDrawDownloads = new IVRCImageDownload[4];
    [HideInInspector] public int feedRequestCount;
    public bool anonymousStatistics = true;
    [HideInInspector] public VRCUrl[] statisticsUrls;
    [HideInInspector] public bool statisticsRuntimeEnabled;
    public string statisticsSite = "unknown";
    public string statisticsEnvironment = "production";
    private int[] statisticsQueue = new int[64];
    private float[] statisticsQueueTimes = new float[64];
    private int statisticsHead, statisticsCount;
    private bool statisticsBoardSeen;
    private float nextStatisticsSend = 60f;
    private int statisticsSent;
    [HideInInspector] public int statisticsDropped;

    [HideInInspector] public VRCUrl[] clickUrls;
    [HideInInspector] public VRCUrl[] portalClickUrls;
    private int[] adClickSlots, topClickSlots;
    private bool[] adClickAllowed, topClickAllowed, adPortalEnabled;
    private int[] clickQueueSlots, clickQueueOrigins;
    private string[] clickQueueIds, clickQueueKeys;
    private float[] clickQueueTimes, clickLastTimes;
    private string[] clickLastAdIds;
    private int clickHead, clickCount, clickSent;
    private float nextClickSend, clickRequestStarted;
    private string clickRequestUrl = "";

    private int ClickEntity(string id, string key, int slot, int origin)
    {
        if (slot < 0 || slot >= 4096 || (origin != 0 && origin != 1 && origin != 2)) return -1;
        if (origin == 2) origin = 0;
        int count = origin == 0 ? topCount : adCount;
        for (int i = 0; i < count; i++)
        {
            if (origin == 0)
            { if (topIds[i] == id && topKeys[i] == key && topClickSlots[i] == slot && topClickAllowed[i] && IsTopActive(i)) return i; }
            else if (adIds[i] == id && adKeys[i] == key && adClickSlots[i] == slot && adClickAllowed[i] && adShowInBanner[i] && IsAdActive(i)) return i;
        }
        return -1;
    }

    private void QueueAdClick(string id, string key, int slot, int origin)
    {
        if (!onlineMode || !anonymousStatistics) return;
        if (sharedDataSource != null && sharedDataSource != this) { sharedDataSource.QueueAdClick(id, key, slot, origin); return; }
        if (clickUrls == null || clickUrls.Length != 8192 || clickSent >= 128 || clickCount >= 32) return;
        int entity = ClickEntity(id, key, slot, origin); if (entity < 0) return;
        int ad = origin == 0 ? topAds[entity] : entity;
        if (origin == 0 && (topTypes[entity] == 0 || (topTypes[entity] == 2 && !CanUseWorld(topWorlds[entity])))) return;
        if (!CanRecordAdDetail(ad)) return;
        string advertisementId = adIds[ad];
        if (clickQueueSlots == null)
        {
            clickQueueSlots = new int[32]; clickQueueOrigins = new int[32];
            clickQueueIds = new string[32]; clickQueueKeys = new string[32]; clickQueueTimes = new float[32];
            clickLastTimes = new float[128]; clickLastAdIds = new string[128];
        }
        // One detail view per advertisement in 30 minutes, across surfaces,
        // boards, slot changes and content revisions. No personal identifier.
        int debounce = -1;
        for (int i = 0; i < clickLastAdIds.Length; i++)
        {
            bool expired = string.IsNullOrEmpty(clickLastAdIds[i]) || Time.realtimeSinceStartup - clickLastTimes[i] >= 1800f;
            if (clickLastAdIds[i] == advertisementId && !expired) return;
            if (debounce < 0 && expired) debounce = i;
        }
        if (debounce < 0) return;
        clickLastAdIds[debounce] = advertisementId; clickLastTimes[debounce] = Time.realtimeSinceStartup;
        int tail = (clickHead + clickCount) % 32;
        clickQueueSlots[tail] = slot; clickQueueOrigins[tail] = origin;
        clickQueueIds[tail] = id; clickQueueKeys[tail] = key; clickQueueTimes[tail] = Time.realtimeSinceStartup;
        clickCount++;
    }

    // Direct-portal attribution is a separate operation count, with no detail-view debounce.
    // It is queued only after the local portal request was accepted.
    private void QueueAdPortal(string id, string key, int slot)
    {
        if (!onlineMode || !anonymousStatistics) return;
        if (sharedDataSource != null && sharedDataSource != this) { sharedDataSource.QueueAdPortal(id, key, slot); return; }
        if (portalClickUrls == null || portalClickUrls.Length != 4096 || clickSent >= 128 || clickCount >= 32) return;
        int entity = ClickEntity(id, key, slot, 0);
        if (entity < 0 || topTypes[entity] != 0 || !CanUseWorld(topWorlds[entity])) return;
        if (clickQueueSlots == null)
        {
            clickQueueSlots = new int[32]; clickQueueOrigins = new int[32];
            clickQueueIds = new string[32]; clickQueueKeys = new string[32]; clickQueueTimes = new float[32];
            clickLastTimes = new float[128]; clickLastAdIds = new string[128];
        }
        int tail = (clickHead + clickCount) % 32;
        clickQueueSlots[tail] = slot; clickQueueOrigins[tail] = 2;
        clickQueueIds[tail] = id; clickQueueKeys[tail] = key; clickQueueTimes[tail] = Time.realtimeSinceStartup;
        clickCount++;
    }
    private VRCUrl AdEventUrl(int index)
    {
        if (index < 0) return null;
        if (index < 8192) return clickUrls != null && index < clickUrls.Length ? clickUrls[index] : null;
        index -= 8192;
        return portalClickUrls != null && index < portalClickUrls.Length ? portalClickUrls[index] : null;
    }
    private bool CanRecordAdDetail(int ad)
    { return IsAdActive(ad) && adDetailCells[ad] >= 0 && adDetailCells[ad] < 16 && (!adPortalEnabled[ad] || CanUseWorld(adWorlds[ad])); }

    private int TakeAdClick()
    {
        if (!onlineMode || !anonymousStatistics) { clickCount = 0; return -1; }
        if (guideV3 && !V3TelemetryAllowed()) return -1;
        if (sharedDataSource != null || clickSent >= 128 || clickUrls == null || clickUrls.Length != 8192) return -1;
        if (!string.IsNullOrEmpty(clickRequestUrl))
        { if (Time.realtimeSinceStartup - clickRequestStarted < 20f) return -1; clickRequestUrl = ""; }
        if (Time.realtimeSinceStartup < nextClickSend || loading || sponsorStatusLoading || pendingDrawTab >= 0 || imageRequestInFlight || InitialImagesPending()) return -1;
        if (Time.realtimeSinceStartup - lastFeedAttempt < 6f || Time.realtimeSinceStartup - sponsorStatusStarted < 6f || Time.realtimeSinceStartup - drawStartedAt < 6f) return -1;
        while (clickCount > 0)
        {
            int at = clickHead; clickHead = (clickHead + 1) % 32; clickCount--;
            if (Time.realtimeSinceStartup - clickQueueTimes[at] > 120f || ClickEntity(clickQueueIds[at], clickQueueKeys[at], clickQueueSlots[at], clickQueueOrigins[at]) < 0) continue;
            int index = clickQueueOrigins[at] == 2 ? 8192 + clickQueueSlots[at] : clickQueueSlots[at] * 2 + clickQueueOrigins[at];
            VRCUrl url = AdEventUrl(index);
            if (url == null || string.IsNullOrEmpty(url.Get())) continue;
            clickSent++; nextClickSend = Time.realtimeSinceStartup + 5.1f;
            nextStatisticsSend = Mathf.Max(nextStatisticsSend, nextClickSend);
            return index;
        }
        return -1;
    }

    private void PumpAdClicks()
    {
        if (!StatisticsRuntimeAllowed()) return;
        int index = TakeAdClick(); if (index < 0) return;
        clickRequestUrl = AdEventUrl(index).Get(); clickRequestStarted = Time.realtimeSinceStartup;
        if (guideV3) v3NextStringAt = Time.realtimeSinceStartup+5.1f;
        VRCStringDownloader.LoadUrl(AdEventUrl(index), (IUdonEventReceiver)this);
    }

    private bool IsAdClickResponse(string url)
    {
        if (!url.StartsWith("https://metarea.net/api/udon/ad-click/") && !url.StartsWith("https://metarea.net/api/udon/ad-portal/")) return false;
        if (url == clickRequestUrl) clickRequestUrl = "";
        return true;
    }

    // schema 2: local readiness, accepted local request, accepted owner request.
    // Bounded best-effort queue: no event merging, no retry after dispatch.
    private void QueueStatistics(int kind)
    {
        if (!onlineMode || !anonymousStatistics || kind < 0 || kind >= 3) return;
        if (sharedDataSource != null && sharedDataSource != this) { sharedDataSource.QueueStatistics(kind); return; }
        if (kind == 0 && statisticsBoardSeen) return;
        if (statisticsUrls == null || statisticsUrls.Length != 12) return;
        if (statisticsSent >= 128 || statisticsCount >= 64) { statisticsDropped++; return; }
        int protocol = !guideV3 ? 0 : !v3Initialized ? 3 : v4Protocol ? 2 : 1;
        int tail = (statisticsHead + statisticsCount) % 64;
        statisticsQueue[tail] = kind * 4 + protocol;
        statisticsQueueTimes[tail] = Time.realtimeSinceStartup;
        statisticsCount++;
        if (kind == 0) statisticsBoardSeen = true;
    }

    private void QueueBoardReady()
    {
        if (!isSharedDataSource && ready && (!v4Layout || v4TopReady) && gameObject.activeInHierarchy && topPanel != null && topPanel.transform.parent != null && topPanel.transform.parent.gameObject.activeInHierarchy)
            QueueStatistics(0);
    }
    private bool IsStatisticsUrl(string url)
    {
        if (statisticsUrls == null) return false;
        for (int i = 0; i < statisticsUrls.Length; i++)
            if (statisticsUrls[i] != null && statisticsUrls[i].Get() == url) return true;
        return false;
    }

    private int TakeStatisticsEvent()
    {
        if (!onlineMode || !anonymousStatistics) { statisticsCount = 0; return -1; }
        if (guideV3 && !V3TelemetryAllowed()) return -1;
        if (sharedDataSource != null || statisticsSent >= 128 || Time.realtimeSinceStartup < nextStatisticsSend) return -1;
        if (loading || sponsorStatusLoading || pendingDrawTab >= 0 || imageRequestInFlight || !string.IsNullOrEmpty(clickRequestUrl)) return -1;
        if (Time.realtimeSinceStartup - lastFeedAttempt < 6f || Time.realtimeSinceStartup - sponsorStatusStarted < 6f) return -1;
        while (statisticsCount > 0)
        {
            int at = statisticsHead; statisticsHead = (statisticsHead + 1) % 64; statisticsCount--;
            int index = statisticsQueue[at];
            if (Time.realtimeSinceStartup - statisticsQueueTimes[at] > 300f || statisticsUrls == null || statisticsUrls.Length != 12 ||
                statisticsUrls[index] == null || string.IsNullOrEmpty(statisticsUrls[index].Get())) { statisticsDropped++; continue; }
            statisticsSent++; nextStatisticsSend = Time.realtimeSinceStartup + 10f; nextClickSend = Time.realtimeSinceStartup + 5.1f;
            return index;
        }
        return -1;
    }

    private bool StatisticsRuntimeAllowed()
    { return statisticsRuntimeEnabled && Networking.LocalPlayer != null; }

    private void PumpStatistics()
    {
        // Enabled only on a build copy. Editor/ClientSim scenes keep this false.
        if (!StatisticsRuntimeAllowed()) return;
        int index = TakeStatisticsEvent();
        if (index >= 0)
        {
            if (guideV3) v3NextStringAt = Time.realtimeSinceStartup + 5.1f;
            VRCStringDownloader.LoadUrl(statisticsUrls[index], (IUdonEventReceiver)this);
        }
    }

    [HideInInspector] public int drawRequestCount;
    [HideInInspector] public int statusRequestCount;
    [HideInInspector] public int imageRequestCount;
    [HideInInspector] public RawImage xIconImage;
    [HideInInspector] public RawImage boothIconImage;
    public Color boothIconColor = new Color(.18f,.73f,.95f,1f);
    [HideInInspector] public RawImage archiveIconImage;
    public Color xIconColor = new Color(.18f, .73f, .95f, 1f);
    public bool archiveIconUseOriginalColor = true;
    [HideInInspector] public Material archiveIconTintMaterial;
    public Color archiveIconColor = new Color(.88f, .94f, 1f, 1f);
    public Color brandTitleColor = new Color(.18f, .73f, .95f, 1);
    public Color guideTitleColor = new Color(.88f, .94f, 1f, 1);
    [HideInInspector] public RawImage brandTitleImage;
    [HideInInspector] public RawImage guideTitleImage;
    [HideInInspector] public string archiveUrl = "https://metarea.net/";
    [HideInInspector] public GameObject archiveUrlPanel;
    [HideInInspector] public InputField archiveUrlField;

    private string appliedSnapshot = "";
    private string appliedPortalId = "";
    private string[] ids;
    private string[] portalIds;
    private bool[] sponsorOnly;
    private string[] titles;
    private string[] authors;
    private string[] descriptions;
    private bool[] recommended;
    private bool[] pc;
    private bool[] android;
    private int[] pcSupport, androidSupport, iosSupport;
    public Text[] topPlatforms, manyPlatforms, rowPlatforms, rowAuthors;
    public Text detailPlatforms;
    private int PlatformState(DataDictionary world, string key)
    {
        DataToken value;
        var support = V3Object(world, "platformSupport");
        if (support == null || !support.TryGetValue(key, TokenType.Boolean, out value)) return 0;
        return value.Boolean ? 1 : -1;
    }
    private void ReadPlatforms(DataDictionary world, int index)
    {
        pcSupport[index] = PlatformState(world, "pc"); androidSupport[index] = PlatformState(world, "android"); iosSupport[index] = PlatformState(world, "ios");
    }
    private string PlatformMark(int state) { return state > 0 ? "✓" : state < 0 ? "—" : "?"; }
    private string PlatformWord(int state) { return state > 0 ? "対応" : state < 0 ? "非対応" : "未確認"; }
    private string Platforms(int index)
    {
        if (pcSupport == null || index < 0 || index >= pcSupport.Length) return "PC ?  Android ?  iOS ?";
        return "PC " + PlatformMark(pcSupport[index]) + "  Android " + PlatformMark(androidSupport[index]) + "  iOS " + PlatformMark(iosSupport[index]);
    }
    private int[] pictureIndices;
    private int[] order;
    private int count;
    private int baseWorldCount;
    private int orderCount;
    private int page;
    private int tab;
    private int view;
    private int selected;
    private int beforeAdView;
    private int dailyOne = -1;
    private int dailyTwo = -1;
    private int dailyChill = -1;
    [HideInInspector] public bool manyVersion;
    [HideInInspector] public Button[] manyButtons;
    [HideInInspector] public RawImage[] manyPictures;
    [HideInInspector] public Image[] manyPhotoGradients;
    [HideInInspector] public Image[] manyTitleBackplates;
    [HideInInspector] public Shadow[] manyCaptionShadows, manyTitleShadows;
    [HideInInspector] public Text[] manyCaptions, manyTitles, manyAuthors;
    [HideInInspector] public Text[] manySummaries;
    private int[] manyWorlds = new int[4];
    private int selectedManySlot = -1;

    private bool TopWorldAvailable(int world)
    { return ready && CanUseWorld(world) && !sponsorOnly[world]; }

    private string TopIntroduction(string description)
    {
        if (string.IsNullOrEmpty(description)) return "";
        string text = description.Trim();
        int end = text.Length;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '\n' || c == '\r') { end = i; break; }
            if (c == '。' || c == '！' || c == '？' || c == '!' || c == '?' ||
                (c == '.' && (i+1 == text.Length || text[i+1] == ' ' || text[i+1] == '\n' || text[i+1] == '\r')))
            { end = i+1; break; }
        }
        return text.Substring(0,end).Trim();
    }

    private void BuildManyWorlds()
    {
        for (int i = 0; i < 4; i++) manyWorlds[i] = -1;
        if (!ready) return;
        manyWorlds[0] = TopWorldAvailable(dailyOne) ? dailyOne : -1;
        manyWorlds[1] = TopWorldAvailable(dailyTwo) ? dailyTwo : -1;
        // v3 base worlds are a daily/ad/image union, NOT introduction order.
        // Use the same dedicated latest segment and eligibility as LATEST.
        int latestStart = guideV3 && onlineMode ? 192 : 0;
        int latestLength = guideV3 && onlineMode ? v3LatestCount : baseWorldCount;
        for (int i = 0; i < latestLength; i++)
            if (TopWorldAvailable(latestStart+i)) { manyWorlds[2] = latestStart+i; break; }
        if (onlineMode)
        {
            for (int i = 0; i < drawCounts[0]; i++)
                if (TopWorldAvailable(64+i)) { manyWorlds[3] = 64+i; break; }
        }
        else
        {
            for (int w = 0; w < baseWorldCount; w++)
                if (TopWorldAvailable(w) && recommended[w]) { manyWorlds[3] = w; break; }
        }
    }
    private void RenderMany()
    {
        BuildManyWorlds();
        for (int k = 0; k < 4; k++)
        {
            int w = manyWorlds[k]; bool valid = TopWorldAvailable(w);
            bool compact = ready && sponsoredCount > 0;
            float height = compact ? 255f : 387f;
            float photoHeight = compact ? 194f : 230f;
            Place((RectTransform)manyButtons[k].transform, (k % 2) * 388, 44 + (k / 2) * (compact ? 267 : 391), 372, height);
            Place(manyPictures[k].rectTransform, 8, 8, 356, photoHeight);
            Place(manyCaptions[k].rectTransform, compact ? 16 : 10, compact ? 14 : photoHeight + 18, compact ? 340 : 352, compact ? 22 : 28);
            manyCaptions[k].resizeTextForBestFit = false;
            manyCaptions[k].fontSize = compact ? 14 : 18;
            Place(manyTitles[k].rectTransform, compact ? 16 : 10, compact ? photoHeight - 22 : photoHeight + 56, compact ? 340 : 352, 30);
            if (manyPhotoGradients != null && k < manyPhotoGradients.Length && manyPhotoGradients[k] != null)
            {
                manyPhotoGradients[k].gameObject.SetActive(false);
                Place(manyPhotoGradients[k].rectTransform,12,photoHeight-46,348,24);
            }
            if (manyTitleBackplates != null && k < manyTitleBackplates.Length && manyTitleBackplates[k] != null)
            {
                manyTitleBackplates[k].gameObject.SetActive(compact);
                Place(manyTitleBackplates[k].rectTransform,12,photoHeight-22,348,30);
            }
            if (manyCaptionShadows != null && k < manyCaptionShadows.Length && manyCaptionShadows[k] != null) manyCaptionShadows[k].enabled = compact;
            if (manyTitleShadows != null && k < manyTitleShadows.Length && manyTitleShadows[k] != null) manyTitleShadows[k].enabled = compact;
            manyTitles[k].fontSize = 20;
            manyTitles[k].resizeTextForBestFit = false;
            manyTitles[k].resizeTextMinSize = 20;
            manyTitles[k].resizeTextMaxSize = compact ? 20 : 24;
            Place(manyAuthors[k].rectTransform, 226, height - (compact ? 27 : 29), 136, compact ? 24 : 26);
            if (manyPlatforms != null && k < manyPlatforms.Length && manyPlatforms[k] != null) { Place(manyPlatforms[k].rectTransform, 10, height - (compact ? 27 : 29), 212, compact ? 24 : 26); manyPlatforms[k].text = valid ? Platforms(w) : ""; }
            manyAuthors[k].alignment = compact ? TextAnchor.UpperRight : TextAnchor.MiddleRight;
            manyButtons[k].interactable = valid;
            manyCaptions[k].text = k == 0 ? "今日のランダムワールド①" : k == 1 ? "今日のランダムワールド②" : k == 2 ? "紹介最新ワールド" : "おすすめランダムワールド";
            bool waiting = !ready && !failed, loadFailed = !ready && failed;
            if (ready && k == 2 && guideV3 && onlineMode)
            {
                waiting = !v3LatestComplete && !v3LatestFailed && v3ListCounts[2] > v3LatestCount;
                loadFailed = v3LatestFailed;
            }
            if (ready && k == 3 && onlineMode)
            {
                loadFailed = !string.IsNullOrEmpty(drawErrors[0]);
                waiting = !loadFailed && (!drawHasResult[0] || (guideV3 && !v3DrawComplete[0] && v3ListCounts[0] > drawCounts[0]));
            }
            manyTitles[k].text = valid ? FitSingleLine(manyTitles[k],titles[w],k) : guideV3 && onlineMode && v3Maintenance ? "メンテナンス中" : waiting ? "読み込み中" : loadFailed ? "読み込みに失敗しました" : "掲載対象がありません";
            if (compact)
            {
                if (manyPhotoGradients != null && k < manyPhotoGradients.Length && manyPhotoGradients[k] != null)
                    Place(manyPhotoGradients[k].rectTransform,12,photoHeight-46,Mathf.Min(348f,manyCaptions[k].preferredWidth+8f),24);
                if (manyTitleBackplates != null && k < manyTitleBackplates.Length && manyTitleBackplates[k] != null)
                    Place(manyTitleBackplates[k].rectTransform,12,photoHeight-22,Mathf.Min(348f,manyTitles[k].preferredWidth+8f),30);
            }
            manyAuthors[k].text = valid ? FitSingleLine(manyAuthors[k], string.IsNullOrEmpty(authors[w]) ? "" : "by " + authors[w], 10+k) : "";
            if (manySummaries != null && k < manySummaries.Length && manySummaries[k] != null)
            {
                Place(manySummaries[k].rectTransform,10,photoHeight+(compact ? 10 : 96),352,24);
                manySummaries[k].text = valid ? FitSingleLine(manySummaries[k],TopIntroduction(descriptions[w]),4+k) : "";
            }
            SetPhoto(manyPictures[k], valid ? w : -1);
            if (!valid && ready && (waiting || loadFailed))
                SetPlaceholder(manyPictures[k], waiting ? 0 : 2);
        }
    }

    public void _Many0() { OpenMany(0); }
    public void _Many1() { OpenMany(1); }
    public void _Many2() { OpenMany(2); }
    public void _Many3() { OpenMany(3); }
    private void OpenMany(int slot) { BuildManyWorlds(); if (manyVersion && slot >= 0 && slot < 4 && TopWorldAvailable(manyWorlds[slot])) { selectedManySlot = slot; Open(manyWorlds[slot]); } }
    private int ManyPosition()
    { if (selectedManySlot >= 0 && selectedManySlot < 4 && manyWorlds[selectedManySlot] == selected) return selectedManySlot; for (int k = 0; k < 4; k++) if (manyWorlds[k] == selected) return k; return -1; }
    private bool CanStepMany(int direction)
    { BuildManyWorlds(); int p = ManyPosition(); if (p < 0) return false; for (int k = p + direction; k >= 0 && k < 4; k += direction) if (TopWorldAvailable(manyWorlds[k])) return true; return false; }
    private void StepMany(int direction)
    { BuildManyWorlds(); int p = ManyPosition(); if (p < 0) return; for (int k = p + direction; k >= 0 && k < 4; k += direction) if (TopWorldAvailable(manyWorlds[k])) { OpenMany(k); return; } }
    private int adWorld = -1;
    private string adName = "MetaRea WORLD ARCHIVE";
    private string adCopy = "次に行く世界を、ここから。";
    private string sponsor = "MetaRea";
    private string revision = "";
    private bool ready;
    private bool loading;
    private bool failed;
    private VRCImageDownloader imageDownloader;
    private IVRCImageDownload atlasDownload;
    private Texture2D atlasTexture;
    private int snapshotIndex = -1;
    private bool imageLoading;
    private bool imageFailed;
    private IVRCImageDownload sponsorDownload;
    private Texture2D sponsorTexture;
    private bool sponsorImageLoading;
    private bool sponsorImageFailed;
    private double lastSponsorImageAttempt = -999;
    private string[] adNames;
    private string[] adDescriptions;
    private string[] adSponsors;
    private string[] adUrls;
    private int[] adCells;
    private int[] adQrCells;
    private int[] adWorlds;
    private int[] adBannerRows;
    private bool bannerAtlasV2;
    private bool bannerAtlasV3;
    private int[] adBannerPages;
    private int[] adDetailCells;
    private int extraSponsorPages;
    private int detailSponsorPages;
    private bool detailAtlasV2;
    private int detailCellsPerPage = 4;
    private int topSponsorPages;
    private int[] adTopImageIndices;
    private bool[] adShowInBanner;
    private int[] topImageIndices, topWeights;
    private bool[] topGoldFrames;
    // Slot 0: extra banner; slots 1..4: details; slots 5..6: TOP images.
    private Texture2D[] extraSponsorTextures = new Texture2D[7];
    private IVRCImageDownload[] extraSponsorDownloads = new IVRCImageDownload[7];
    private bool[] extraSponsorLoading = new bool[7];
    private bool[] extraSponsorFailed = new bool[7];
    private float[] extraSponsorAttempts = new float[] { -999f, -999f, -999f, -999f, -999f, -999f, -999f };
    private int adCount;
    private int activeAdCount;
    private string[] adIds;
    private string[] adKeys;
    private int[] adTrackingSlots;
    private bool[] adTrackingAllowed;
    [HideInInspector] public VRCUrl[] detailTrackingUrls;
    private bool IsDetailTrackingResult(IVRCImageDownload result)
    {
        if (!result.Url.Get().StartsWith("https://metarea.net/api/udon/ad-detail/")) return false;

        result.Dispose();
        return true;
    }
    private double[] adStarts;
    private double[] adEnds;
    private bool[] adAllowed;
    private bool[] adActive;
    private string[] placementIds;
    private string[] placementKeys;
    private int[] placementWorlds;
    private double[] placementStarts;
    private double[] placementEnds;
    private bool[] placementAllowed;
    private bool[] placementActive;
    private int placementCount;
    private bool sponsorStatusProtocol;
    private bool sponsorStatusValid;
    private bool sponsorStatusLoading;
    private bool sponsorStatusFailed;
    private double sponsorStatusUtc;
    private float sponsorStatusReceived;
    private float sponsorStatusStarted;
    private float nextSponsorStatusAttempt;
    private int currentAd = -1;
    private int openedAd = -1;
    private int[] adOrder;
    private int adCursor;
    private int[] sponsoredWorlds;
    private int sponsoredCount;
    private int[] sponsorOrder;
    private int sponsorCursor;
    private int sponsorLeft = -1;
    private int sponsorRight = -1;
    private bool sponsoredDetail;
    private bool topPlacementProtocol;
    private int topCount;
    private string[] topIds, topKeys, topAdTitles;
    private int[] topTypes, topWorlds, topAds;
    private double[] topStarts, topEnds;
    private bool[] topAllowed, topActive;
    private int openedTop = -1;
    private float nextSponsorRotation;
    private bool sponsorRotationReady;
    private int sponsorFadeStage;
    private float sponsorFadeStarted;
    private double lastImageAttempt = -999;
    private double lastFeedAttempt = -999;
    private double localPortalRequestTime = -999;
    private float nextUiTick;
    private int[] rowMap = new int[4];
    private int[] cachedOrders = new int[256];
    private int[] cachedCounts = new int[4];
    private int[] cachedPages = new int[4];
    private bool[] cachedTabs = new bool[4];
    // Fixed segments keep the instance snapshot and both shared draw categories independent.
    // 0..63: shared snapshot; 64..127: recommended; 128..191: random.
    private bool[] drawHasResult = new bool[2];
    private int[] drawCounts = new int[2];
    private int[] drawTotalCandidates = new int[2];
    private Texture2D[] drawTextures = new Texture2D[2];
    private IVRCImageDownload[] drawDownloads = new IVRCImageDownload[2];
    private string[] drawErrors = new string[2];
    private int pendingDrawTab = -1;
    private int pendingDrawAtlas = -1;
    private string pendingDrawJson = "";
    private string pendingDrawGeneration = "";
    private bool[] initialDrawAttempted = new bool[2];
    private float drawCooldownUntil;
    private float drawStartedAt;
    private float drawLeaseUntil;
    private bool drawLoadingMetadata;
    private bool drawImageRequested;
    private bool drawVerifying;
    private bool drawReadyToCommit;
    private IVRCImageDownload pendingDrawDownload;
    private Texture2D pendingDrawTexture;
    private Color32[] drawGenerationPixels = new Color32[2048];
    private float nextImageRequestAt;
    private float imageRequestStarted;
    private bool imageRequestInFlight;
    private Text reshuffleLabel;

    [HideInInspector] public bool guideV3;
    [HideInInspector] public bool preferGuideV4;
    [HideInInspector] public VRCUrl v4FeedUrl, v4StatusUrl;
    [HideInInspector] public VRCUrl[] v4ManifestUrls, v4SetUrls, v4LatestUrls, v4ImageUrls;
    [UdonSynced] public int sharedGuideProtocol;
    private bool v4Protocol, v4Layout, v4TopReady;
    private VRCUrl v4LegacyFeed, v4LegacyStatus;
    private string v4PendingBootstrap = "", v4PendingStatus = "";
    private int v4PendingBank = -1;
    private string v4PendingGeneration = "";
    private float v4PendingStatusAt;
    private double v4PendingUntil;
    private int v4TopUsed, v4BannerUsed, v4LastTop = -1, v4LastBanner = -1;
    private bool v4FirstTopCycle = true, v4FirstBannerCycle = true;
    private DataDictionary[] v4PreviewImages = new DataDictionary[16];
    private int v4ProbeFailures;
    [HideInInspector] public VRCUrl[] v3ManifestUrls;
    [HideInInspector] public VRCUrl[] v3SetUrls;
    [HideInInspector] public VRCUrl[] v3LatestUrls;
    [HideInInspector] public VRCUrl[] v3ImageUrls;
    [UdonSynced] public string v3SharedBaseGeneration = "";
    [UdonSynced] public int v3SharedBaseBank = -1;
    [UdonSynced] public string v3SharedRecommendedGeneration = "";
    [UdonSynced] public int v3SharedRecommendedBank = -1;
    [UdonSynced] public int v3SharedRecommendedSet;
    [UdonSynced] public int v3SharedRecommendedUsed = 1;
    [UdonSynced] public string v3SharedRandomGeneration = "";
    [UdonSynced] public int v3SharedRandomBank = -1;
    [UdonSynced] public int v3SharedRandomSet;
    [UdonSynced] public int v3SharedRandomUsed = 1;
    [UdonSynced] public int v3OwnerTerm;
    [UdonSynced] public int v3SelectionSequence;

    // Network state contains identifiers only. Every client verifies content and
    // publication status directly; an owner cannot authorize an advertisement.
    private string v3Bootstrap = "";
    private string v3BaseGeneration = "";
    private int v3BaseBank = -1;
    private string[] v3DrawGenerations = new string[2];
    private int[] v3DrawBanks = new int[] { -1, -1 };
    private int[] v3DrawSets = new int[] { -1, -1 };
    private string[] v3LocalDrawJson = new string[2];
    private bool[] v3DrawComplete = new bool[2];
    private DataDictionary[] v3WorldImages = new DataDictionary[256];
    private DataDictionary[] v3InitialImages = new DataDictionary[256];
    private string[] v3InitialWorldIds = new string[256];
    private int v3InitialWorldCount;
    private DataDictionary[] v3BannerImages = new DataDictionary[16];
    private DataDictionary[] v3QrImages = new DataDictionary[16];
    private DataDictionary[] v3DetailImages = new DataDictionary[16];
    private DataDictionary[] v3TopImages = new DataDictionary[16];
    private string[] v3InitialTopIds = new string[0];
    private string[] v3InitialBannerIds = new string[0];
    private int v3AdRotation;
    private int v3Version;
    private int v3DisplayVersion = -1;

    // There are only 32 possible image locations per bank. Definitions do not
    // allocate textures. The resident GPU cache has an independent hard limit.
    private DataDictionary[] v3Assets = new DataDictionary[1024];
    private string[] v3AssetGenerations = new string[1024];
    private int[] v3ImageAttempts = new int[1024];
    private float[] v3ImageRetryAt = new float[1024];
    private string[] v3BankGenerations = new string[32];
    private double[] v3BankExpiry = new double[32];
    private string[] v3CacheTokens = new string[12];
    private Texture2D[] v3CacheTextures = new Texture2D[12];
    private IVRCImageDownload[] v3CacheDownloads = new IVRCImageDownload[12];
    private float[] v3CacheUsed = new float[12];
    private int[] v3CacheSizes = new int[12];
    // Texture residency, including a conservative reservation for one download.
    // Transport/decode buffers belong to the SDK and are not included here.
#if UNITY_ANDROID
    private int v3MemoryLimit = 64 * 1024 * 1024;
#else
    private int v3MemoryLimit = 96 * 1024 * 1024;
#endif
    private int v3ReservedBytes;
    private bool v3PassiveImages;
    private float v3NextPrefetchAt;
    private bool v3ReadbackPending;
    private string v3ImageGeneration = "";
    [HideInInspector] public int residentImageBytes;
    [HideInInspector] public int peakImageBytes;
    [HideInInspector] public int imageEvictionCount;
    [HideInInspector] public int imageVerificationFailures;
    [HideInInspector] public int staleResponseCount;
    [HideInInspector] public int metadataTimeoutCount;
    [HideInInspector] public int serverWaitCount;
    private int v3ImageKey = -1;
    private IVRCImageDownload v3ImageDownload;
    private Texture2D v3ImageTexture;
    private bool v3Verifying;
    private Color32[] v3MarkerPixels;
    private string v3ImageToken = "";

    // At most one metadata request is issued at a time, including status.
    private VRCUrl v3RequestUrl;
    private int v3RequestKind; // 1 bootstrap, 2 catalog, 3 set, 4 status, 5 recovery status, 6 selection manifest
    private int v3RequestBank = -1;
    private string v3RequestGeneration = "";
    private int v3RequestCategory;
    private int v3RequestSet;
    private int v3RequestAttempts;
    private float v3RequestStarted;
    private float v3RequestRetryAt;
    private bool v3RequestInFlight;
    // LoadUrl has no request handle. Never reuse an unresolved URL after a
    // timeout: its eventual callback must drain before a newer request starts.
    private string[] v3WireUrls = new string[8];
    private bool[] v3WireAbandoned = new bool[8];
    private float[] v3WireStarted = new float[8];
    private float v3RequestQueuedAt;
    private int v3RequestOwnerTerm;
    private int v3RequestSelectionSequence;
    private string v3RequestBaseSelection = "";
    private float v3ManualRetryAt;
    private float v3DrawRetryAt;
    private float v3ServerWaitUntil;
    // Busy only throttles JSON; immutable images may continue from a valid manifest.
    private float v3JsonBusyUntil;
    private bool v3BusyResponse;
    private bool v3Initialized;
    private float v3NextStringAt;
    private bool v3BootstrapStopped;
    private string v3BootstrapAttemptGeneration = "";
    private bool v3StatusStopped;
    private float v3NextStatus;
    private double v3ServerTime;
    private float v3ServerReceived;
    private double v3StatusUntil;
    private string[] v3BlockedWorldIds = new string[0];
    private int v3LatestBank = -1;
    private string v3LatestGeneration = "";

    private int v3DrawCategory = -1;
    private int v3TargetBank = -1;
    private string v3TargetGeneration = "";
    private int v3TargetSet;
    private string v3PendingJson = "";
    private bool v3UserDraw;
    private bool v3AwaitCatalog;
    private int v3DrawOwnerTerm;
    private int v3DrawSequence;
    private float v3DrawDeadline;
    private string[] v3StoppedSelections = new string[2];
    private float v3NextMigrationAttempt;
    private bool v3RecoveryPending;
    private float v3RecoveryAt;
    private int v3RecoveryRound;
    private bool v3RecoveryBootstrap;
    private bool v3SerializationPending;
    private float v3SerializationRetryAt;

    public override void OnPostSerialization(VRC.Udon.Common.SerializationResult result)
    {
        if (!guideV3 || !onlineMode || sharedDataSource != null || !V3Owner()) return;
        v3SerializationPending = !result.success;
        if (v3SerializationPending) v3SerializationRetryAt = Time.realtimeSinceStartup+5f+Random.Range(0f,1f);
    }

    private void V3ArmRecovery()
    {
        if (v3RecoveryPending) return;
        float delay = v3RecoveryRound == 0 ? 60f : v3RecoveryRound == 1 ? 120f : v3RecoveryRound == 2 ? 240f : 270f;
        v3RecoveryRound = Mathf.Min(3,v3RecoveryRound+1);
        v3RecoveryAt = Time.realtimeSinceStartup+delay+Random.Range(0f,30f);
        v3ManualRetryAt = Mathf.Max(v3ManualRetryAt,v3RecoveryAt);
        v3RecoveryPending = true;
    }

    private void V3Recover()
    {
        if (!v3RecoveryPending || v3Maintenance || Time.realtimeSinceStartup < v3RecoveryAt || Time.realtimeSinceStartup < v3ServerWaitUntil || v3RequestKind != 0 || v3DrawCategory >= 0) return;
        v3RecoveryPending = false;
        // Only the owner may refresh the common base. Followers keep the
        // synchronized identifiers; a retry must never make a new local draw.
        v3RecoveryBootstrap = V3Owner() && (v3BootstrapStopped || !V3BankUsable(v3BaseBank,v3BaseGeneration) || V3SharedExpired(0) || V3SharedExpired(1));
        v3BootstrapStopped = false; v3LatestFailed = false;
        v3StoppedSelections[0] = ""; v3StoppedSelections[1] = "";
        for (int i = 0; i < v3ImageAttempts.Length; i++)
            if (v3ImageAttempts[i] >= 4) { v3ImageAttempts[i] = 0; v3ImageRetryAt[i] = 0; }
        failed = false; imageFailed = false; loading = !ready;
        v3NextStringAt = Mathf.Max(v3NextStringAt,Time.realtimeSinceStartup+Random.Range(.1f,1f));
        v3Version++;
    }

    private bool V3SelectionPending(int c)
    {
        return V3Hex(V3SharedGeneration(c)) && (v3DrawGenerations[c] != V3SharedGeneration(c) || v3DrawBanks[c] != V3SharedBank(c) || v3DrawSets[c] != V3SharedSet(c));
    }

    private bool V3SharedExpired(int c)
    {
        int bank = V3SharedBank(c);
        if (bank < 0 || bank >= 32 || !V3Hex(V3SharedGeneration(c))) return false;
        return (!string.IsNullOrEmpty(v3BankGenerations[bank]) && v3BankGenerations[bank] != V3SharedGeneration(c)) || (v3BankExpiry[bank] > 0 && V3Now() >= v3BankExpiry[bank]);
    }

    private void V3PublishSelection()
    {
        if (!v3UserDraw || !V3Owner() || v3DrawOwnerTerm != v3OwnerTerm || v3DrawSequence != v3SelectionSequence) return;
        int c = v3DrawCategory;
        if (c < 0 || c > 1) return;
        int limit = c == 0 ? 8 : 16;
        int used = v3TargetGeneration == V3SharedGeneration(c) ? (c == 0 ? v3SharedRecommendedUsed : v3SharedRandomUsed) : 0;
        if ((used & ((1 << limit)-1)) == (1 << limit)-1) used = 0;
        used |= 1 << v3TargetSet;
        if (c == 0) { v3SharedRecommendedGeneration = v3TargetGeneration; v3SharedRecommendedBank = v3TargetBank; v3SharedRecommendedSet = v3TargetSet; v3SharedRecommendedUsed = used; }
        else { v3SharedRandomGeneration = v3TargetGeneration; v3SharedRandomBank = v3TargetBank; v3SharedRandomSet = v3TargetSet; v3SharedRandomUsed = used; }
        v3SelectionSequence++; v3DrawSequence = v3SelectionSequence;
        // Publish verified metadata BEFORE waiting for the owner's image.
        // From here even the owner is a follower of this immutable selection.
        v3UserDraw = false; v3AwaitCatalog = false;
        sharedDrawCooldownEnd = TimeNow()+15;
        RequestSerialization(); v3Version++;
    }

    private void V3FinishSharedDraw(int c)
    {
        if (V3Owner() && c < 2 && sharedDrawBusyTab == (c == 0 ? 1 : 3) && v3DrawSequence == v3SelectionSequence)
        { sharedDrawBusyTab = -1; sharedDrawBusyEnd = 0; RequestSerialization(); }
    }


    private int v3LatestCount;
    private int[] v3ListCounts = new int[3];
    private bool v3LatestComplete;
    private bool v3LatestFailed;
    private string v3LatestJson = "";
    [HideInInspector] public GameObject maintenancePanel;
    [HideInInspector] public GameObject maintenanceNotice;
    [HideInInspector] public Text maintenanceMessage;
    [HideInInspector] public Text maintenanceHint;
    [HideInInspector] public Text maintenanceNoticeText;
    private bool v3Maintenance;
    private int v3NormalDrawFontSize;
    private const string MaintenanceMessage = "ただいまメンテナンス中です。\n５分程度で終了します。\nしばらくお待ちください。";

    private void Start()
    {
        nextStatisticsSend = Time.realtimeSinceStartup + 60f + Random.Range(0f,15f);
        if (isSharedDataSource) { SendCustomEventDelayedSeconds(nameof(_Initialize), 1f); return; }
        if (archiveUrlPanel != null) archiveUrlPanel.SetActive(false);
        if (reshuffleButton != null) reshuffleLabel = reshuffleButton.GetComponentInChildren<Text>(true);
        if (xUrlPanel != null) xUrlPanel.SetActive(false);
        if (boothUrlPanel != null) boothUrlPanel.SetActive(false);
        if (favoritesEnabled && favoritesSettings != null)
        {
            favoriteWorldIds = favoritesSettings.favoriteWorldIds;
            favoriteWorldTitles = favoritesSettings.favoriteWorldTitles;
            favoriteWorldAuthors = favoritesSettings.favoriteWorldAuthors;
            favoriteFolderNames = favoritesSettings.favoriteFolderNames;
            favoriteFolderDescriptions = favoritesSettings.favoriteFolderDescriptions;
            favoriteFlatList = favoritesSettings.favoriteFlatList;
            favoriteFolderIndices = favoritesSettings.favoriteFolderIndices;
        }
        BuildFavorites();
        InitializeDisplayMode();
        _ApplyAppearance();
        if (portalMarker != null) portalMarker.gameObject.SetActive(false);
        if (editorPortalPreview != null) editorPortalPreview.SetActive(false);
        Render();
        SendCustomEventDelayedSeconds(nameof(_Initialize), 1f);
    }

    public void _Initialize()
    {
        if (guideV3 && onlineMode) { V3Initialize(); return; }
        if (sharedDataSource != null) { PullSharedData(); return; }
        if (!string.IsNullOrEmpty(pinnedSnapshot)) { ApplySnapshot(); return; }
        if (Networking.LocalPlayer != null && !Networking.IsOwner(gameObject))
        {
            if (statusLabel != null) statusLabel.text = "インスタンスの初期データを待っています…";
            return;
        }
        if (onlineMode) BeginDownload();
        else if (sampleFeed != null && Parse(sampleFeed.text))
        {
            pinnedSnapshot = sampleFeed.text;
            appliedSnapshot = pinnedSnapshot;
            RequestSerialization();
            Render();
        }
        else { failed = true; Render(); }
    }

    private void BeginDownload()
    {
        if (sharedDataSource != null) return;
        if (loading || !string.IsNullOrEmpty(pinnedSnapshot)) return;
        if (Time.realtimeSinceStartup - lastFeedAttempt < 5) return;
        lastFeedAttempt = Time.realtimeSinceStartup;
        if (feedUrl == null || string.IsNullOrEmpty(feedUrl.Get()))
        { failed = true; Render(); return; }
        loading = true;
        failed = false;
        Render();
        feedRequestCount++;
        VRCStringDownloader.LoadUrl(feedUrl, (IUdonEventReceiver)this);
    }

    public override void OnStringLoadSuccess(IVRCStringDownload result)
    {
        if (IsAdClickResponse(result.Url.Get())) return;
        if (IsStatisticsUrl(result.Url.Get())) return;
        if (guideV3 && onlineMode) { V3StringSuccess(result); return; }
        if (IsDrawMetadataUrl(result.Url.Get()))
        {
            if (!drawLoadingMetadata || pendingDrawTab < 0 || result.Url.Get() != DrawUrl(pendingDrawTab).Get()) return;
            drawLoadingMetadata = false;
            if (!ValidateDraw(result.Result)) { FailDraw("抽選結果を取得できませんでした"); return; }
            pendingDrawJson = result.Result;
            if (pendingDrawAtlas < 0) { drawReadyToCommit = true; TryCommitDraw(); }
            else PumpImageDownloads();
            Render(); return;
        }
        if (sponsorStatusUrl != null && result.Url.Get() == sponsorStatusUrl.Get())
        {
            bool accepted = ApplySponsorStatus(result.Result);

            if (!accepted) FailSponsorStatus();
            Render(); return;
        }
        loading = false;
        if (!Networking.IsOwner(gameObject) || !string.IsNullOrEmpty(pinnedSnapshot)) return;
        if (Parse(result.Result))
        {

            // Pin the actual small metadata snapshot, not a mutable URL. This
            // also gives late joiners the same metadata without a new request.
            pinnedSnapshot = result.Result;
            appliedSnapshot = pinnedSnapshot;
            RequestSerialization();
        }
        else { failed = true; }
        Render();
    }

    public override void OnStringLoadError(IVRCStringDownload result)
    {
        if (IsAdClickResponse(result.Url.Get())) return;
        if (IsStatisticsUrl(result.Url.Get())) return;
        if (guideV3 && onlineMode)
        { if (V3TakeStringResponse(result.Url.Get())) V3RequestFailed(false); return; }
        if (IsDrawMetadataUrl(result.Url.Get()))
        {
            if (drawLoadingMetadata && pendingDrawTab >= 0 && result.Url.Get() == DrawUrl(pendingDrawTab).Get())
                FailDraw("抽選に失敗しました。再試行できます");
            return;
        }
        if (sponsorStatusUrl != null && result.Url.Get() == sponsorStatusUrl.Get())
        { FailSponsorStatus(); Render(); return; }
        loading = false; if (!string.IsNullOrEmpty(pinnedSnapshot)) return; failed = true; Render();
    }

    public void _Retry()
    {
        if (guideV3 && onlineMode) { V3Retry(); return; }
        if (sharedDataSource != null) { sharedDataSource._Retry(); return; }
        if (isSharedDataSource) { attemptedSharedDrawJson[0] = ""; attemptedSharedDrawJson[1] = ""; }
        if (!string.IsNullOrEmpty(pinnedSnapshot))
        {
            ApplySnapshot(); imageFailed = false; sponsorImageFailed = false;
            for (int i = 0; i < 7; i++) extraSponsorFailed[i] = false;
            PumpImageDownloads(); BeginSponsorStatusDownload(); Render(); return;
        }
        if (Networking.LocalPlayer == null || Networking.IsOwner(gameObject)) _RetryOnOwner();
        else SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(_RetryOnOwner));
    }

    [NetworkCallable(maxEventsPerSecond: 1)]
    public void _RetryOnOwner()
    {
        if (Networking.LocalPlayer != null && !Networking.IsOwner(gameObject)) return;
        if (string.IsNullOrEmpty(pinnedSnapshot)) _Initialize();
    }

    public override void OnDeserialization()
    {
        if (guideV3 && onlineMode) { if (sharedDataSource == null) V3ReceiveSelection(); ApplyPortal(); return; }
        if (sharedDataSource == null) ApplySnapshot();
        if (isSharedDataSource) ReceiveSharedDraw();
        else ApplyPortal();
    }

    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        if (sharedDataSource != null) return;
        if (player != null && player.isLocal && (isSharedDataSource || (guideV3 && onlineMode)))
        {
            if (guideV3) v3OwnerTerm++;
            sharedDrawBusyTab = -1; sharedDrawBusyEnd = 0;
            RequestSerialization();
        }
        if (guideV3 && onlineMode) { V3Initialize(); return; }
        if (player.isLocal && string.IsNullOrEmpty(pinnedSnapshot))
        { loading = false; SendCustomEventDelayedSeconds(nameof(_Initialize), 1f); }
    }

    public override void OnPlayerJoined(VRCPlayerApi player)
    { if (Networking.IsOwner(gameObject)) RequestSerialization(); }

    private void ApplySnapshot()
    {
        if (string.IsNullOrEmpty(pinnedSnapshot) || pinnedSnapshot == appliedSnapshot) return;
        if (Parse(pinnedSnapshot)) appliedSnapshot = pinnedSnapshot;
        else failed = true;
        Render();
    }

    private string Str(DataDictionary d, string key)
    { DataToken t; return d.TryGetValue(key, TokenType.String, out t) ? t.String : ""; }
    private bool Flag(DataDictionary d, string key)
    { DataToken t; return d.TryGetValue(key, TokenType.Boolean, out t) && t.Boolean; }
    private int Integer(DataDictionary d, string key, int fallback)
    {
        DataToken t;
        if (!d.TryGetValue(key, TokenType.Double, out t)) return fallback;
        double v = t.Double;
        if (v < -1 || v > 100000 || v != (int)v) return fallback;
        return (int)v;
    }
    private int FindId(string id)
    {
        if (string.IsNullOrEmpty(id)) return -1;
        for (int i = 0; i < count; i++) if (ids[i] == id) return i;
        return -1;
    }

    private bool Parse(string json)
    {
        if (string.IsNullOrEmpty(json) || json.Length > (guideV3 ? 300000 : 60000)) return false;
        DataToken root;
        if (!VRCJson.TryDeserializeFromJson(json, out root) || root.TokenType != TokenType.DataDictionary) return false;
        DataDictionary d = root.DataDictionary;
        if (Str(d, "schema") != (guideV3 && onlineMode ? (v4Protocol ? "metarea-guide-v4" : "metarea-guide-v3") : "metarea-wall-v1")) return false;
        int incomingSnapshot = Integer(d, "snapshotIndex", -1);
        if (onlineMode && !guideV3 && (incomingSnapshot < 0 || atlasUrls == null || incomingSnapshot >= atlasUrls.Length)) return false;
        DataToken wt;
        if (!d.TryGetValue("worlds", TokenType.DataList, out wt)) return false;
        DataList list = wt.DataList;
        int n = list.Count;
        if (n < (v4Protocol ? 0 : 1) || n > 64) return false;
        // Validate first, so malformed responses never replace working data.
        for (int i = 0; i < n; i++)
        {
            if (list[i].TokenType != TokenType.DataDictionary) return false;
            DataDictionary w = list[i].DataDictionary;
            string id = Str(w, "id");
            if (!id.StartsWith("wrld_") || id.Length != 41 || string.IsNullOrEmpty(Str(w, "name"))) return false;
            int imageIndex = Integer(w, "imageIndex", -1);
            if (imageIndex > 63) return false;
            for (int j = 0; j < i; j++) if (Str(list[j].DataDictionary, "id") == id) return false;
            string portal = Str(w, "portalWorldId");
            if (!string.IsNullOrEmpty(portal) && (!portal.StartsWith("wrld_") || portal.Length != 41)) return false;
        }
        baseWorldCount = n; count = guideV3 && onlineMode ? 256 : 192;
        ids = new string[count]; titles = new string[count]; authors = new string[count]; descriptions = new string[count];
        pcSupport = new int[count]; androidSupport = new int[count]; iosSupport = new int[count];
        recommended = new bool[count]; pc = new bool[count]; android = new bool[count]; pictureIndices = new int[count];
        portalIds = new string[count]; sponsorOnly = new bool[count];
        for (int i = 0; i < count; i++) pictureIndices[i] = -1;
        for (int i = 0; i < n; i++)
        {
            DataDictionary w = list[i].DataDictionary;
            ids[i] = Str(w, "id"); titles[i] = Str(w, "name"); descriptions[i] = Str(w, "description");
            authors[i] = Str(w, "authorName");
            ReadPlatforms(w, i); recommended[i] = Flag(w, "recommended"); pc[i] = Flag(w, "pc"); android[i] = Flag(w, "android");
            pictureIndices[i] = Integer(w, "imageIndex", -1);
            sponsorOnly[i] = Flag(w, "isSponsorOnly");
            string portalId = Str(w, "portalWorldId");
            portalIds[i] = string.IsNullOrEmpty(portalId) ? ids[i] : portalId;
        }
        dailyOne = FindId(Str(d, "dailyOne")); dailyTwo = FindId(Str(d, "dailyTwo")); dailyChill = FindId(Str(d, "dailyChill"));
        if (dailyOne >= 0 && sponsorOnly[dailyOne]) dailyOne = -1;
        if (dailyTwo >= 0 && sponsorOnly[dailyTwo]) dailyTwo = -1;
        if (dailyChill >= 0 && sponsorOnly[dailyChill]) dailyChill = -1;
        if (dailyTwo == dailyOne) dailyTwo = -1;
        revision = Str(d, "revision");
        DataToken at;
        adWorld = -1;
        adName = "広告掲載準備中"; adCopy = "現在、掲載中の広告はありません。"; sponsor = "—";
        if (d.TryGetValue("ad", TokenType.DataDictionary, out at))
        {
            DataDictionary a = at.DataDictionary;
            adName = Str(a, "title"); adCopy = Str(a, "description"); sponsor = Str(a, "sponsor");
            adWorld = FindId(Str(a, "worldId"));
        }
        ReadSponsors(d);
        ready = true; failed = false; loading = false;
        snapshotIndex = incomingSnapshot;
        tab = 0; view = favoriteTabSelected ? 4 : 0; page = 0; selected = dailyOne >= 0 ? dailyOne : 0;
        cachedTabs = new bool[4]; cachedPages = new int[4];
        BuildOrder();
        if (onlineMode && !guideV3) { BeginSponsorStatusDownload(); PumpImageDownloads(); }
        return true;
    }

    private void ReadSponsors(DataDictionary d)
    {
        adCount = 0; activeAdCount = 0; currentAd = -1; openedAd = -1; sponsoredCount = 0; sponsoredDetail = false;
        placementCount = 0; sponsorLeft = -1; sponsorRight = -1;
        sponsorStatusProtocol = v4Protocol || Integer(d, "sponsorStatusVersion", 0) == 1;
        sponsorStatusValid = false; sponsorStatusLoading = false; sponsorStatusFailed = false;
        sponsorStatusStarted = -999f; nextSponsorStatusAttempt = 0;
        bannerAtlasV3 = Str(d, "sponsorAtlasLayout") == "banner-v3";
        bannerAtlasV2 = bannerAtlasV3 || Str(d, "sponsorAtlasLayout") == "banner-v2";
        extraSponsorPages = bannerAtlasV3 ? 0 : Mathf.Clamp(Integer(d, "sponsorBannerPages", 1) - 1, 0, 1);
        detailAtlasV2 = Str(d, "sponsorDetailAtlasLayout") == "detail-v2";
        detailCellsPerPage = detailAtlasV2 ? 9 : 4;
        detailSponsorPages = detailAtlasV2 ? Mathf.Clamp(Integer(d, "sponsorDetailPages", 0), 0, 2) : Str(d, "sponsorDetailAtlasLayout") == "detail-v1" ? Mathf.Clamp(Integer(d, "sponsorDetailPages", 0), 0, 4) : 0;
        topSponsorPages = Str(d, "sponsorTopAtlasLayout") == "top-v1" ? Mathf.Clamp(Integer(d, "sponsorTopPages", 0), 0, 2) : 0;
        DataToken token;
        if (d.TryGetValue("ads", TokenType.DataList, out token))
        {
            DataList list = token.DataList;
            int capacity = Mathf.Min(16, list.Count);
            adNames = new string[capacity]; adDescriptions = new string[capacity]; adSponsors = new string[capacity]; adUrls = new string[capacity];
            adCells = new int[capacity]; adQrCells = new int[capacity]; adWorlds = new int[capacity];
            adBannerRows = new int[capacity];
            adBannerPages = new int[capacity]; adDetailCells = new int[capacity];
            adIds = new string[capacity]; adKeys = new string[capacity];
            adTrackingSlots = new int[capacity]; adTrackingAllowed = new bool[capacity];
            adClickSlots = new int[capacity]; adClickAllowed = new bool[capacity]; adPortalEnabled = new bool[capacity];
            adStarts = new double[capacity]; adEnds = new double[capacity];
            adAllowed = new bool[capacity]; adActive = new bool[capacity];
            adTopImageIndices = new int[capacity]; adShowInBanner = new bool[capacity];
            for (int i = 0; i < capacity; i++)
            {
                if (list[i].TokenType != TokenType.DataDictionary) continue;
                DataDictionary a = list[i].DataDictionary;
                string url = Str(a, "url");
                if (!url.StartsWith("https://") || string.IsNullOrEmpty(Str(a, "title"))) continue;
                if (!sponsorStatusProtocol || string.IsNullOrEmpty(Str(a, "id")) || Str(a, "contentKey").Length != 64 || !ValidPeriod(a)) continue;
                int cell = Integer(a, "imageIndex", -1); int qr = Integer(a, "qrImageIndex", -1);
                if (cell > 63 || qr > 63) continue;
                int row = Integer(a, "bannerRow", i);
                int bannerPage = Integer(a, "bannerPage", 0);
                int detailCell = Integer(a, "detailImageIndex", -1);
                if (!v4Protocol && (bannerPage < 0 || bannerPage > extraSponsorPages || detailCell >= detailSponsorPages * detailCellsPerPage || detailCell > 15)) continue;
                if (bannerAtlasV2 && (row < 0 || row >= (bannerAtlasV3 ? 16 : 8))) continue;
                adNames[adCount] = Str(a, "title"); adDescriptions[adCount] = Str(a, "description");
                adSponsors[adCount] = Str(a, "sponsor"); adUrls[adCount] = url;
                adCells[adCount] = cell; adQrCells[adCount] = qr; adWorlds[adCount] = FindId(Str(a, "worldId"));
                adBannerRows[adCount] = row;
                adBannerPages[adCount] = bannerPage; adDetailCells[adCount] = detailCell;
                int topImage = Integer(a, "topImageIndex", -1);
                adTopImageIndices[adCount] = topImage >= 0 && topImage < topSponsorPages * 8 ? topImage : -1;
                DataToken showToken;
                adShowInBanner[adCount] = !a.TryGetValue("showInBanner", out showToken) || (showToken.TokenType == TokenType.Boolean && showToken.Boolean);
                adIds[adCount] = Str(a, "id"); adKeys[adCount] = Str(a, "contentKey");
                int clickSlot = Integer(a, "clickTrackingSlot", -1);
                adClickSlots[adCount] = clickSlot >= 0 && clickSlot < 4096 ? clickSlot : -1;
                adPortalEnabled[adCount] = Str(a, "adType") == "world" || Str(a, "adType") == "image_world";
                int trackingSlot = Integer(a, "detailTrackingSlot", -1);
                adTrackingSlots[adCount] = trackingSlot >= 0 && trackingSlot < 65535 ? trackingSlot : -1;
                adStarts[adCount] = Number(a, "startsAtUnix"); adEnds[adCount] = Number(a, "endsAtUnix");
                adCount++;
            }
        }
        sponsoredWorlds = new int[16]; sponsorOrder = new int[16];
        placementIds = new string[16]; placementKeys = new string[16]; placementWorlds = new int[16];
        placementStarts = new double[16]; placementEnds = new double[16]; placementAllowed = new bool[16]; placementActive = new bool[16];
        if (sponsorStatusProtocol && d.TryGetValue("sponsoredWorlds", TokenType.DataList, out token))
        {
            DataList list = token.DataList;
            for (int i = 0; i < list.Count && placementCount < 16 - adCount; i++)
            {
                if (list[i].TokenType != TokenType.DataDictionary) continue;
                DataDictionary p = list[i].DataDictionary;
                int world = FindId(Str(p, "worldId")); if (world < 0 || string.IsNullOrEmpty(Str(p, "id")) || Str(p, "contentKey").Length != 64 || !ValidPeriod(p)) continue;
                bool duplicate = false; for (int j = 0; j < placementCount; j++) if (placementIds[j] == Str(p, "id")) duplicate = true;
                if (duplicate) continue;
                placementIds[placementCount] = Str(p, "id"); placementKeys[placementCount] = Str(p, "contentKey");
                placementWorlds[placementCount] = world; placementStarts[placementCount] = Number(p, "startsAtUnix"); placementEnds[placementCount] = Number(p, "endsAtUnix");
                placementCount++;
            }
        }
        ReadTopPlacements(d);
        adOrder = new int[adCount];
        adCursor = 0; sponsorCursor = 0;
        AdvanceSponsors();
    }

    private void ReadTopPlacements(DataDictionary d)
    {
        topCount = 0; openedTop = -1;
        topIds = new string[16]; topKeys = new string[16]; topAdTitles = new string[16];
        topClickSlots = new int[16]; topClickAllowed = new bool[16];
        topImageIndices = new int[16]; topWeights = new int[16]; topGoldFrames = new bool[16];
        topTypes = new int[16]; topWorlds = new int[16]; topAds = new int[16];
        topStarts = new double[16]; topEnds = new double[16]; topAllowed = new bool[16]; topActive = new bool[16];
        DataToken token;
        topPlacementProtocol = d.TryGetValue("topPlacements", out token);
        if (!topPlacementProtocol || token.TokenType != TokenType.DataList || !sponsorStatusProtocol) return;
        DataList list = token.DataList;
        for (int i = 0; i < list.Count && topCount < 16; i++)
        {
            if (list[i].TokenType != TokenType.DataDictionary) continue;
            DataDictionary p = list[i].DataDictionary;
            string type = Str(p, "type"); int kind = type == "world" ? 0 : type == "image" ? 1 : type == "image_world" ? 2 : -1;
            string id = Str(p, "id");
            if (kind < 0 || string.IsNullOrEmpty(id) || Str(p, "contentKey").Length != 64 || !ValidPeriod(p)) continue;
            bool duplicate = false; for (int j = 0; j < topCount; j++) if (topIds[j] == id) duplicate = true;
            if (duplicate) continue;
            int world = FindId(Str(p, "worldId")), ad = -1;
            for (int j = 0; j < adCount; j++) if (adIds[j] == Str(p, "adId")) ad = j;
            if ((kind != 1 && world < 0) || ((v4Protocol || kind != 0) && ad < 0)) continue;
            topIds[topCount] = id; topKeys[topCount] = Str(p, "contentKey"); topTypes[topCount] = kind;
            int clickSlot = Integer(p, "clickTrackingSlot", -1);
            topClickSlots[topCount] = clickSlot >= 0 && clickSlot < 4096 ? clickSlot : -1;
            topWorlds[topCount] = world; topAds[topCount] = ad;
            int topImage = Integer(p, "topImageIndex", ad >= 0 ? adTopImageIndices[ad] : -1);
            topImageIndices[topCount] = topImage >= 0 && topImage < topSponsorPages * 8 ? topImage : -1;
            topWeights[topCount] = Mathf.Clamp(Integer(p, "displayWeight", 1), 1, 10);
            DataToken goldToken;
            topGoldFrames[topCount] = p.TryGetValue("goldFrame", TokenType.Boolean, out goldToken) && goldToken.Boolean;
            topAdTitles[topCount] = Str(p, "title");
            if (string.IsNullOrEmpty(topAdTitles[topCount])) topAdTitles[topCount] = kind == 0 ? titles[world] : adNames[ad];
            topStarts[topCount] = Number(p, "startsAtUnix"); topEnds[topCount] = Number(p, "endsAtUnix");
            topCount++;
        }
    }

    private bool IsTopActive(int index)
    {
        if (index < 0 || index >= topCount || !StatusUsable() || !topAllowed[index] || !InPeriod(topStarts[index], topEnds[index])) return false;
        return (!v4Layout && topTypes[index] == 0) || IsAdActive(topAds[index]);
    }

    private bool TopPortalAvailable()
    {
        if (openedTop < 0) return IsAdActive(openedAd) && adPortalEnabled[openedAd] && CanUseWorld(adWorlds[openedAd]);
        return topPlacementProtocol && IsTopActive(openedTop) && (topTypes[openedTop] == 2 || (v4Layout && topTypes[openedTop] == 0)) && topAds[openedTop] == openedAd && CanUseWorld(topWorlds[openedTop]);
    }

    private double Number(DataDictionary d, string key)
    {
        DataToken t;
        if (!d.TryGetValue(key, TokenType.Double, out t)) return -1;
        double n = t.Double;
        return n >= 0 && n <= 253402300799d ? n : -1;
    }

    private bool ValidPeriod(DataDictionary d)
    {
        double start = Number(d, "startsAtUnix"); double end = Number(d, "endsAtUnix");
        return start >= 0 && end >= 0 && (end == 0 || end > start);
    }

    private bool StatusUsable()
    { return sponsorStatusValid && (guideV3 && onlineMode ? V3Now() < v3StatusUntil : Time.realtimeSinceStartup - sponsorStatusReceived < 300f); }

    private bool InPeriod(double start, double end)
    {
        double now = guideV3 && onlineMode ? V3Now() : sponsorStatusUtc + Time.realtimeSinceStartup - sponsorStatusReceived;
        return now >= start && (end == 0 || now < end);
    }

    private bool IsAdActive(int index)
    { return index >= 0 && index < adCount && StatusUsable() && adAllowed[index] && InPeriod(adStarts[index], adEnds[index]); }

    private bool IsSponsoredWorldActive(int world)
    {
        if (world < 0 || !StatusUsable()) return false;
        for (int i = 0; i < topCount; i++)
            if (topTypes[i] != 1 && topWorlds[i] == world && IsTopActive(i)) return true;
        for (int i = 0; i < placementCount; i++)
            if (placementWorlds[i] == world && placementAllowed[i] && InPeriod(placementStarts[i], placementEnds[i])) return true;
        for (int i = 0; i < adCount; i++)
            if (adPortalEnabled[i] && adWorlds[i] == world && IsAdActive(i)) return true;
        return false;
    }

    private bool CanUseWorld(int world)
    { return world >= 0 && world < count && !string.IsNullOrEmpty(ids[world]) && (!guideV3 || V3WorldAllowed(world)) && (!sponsorOnly[world] || IsSponsoredWorldActive(world)); }

    private void BeginSponsorStatusDownload()
    {
        if (guideV3 && onlineMode) { if (sharedDataSource == null) v3NextStatus = 0; return; }
        if (sharedDataSource != null) return;
        if (!onlineMode || !ready || sponsorStatusLoading || Time.realtimeSinceStartup - sponsorStatusStarted < 5f) return;
        if (!sponsorStatusProtocol || (adCount == 0 && placementCount == 0 && topCount == 0)) return;
        if (sponsorStatusUrl == null || string.IsNullOrEmpty(sponsorStatusUrl.Get())) { FailSponsorStatus(); return; }
        sponsorStatusLoading = true;
        sponsorStatusStarted = Time.realtimeSinceStartup;
        nextSponsorStatusAttempt = sponsorStatusStarted + 285f;
        statusRequestCount++;
        VRCStringDownloader.LoadUrl(sponsorStatusUrl, (IUdonEventReceiver)this);
    }

    private void FailSponsorStatus()
    {
        sponsorStatusLoading = false; sponsorStatusValid = false; sponsorStatusFailed = true;
        nextSponsorStatusAttempt = Time.realtimeSinceStartup + 30f;
        RefreshSponsorAvailability();
    }

    private bool ValidStatusList(DataList list)
    {
        if (list.Count > 16) return false;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].TokenType != TokenType.DataDictionary) return false;
            DataDictionary item = list[i].DataDictionary;
            string id = Str(item, "id");
            if (string.IsNullOrEmpty(id) || id.Length > 128 || Str(item, "contentKey").Length != 64 || !ValidPeriod(item)) return false;
            for (int j = 0; j < i; j++) if (Str(list[j].DataDictionary, "id") == id) return false;
        }
        return true;
    }

    private bool ApplySponsorStatus(string json)
    {
        if (!ready || !sponsorStatusProtocol || string.IsNullOrEmpty(json) || json.Length > (guideV3 ? 300000 : 60000)) return false;
        DataToken token;
        if (!VRCJson.TryDeserializeFromJson(json, out token) || token.TokenType != TokenType.DataDictionary) return false;
        DataDictionary d = token.DataDictionary;
        if (Str(d, "schema") != "metarea-sponsor-status-v1") return false;
        double generated = Number(d, "generatedAtUnix");
        if (generated <= 0 || generated < sponsorStatusUtc) return false;
        DataToken adsToken; DataToken worldsToken;
        if (!d.TryGetValue("ads", TokenType.DataList, out adsToken) || !d.TryGetValue("sponsoredWorlds", TokenType.DataList, out worldsToken)) return false;
        DataList ads = adsToken.DataList; DataList worlds = worldsToken.DataList;
        if (!ValidStatusList(ads) || !ValidStatusList(worlds)) return false;
        DataList tops = new DataList();
        if (topPlacementProtocol)
        {
            DataToken topToken;
            if (!d.TryGetValue("topPlacements", TokenType.DataList, out topToken) || !ValidStatusList(topToken.DataList)) return false;
            tops = topToken.DataList;
        }
        for (int i = 0; i < adCount; i++)
        {
            adAllowed[i] = false;
            adClickAllowed[i] = false;
            adTrackingAllowed[i] = false;
            for (int j = 0; j < ads.Count; j++)
            {
                DataDictionary item = ads[j].DataDictionary;
                if (Str(item, "id") != adIds[i] || Str(item, "contentKey") != adKeys[i]) continue;
                adAllowed[i] = true; adStarts[i] = Number(item, "startsAtUnix"); adEnds[i] = Number(item, "endsAtUnix");
                adClickAllowed[i] = adClickSlots[i] >= 0 && Integer(item, "clickTrackingSlot", -1) == adClickSlots[i];
                adTrackingAllowed[i] = adTrackingSlots[i] >= 0 && Integer(item, "detailTrackingSlot", -1) == adTrackingSlots[i];
            }
        }
        for (int i = 0; i < placementCount; i++)
        {
            placementAllowed[i] = false;
            for (int j = 0; j < worlds.Count; j++)
            {
                DataDictionary item = worlds[j].DataDictionary;
                if (Str(item, "id") != placementIds[i] || Str(item, "contentKey") != placementKeys[i]) continue;
                placementAllowed[i] = true; placementStarts[i] = Number(item, "startsAtUnix"); placementEnds[i] = Number(item, "endsAtUnix");
            }
        }
        for (int i = 0; i < topCount; i++)
        {
            topAllowed[i] = false;
            topClickAllowed[i] = false;
            for (int j = 0; j < tops.Count; j++)
            {
                DataDictionary item = tops[j].DataDictionary;
                if (Str(item, "id") != topIds[i] || Str(item, "contentKey") != topKeys[i]) continue;
                topAllowed[i] = true; topStarts[i] = Number(item, "startsAtUnix"); topEnds[i] = Number(item, "endsAtUnix");
                topClickAllowed[i] = topClickSlots[i] >= 0 && Integer(item, "clickTrackingSlot", -1) == topClickSlots[i];
            }
        }
        sponsorStatusJson = json; sponsorStatusVersion++;
        sponsorStatusUtc = generated; sponsorStatusReceived = Time.realtimeSinceStartup;
        sponsorStatusValid = true; sponsorStatusLoading = false; sponsorStatusFailed = false;
        nextSponsorStatusAttempt = Time.realtimeSinceStartup + 285f;
        RefreshSponsorAvailability();
        return true;
    }

    private bool RefreshSponsorAvailability()
    {
        bool changed = false;
        for (int i = 0; i < topCount; i++)
        {
            bool active = IsTopActive(i);
            if (topActive[i] != active) { topActive[i] = active; changed = true; }
        }
        for (int i = 0; i < adCount; i++)
        {
            bool active = IsAdActive(i);
            if (adActive[i] != active) { adActive[i] = active; changed = true; }
        }
        for (int i = 0; i < placementCount; i++)
        {
            bool active = StatusUsable() && placementAllowed[i] && InPeriod(placementStarts[i], placementEnds[i]);
            if (placementActive[i] != active) { placementActive[i] = active; changed = true; }
        }
        if (changed)
        {
            CancelSponsorFade();
            activeAdCount = 0;
            for (int i = 0; i < adCount; i++) if (adActive[i] && adShowInBanner[i]) adOrder[activeAdCount++] = i;
            sponsoredCount = 0;
            if (topPlacementProtocol)
            {
                for (int i = 0; i < topCount; i++) if (topActive[i]) sponsoredWorlds[sponsoredCount++] = i;
            }
            else for (int i = 0; i < placementCount; i++)
            {
                if (!placementActive[i]) continue;
                int world = placementWorlds[i]; bool duplicate = false;
                for (int j = 0; j < sponsoredCount; j++) if (sponsoredWorlds[j] == world) duplicate = true;
                if (!duplicate) sponsoredWorlds[sponsoredCount++] = world;
            }
            for (int i = 0; i < sponsoredCount; i++) sponsorOrder[i] = sponsoredWorlds[i];
            ShuffleAds(); ShuffleSponsorWorlds(); adCursor = 0; sponsorCursor = 0; AdvanceSponsors();
        }
        if (view == 3 && (!IsAdActive(openedAd) || (openedTop >= 0 && !IsTopActive(openedTop)))) { view = beforeAdView == 3 ? 0 : beforeAdView; openedAd = -1; openedTop = -1; changed = true; }
        if (view == 2 && sponsoredDetail && !IsSponsoredWorldActive(selected))
        { view = tab == 0 ? 0 : 1; sponsoredDetail = false; changed = true; }
        int portal = sharedPortalFromDraw ? -1 : FindId(sharedPortalWorldId);
        if (portal >= 0 && !CanUseWorld(portal))
        {
            if (portalMarker != null) portalMarker.gameObject.SetActive(false);
            if (editorPortalPreview != null) editorPortalPreview.SetActive(false);
            if (portalDestinationLabel != null) portalDestinationLabel.text = "";
            appliedPortalId = "";
            if (Networking.LocalPlayer == null || Networking.IsOwner(gameObject)) { sharedPortalWorldId = ""; RequestSerialization(); }
        }
        else if (portal >= 0) ApplyPortal();
        return changed;
    }

    private void ShuffleAds()
    {
        for (int i = activeAdCount - 1; i > 0; i--) { int j = Random.Range(0, i + 1); int v = adOrder[i]; adOrder[i] = adOrder[j]; adOrder[j] = v; }
        if (activeAdCount > 1 && adOrder[0] == currentAd) { int v = adOrder[0]; adOrder[0] = adOrder[1]; adOrder[1] = v; }
    }

    private void ShuffleSponsorWorlds()
    { for (int i = sponsoredCount - 1; i > 0; i--) { int j = Random.Range(0, i + 1); int v = sponsorOrder[i]; sponsorOrder[i] = sponsorOrder[j]; sponsorOrder[j] = v; } }

    private void AdvanceSponsors()
    {
        if (guideV3 && onlineMode) { V3AdvanceSponsors(); return; }
        if (activeAdCount > 0) { if (adCursor >= activeAdCount) { ShuffleAds(); adCursor = 0; } currentAd = adOrder[adCursor++]; }
        else currentAd = -1;
        if (topPlacementProtocol && sponsoredCount > 0)
        {
            sponsorLeft = PickWeightedTop(-1);
            sponsorRight = PickWeightedTop(sponsorLeft);
        }
        else if (sponsoredCount > 0)
        {
            if (sponsorCursor >= sponsoredCount) { ShuffleSponsorWorlds(); sponsorCursor = 0; }
            sponsorLeft = sponsorOrder[sponsorCursor++]; sponsorRight = -1;
            if (sponsoredCount > 1)
            {
                if (sponsorCursor >= sponsoredCount) { ShuffleSponsorWorlds(); sponsorCursor = 0; }
                sponsorRight = sponsorOrder[sponsorCursor++];
                if (sponsorRight == sponsorLeft) { sponsorRight = sponsorOrder[sponsorCursor % sponsoredCount]; sponsorCursor++; }
            }
        }
        else { sponsorLeft = -1; sponsorRight = -1; }
        sponsorRotationReady = false;
        nextSponsorRotation = Time.time + Mathf.Max(5f, sponsorRotationSeconds);
    }

    private bool BannerImageSettled(int ad)
    {
        if (guideV3 && onlineMode) return ad < 0 || ad >= adCount || V3RefLoaded(v3BannerImages[ad]);
        if (ad < 0 || ad >= adCount || adCells[ad] < 0) return true;
        if (adBannerPages[ad] == 0) return sponsorTexture != null || sponsorImageFailed;
        return extraSponsorTextures[0] != null || extraSponsorFailed[0];
    }

    private bool TopImageSettled(int index)
    {
        if (guideV3 && onlineMode) return V3TopLoaded(index);
        if (index < 0) return true;
        if (!topPlacementProtocol) return atlasTexture != null || imageFailed;
        if (index >= topCount) return true;
        int cell = topImageIndices[index];
        if (cell >= 0)
        {
            int slot = 5 + cell / 8;
            return extraSponsorTextures[slot] != null || extraSponsorFailed[slot];
        }
        if (topTypes[index] == 0) return atlasTexture != null || imageFailed;
        return BannerImageSettled(topAds[index]);
    }

    private bool SponsorImagesSettled()
    {
        // A terminal image failure must not freeze every other advertisement.
        return !onlineMode || (BannerImageSettled(currentAd) && TopImageSettled(sponsorLeft) && TopImageSettled(sponsorRight));
    }

    private void UpdateSponsorRotation()
    {
        if (sponsorFadeStage != 0) return;
        if (!SponsorImagesSettled())
        {
            sponsorRotationReady = false;
            return;
        }
        if (!sponsorRotationReady)
        {
            // Start a full display interval only after this selection has images.
            sponsorRotationReady = true;
            nextSponsorRotation = Time.time + Mathf.Max(5f, sponsorRotationSeconds);
            return;
        }
        if (Time.time >= nextSponsorRotation) BeginSponsorFade();
    }

    private int PickWeightedTop(int exclude)
    {
        int total = 0;
        for (int i = 0; i < topCount; i++) if (i != exclude && IsTopActive(i)) total += topWeights[i];
        if (total <= 0) return -1;
        int pick = Random.Range(0, total);
        for (int i = 0; i < topCount; i++)
        {
            if (i == exclude || !IsTopActive(i)) continue;
            pick -= topWeights[i]; if (pick < 0) return i;
        }
        return -1;
    }

    private void SetSponsorFade(float alpha, bool interactive)
    {
        if (sponsorBannerFade != null) { sponsorBannerFade.alpha = alpha; sponsorBannerFade.interactable = interactive; sponsorBannerFade.blocksRaycasts = interactive; }
        if (sponsorWorldFades != null) for (int i = 0; i < sponsorWorldFades.Length; i++)
        {
            if (sponsorWorldFades[i] == null) continue;
            sponsorWorldFades[i].alpha = alpha; sponsorWorldFades[i].interactable = interactive; sponsorWorldFades[i].blocksRaycasts = interactive;
        }
    }

    private void CancelSponsorFade()
    { sponsorFadeStage = 0; SetSponsorFade(1f, true); }

    private void BeginSponsorFade()
    {
        if (sponsorFadeStage != 0) return;
        if (activeAdCount <= 1 && sponsoredCount <= 2)
        {
            // Two cards already show every sponsored world; no needless flashing.
            nextSponsorRotation = Time.time + Mathf.Max(5f, sponsorRotationSeconds);
            return;
        }
        sponsorFadeStage = 1; sponsorFadeStarted = Time.time; SetSponsorFade(1f, false);
    }

    private void UpdateSponsorFade()
    {
        if (sponsorFadeStage == 0) return;
        if (view == 3) { CancelSponsorFade(); return; }
        float half = Mathf.Clamp(sponsorFadeSeconds, .2f, 2f) / 2f;
        float progress = Mathf.Clamp01((Time.time - sponsorFadeStarted) / half);
        float eased = progress * progress * (3f - 2f * progress);
        SetSponsorFade(sponsorFadeStage == 1 ? 1f - eased : eased, false);
        if (progress < 1f) return;
        if (sponsorFadeStage == 1)
        {
            AdvanceSponsors(); Render(); sponsorFadeStage = 2; sponsorFadeStarted = Time.time; SetSponsorFade(0f, false);
        }
        else { CancelSponsorFade(); nextSponsorRotation = Time.time + Mathf.Max(5f, sponsorRotationSeconds); }
    }

    public void _SponsoredWorld0() { OpenSponsored(sponsorLeft); }
    public void _SponsoredWorld1() { OpenSponsored(sponsorRight); }
    private void OpenSponsored(int world)
    {
        if (!ready) return;
        if (topPlacementProtocol)
        {
            if (!IsTopActive(world)) return;
            if (topTypes[world] == 0) { if (RequestPortal(topWorlds[world])) QueueAdPortal(topIds[world], topKeys[world], topClickSlots[world]); return; }
            CancelSponsorFade(); beforeAdView = 0; openedTop = world; openedAd = topAds[world]; view = 3; openedDetailRequestedAt = Time.realtimeSinceStartup; PumpImageDownloads(); Render();
            if (view == 3 && adPanel.activeSelf) QueueAdClick(topIds[world], topKeys[world], topClickSlots[world], 0);
            return;
        }
        if (!IsSponsoredWorldActive(world)) return;
        RequestPortal(world);
    }
    private int SponsoredPosition()
    { for (int i = 0; i < sponsoredCount; i++) if (sponsoredWorlds[i] == selected) return i; return -1; }

    private VRCUrl ExtraSponsorUrl(int slot)
    {
        if (snapshotIndex < 0) return null;
        if (slot == 0) return sponsorExtraAtlasUrls != null && snapshotIndex < sponsorExtraAtlasUrls.Length ? sponsorExtraAtlasUrls[snapshotIndex] : null;
        if (slot >= 5)
        {
            int topIndex = snapshotIndex * 2 + slot - 5;
            return sponsorTopAtlasUrls != null && topIndex < sponsorTopAtlasUrls.Length ? sponsorTopAtlasUrls[topIndex] : null;
        }
        int index = snapshotIndex * 4 + slot - 1;
        return sponsorDetailAtlasUrls != null && index < sponsorDetailAtlasUrls.Length ? sponsorDetailAtlasUrls[index] : null;
    }

    private int ExtraSponsorSlot(string url)
    {
        for (int slot = 0; slot < 7; slot++)
        { var candidate = ExtraSponsorUrl(slot); if (candidate != null && candidate.Get() == url) return slot; }
        return -1;
    }

    private bool IsAdInVisibleTop(int ad)
    {
        if (!gameObject.activeInHierarchy || topPanel == null || !topPanel.activeInHierarchy || !topPlacementProtocol) return false;
        return (sponsorLeft >= 0 && sponsorLeft < topCount && topAds[sponsorLeft] == ad) ||
            (sponsorRight >= 0 && sponsorRight < topCount && topAds[sponsorRight] == ad);
    }

    private float openedDetailRequestedAt;

    private int OpenedDetailSlot()
    {
        if (!gameObject.activeInHierarchy || view != 3 || !IsAdActive(openedAd)) return -1;
        int cell = adDetailCells[openedAd]; if (cell < 0 || cell >= 16) return -1;
        int slot = 1 + cell / detailCellsPerPage;
        return extraSponsorFailed[slot] || extraSponsorLoading[slot] || extraSponsorTextures[slot] != null || Time.realtimeSinceStartup - extraSponsorAttempts[slot] < 5f ? -1 : slot;
    }

    private int NextOpenedDetailSlot()
    {
        int slot = OpenedDetailSlot(); float newest = slot >= 0 ? openedDetailRequestedAt : -1f;
        if (isSharedDataSource && sharedDisplays != null)
            for (int d = 0; d < sharedDisplays.Length; d++)
            {
                var display = sharedDisplays[d]; if (display == null) continue;
                int candidate = display.OpenedDetailSlot();
                if (candidate >= 0 && (extraSponsorTextures[candidate] != null || extraSponsorLoading[candidate] || extraSponsorFailed[candidate])) continue;
                if (candidate >= 0 && display.openedDetailRequestedAt > newest)
                { slot = candidate; newest = display.openedDetailRequestedAt; }
            }
        return slot;
    }

    private int NextDetailPrefetchSlot()
    {
        int bestSlot = -1; int bestPriority = 4;
        for (int ad = 0; ad < adCount; ad++)
        {
            int cell = adDetailCells[ad];
            if (!IsAdActive(ad) || cell < 0 || cell >= 16) continue;
            int slot = 1 + cell / detailCellsPerPage;
            if (extraSponsorFailed[slot] || extraSponsorLoading[slot] || extraSponsorTextures[slot] != null || Time.realtimeSinceStartup - extraSponsorAttempts[slot] < 5f) continue;
            int priority = IsAdInVisibleTop(ad) ? 0 : view == 3 && openedAd == ad ? 1 : currentAd == ad ? 2 : 3;
            if (isSharedDataSource && sharedDisplays != null)
                for (int d = 0; d < sharedDisplays.Length; d++)
                {
                    var display = sharedDisplays[d];
                    if (display == null || !display.gameObject.activeInHierarchy) continue;
                    int other = display.IsAdInVisibleTop(ad) ? 0 : display.view == 3 && display.openedAd == ad ? 1 : display.currentAd == ad ? 2 : 3;
                    if (other < priority) priority = other;
                }
            if (priority < bestPriority) { bestPriority = priority; bestSlot = slot; }
        }
        return bestSlot;
    }

    // 0: TOP images, 1: extra banner, 2: background details, 3: opened detail.
    private void BeginExtraSponsorImages(int group)
    {
        if (!onlineMode || !ready || adCount == 0) return;
        int detailSlot = group == 3 ? NextOpenedDetailSlot() : group == 2 ? NextDetailPrefetchSlot() : -1;
        if (group >= 2 && detailSlot < 0) return;
        for (int slot = group == 0 ? 5 : group == 1 ? 0 : detailSlot; slot < (group == 0 ? 7 : group == 1 ? 1 : detailSlot + 1); slot++)
        {
            bool needed = slot >= 5 ? slot - 5 < topSponsorPages : slot == 0 ? extraSponsorPages > 0 : true;
            if (!needed || extraSponsorFailed[slot] || extraSponsorLoading[slot] || extraSponsorTextures[slot] != null || Time.realtimeSinceStartup - extraSponsorAttempts[slot] < 5f) continue;
            var url = ExtraSponsorUrl(slot);
            if (url == null || string.IsNullOrEmpty(url.Get())) { extraSponsorFailed[slot] = true; continue; }
            extraSponsorAttempts[slot] = Time.realtimeSinceStartup;
            extraSponsorLoading[slot] = true; extraSponsorFailed[slot] = false;
            if (imageDownloader == null) imageDownloader = new VRCImageDownloader();
            if (extraSponsorDownloads[slot] != null) extraSponsorDownloads[slot].Dispose();
            TextureInfo info = new TextureInfo(); info.GenerateMipMaps = false;
            info.FilterMode = slot == 0 ? FilterMode.Point : FilterMode.Bilinear;
            info.WrapModeU = TextureWrapMode.Clamp; info.WrapModeV = TextureWrapMode.Clamp;
            MarkImageRequest();
            extraSponsorDownloads[slot] = imageDownloader.DownloadImage(url, null, (IUdonEventReceiver)this, info);
            return;
        }
    }

    private bool ExtraSponsorImagesLoading()
    { for (int i = 0; i < 7; i++) if (extraSponsorLoading[i]) return true; return false; }
    private bool ExtraSponsorImagesFailed()
    { for (int i = 0; i < 7; i++) if (extraSponsorFailed[i] && !extraSponsorLoading[i]) return true; return false; }

    private void BeginSponsorImageDownload()
    {
        if (!onlineMode || !ready || adCount == 0 || sponsorImageFailed || sponsorImageLoading || sponsorTexture != null || Time.realtimeSinceStartup - lastSponsorImageAttempt < 5) return;
        if (sponsorAtlasUrls == null || snapshotIndex < 0 || snapshotIndex >= sponsorAtlasUrls.Length) { sponsorImageFailed = true; return; }
        lastSponsorImageAttempt = Time.realtimeSinceStartup; sponsorImageLoading = true; sponsorImageFailed = false;
        if (imageDownloader == null) imageDownloader = new VRCImageDownloader();
        if (sponsorDownload != null) { sponsorDownload.Dispose(); sponsorDownload = null; }
        TextureInfo info = new TextureInfo(); info.GenerateMipMaps = false; info.FilterMode = FilterMode.Point;
        info.WrapModeU = TextureWrapMode.Clamp; info.WrapModeV = TextureWrapMode.Clamp;
        MarkImageRequest();
        sponsorDownload = imageDownloader.DownloadImage(sponsorAtlasUrls[snapshotIndex], null, (IUdonEventReceiver)this, info);
    }

    private void BeginImageDownload()
    {
        if (!onlineMode || !ready || imageFailed || imageLoading || atlasTexture != null || Time.realtimeSinceStartup - lastImageAttempt < 5) return;
        if (snapshotIndex < 0 || atlasUrls == null || snapshotIndex >= atlasUrls.Length) { imageFailed = true; return; }
        VRCUrl url = atlasUrls[snapshotIndex];
        if (url == null || string.IsNullOrEmpty(url.Get())) { imageFailed = true; return; }
        lastImageAttempt = Time.realtimeSinceStartup; imageLoading = true; imageFailed = false;
        if (imageDownloader == null) imageDownloader = new VRCImageDownloader();
        if (atlasDownload != null) { atlasDownload.Dispose(); atlasDownload = null; }
        TextureInfo info = new TextureInfo();
        info.GenerateMipMaps = false;
        info.FilterMode = FilterMode.Bilinear;
        info.WrapModeU = TextureWrapMode.Clamp;
        info.WrapModeV = TextureWrapMode.Clamp;
        MarkImageRequest();
        atlasDownload = imageDownloader.DownloadImage(url, null, (IUdonEventReceiver)this, info);
    }

    private void MarkImageRequest()
    { imageRequestCount++; imageRequestInFlight = true; imageRequestStarted = Time.realtimeSinceStartup; nextImageRequestAt = imageRequestStarted + 5.1f; }

    private bool InitialImagesPending()
    {
        if (guideV3 && onlineMode) return !ready || (V3Texture(v3BaseBank,v3BaseGeneration,0) == null && !imageFailed);
        if (atlasTexture == null && !imageFailed) return true;
        if (adCount == 0) return false;
        if (sponsorTexture == null && !sponsorImageFailed) return true;
        if (extraSponsorPages > 0 && extraSponsorTextures[0] == null && !extraSponsorFailed[0]) return true;
        for (int slot = 5; slot < 5 + topSponsorPages; slot++)
            if (extraSponsorTextures[slot] == null && !extraSponsorFailed[slot]) return true;
        return false;
    }

    private void PumpImageDownloads()
    {
        if (guideV3 && onlineMode) { if (sharedDataSource != null) sharedDataSource.V3PumpImages(); else V3PumpImages(); return; }
        if (sharedDataSource != null) { if (sharedDataSource != this) sharedDataSource.PumpImageDownloads(); return; }
        if (!onlineMode || !ready || imageRequestInFlight || drawVerifying || Time.realtimeSinceStartup < nextImageRequestAt) return;
        // A clicked detail wins the next available request; never cancel an
        // in-flight image or bypass the SDK interval. Passive prefetch stays last.
        BeginExtraSponsorImages(3); if (imageRequestInFlight) return;
        BeginImageDownload(); if (imageRequestInFlight) return;
        BeginExtraSponsorImages(0); if (imageRequestInFlight) return;
        BeginSponsorImageDownload(); if (imageRequestInFlight) return;
        BeginExtraSponsorImages(1); if (imageRequestInFlight) return;
        BeginDrawImage(); if (imageRequestInFlight) return;
        if (pendingDrawTab < 0 && !InitialImagesPending()) BeginExtraSponsorImages(2);
    }

    public override void OnImageLoadSuccess(IVRCImageDownload result)
    {
        if (IsDetailTrackingResult(result)) return;
        if (guideV3 && onlineMode) { V3ImageSuccess(result); return; }
        if (result.Url.Get().Contains("/draw-atlas/")) { VerifyDrawImage(result); return; }
        int extraSlot = ExtraSponsorSlot(result.Url.Get());
        if (extraSlot >= 0)
        {
            if (result != extraSponsorDownloads[extraSlot]) { result.Dispose(); return; }
            imageRequestInFlight = false;
            extraSponsorLoading[extraSlot] = false;
            int width = extraSlot > 0 && extraSlot < 5 && detailAtlasV2 ? 2016 : 2048;
            int height = extraSlot >= 5 ? 1536 : extraSlot == 0 ? 2048 : detailAtlasV2 ? 1701 : 1728;
            if (result.Result == null || result.Result.width != width || result.Result.height != height)
            { result.Dispose(); extraSponsorDownloads[extraSlot] = null; extraSponsorFailed[extraSlot] = true; Render(); return; }
            extraSponsorTextures[extraSlot] = result.Result; extraSponsorFailed[extraSlot] = false; Render(); return;
        }
        if (sponsorAtlasUrls != null && snapshotIndex >= 0 && snapshotIndex < sponsorAtlasUrls.Length && result.Url.Get() == sponsorAtlasUrls[snapshotIndex].Get())
        {
            if (result != sponsorDownload) { result.Dispose(); return; }
            imageRequestInFlight = false;
            sponsorImageLoading = false;
            if (result.Result == null || result.Result.width != 2048 || result.Result.height != 2048)
            { result.Dispose(); sponsorDownload = null; sponsorImageFailed = true; Render(); return; }
            sponsorTexture = result.Result; sponsorImageFailed = false; Render(); return;
        }
        if (result != atlasDownload) { result.Dispose(); return; }
        imageRequestInFlight = false; imageLoading = false;
        // v1 is an 8 x 8 atlas with 256 x 144 cells, ordered from top-left.
        if (result.Result == null || result.Result.width != 2048 || result.Result.height != 1152)
        { result.Dispose(); atlasDownload = null; imageFailed = true; Render(); return; }
        atlasTexture = result.Result; imageFailed = false;
        Render();
    }

    public override void OnImageLoadError(IVRCImageDownload result)
    {
        if (IsDetailTrackingResult(result)) return;
        if (guideV3 && onlineMode) { if (result == v3ImageDownload) V3ImageFailed(); return; }
        if (result.Url.Get().Contains("/draw-atlas/"))
        { if (pendingDrawTab >= 0 && result == pendingDrawDownload) { imageRequestInFlight = false; FailDraw("抽選画像の取得に失敗しました。再試行できます"); } return; }
        int extraSlot = ExtraSponsorSlot(result.Url.Get());
        if (extraSlot >= 0)
        { if (result != extraSponsorDownloads[extraSlot]) return; imageRequestInFlight = false; extraSponsorLoading[extraSlot] = false; extraSponsorFailed[extraSlot] = true; Render(); return; }
        if (sponsorAtlasUrls != null && snapshotIndex >= 0 && snapshotIndex < sponsorAtlasUrls.Length && result.Url.Get() == sponsorAtlasUrls[snapshotIndex].Get())
        { if (result != sponsorDownload) return; imageRequestInFlight = false; sponsorImageLoading = false; sponsorImageFailed = true; }
        else { if (result != atlasDownload) return; imageRequestInFlight = false; imageLoading = false; imageFailed = true; }
        Render();
    }

    private void OnDestroy()
    { if (imageDownloader != null) imageDownloader.Dispose(); }

    private Texture Picture(int i)
    {
        if (onlineMode)
        {
            if (i < 0 || i >= count || pictureIndices[i] < 0) return null;
            return i < 64 ? atlasTexture : drawTextures[i < 128 ? 0 : 1];
        }
        if (samplePictures == null || samplePictures.Length == 0 || i < 0) return null;
        // Never show an unrelated bundled photo for a newly received world.
        if (i >= count || pictureWorldIds == null) return null;
        for (int p = 0; p < pictureWorldIds.Length && p < samplePictures.Length; p++)
            if (pictureWorldIds[p] == ids[i]) return samplePictures[p];
        return null;
    }

    private void SetPhoto(RawImage image, int worldIndex)
    {
        if (guideV3 && onlineMode) { V3PaintWorld(image,worldIndex); return; }
        Texture texture = Picture(worldIndex);
        if (texture == null)
        {
            int state = 1;
            if (onlineMode)
            {
                if (!ready) state = failed ? 2 : 0;
                else if (worldIndex >= 0 && worldIndex < count && pictureIndices[worldIndex] >= 0)
                    state = worldIndex < 64 ? (imageFailed ? 2 : 0) : (string.IsNullOrEmpty(drawErrors[worldIndex < 128 ? 0 : 1]) ? 0 : 2);
            }
            SetPlaceholder(image, state);
            return;
        }
        image.texture = texture;
        image.color = texture == null ? new Color(.04f, .12f, .19f, 1) : Color.white;
        image.uvRect = new Rect(0, 0, 1, 1);
        if (texture == null) return;
        float sourceAspect = onlineMode ? 256f / 144f : (float)texture.width / texture.height;
        float slotAspect = image.rectTransform.rect.width / image.rectTransform.rect.height;
        if (slotAspect > sourceAspect)
        { float h = sourceAspect / slotAspect; image.uvRect = new Rect(0, (1 - h) / 2, 1, h); }
        else
        { float w = slotAspect / sourceAspect; image.uvRect = new Rect((1 - w) / 2, 0, w, 1); }
        if (onlineMode)
        {
            int cell = pictureIndices[worldIndex];
            Rect crop = image.uvRect;
            // Inset half a texel to avoid sampling the neighbouring photo.
            float x = (cell % 8) / 8f + .5f / 2048f;
            float atlasHeight = worldIndex < 64 ? 1152f : 1168f;
            float stripeHeight = worldIndex < 64 ? 0f : 16f;
            float y = (stripeHeight + (7 - cell / 8) * 144f + .5f) / atlasHeight;
            float w = 1f / 8f - 1f / 2048f;
            float h = 143f / atlasHeight;
            image.uvRect = new Rect(x + crop.x * w, y + crop.y * h, crop.width * w, crop.height * h);
        }
    }

    // A few small shared textures replace blank slots. No extra downloads or
    // frame textures are needed; the indicator below animates at the UI tick rate.
    private void SetPlaceholder(RawImage image, int state)
    {
        if (image == null) return;
        image.texture = state == 0 ? loadingPicture : state == 2 ? failedPicture : unavailablePicture;
        image.color = image.texture == null ? (state == 0 ? Color.black : new Color(.04f, .12f, .19f, 1)) : Color.white;
        image.uvRect = new Rect(0, 0, 1, 1);
        if (image.texture != null && image.rectTransform.rect.height > 0)
        {
            float source = (float)image.texture.width / image.texture.height;
            float target = image.rectTransform.rect.width / image.rectTransform.rect.height;
            if (target > source) { float h = source / target; image.uvRect = new Rect(0, (1-h)/2, 1, h); }
            else { float w = target / source; image.uvRect = new Rect((1-w)/2, 0, w, 1); }
        }
    }

    private void UpdateLoadingIndicators()
    {
        if (loadingTargets == null || loadingIndicators == null) return;
        int frame = (int)(Time.unscaledTime * 3f) % 3;
        string dots = frame == 0 ? "●  ○  ○" : frame == 1 ? "○  ●  ○" : "○  ○  ●";
        for (int i = 0; i < loadingTargets.Length && i < loadingIndicators.Length; i++)
        {
            RawImage target = loadingTargets[i]; Text indicator = loadingIndicators[i];
            if (target == null || indicator == null) continue;
            bool show = loadingPicture != null && target.texture == loadingPicture;
            indicator.enabled = show;
            if (show && target.gameObject.activeInHierarchy && indicator.text != dots) indicator.text = dots;
        }
    }

    private void RenderWorldRows()
    {
        bool drawTab = onlineMode && (tab == 1 || tab == 3);
        int slot = tab == 1 ? 0 : 1;
        bool waiting = !ready ? !failed : drawTab && !drawHasResult[slot] && string.IsNullOrEmpty(drawErrors[slot]);
        bool loadFailed = !ready ? failed : drawTab && !string.IsNullOrEmpty(drawErrors[slot]);
        if (guideV3 && tab == 2 && v3LatestFailed) { loadFailed = true; waiting = false; }
        if (ready && guideV3 && onlineMode && page * 4 >= orderCount && page * 4 < PageItemCount()) waiting = !loadFailed;
        for (int r = 0; r < 4; r++)
        {
            int p = page * 4 + r;
            bool valid = ready && p < orderCount;
            rows[r].SetActive(valid || waiting || (r == 0 && (orderCount == 0 || loadFailed)));
            rowMap[r] = valid ? order[p] : -1;
            rows[r].GetComponent<Button>().interactable = valid;
            if (rowPlatforms != null && r < rowPlatforms.Length && rowPlatforms[r] != null) rowPlatforms[r].text = valid ? Platforms(rowMap[r]) : "";
            if (rowAuthors != null && r < rowAuthors.Length && rowAuthors[r] != null) rowAuthors[r].text = valid ? FitSingleLine(rowAuthors[r], string.IsNullOrEmpty(authors[rowMap[r]]) ? "" : "by " + authors[rowMap[r]], 16+r) : "";
            if (valid)
            {
                int i = rowMap[r]; rowTitles[r].text = titles[i]; rowSummaries[r].text = descriptions[i]; SetPhoto(rowPictures[r], i);
            }
            else
            {
                rowTitles[r].text = waiting ? "読み込み中" : loadFailed ? "読み込みに失敗しました" : "掲載準備中";
                rowSummaries[r].text = waiting ? (tab == 1 ? "おすすめワールドを取得しています" : tab == 3 ? "ランダムワールドを取得しています" : "ワールド情報を取得しています") : loadFailed ? "再試行してください" : "対象のワールドはありません";
                SetPlaceholder(rowPictures[r], waiting ? 0 : loadFailed ? 2 : 1);
                if (v3Maintenance && ready)
                {
                    rowTitles[r].text = "メンテナンス中";
                    rowSummaries[r].text = "";
                    rowPictures[r].texture = null; rowPictures[r].color = cardColor;
                }
            }
        }
    }

    private VRCUrl DrawUrl(int drawTab)
    { return drawTab == 1 ? recommendedDrawUrl : randomDrawUrl; }

    private bool IsDrawMetadataUrl(string url)
    { return (recommendedDrawUrl != null && url == recommendedDrawUrl.Get()) || (randomDrawUrl != null && url == randomDrawUrl.Get()); }

    private bool ValidWorldId(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length != 41 || !value.StartsWith("wrld_")) return false;
        for (int i = 5; i < 41; i++)
        {
            if (i == 13 || i == 18 || i == 23 || i == 28) { if (value[i] != '-') return false; }
            else if ("0123456789abcdefABCDEF".IndexOf(value[i]) < 0) return false;
        }
        return true;
    }

    private void BeginDraw() { RequestDraw(tab, false); }

    private void RequestDraw(int targetTab, bool initial)
    {
        if (guideV3 && onlineMode)
        { if (!initial) { if (sharedDataSource != null) sharedDataSource._RequestSharedDraw(targetTab); else _RequestSharedDraw(targetTab); } return; }
        if (sharedDataSource != null) { if (!initial) sharedDataSource._RequestSharedDraw(targetTab); return; }
        if (isSharedDataSource && Networking.LocalPlayer != null && !Networking.IsOwner(gameObject)) return;
        if (!onlineMode || !ready || (targetTab != 1 && targetTab != 3) || pendingDrawTab >= 0 || (!initial && Time.realtimeSinceStartup < drawCooldownUntil)) return;
        int slot = targetTab == 1 ? 0 : 1;
        initialDrawAttempted[slot] = true;
        var url = DrawUrl(targetTab);
        if (url == null || string.IsNullOrEmpty(url.Get()) || drawAtlasUrls == null || drawAtlasUrls.Length == 0)
        { drawErrors[slot] = "抽選URLが未設定です。最新版を設定してください"; Render(); return; }
        pendingDrawTab = targetTab; pendingDrawAtlas = -1; pendingDrawJson = ""; pendingDrawGeneration = "";
        drawStartedAt = Time.realtimeSinceStartup; if (!initial) drawCooldownUntil = drawStartedAt + 15f;
        drawLoadingMetadata = true; drawImageRequested = false; drawVerifying = false; drawReadyToCommit = false;
        drawErrors[slot] = "";
        receivingSharedDraw = false; sharedImageUnavailable = false;
        if (isSharedDataSource)
        {
            sharedDrawBusyTab = targetTab; sharedDrawBusyEnd = TimeNow() + 90;
            if (!initial) sharedDrawCooldownEnd = TimeNow() + 15;
            RequestSerialization();
        }
        drawRequestCount++;
        VRCStringDownloader.LoadUrl(url, (IUdonEventReceiver)this);
        Render();
    }

    private bool ValidateDraw(string json)
    {
        if (string.IsNullOrEmpty(json) || json.Length > 60000) return false;
        DataToken token;
        if (!VRCJson.TryDeserializeFromJson(json, out token) || token.TokenType != TokenType.DataDictionary) return false;
        DataDictionary d = token.DataDictionary;
        if (Str(d, "schema") != "metarea-draw-v1" || Str(d, "kind") != (pendingDrawTab == 1 ? "recommended" : "random")) return false;
        DataToken wt;
        if (!d.TryGetValue("worlds", TokenType.DataList, out wt) || wt.DataList.Count > 64) return false;
        DataList worlds = wt.DataList;
        for (int i = 0; i < worlds.Count; i++)
        {
            if (worlds[i].TokenType != TokenType.DataDictionary) return false;
            var w = worlds[i].DataDictionary;
            if (!ValidWorldId(Str(w, "id")) || string.IsNullOrEmpty(Str(w, "name")) || Str(w, "name").Length > 256 || Flag(w, "isSponsorOnly")) return false;
            if (pendingDrawTab == 1 && !Flag(w, "recommended")) return false;
            int cell = Integer(w, "imageIndex", -2);
            if (cell != i && cell != -1) return false;
            string portal = Str(w, "portalWorldId");
            if (!string.IsNullOrEmpty(portal) && !ValidWorldId(portal)) return false;
            for (int j = 0; j < i; j++) if (Str(worlds[j].DataDictionary, "id") == Str(w, "id")) return false;
        }
        if (worlds.Count == 0) { pendingDrawAtlas = -1; return true; }
        int index = Integer(d, "atlasIndex", -1);
        if (index < 0 || drawAtlasUrls == null || index >= drawAtlasUrls.Length || drawAtlasUrls[index] == null) return false;
        string generation = Str(d, "generation");
        if (generation.Length != 16) return false;
        for (int i = 0; i < generation.Length; i++) if ("0123456789abcdef".IndexOf(generation[i]) < 0) return false;
        double generated = Number(d, "generatedAtUnix"), expires = Number(d, "leaseExpiresAtUnix");
        double seconds = expires - generated;
        if (generated <= 0 || seconds < 10 || seconds > 600) return false;
        // Include all request/queue time, avoiding reliance on the visitor's wall clock.
        drawLeaseUntil = drawStartedAt + (float)seconds;
        if (Time.realtimeSinceStartup + 5f >= drawLeaseUntil) return false;
        pendingDrawAtlas = index; pendingDrawGeneration = generation;
        return true;
    }

    private void BeginDrawImage()
    {
        if (pendingDrawTab < 0 || drawLoadingMetadata || drawImageRequested || pendingDrawAtlas < 0 || string.IsNullOrEmpty(pendingDrawJson)) return;
        if (Time.realtimeSinceStartup >= drawLeaseUntil) { FailDraw("抽選画像の有効時間を過ぎました。再試行できます"); return; }
        if (imageDownloader == null) imageDownloader = new VRCImageDownloader();
        TextureInfo info = new TextureInfo(); info.GenerateMipMaps = false; info.FilterMode = FilterMode.Bilinear;
        info.WrapModeU = TextureWrapMode.Clamp; info.WrapModeV = TextureWrapMode.Clamp;
        drawImageRequested = true; MarkImageRequest();
        pendingDrawDownload = imageDownloader.DownloadImage(drawAtlasUrls[pendingDrawAtlas], null, (IUdonEventReceiver)this, info);
    }

    private void VerifyDrawImage(IVRCImageDownload result)
    {
        if (pendingDrawTab < 0 || result != pendingDrawDownload) { result.Dispose(); return; }
        imageRequestInFlight = false;
        if (result.Result == null || result.Result.width != 2048 || result.Result.height != 1168 || Time.realtimeSinceStartup >= drawLeaseUntil)
        { FailDraw("抽選画像を確認できませんでした。再試行できます"); return; }
        pendingDrawTexture = result.Result; drawVerifying = true;
        // Read only the hidden 64-bit generation stripe, not all 64 photos.
        VRCAsyncGPUReadback.Request(pendingDrawTexture, 0, 0, 2048, 8, 1, 0, 1, TextureFormat.RGBA32, (IUdonEventReceiver)this);
    }

    public override void OnAsyncGpuReadbackComplete(VRCAsyncGPUReadbackRequest request)
    {
        if (guideV3 && onlineMode) { V3Readback(request); return; }
        if (!drawVerifying || pendingDrawTab < 0) return;
        drawVerifying = false;
        if (request.hasError || !request.TryGetData(drawGenerationPixels) || Time.realtimeSinceStartup >= drawLeaseUntil)
        { FailDraw("抽選画像の確認に失敗しました。再試行できます"); return; }
        for (int digit = 0; digit < 16; digit++)
        {
            int actual = 0;
            for (int bit = 0; bit < 4; bit++)
            {
                Color32 c = drawGenerationPixels[(digit * 4 + bit) * 32 + 16];
                if ((c.r > 64 && c.r < 192) || Mathf.Abs((int)c.r - c.g) > 24 || Mathf.Abs((int)c.r - c.b) > 24)
                { FailDraw("抽選画像が一致しません。再試行できます"); return; }
                actual = actual * 2 + (c.r >= 192 ? 1 : 0);
            }
            if (actual != "0123456789abcdef".IndexOf(pendingDrawGeneration[digit]))
            { FailDraw("古い抽選画像を検出しました。再試行できます"); return; }
        }
        drawReadyToCommit = true; TryCommitDraw(); Render();
    }

    private void FailDraw(string message)
    {
        if (pendingDrawTab < 0) return;
        if (isSharedDataSource && receivingSharedDraw && !string.IsNullOrEmpty(pendingDrawJson))
        {
            if (pendingDrawDownload != null) pendingDrawDownload.Dispose();
            pendingDrawDownload = null; pendingDrawTexture = null;
            imageRequestInFlight = false; drawVerifying = false; drawLoadingMetadata = false;
            sharedImageUnavailable = true; drawReadyToCommit = true; TryCommitDraw(); return;
        }
        if (isSharedDataSource && !receivingSharedDraw && (Networking.LocalPlayer == null || Networking.IsOwner(gameObject)))
        {
            sharedDrawBusyTab = -1; sharedDrawBusyEnd = 0;
            sharedDrawCooldownEnd = TimeNow() + 15; RequestSerialization();
        }
        drawErrors[pendingDrawTab == 1 ? 0 : 1] = message;
        if (drawImageRequested && !drawVerifying && !drawReadyToCommit) imageRequestInFlight = false;
        if (pendingDrawDownload != null) { pendingDrawDownload.Dispose(); pendingDrawDownload = null; }
        pendingDrawTexture = null; pendingDrawJson = ""; pendingDrawTab = -1; pendingDrawAtlas = -1;
        drawLoadingMetadata = false; drawImageRequested = false; drawVerifying = false; drawReadyToCommit = false;
        drawCooldownUntil = Time.realtimeSinceStartup + 15f;
        Render();
    }

    private void TryCommitDraw()
    {
        if (!drawReadyToCommit || pendingDrawTab < 0) return;
        int slot = pendingDrawTab == 1 ? 0 : 1; int start = 64 + slot * 64;
        // Leave room for the remaining synchronized fields and serialization overhead.
        if (isSharedDataSource && !receivingSharedDraw && Utf8Bytes(pinnedSnapshot) + Utf8Bytes(pendingDrawJson) + Utf8Bytes(slot == 0 ? sharedRandomDraw : sharedRecommendedDraw) > 240000)
        { FailDraw("抽選結果が同期できるサイズを超えました。前回の結果を表示します"); return; }
        // Keep a detail page the visitor is reading unchanged until they leave it.
        if ((view == 2 || (view == 3 && beforeAdView == 2)) && selected >= start && selected < start + 64) return;
        DataToken token;
        if (!VRCJson.TryDeserializeFromJson(pendingDrawJson, out token)) { FailDraw("抽選結果を読み込めませんでした"); return; }
        var d = token.DataDictionary; var worlds = d["worlds"].DataList;
        for (int i = 0; i < 64; i++)
        {
            int index = start + i;
            ids[index] = ""; portalIds[index] = ""; titles[index] = ""; authors[index] = ""; descriptions[index] = "";
            pcSupport[index] = 0; androidSupport[index] = 0; iosSupport[index] = 0; pc[index] = false; android[index] = false; recommended[index] = false; sponsorOnly[index] = false; pictureIndices[index] = -1;
            if (i >= worlds.Count) continue;
            var w = worlds[i].DataDictionary;
            ids[index] = Str(w, "id"); titles[index] = Str(w, "name"); authors[index] = Str(w, "authorName"); descriptions[index] = Str(w, "description");
            ReadPlatforms(w, index); pc[index] = Flag(w, "pc"); android[index] = Flag(w, "android"); recommended[index] = Flag(w, "recommended"); pictureIndices[index] = Integer(w, "imageIndex", -1);
            portalIds[index] = string.IsNullOrEmpty(Str(w, "portalWorldId")) ? ids[index] : Str(w, "portalWorldId");
        }
        IVRCImageDownload previous = drawDownloads[slot];
        drawTextures[slot] = pendingDrawTexture; drawDownloads[slot] = pendingDrawDownload;
        pendingDrawDownload = null; pendingDrawTexture = null;
        drawCounts[slot] = worlds.Count; drawHasResult[slot] = true; drawErrors[slot] = "";
        drawTotalCandidates[slot] = Integer(d, "totalCandidates", worlds.Count);
        cachedTabs[pendingDrawTab] = false; cachedPages[pendingDrawTab] = 0;
        if (tab == pendingDrawTab) { page = 0; BuildOrder(); }
        committedDrawJson[slot] = pendingDrawJson;
        if (isSharedDataSource && !receivingSharedDraw)
        {
            double expiry = TimeNow() + Mathf.Max(0, drawLeaseUntil - Time.realtimeSinceStartup);
            if (slot == 0) { sharedRecommendedDraw = pendingDrawJson; sharedRecommendedExpiry = expiry; }
            else { sharedRandomDraw = pendingDrawJson; sharedRandomExpiry = expiry; }
            sharedDrawBusyTab = -1; sharedDrawBusyEnd = 0; RequestSerialization();
        }
        if (sharedImageUnavailable) drawErrors[slot] = "画像の有効時間を過ぎたため、一覧情報のみ表示しています";
        receivingSharedDraw = false; sharedImageUnavailable = false;
        pendingDrawTab = -1; pendingDrawAtlas = -1; pendingDrawJson = "";
        drawReadyToCommit = false; drawImageRequested = false;
        Render();
        if (previous != null)
        {
            if (isSharedDataSource) RetireDraw(previous);
            else previous.Dispose();
        }
    }

    private void UpdateDrawUi()
    {
        if (guideV3 && onlineMode) { V3DrawUi(); return; }
        if (isSharedDataSource || !onlineMode || favoriteTabSelected || (tab != 1 && tab != 3)) return;
        int slot = tab == 1 ? 0 : 1;
        int wait = Mathf.Max(0, Mathf.CeilToInt(drawCooldownUntil - Time.realtimeSinceStartup));
        bool busy = sharedDataSource != null ? sharedDataSource.SharedDrawBusy() : pendingDrawTab >= 0;
        reshuffleButton.interactable = ready && !busy && wait == 0;
        if (reshuffleLabel == null) reshuffleLabel = reshuffleButton.GetComponentInChildren<Text>(true);
        if (reshuffleLabel != null) reshuffleLabel.text = busy ? "抽選中…" : wait > 0 ? "あと " + wait + " 秒" : string.IsNullOrEmpty(drawErrors[slot]) ? "再抽選" : "再試行";
        if (pendingDrawTab == tab)
            statusLabel.text = drawReadyToCommit ? "抽選が完了しました。一覧に戻ると切り替わります" : drawLoadingMetadata ? "サイトの全候補から抽選しています…" : "抽選した64件までの写真を読み込んでいます…";
        else if (!string.IsNullOrEmpty(drawErrors[slot])) statusLabel.text = drawErrors[slot];
        else if (drawHasResult[slot]) statusLabel.text = "全 " + drawTotalCandidates[slot] + " 件から " + drawCounts[slot] + " 件を抽選しました";
        else statusLabel.text = wait > 0 ? "おすすめ・ランダム共通：あと " + wait + " 秒で抽選できます" : "サイトからの抽選を待っています…";
        if (orderCount == 0) pageLabel.text = drawHasResult[slot] ? "対象ワールドなし" : "読み込み中…";
    }

    private void BuildOrder()
    {
        order = new int[count]; orderCount = 0;
        if (guideV3 && onlineMode && tab == 2)
        { for (int i = 0; i < v3LatestCount; i++) if (V3WorldAllowed(192+i)) order[orderCount++] = 192+i; }
        else if (onlineMode && (tab == 1 || tab == 3))
        {
            int slot = tab == 1 ? 0 : 1;
            int start = 64 + slot * 64;
            for (int i = 0; i < drawCounts[slot]; i++) order[orderCount++] = start + i;
        }
        else for (int i = 0; i < baseWorldCount; i++) if (!sponsorOnly[i] && (tab != 1 || recommended[i])) order[orderCount++] = i;
        if (!onlineMode && (tab == 1 || tab == 3))
        {
            for (int i = orderCount - 1; i > 0; i--)
            { int j = Random.Range(0, i + 1); int a = order[i]; order[i] = order[j]; order[j] = a; }
        }
        cachedTabs[tab] = true; cachedCounts[tab] = orderCount;
        for (int i = 0; i < orderCount; i++) cachedOrders[tab * 64 + i] = order[i];
    }

    private void SetTab(int target)
    {
        favoriteTabSelected = false;
        cachedPages[tab] = page;
        tab = target; view = target == 0 ? 0 : 1; page = cachedPages[tab];
        if (ready)
        {
            if (!cachedTabs[tab]) BuildOrder();
            else { orderCount = cachedCounts[tab]; order = new int[count]; for (int i = 0; i < orderCount; i++) order[i] = cachedOrders[tab * 64 + i]; }
        }
        TryCommitDraw();
        if (onlineMode && ready && (tab == 1 || tab == 3))
        {
            int slot = tab == 1 ? 0 : 1;
            // Initial results are loaded automatically, independently of the visible tab.
        }
        Render();
    }
    public void _Top()
    {
        SetTab(0);
    }
    private void InitializeDisplayMode()
    {
        if (!unifiedDisplay) return;
        int mode = initialDisplayMode;
        if (mode < 0 || mode > 1) mode = 0;
        manyVersion = mode == 1;
        favoriteTabSelected = false;
        view = 0;
        LayoutNavigation();
    }
    private void LayoutNavigation()
    {
        if (navImages == null) return;
        int count = favoritesEnabled ? 5 : 4;
        float width = (760f - (count - 1) * 5f) / count;
        for (int i = 0; i < navImages.Length; i++)
        {
            if (navImages[i] == null) continue;
            navImages[i].gameObject.SetActive(i < count);
            if (i >= count) continue;
            Place(navImages[i].rectTransform, 20 + i * (width + 5), 156, width, 92);
            Text label = navImages[i].GetComponentInChildren<Text>(true);
            if (label != null) Place(label.rectTransform, 0, 0, width, 92);
        }
        if (!favoritesEnabled && favoritesPanel != null) favoritesPanel.SetActive(false);
    }
    public void _ToggleArchive()
    {
        if (archiveUrlPanel == null) return;
        if (archiveUrlField != null) archiveUrlField.text = archiveUrl;
        if (xUrlPanel != null) xUrlPanel.SetActive(false);
        if (boothUrlPanel != null) boothUrlPanel.SetActive(false);
        archiveUrlPanel.SetActive(!archiveUrlPanel.activeSelf);
    }
    public void _CloseArchive() { if (archiveUrlPanel != null) archiveUrlPanel.SetActive(false); }
    public void _ToggleX()
    {
        if (boothUrlPanel != null) boothUrlPanel.SetActive(false);
        if (archiveUrlPanel != null) archiveUrlPanel.SetActive(false);
        if (xUrlPanel == null) return;
        if (xUrlField != null) xUrlField.text = xProfileUrl;
        xUrlPanel.SetActive(!xUrlPanel.activeSelf);
    }
    public void _ToggleBooth()
    {
        if (boothUrlPanel == null) return;
        bool open = !boothUrlPanel.activeSelf;
        if (xUrlPanel != null) xUrlPanel.SetActive(false);
        if (archiveUrlPanel != null) archiveUrlPanel.SetActive(false);
        if (boothUrlField != null) boothUrlField.text = boothUrl;
        boothUrlPanel.SetActive(open);
    }
    public void _CloseBooth() { if (boothUrlPanel != null) boothUrlPanel.SetActive(false); }
    public void _CloseX() { if (xUrlPanel != null) xUrlPanel.SetActive(false); }
    public void _Recommended() { SetTab(1); }
    public void _Latest() { SetTab(2); }
    public void _Random() { SetTab(3); }
    public void _Reshuffle() { if (!ready) return; if (onlineMode) BeginDraw(); else { BuildOrder(); page = 0; Render(); } }
    public void _PreviousPage() { if (page > 0) page--; Render(); }
    public void _NextPage() { if ((page + 1) * 4 < PageItemCount()) page++; Render(); }

    private int PageItemCount()
    {
        if (!guideV3 || !onlineMode || tab < 1 || tab > 3) return orderCount;
        int category = tab == 1 ? 0 : tab == 3 ? 1 : 2;
        bool complete = category == 2 ? v3LatestComplete : v3DrawComplete[category];
        return complete ? orderCount : Mathf.Max(orderCount,v3ListCounts[category]);
    }

    private int V3InitialCount(DataDictionary d, string field, DataList initial)
    {
        int fallback = initial.Count < 8 ? initial.Count : 64;
        return Mathf.Clamp(Integer(d,field,fallback),initial.Count,64);
    }
    public void _DailyOne() { Open(dailyOne); }
    public void _DailyTwo() { Open(secondSlotIsChill ? dailyChill : dailyTwo); }
    public void _Row0() { Open(rowMap[0]); }
    public void _Row1() { Open(rowMap[1]); }
    public void _Row2() { Open(rowMap[2]); }
    public void _Row3() { Open(rowMap[3]); }
    private void Open(int i) { if (!ready || !CanUseWorld(i) || sponsorOnly[i]) return; selected = i; sponsoredDetail = false; view = 2; Render(); }
    public void _Back() { view = tab == 0 ? 0 : 1; TryCommitDraw(); Render(); }
    public void _Ad() { if (onlineMode && !IsAdActive(currentAd)) return; CancelSponsorFade(); if (view != 3) beforeAdView = view; openedTop = -1; openedAd = currentAd; view = 3; openedDetailRequestedAt = Time.realtimeSinceStartup; PumpImageDownloads(); Render(); if (onlineMode && view == 3 && adPanel.activeSelf) QueueAdClick(adIds[openedAd], adKeys[openedAd], adClickSlots[openedAd], 1); }
    public void _AdBack() { view = beforeAdView == 3 ? 0 : beforeAdView; nextSponsorRotation = Time.time + Mathf.Max(5f, sponsorRotationSeconds); Render(); }
    private int Position()
    { for (int i = 0; i < orderCount; i++) if (order[i] == selected) return i; return -1; }
    public void _PreviousWorld()
    { if (sponsoredDetail) { int s = SponsoredPosition(); if (s > 0) OpenSponsored(sponsoredWorlds[s - 1]); } else if (tab == 0 && manyVersion) StepMany(-1); else if (tab == 0) Open(dailyOne); else { int p = Position(); if (p > 0) Open(order[p - 1]); } }
    public void _NextWorld()
    { if (sponsoredDetail) { int s = SponsoredPosition(); if (s >= 0 && s + 1 < sponsoredCount) OpenSponsored(sponsoredWorlds[s + 1]); } else if (tab == 0 && manyVersion) StepMany(1); else if (tab == 0) Open(secondSlotIsChill ? dailyChill : dailyTwo); else { int p = Position(); if (p >= 0 && p + 1 < orderCount) Open(order[p + 1]); } }

    public void _Portal() { if (!sponsoredDetail || IsSponsoredWorldActive(selected)) RequestPortal(selected); }
    public void _AdPortal() { if (!onlineMode) RequestPortal(adWorld); else if (view == 3 && TopPortalAvailable()) RequestPortal(openedTop >= 0 ? topWorlds[openedTop] : adWorlds[openedAd]); }
    private double TimeNow()
    { return Networking.LocalPlayer == null ? Time.timeAsDouble : Networking.GetServerTimeInSeconds(); }
    private float PortalWait()
    {
        if (!portalCooldownStarted) return 0f;
        double remaining = Networking.LocalPlayer == null ? nextPortalTime - TimeNow() : Networking.CalculateServerDeltaTime(nextPortalTime, TimeNow());
        // Also normalize ClientSim's raw subtraction; live networking normally does this itself.
        if (remaining > 2147483.648d) remaining -= 4294967.296d;
        else if (remaining < -2147483.648d) remaining += 4294967.296d;
        return remaining > 0 && remaining <= Mathf.Max(1f, portalCooldownSeconds) ? (float)remaining : 0f;
    }
    private bool RequestPortal(int i)
    {
        if (!ready || !CanUseWorld(i) || PortalWait() > 0 || Time.realtimeSinceStartup - localPortalRequestTime < 1) return false;
        localPortalRequestTime = Time.realtimeSinceStartup;
        QueueStatistics(1);
        if (onlineMode && i >= 64)
        {
            if (Networking.LocalPlayer == null || Networking.IsOwner(gameObject)) _DrawPortalOnOwner(portalIds[i], titles[i]);
            else SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(_DrawPortalOnOwner), portalIds[i], titles[i]);
            return true;
        }
        if (Networking.LocalPlayer == null || Networking.IsOwner(gameObject)) _PortalOnOwner(ids[i]);
        else SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(_PortalOnOwner), ids[i]);
        return true;
    }

    [NetworkCallable(maxEventsPerSecond: 2)]
    public void _PortalOnOwner(string worldId)
    {
        if (Networking.LocalPlayer != null && !Networking.IsOwner(gameObject)) return;
        int index = FindId(worldId);
        if (!ready || index >= baseWorldCount || !CanUseWorld(index) || PortalWait() > 0) return;
        sharedPortalFromDraw = false; sharedPortalDrawTitle = "";
        sharedPortalWorldId = worldId;
        portalExpiresAt = TimeNow() + 20d;
        nextPortalTime = TimeNow() + Mathf.Max(1f, portalCooldownSeconds);
        portalCooldownStarted = true;
        QueueStatistics(2);
        RequestSerialization();
        ApplyPortal();
    }

    [NetworkCallable(maxEventsPerSecond: 2)]
    public void _DrawPortalOnOwner(string worldId, string worldTitle)
    {
        if (Networking.LocalPlayer != null && !Networking.IsOwner(gameObject)) return;
        // Personal draws differ between players; the owner need not load their lists.
        // Accept only a bare VRChat world UUID, never an arbitrary URL or instance ID.
        if (!ready || !ValidWorldId(worldId) || string.IsNullOrEmpty(worldTitle) || worldTitle.Length > 256 || PortalWait() > 0) return;
        sharedPortalFromDraw = true; sharedPortalDrawTitle = worldTitle.Replace("\r", " ").Replace("\n", " ");
        sharedPortalWorldId = worldId;
        portalExpiresAt = TimeNow() + 20d; nextPortalTime = TimeNow() + Mathf.Max(1f, portalCooldownSeconds);
        portalCooldownStarted = true;
        QueueStatistics(2);
        RequestSerialization(); ApplyPortal();
    }

    private void BuildFavorites()
    {
        favoriteCount = 0;
        if (favoriteFolderNames == null || (favoriteFolderNames.Length == 0 && favoriteWorldIds != null && favoriteWorldIds.Length > 0)) favoriteFolderNames = new string[] { "フォルダ1" };
        favoriteFolderCountsCache = new int[favoriteFolderNames.Length];
        if (!favoritesEnabled || favoriteWorldIds == null || favoriteWorldTitles == null) return;
        int length = Mathf.Min(favoriteWorldIds.Length, favoriteWorldTitles.Length);
        favoriteOrder = new int[length];
        for (int i = 0; i < length; i++)
        {
            if (!ValidWorldId(favoriteWorldIds[i]) || string.IsNullOrEmpty(favoriteWorldTitles[i]) || string.IsNullOrEmpty(favoriteWorldTitles[i].Trim()) || favoriteWorldTitles[i].Length > 256) continue;
            bool duplicate = false;
            int folder = FavoriteFolderOf(i);
            if (folder < 0 || folder >= favoriteFolderNames.Length) continue;
            for (int j = 0; j < i; j++) if ((favoriteFlatList || FavoriteFolderOf(j) == folder) && favoriteWorldIds[j] == favoriteWorldIds[i] && !string.IsNullOrEmpty(favoriteWorldTitles[j]) && !string.IsNullOrEmpty(favoriteWorldTitles[j].Trim()) && favoriteWorldTitles[j].Length <= 256) duplicate = true;
            if (duplicate) continue;
            favoriteFolderCountsCache[folder]++;
            if (favoriteFlatList || folder == favoriteFolder) favoriteOrder[favoriteCount++] = i;
        }
    }
    private int FavoriteFolderOf(int entry) { return favoriteFolderIndices != null && entry < favoriteFolderIndices.Length ? favoriteFolderIndices[entry] : 0; }
    public void _FavoriteFolders() { favoriteFolder = -1; BuildFavorites(); RenderFavorites(); }
    public void _FavoriteFolder0() { OpenFavoriteFolder(0); }
    public void _FavoriteFolder1() { OpenFavoriteFolder(1); }
    public void _FavoriteFolder2() { OpenFavoriteFolder(2); }
    public void _FavoriteFolder3() { OpenFavoriteFolder(3); }
    public void _FavoriteFolder4() { OpenFavoriteFolder(4); }
    public void _FavoriteFolder5() { OpenFavoriteFolder(5); }
    public void _FavoriteFolder6() { OpenFavoriteFolder(6); }
    public void _FavoriteFolder7() { OpenFavoriteFolder(7); }
    private void OpenFavoriteFolder(int row)
    {
        int index = favoriteFolderPage * 8 + row;
        if (favoriteFlatList || view != 4 || favoriteFolder != -1 || index >= favoriteFolderNames.Length) return;
        favoriteFolder = index; favoritePage = 0; BuildFavorites(); RenderFavorites();
    }

    public void _Favorites()
    {
        if (!favoritesEnabled) return;
        favoriteTabSelected = true; favoriteFolder = -1; BuildFavorites(); view = 4; CancelSponsorFade(); Render();
    }
    public void _FavoritePrevious() { if (!favoriteFlatList && favoriteFolder < 0) { if (favoriteFolderPage > 0) favoriteFolderPage--; } else if (favoritePage > 0) favoritePage--; RenderFavorites(); }
    public void _FavoriteNext() { if (!favoriteFlatList && favoriteFolder < 0) { if ((favoriteFolderPage + 1) * 8 < favoriteFolderNames.Length) favoriteFolderPage++; } else if ((favoritePage + 1) * 8 < favoriteCount) favoritePage++; RenderFavorites(); }
    public void _Favorite0() { RequestFavorite(0); }
    public void _Favorite1() { RequestFavorite(1); }
    public void _Favorite2() { RequestFavorite(2); }
    public void _Favorite3() { RequestFavorite(3); }
    public void _Favorite4() { RequestFavorite(4); }
    public void _Favorite5() { RequestFavorite(5); }
    public void _Favorite6() { RequestFavorite(6); }
    public void _Favorite7() { RequestFavorite(7); }

    private void RenderFavorites()
    {
        if (favoritesPanel == null) return;
        if (!favoritesEnabled) { favoritesPanel.SetActive(false); return; }
        favoritesPanel.SetActive(view == 4);
        if (view != 4) return;
        bool folders = !favoriteFlatList && favoriteFolder < 0;
        if (favoriteBack != null) favoriteBack.gameObject.SetActive(!favoriteFlatList && !folders);
        if (favoriteSection != null) favoriteSection.text = folders ? "お気に入りフォルダ" : favoriteFlatList ? "お気に入りワールド" : "";
        if (favoriteFolderButtons != null) for (int r = 0; r < favoriteFolderButtons.Length; r++)
        {
            int at = favoriteFolderPage * 8 + r;
            bool visible = folders && at < favoriteFolderNames.Length;
            favoriteFolderButtons[r].gameObject.SetActive(visible);
            if (visible) { favoriteFolderTitles[r].text = favoriteFolderNames[at]; favoriteFolderCounts[r].text = favoriteFolderDescriptions != null && at < favoriteFolderDescriptions.Length ? favoriteFolderDescriptions[at] : ""; }
        }
        if (folders)
        {
            if (favoriteButtons != null) for (int r = 0; r < favoriteButtons.Length; r++) favoriteButtons[r].gameObject.SetActive(false);
            int folderPages = Mathf.Max(1, (favoriteFolderNames.Length + 7) / 8);
            if (favoritePageLabel != null) favoritePageLabel.text = (favoriteFolderPage + 1) + " / " + folderPages;
            if (favoritePrevious != null) favoritePrevious.interactable = favoriteFolderPage > 0;
            if (favoriteNext != null) favoriteNext.interactable = favoriteFolderPage + 1 < folderPages;
            if (favoriteHelp != null) favoriteHelp.text = favoriteFolderNames.Length == 0 ? "フォルダはまだ登録されていません。" : "フォルダを選ぶとワールド一覧を開きます。";
            if (statusLabel != null) statusLabel.text = "設置者が登録したお気に入りワールド";
            return;
        }
        int pages = Mathf.Max(1, (favoriteCount + 7) / 8);
        favoritePage = Mathf.Clamp(favoritePage, 0, pages - 1);
        if (favoritePageLabel != null) favoritePageLabel.text = favoriteCount == 0 ? "登録なし" : (favoritePage + 1) + " / " + pages;
        if (favoritePrevious != null) favoritePrevious.interactable = favoritePage > 0;
        if (favoriteNext != null) favoriteNext.interactable = favoritePage + 1 < pages;
        float wait = PortalWait();
        if (favoriteHelp != null) favoriteHelp.text = (favoriteFlatList ? "" : favoriteFolderNames[favoriteFolder] + "  /  ") + (favoriteCount == 0 ? "登録なし" : wait > 0 ? "ポータルの再作成まで あと " + Mathf.CeilToInt(wait) + " 秒" : "ワールドを押すと共有ポータルが出現します。");
        if (view == 4) statusLabel.text = "設置者が登録したお気に入りワールド";
        if (favoriteButtons == null) return;
        for (int row = 0; row < favoriteButtons.Length; row++)
        {
            int position = favoritePage * 8 + row;
            bool visible = position < favoriteCount && favoriteOrder != null && position < favoriteOrder.Length;
            int entry = visible ? favoriteOrder[position] : -1;
            visible = visible && entry >= 0 && favoriteWorldTitles != null && entry < favoriteWorldTitles.Length;
            favoriteButtons[row].gameObject.SetActive(visible);
            favoriteButtons[row].interactable = visible && wait <= 0;
            if (!visible) continue;
            favoriteTitles[row].text = favoriteWorldTitles[entry];
            string author = favoriteWorldAuthors != null && entry < favoriteWorldAuthors.Length ? favoriteWorldAuthors[entry] : "";
            favoriteAuthors[row].text = string.IsNullOrEmpty(author) ? "作者名未設定" : "by " + author;
        }
    }

    private void RequestFavorite(int row)
    {
        if (!favoritesEnabled || view != 4 || (!favoriteFlatList && favoriteFolder < 0) || row < 0 || row >= 8 || PortalWait() > 0 || Time.realtimeSinceStartup - localPortalRequestTime < 1) return;
        int position = favoritePage * 8 + row;
        if (position >= favoriteCount || favoriteOrder == null || position >= favoriteOrder.Length) return;
        int entry = favoriteOrder[position];
        if (entry < 0 || favoriteWorldIds == null || entry >= favoriteWorldIds.Length) return;
        localPortalRequestTime = Time.realtimeSinceStartup;
        QueueStatistics(1);
        if (Networking.LocalPlayer == null || Networking.IsOwner(gameObject)) _FavoritePortalOnOwner(entry);
        else SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(_FavoritePortalOnOwner), entry);
        RenderFavorites();
    }

    [NetworkCallable(maxEventsPerSecond: 2)]
    public void _FavoritePortalOnOwner(int entry)
    {
        if (Networking.LocalPlayer != null && !Networking.IsOwner(gameObject)) return;
        if (!favoritesEnabled || favoriteWorldIds == null || favoriteWorldTitles == null || entry < 0 || entry >= favoriteWorldIds.Length || entry >= favoriteWorldTitles.Length || PortalWait() > 0) return;
        string worldId = favoriteWorldIds[entry]; string title = favoriteWorldTitles[entry];
        if (!ValidWorldId(worldId) || string.IsNullOrEmpty(title) || string.IsNullOrEmpty(title.Trim()) || title.Length > 256) return;
        // Installed favorites are shared by all visitors. The owner resolves the index locally.
        // No feed request is required, so favorites still work if the site cannot be reached.
        sharedPortalFromDraw = true; sharedPortalDrawTitle = title.Replace("\r", " ").Replace("\n", " ");
        sharedPortalWorldId = worldId;
        portalExpiresAt = TimeNow() + 20d; nextPortalTime = TimeNow() + Mathf.Max(1f, portalCooldownSeconds);
        portalCooldownStarted = true;
        QueueStatistics(2);
        RequestSerialization(); ApplyPortal(); RenderFavorites();
    }

    private bool PortalExpired()
    {
        double remaining = Networking.LocalPlayer == null ? portalExpiresAt - TimeNow() : Networking.CalculateServerDeltaTime(portalExpiresAt, TimeNow());
        if (remaining > 2147483.648d) remaining -= 4294967.296d;
        else if (remaining < -2147483.648d) remaining += 4294967.296d;
        return remaining <= 0d;
    }

    private void ApplyPortal()
    {
        if (string.IsNullOrEmpty(sharedPortalWorldId) || PortalExpired())
        {
            if (portalMarker != null) portalMarker.gameObject.SetActive(false);
            if (editorPortalPreview != null) editorPortalPreview.SetActive(false);
            if (portalDestinationLabel != null) portalDestinationLabel.text = "";
            appliedPortalId = ""; return;
        }
        if (portalMarker == null) return;
        if (sharedPortalFromDraw ? !ValidWorldId(sharedPortalWorldId) : !CanUseWorld(FindId(sharedPortalWorldId)))
        {
            portalMarker.gameObject.SetActive(false);
            if (editorPortalPreview != null) editorPortalPreview.SetActive(false);
            if (portalDestinationLabel != null) portalDestinationLabel.text = "";
            appliedPortalId = ""; return;
        }
        if (appliedPortalId == sharedPortalWorldId) return;
        portalMarker.gameObject.SetActive(false);
        portalMarker.transform.SetPositionAndRotation(portalAnchor.position, portalAnchor.rotation);
        portalMarker.roomId = sharedPortalFromDraw ? sharedPortalWorldId : portalIds[FindId(sharedPortalWorldId)];
        portalMarker.gameObject.SetActive(true);
        portalMarker.RefreshPortal();
        appliedPortalId = sharedPortalWorldId;
#if UNITY_EDITOR
        if (editorPortalPreview != null) editorPortalPreview.SetActive(true);
#endif
        if (portalDestinationLabel != null) portalDestinationLabel.text = sharedPortalFromDraw ? sharedPortalDrawTitle : titles[FindId(sharedPortalWorldId)];
    }

    private void Update()
    {
        // Share the expiry itself so late joiners and a new owner keep the same deadline.
        if (!string.IsNullOrEmpty(sharedPortalWorldId) && PortalExpired())
        {
            if (Networking.LocalPlayer == null || Networking.IsOwner(gameObject))
            { sharedPortalWorldId = ""; RequestSerialization(); }
            if (!string.IsNullOrEmpty(appliedPortalId)) ApplyPortal();
        }
        if (sharedDataSource == null) UpdateSponsorFade();
        if (Time.time < nextUiTick) return;
        nextUiTick = Time.time + 0.2f;
        if (guideV3 && onlineMode) { V3Tick(); return; }
        if (sharedDataSource == null)
        {
            if (loading && Time.realtimeSinceStartup - lastFeedAttempt > 60f) { loading = false; failed = true; Render(); }
            if (pendingDrawTab >= 0 && !drawReadyToCommit && Time.realtimeSinceStartup - drawStartedAt > 90f)
                FailDraw("抽選の読み込みがタイムアウトしました。再試行できます");
        }
        if (sharedDataSource != null) PullSharedData();
        if (isSharedDataSource) { ReceiveSharedDraw(); ReleaseUnusedDraws(); }
        if (ready && onlineMode && sharedDataSource == null)
        {
            if (imageRequestInFlight && Time.realtimeSinceStartup - imageRequestStarted > 60f)
            {
                imageRequestInFlight = false;
                if (sponsorImageLoading) { sponsorImageLoading = false; sponsorImageFailed = true; if (sponsorDownload != null) sponsorDownload.Dispose(); sponsorDownload = null; }
                else if (imageLoading) { imageLoading = false; imageFailed = true; if (atlasDownload != null) atlasDownload.Dispose(); atlasDownload = null; }
                else if (pendingDrawTab >= 0 && drawImageRequested && !drawVerifying && !drawReadyToCommit) FailDraw("抽選画像の読み込みがタイムアウトしました");
                else for (int i = 0; i < 7; i++) if (extraSponsorLoading[i])
                { extraSponsorLoading[i] = false; extraSponsorFailed[i] = true; if (extraSponsorDownloads[i] != null) extraSponsorDownloads[i].Dispose(); extraSponsorDownloads[i] = null; }
                Render();
            }
            TryCommitDraw();
            // Start the initial draw lease after the higher-priority images,
            // so a slow daily/ad download cannot consume the draw timeout.
            if (pendingDrawTab < 0 && !InitialImagesPending())
            {
                if (!initialDrawAttempted[1] && string.IsNullOrEmpty(sharedRandomDraw)) RequestDraw(3, true);
                else if (!initialDrawAttempted[0] && string.IsNullOrEmpty(sharedRecommendedDraw)) RequestDraw(1, true);
            }
            bool wasLoadingImage = imageRequestInFlight;
            PumpImageDownloads();
            if (wasLoadingImage != imageRequestInFlight) Render();
            UpdateDrawUi();
        }
        if (sharedDataSource == null && ready && onlineMode && sponsorStatusProtocol && (adCount > 0 || placementCount > 0 || topCount > 0))
        {
            bool failedNow = (sponsorStatusLoading && Time.realtimeSinceStartup - sponsorStatusStarted >= 15f) || (sponsorStatusValid && !StatusUsable());
            if (failedNow) { FailSponsorStatus(); }
            if (Time.realtimeSinceStartup >= nextSponsorStatusAttempt) BeginSponsorStatusDownload();
            if (RefreshSponsorAvailability() || failedNow) Render();
        }
        if (sharedDataSource == null && ready && view != 3) UpdateSponsorRotation();
        PumpAdClicks(); PumpStatistics();
        if (isSharedDataSource) return;
        UpdateLoadingIndicators();
        QueueBoardReady();
        UpdateDrawUi();
        RenderFavorites();
        int wait = Mathf.CeilToInt(PortalWait());
        portalButton.interactable = ready && CanUseWorld(selected) && (!sponsoredDetail || IsSponsoredWorldActive(selected)) && wait == 0;
        adPortalButton.interactable = ready && wait == 0 && (onlineMode ? view == 3 && TopPortalAvailable() : adWorld >= 0);
        portalButtonLabel.text = wait > 0 ? "あと " + wait + " 秒" : "PORTAL\nこの世界へ";
        adPortalLabel.text = wait > 0 ? "あと " + wait + " 秒" : "PORTAL  この世界へ";
    }

    private void Render()
    {
        if (isSharedDataSource) return;
        topPanel.SetActive(view == 0); listPanel.SetActive(view == 1); detailPanel.SetActive(view == 2); adPanel.SetActive(view == 3);
        for (int i = 0; i < navImages.Length; i++) navImages[i].color = i == (favoriteTabSelected ? 4 : tab) ? selectedTabColor : cardColor;
        retryButton.gameObject.SetActive((!ready && !loading) || (ready && ((imageFailed && !imageLoading) || (sponsorImageFailed && !sponsorImageLoading) || sponsorStatusFailed || ExtraSponsorImagesFailed())));
        statusLabel.text = loading ? "読み込み中…" : failed ? "情報を取得できません。URL許可を確認して再試行。" : ready ? (imageLoading || sponsorImageLoading || ExtraSponsorImagesLoading() ? "写真を読み込んでいます…" : imageFailed || sponsorImageFailed || ExtraSponsorImagesFailed() ? "写真を取得できません。URL許可を確認して再試行。" : onlineMode ? "入室時の情報を表示しています" : "サンプル表示") : "初期データを待っています…";
        if (ready && sponsorStatusFailed) statusLabel.text = "スポンサーの掲載確認に失敗しました。自動で再確認します。";
        if (guideV3 && onlineMode && v3RecoveryPending) statusLabel.text = "接続できません。自動再接続を待っています";
        sectionLabel.text = tab == 1 ? "PICKUP WORLDS" : tab == 2 ? "LATEST WORLDS" : "RANDOM DISCOVERY";
        if (!ready)
        {
            pageLabel.text = failed ? "取得失敗" : "読み込み中…";
            previousPage.interactable = false; nextPage.interactable = false;
            reshuffleButton.gameObject.SetActive(false);
        }
        RenderFavorites();
        LayoutDailyCards();
        for (int k = 0; view == 0 && !manyVersion && k < 2; k++)
        {
            int i = k == 0 ? dailyOne : secondSlotIsChill ? dailyChill : dailyTwo;
            topCaptions[k].text = k == 0 ? "今日のランダムワールド①" : secondSlotIsChill ? "今日のランダムチルワールド" : "今日のランダムワールド②";
            bool valid = TopWorldAvailable(i);
            topButtons[k].interactable = valid;
            topTitles[k].text = !ready ? (failed ? "読み込みに失敗しました" : "読み込み中") : valid ? titles[i] : "本日の追加紹介はありません";
            if (topAuthors != null && k < topAuthors.Length && topAuthors[k] != null)
                topAuthors[k].text = valid ? FitSingleLine(topAuthors[k], string.IsNullOrEmpty(authors[i]) ? "" : "by " + authors[i], 14+k) : "";
            if (topPlatforms != null && k < topPlatforms.Length && topPlatforms[k] != null) topPlatforms[k].text = valid ? Platforms(i) : "";
            topSummaries[k].text = valid ? descriptions[i] : "";
            SetPhoto(topPictures[k], valid ? i : -1);
        }
        if (view == 1) RenderWorldRows();
        UpdateLoadingIndicators();
        if (!ready)
        {
            if (sponsorBannerButton != null) sponsorBannerButton.gameObject.SetActive(!onlineMode);
            if (sponsorDisclosure != null) sponsorDisclosure.SetActive(!onlineMode);
            RenderMaintenance();
            return;
        }
        sectionLabel.text = tab == 1 ? "PICKUP WORLDS" : tab == 2 ? "LATEST WORLDS" : "RANDOM DISCOVERY";
        reshuffleButton.gameObject.SetActive(tab == 1 || tab == 3);
        int pageItems = PageItemCount();
        pageLabel.text = pageItems == 0 ? "掲載準備中" : (page + 1) + " / " + Mathf.CeilToInt(pageItems / 4f);
        previousPage.interactable = page > 0; nextPage.interactable = (page + 1) * 4 < pageItems;
        detailTitle.text = titles[selected].Replace("\r", " ").Replace("\n", " "); detailDescription.text = descriptions[selected]; SetPhoto(detailPicture, selected);
        if (detailAuthor != null)
        {
            string fullAuthor = string.IsNullOrEmpty(authors[selected]) ? "" : "by " + authors[selected];
            detailAuthor.text = fullAuthor;
            if (detailAuthor.preferredWidth > detailAuthor.rectTransform.rect.width - 2f)
            {
                detailAuthor.text = FitSingleLine(detailAuthor, fullAuthor, 20);
                detailDescription.text = "作者：" + authors[selected] + "\n\n" + descriptions[selected];
            }
        }
        bool supported = pcSupport[selected] > 0;
#if UNITY_ANDROID
        supported = androidSupport[selected] > 0;
#elif UNITY_IOS
        supported = iosSupport[selected] > 0;
#endif
        if (detailPlatforms != null) detailPlatforms.text = "PC：" + PlatformWord(pcSupport[selected]) + "　Android：" + PlatformWord(androidSupport[selected]) + "　iOS：" + PlatformWord(iosSupport[selected]);
        platformNotice.text = supported ? "" : "この端末の対応は未確認、または非対応です。ポータルは生成できます。";
        int second = secondSlotIsChill ? dailyChill : dailyTwo;
        previousWorld.interactable = sponsoredDetail ? SponsoredPosition() > 0 : tab == 0 ? selected != dailyOne && TopWorldAvailable(dailyOne) : Position() > 0;
        nextWorld.interactable = sponsoredDetail ? SponsoredPosition() + 1 < sponsoredCount : tab == 0 ? selected != second && TopWorldAvailable(second) : Position() >= 0 && Position() + 1 < orderCount;
        if (manyVersion && tab == 0 && !sponsoredDetail) { previousWorld.interactable = CanStepMany(-1); nextWorld.interactable = CanStepMany(1); }
        returnLabel.text = tab == 0 ? "TOP に戻る" : "一覧に戻る";
        adTitle.text = adName; adDescription.text = adCopy; adSponsor.text = "広告主: " + sponsor;
        bannerTitle.text = adName; SetPhoto(adPicture, adWorld); SetPhoto(bannerPicture, adWorld);
        if (!onlineMode)
        {
            if (sampleAdBanner != null) { bannerPicture.texture = sampleAdBanner; bannerPicture.uvRect = new Rect(0, 0, 1, 1); bannerPicture.color = Color.white; }
            if (sampleAdDetail != null) { adPicture.texture = sampleAdDetail; adPicture.uvRect = new Rect(0, 0, 1, 1); adPicture.color = Color.white; }
        }
        RenderSponsors();
        UpdateDrawUi();
        RenderFavorites();
        RenderMaintenance();
    }

    private void RenderMaintenance()
    {
        bool active = guideV3 && onlineMode && v3Maintenance;
        bool full = active && !favoriteTabSelected && (!ready || V3Texture(v3BaseBank,v3BaseGeneration,0) == null);
        if (maintenancePanel != null) maintenancePanel.SetActive(full);
        if (maintenanceNotice != null) maintenanceNotice.SetActive(active && !full);
        if (!active)
        {
            if (guideV3 && onlineMode && Time.realtimeSinceStartup < v3ServerWaitUntil && statusLabel != null && !favoriteTabSelected)
                statusLabel.text = "混雑しています。時間をおいて自動で再試行します";
            return;
        }
        if (maintenancePanel != null) maintenancePanel.GetComponent<Image>().color = backgroundColor;
        if (maintenanceNotice != null) maintenanceNotice.GetComponent<Image>().color = cardColor;
        if (maintenanceMessage != null) { maintenanceMessage.text = MaintenanceMessage; maintenanceMessage.color = headingColor; }
        if (maintenanceNoticeText != null) { maintenanceNoticeText.text = MaintenanceMessage; maintenanceNoticeText.color = bodyColor; }
        if (maintenanceHint != null)
        {
            maintenanceHint.text = favoritesEnabled ? "お気に入りワールドは引き続き利用できます。\n終了後、自動で読み込みを再開します。" : "終了後、自動で読み込みを再開します。";
            maintenanceHint.color = secondaryColor;
        }
        if (sponsorBannerButton != null) sponsorBannerButton.gameObject.SetActive(false);
        if (sponsorDisclosure != null) sponsorDisclosure.SetActive(false);
        if (retryButton != null) retryButton.gameObject.SetActive(false);
        if (statusLabel != null) statusLabel.text = favoriteTabSelected ? "設置者が登録したお気に入りワールド" : "復旧を自動で確認しています";
    }

    private void Place(RectTransform r, float x, float y, float w, float h)
    {
        // Both prefabs share the standard page coordinates.
        float scale = 1f;
        r.anchoredPosition = new Vector2(x * scale, -y); r.sizeDelta = new Vector2(w * scale, h);
    }

    private void LayoutDailyCards()
    {
        for (int k = 0; k < topButtons.Length; k++) topButtons[k].gameObject.SetActive(!manyVersion);
        if (manyButtons != null) for (int k = 0; k < manyButtons.Length; k++) if (manyButtons[k] != null) manyButtons[k].gameObject.SetActive(manyVersion);
        if (view != 0) return;
        if (manyVersion)
        {
            if (sponsoredWorldPanel != null) sponsoredWorldPanel.SetActive(ready && sponsoredCount > 0);
            for (int k = 0; k < 2; k++) topButtons[k].gameObject.SetActive(false);
            RenderMany();
            return;
        }
        bool compact = ready && sponsoredCount > 0;
        if (sponsoredWorldPanel != null) sponsoredWorldPanel.SetActive(compact);
        for (int i = 0; i < 2; i++)
        {
            Place((RectTransform)topButtons[i].transform, 0, compact ? 44 + i * 267 : 44 + i * 391, 760, compact ? 255 : 387);
            Place(topPictures[i].rectTransform, compact ? 8 : 6, compact ? 8 : 6, compact ? 310 : 748, compact ? 174 : 218);
            Place(topCaptions[i].rectTransform, compact ? 333 : 20, compact ? 15 : 231, compact ? 410 : 720, compact ? 55 : 32);
            Place(topTitles[i].rectTransform, compact ? 333 : 20, compact ? 80 : 268, compact ? 410 : 704, compact ? 62 : 34);
            if (topAuthors != null && i < topAuthors.Length && topAuthors[i] != null)
                Place(topAuthors[i].rectTransform, compact ? 559 : 300, compact ? 147 : 302, compact ? 184 : 424, compact ? 27 : 22);
            if (topPlatforms != null && i < topPlatforms.Length && topPlatforms[i] != null) Place(topPlatforms[i].rectTransform, compact ? 333 : 20, compact ? 147 : 302, compact ? 222 : 276, compact ? 27 : 22);
            Place(topSummaries[i].rectTransform, compact ? 12 : 20, compact ? 190 : 322, compact ? 736 : 704, 55);
        }
    }

    public void _ApplyAppearance()
    {
        if (xIconImage != null) xIconImage.color = xIconColor;
        if (boothIconImage != null) boothIconImage.color = boothIconColor;
        if (archiveIconImage != null) { archiveIconImage.material = archiveIconUseOriginalColor ? null : archiveIconTintMaterial; archiveIconImage.color = archiveIconUseOriginalColor ? Color.white : archiveIconColor; }
        TintTexts(headingTexts, headingColor); TintTexts(bodyTexts, bodyColor);
        TintTexts(secondaryTexts, secondaryColor); TintTexts(accentTexts, accentColor);
        TintTexts(buttonTexts, buttonTextColor);
        if (brandTitleImage != null) brandTitleImage.color = brandTitleColor;
        if (guideTitleImage != null) guideTitleImage.color = guideTitleColor;
        if (archiveUrlPanel != null)
        {
            var archiveCard = archiveUrlPanel.transform.Find("Card");
            archiveCard.GetComponent<Image>().color = backgroundColor;
            archiveCard.GetComponent<Outline>().effectColor = borderColor;
            archiveCard.Find("X heading").GetComponent<Text>().color = headingColor;
            archiveCard.Find("X help").GetComponent<Text>().color = secondaryColor;
            archiveCard.Find("Close").GetComponent<Image>().color = cardColor;
            archiveCard.Find("Close/Label").GetComponent<Text>().color = buttonTextColor;
            if (archiveUrlField != null) { archiveUrlField.GetComponent<Image>().color = cardColor; archiveUrlField.textComponent.color = bodyColor; }
        }
        if (backgroundImage != null) backgroundImage.color = backgroundColor;
        if (cardImages != null) for (int i = 0; i < cardImages.Length; i++) if (cardImages[i] != null) cardImages[i].color = cardColor;
        if (portalImages != null) for (int i = 0; i < portalImages.Length; i++) if (portalImages[i] != null) portalImages[i].color = portalColor;
        if (borderOutlines != null) for (int i = 0; i < borderOutlines.Length; i++) if (borderOutlines[i] != null) borderOutlines[i].effectColor = borderColor;
        if (dividerImages != null) for (int i = 0; i < dividerImages.Length; i++) if (dividerImages[i] != null) dividerImages[i].color = borderColor;
        if (navImages != null) for (int i = 0; i < navImages.Length; i++) if (navImages[i] != null) navImages[i].color = i == (favoriteTabSelected ? 4 : tab) ? selectedTabColor : cardColor;
        if (xUrlPanel != null)
        {
            xUrlPanel.GetComponent<Image>().color = new Color(0, 0, 0, .6f);
            var card = xUrlPanel.transform.Find("Card");
            card.GetComponent<Image>().color = backgroundColor;
            card.GetComponent<Outline>().effectColor = borderColor;
            card.Find("X heading").GetComponent<Text>().color = headingColor;
            card.Find("X help").GetComponent<Text>().color = secondaryColor;
            card.Find("Close").GetComponent<Image>().color = cardColor;
            card.Find("Close/Label").GetComponent<Text>().color = buttonTextColor;
            if (xUrlField != null) { xUrlField.GetComponent<Image>().color = cardColor; xUrlField.textComponent.color = bodyColor; }
        }
    }

    private void TintTexts(Text[] group, Color color)
    { if (group != null) for (int i = 0; i < group.Length; i++) if (group[i] != null) group[i].color = color; }

    private void SponsorPhoto(RawImage image, int cell)
    {
        if (image == null) return;
        image.texture = cell >= 0 && cell < 64 ? sponsorTexture : null;
        image.color = image.texture == null ? new Color(.04f, .12f, .19f, 1) : Color.white;
        image.uvRect = cell < 0 ? new Rect(0, 0, 1, 1) : new Rect((cell % 8) / 8f + .5f / 2048f, (7 - cell / 8) / 8f + .5f / 2048f, 1f / 8f - 1f / 2048f, 1f / 8f - 1f / 2048f);
    }

    private void SponsorBanner(RawImage image, int ad)
    {
        bool footer = image == bannerPicture;
        float width = v4Layout || bannerAtlasV2 ? 760f : 760f / 6f;
        // TOP cards already have a 356-wide image area; do not expand them to
        // the 760-wide footer/detail coordinates.
        if (footer || image == adPicture)
            Place(image.rectTransform, (760f - width) / 2f, footer ? 0 : 23, width, 760f / 6f);
        if (guideV3 && onlineMode) { V3Paint(image,v3BannerImages[ad],v3BaseBank,v3BaseGeneration,false); return; }
        if (!bannerAtlasV2) { SponsorPhoto(image, adCells[ad]); return; }
        image.texture = adCells[ad] >= 0 ? (adBannerPages[ad] == 0 ? sponsorTexture : extraSponsorTextures[0]) : null;
        image.color = image.texture == null ? new Color(.04f, .12f, .19f, 1) : Color.white;
        if (bannerAtlasV3)
        {
            // Two columns of 1024x256 blocks: banner at top-left, QR at right.
            int cell = adBannerRows[ad];
            image.uvRect = new Rect((cell % 2) * .5f + .5f / 2048f,
                1f - (cell / 2) * .125f - .0625f + .5f / 2048f,
                .375f - 1f / 2048f, .0625f - 1f / 2048f);
        }
        else image.uvRect = new Rect(.5f / 2048f, (7 - adBannerRows[ad]) / 8f + .5f / 2048f, .75f - 1f / 2048f, .125f - 1f / 2048f);
    }

    private void SponsorQr(int ad)
    {
        if (guideV3 && onlineMode) { V3Paint(sponsorQrPicture,v3QrImages[ad],v3BaseBank,v3BaseGeneration,false); return; }
        if (!bannerAtlasV2) { SponsorPhoto(sponsorQrPicture, adQrCells[ad]); return; }
        sponsorQrPicture.texture = adQrCells[ad] >= 0 ? (adBannerPages[ad] == 0 ? sponsorTexture : extraSponsorTextures[0]) : null;
        sponsorQrPicture.color = sponsorQrPicture.texture == null ? new Color(.04f, .12f, .19f, 1) : Color.white;
        if (bannerAtlasV3)
        {
            int cell = adBannerRows[ad];
            sponsorQrPicture.uvRect = new Rect((cell % 2) * .5f + .375f + .5f / 2048f,
                (7 - cell / 2) * .125f + .5f / 2048f,
                .125f - 1f / 2048f, .125f - 1f / 2048f);
        }
        else sponsorQrPicture.uvRect = new Rect(.75f + .5f / 2048f, (7 - adBannerRows[ad]) / 8f + .5f / 2048f, .125f - 1f / 2048f, .125f - 1f / 2048f);
    }

    private void SponsorDetail(int ad)
    {
        bool premium = TopPortalAvailable();
        float detailHeight = premium ? 566f : 641.25f;
        if (guideV3 && onlineMode)
        {
            if (v4Layout) detailHeight = 500f;
            float width = detailHeight*672f/567f;
            Place(adPicture.rectTransform,(760f-width)*.5f,v4Layout ? 70 : 23,width,detailHeight);
            DataDictionary image = v3DetailImages[ad];
            if (v4Layout && !V4Loaded(image) && V4Loaded(v4PreviewImages[ad])) image = v4PreviewImages[ad];
            V3Paint(adPicture,image,v3BaseBank,v3BaseGeneration,false); return;
        }
        int cell = adDetailCells[ad];
        if (cell >= 0 && cell < 16 && extraSponsorTextures[1 + cell / detailCellsPerPage] != null)
        {
            float detailWidth = detailHeight * 1024f / 864f;
            Place(adPicture.rectTransform, (760f - detailWidth) * .5f, 23, detailWidth, detailHeight);
            adPicture.texture = extraSponsorTextures[1 + cell / detailCellsPerPage]; adPicture.color = Color.white;
            int local = cell % detailCellsPerPage;
            if (detailAtlasV2)
                adPicture.uvRect = new Rect((local % 3) / 3f + .5f / 2016f, (2 - local / 3) / 3f + .5f / 1701f, 1f / 3f - 1f / 2016f, 1f / 3f - 1f / 1701f);
            else adPicture.uvRect = new Rect((local % 2) * .5f + .5f / 2048f, (1 - local / 2) * .5f + .5f / 1728f, .5f - 1f / 2048f, .5f - 1f / 1728f);
            return;
        }
        float placeholderWidth = detailHeight * 1024f / 864f;
        Place(adPicture.rectTransform, (760f - placeholderWidth) * .5f, 23, placeholderWidth, detailHeight);
        SetPlaceholder(adPicture, cell < 0 || cell >= 16 ? 1 : extraSponsorFailed[1 + cell / detailCellsPerPage] ? 2 : 0);
    }

    // Independent caches for titles, summaries and author labels.
    private Text[] fittedLabels = new Text[21];
    private string[] fittedInputs = new string[21];
    private string[] fittedOutputs = new string[21];
    private UnityEngine.Object[] fittedFonts = new UnityEngine.Object[21];
    private int[] fittedSizes = new int[21];
    private int[] fittedStyles = new int[21];
    private float[] fittedWidths = new float[21];

    private string FitSingleLine(Text label, string value, int slot)
    {
        if (label == null) return "";
        string text = string.IsNullOrEmpty(value) ? "" : value.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");
        // Measurement and rendering use the same fixed-size, plain-text UI label.
        label.resizeTextForBestFit = false;
        label.supportRichText = false;
        float width = Mathf.Max(0f, label.rectTransform.rect.width - 2f);
        if (fittedLabels[slot] == label && fittedInputs[slot] == text &&
            fittedFonts[slot] == label.font && fittedSizes[slot] == label.fontSize &&
            fittedStyles[slot] == (int)label.fontStyle && fittedWidths[slot] == width)
            return fittedOutputs[slot];

        label.text = text;
        string result = text;
        if (text.Length > 0 && label.preferredWidth > width)
        {
            label.text = "…";
            result = "";
            if (label.preferredWidth <= width)
            {
                int low = 0, high = text.Length - 1, best = 0;
                while (low <= high)
                {
                    int middle = low + (high - low) / 2;
                    int end = SafeLineBoundary(text, middle);
                    label.text = text.Substring(0, end) + "…";
                    if (label.preferredWidth <= width) { best = end; low = middle + 1; }
                    else high = middle - 1;
                }
                result = text.Substring(0, best) + "…";
            }
        }
        // Intermediate candidates are replaced synchronously, before a rendered frame.
        label.text = result;
        fittedLabels[slot] = label;
        fittedInputs[slot] = text;
        fittedOutputs[slot] = result;
        fittedFonts[slot] = label.font;
        fittedSizes[slot] = label.fontSize;
        fittedStyles[slot] = (int)label.fontStyle;
        fittedWidths[slot] = width;
        return result;
    }

    private int SafeLineBoundary(string text, int end)
    {
        // Keep surrogate pairs, combining marks, and joined emoji together.
        while (end > 0 && end < text.Length)
        {
            char next = text[end];
            System.Globalization.UnicodeCategory category = char.GetUnicodeCategory(text, end);
            if ((next >= '\uDC00' && next <= '\uDFFF') || next == '\u200D' || text[end - 1] == '\u200D' ||
                category == System.Globalization.UnicodeCategory.NonSpacingMark ||
                category == System.Globalization.UnicodeCategory.SpacingCombiningMark ||
                category == System.Globalization.UnicodeCategory.EnclosingMark)
                end--;
            else break;
        }
        return end;
    }

    private void RenderSponsors()
    {
        if (sponsoredWorldButtons != null && sponsoredWorldButtons.Length == 2)
        {
            for (int s = 0; s < 2; s++)
            {
                int world = s == 0 ? sponsorLeft : sponsorRight;
                sponsoredWorldButtons[s].gameObject.SetActive(world >= 0);
                if (sponsorGoldFrames != null && s < sponsorGoldFrames.Length && sponsorGoldFrames[s] != null)
                    sponsorGoldFrames[s].gameObject.SetActive(world >= 0 && topPlacementProtocol && topGoldFrames[world]);
                if (world >= 0)
                {
                    RawImage photo = sponsoredWorldPictures[s];
                    Text cardTitle = sponsoredWorldTitles[s];
                    Place(cardTitle.rectTransform,10,v4Layout ? 214 : 150,352,v4Layout ? 30 : 92);
                    cardTitle.fontSize = v4Layout ? 20 : 24;
                    cardTitle.resizeTextForBestFit = !v4Layout;
                    cardTitle.horizontalOverflow = HorizontalWrapMode.Wrap;
                    cardTitle.verticalOverflow = VerticalWrapMode.Truncate;
                    if (topPlacementProtocol)
                    {
                        sponsoredWorldButtons[s].interactable = IsTopActive(world);
                        sponsoredWorldTitles[s].text = v4Layout ? FitSingleLine(cardTitle,topAdTitles[world],8+s) : topAdTitles[world];
                        if (guideV3 && onlineMode && topAds[world] >= 0 && v3TopImages[topAds[world]] != null)
                        { Place(photo.rectTransform,8,8,356,v4Layout ? 200.25f : 133); V3Paint(photo,v3TopImages[topAds[world]],v3BaseBank,v3BaseGeneration,false); }
                        else if (!guideV3 && topImageIndices[world] >= 0)
                        {
                            int cell = topImageIndices[world]; int slot = 5 + cell / 8;
                            Place(photo.rectTransform,8,8,356,133);
                            if (extraSponsorTextures[slot] == null) SetPlaceholder(photo, extraSponsorFailed[slot] ? 2 : 0);
                            else { photo.texture = extraSponsorTextures[slot]; photo.color = Color.white; photo.uvRect = new Rect((cell % 2) * .5f, 1f - (cell % 8 / 2 + 1) * .25f, .5f, .25f); }
                        }
                        else if (topTypes[world] == 0) { Place(photo.rectTransform,8,8,356,133); SetPhoto(photo,topWorlds[world]); }
                        else { Place(photo.rectTransform,8,8+(133f-356f/6f)*.5f,356,356f/6f); SponsorBanner(photo,topAds[world]); }
                    }
                    else { sponsoredWorldTitles[s].text = titles[world]; Place(photo.rectTransform,8,8,356,133); SetPhoto(photo,world); }
                }
            }
        }
        if (sponsorBannerButton != null) { sponsorBannerButton.gameObject.SetActive(!onlineMode || activeAdCount > 0); sponsorBannerButton.interactable = !onlineMode || IsAdActive(currentAd); }
        if (sponsorDisclosure != null) sponsorDisclosure.SetActive(!onlineMode || activeAdCount > 0);
        if (sponsorLinkHelp != null) sponsorLinkHelp.gameObject.SetActive(activeAdCount > 0 || (view == 3 && IsAdActive(openedAd)));
        if ((activeAdCount == 0 || !IsAdActive(currentAd)) && !(view == 3 && IsAdActive(openedAd)))
        {
            if (sponsorQrPicture != null) sponsorQrPicture.gameObject.SetActive(false);
            if (sponsorUrlField != null) sponsorUrlField.gameObject.SetActive(false);
            adPortalButton.gameObject.SetActive(!onlineMode && adWorld >= 0);
            return;
        }
        int a = view == 3 && IsAdActive(openedAd) ? openedAd : currentAd;
        if (currentAd >= 0) { bannerTitle.text = adNames[currentAd]; SponsorBanner(bannerPicture, currentAd); }
        adTitle.text = adNames[a]; adDescription.text = adDescriptions[a]; adSponsor.text = "広告主: " + adSponsors[a];
        SponsorDetail(a);
        bool premium = view == 3 && TopPortalAvailable();
        float linkY = v4Layout ? 675f : premium ? 600f : 675f;
        if (sponsorQrPicture != null) Place(sponsorQrPicture.rectTransform,664,linkY,96,96);
        if (sponsorUrlField != null) Place((RectTransform)sponsorUrlField.transform,0,linkY,650,96);
        if (sponsorLinkHelp != null) Place(sponsorLinkHelp.rectTransform,10,linkY+66,630,25);
        Place((RectTransform)adPortalButton.transform,v4Layout ? 388 : 130,v4Layout ? 788 : 711,v4Layout ? 372 : 500,v4Layout ? 35 : 62);
        Transform back = adPanel.transform.Find("Ad back");
        if (back != null) { float backWidth = v4Layout && premium ? 372 : 760; Place((RectTransform)back,0,788,backWidth,35); Text backText = back.GetComponentInChildren<Text>(); if (backText != null) Place(backText.rectTransform,0,0,backWidth,35); }
        if (v4Layout) { Place(adPortalLabel.rectTransform,0,0,372,35); adPortalLabel.fontSize = 20; }
        if (sponsorQrPicture != null) { sponsorQrPicture.gameObject.SetActive(true); SponsorQr(a); }
        if (sponsorUrlField != null) { sponsorUrlField.gameObject.SetActive(true); sponsorUrlField.text = adUrls[a]; }
        adPortalButton.gameObject.SetActive(premium);
        if (v4Layout)
        {
            adTitle.gameObject.SetActive(true); adDescription.gameObject.SetActive(true);
            adSponsor.gameObject.SetActive(false);
            Place(adTitle.rectTransform,10,5,740,55);
            Place(adDescription.rectTransform,10,605,740,60);
        }
    }

    private int Utf8Bytes(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        int bytes = 0;
        for (int i = 0; i < text.Length; i++)
        {
            int c = text[i];
            if (c < 128) bytes++;
            else if (c < 2048) bytes += 2;
            else if (c >= 55296 && c <= 56319 && i + 1 < text.Length && text[i + 1] >= 56320 && text[i + 1] <= 57343) { bytes += 4; i++; }
            else bytes += 3;
        }
        return bytes;
    }

    private float SharedRemaining(double deadline)
    {
        if (deadline == 0) return 0;
        double remaining = Networking.LocalPlayer == null ? deadline - TimeNow() : Networking.CalculateServerDeltaTime(deadline, TimeNow());
        if (remaining > 2147483.648d) remaining -= 4294967.296d;
        else if (remaining < -2147483.648d) remaining += 4294967.296d;
        return remaining > 0 && remaining <= 600 ? (float)remaining : 0;
    }

    public bool SharedDrawBusy()
    { return (guideV3 ? v3DrawCategory >= 0 : pendingDrawTab >= 0) || (sharedDrawBusyTab >= 0 && SharedRemaining(sharedDrawBusyEnd) > 0); }

    public void _RequestSharedDraw(int targetTab)
    {
        if ((!isSharedDataSource && !(guideV3 && onlineMode)) || (targetTab != 1 && targetTab != 3) || SharedDrawBusy() || SharedRemaining(sharedDrawCooldownEnd) > 0) return;
        if (Networking.LocalPlayer == null || Networking.IsOwner(gameObject)) _SharedDrawOnOwner(targetTab);
        else SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(_SharedDrawOnOwner), targetTab);
    }

    [NetworkCallable(maxEventsPerSecond: 1)]
    public void _SharedDrawOnOwner(int targetTab)
    {
        if ((!isSharedDataSource && !(guideV3 && onlineMode)) || (Networking.LocalPlayer != null && !Networking.IsOwner(gameObject))) return;
        if (!ready || (targetTab != 1 && targetTab != 3) || SharedDrawBusy() || SharedRemaining(sharedDrawCooldownEnd) > 0) return;
        if (guideV3 && onlineMode) { V3BeginDraw(targetTab == 1 ? 0 : 1,true); return; }
        RequestDraw(targetTab, false);
    }

    // The small verified result is synchronized. Each client downloads a leased atlas
    // at most once; an expired/reused atlas is never displayed for a different result.
    private void ReceiveSharedDraw()
    {
        if (!isSharedDataSource || !ready || pendingDrawTab >= 0) return;
        for (int slot = 0; slot < 2; slot++)
        {
            string json = slot == 0 ? sharedRecommendedDraw : sharedRandomDraw;
            if (string.IsNullOrEmpty(json) || json == committedDrawJson[slot] || json == attemptedSharedDrawJson[slot]) continue;
            attemptedSharedDrawJson[slot] = json;
            pendingDrawTab = slot == 0 ? 1 : 3;
            drawStartedAt = Time.realtimeSinceStartup;
            pendingDrawAtlas = -1; pendingDrawJson = ""; pendingDrawGeneration = "";
            receivingSharedDraw = true; sharedImageUnavailable = false;
            drawLoadingMetadata = false; drawImageRequested = false; drawVerifying = false; drawReadyToCommit = false;
            pendingDrawTexture = null; pendingDrawDownload = null;
            if (!ValidateDraw(json)) { FailDraw("共有された抽選結果を確認できませんでした"); receivingSharedDraw = false; return; }
            pendingDrawJson = json;
            drawLeaseUntil = Time.realtimeSinceStartup + SharedRemaining(slot == 0 ? sharedRecommendedExpiry : sharedRandomExpiry);
            initialDrawAttempted[slot] = true;
            if (pendingDrawAtlas < 0 || drawLeaseUntil <= Time.realtimeSinceStartup + 5)
            {
                sharedImageUnavailable = pendingDrawAtlas >= 0;
                pendingDrawAtlas = -1; drawReadyToCommit = true; TryCommitDraw();
            }
            else PumpImageDownloads();
            return;
        }
    }

    // Display-local arrays keep a detail page stable while a newer result waits.
    // Textures alone are shared; downloads and Dispose remain the source's responsibility.
    private void PullSharedData()
    {
        if (guideV3 && onlineMode) { V3Pull(); return; }
        var source = sharedDataSource;
        if (source == null || !source.isSharedDataSource) return;
        if (!source.ready)
        {
            loading = source.loading; failed = source.failed; Render(); return;
        }
        bool changed = false;
        if (appliedSnapshot != source.pinnedSnapshot)
        {
            if (string.IsNullOrEmpty(source.pinnedSnapshot) || !Parse(source.pinnedSnapshot)) return;
            appliedSnapshot = source.pinnedSnapshot;
            changed = true;
        }
        loading = source.loading; failed = source.failed;
        if (atlasTexture != source.atlasTexture || sponsorTexture != source.sponsorTexture || imageLoading != source.imageLoading || imageFailed != source.imageFailed || sponsorImageFailed != source.sponsorImageFailed) changed = true;
        atlasTexture = source.atlasTexture; sponsorTexture = source.sponsorTexture;
        imageLoading = source.imageLoading; imageFailed = source.imageFailed;
        sponsorImageLoading = source.sponsorImageLoading; sponsorImageFailed = source.sponsorImageFailed;
        for (int i = 0; i < 7; i++)
        {
            if (extraSponsorTextures[i] != source.extraSponsorTextures[i] || extraSponsorLoading[i] != source.extraSponsorLoading[i] || extraSponsorFailed[i] != source.extraSponsorFailed[i]) changed = true;
            extraSponsorTextures[i] = source.extraSponsorTextures[i];
            extraSponsorLoading[i] = source.extraSponsorLoading[i]; extraSponsorFailed[i] = source.extraSponsorFailed[i];
        }
        if (appliedSponsorStatusVersion != source.sponsorStatusVersion)
        {
            if (!string.IsNullOrEmpty(source.sponsorStatusJson)) ApplySponsorStatus(source.sponsorStatusJson);
            appliedSponsorStatusVersion = source.sponsorStatusVersion;
            changed = true;
        }
        if (sponsorStatusValid != source.sponsorStatusValid) changed = true;
        sponsorStatusValid = source.sponsorStatusValid; sponsorStatusLoading = source.sponsorStatusLoading; sponsorStatusFailed = source.sponsorStatusFailed;
        sponsorStatusUtc = source.sponsorStatusUtc; sponsorStatusReceived = source.sponsorStatusReceived;
        if (RefreshSponsorAvailability()) changed = true;
        if (currentAd != source.currentAd || sponsorLeft != source.sponsorLeft || sponsorRight != source.sponsorRight) changed = true;
        currentAd = source.currentAd; sponsorLeft = source.sponsorLeft; sponsorRight = source.sponsorRight;
        drawCooldownUntil = Time.realtimeSinceStartup + source.SharedRemaining(source.sharedDrawCooldownEnd);
        for (int slot = 0; slot < 2; slot++)
        {
            if (drawErrors[slot] != source.drawErrors[slot]) { drawErrors[slot] = source.drawErrors[slot]; changed = true; }
            string json = source.committedDrawJson[slot];
            if (string.IsNullOrEmpty(json)) continue;
            if (appliedSharedDrawJson[slot] != json)
            {
                int start = 64 + slot * 64;
                if ((view == 2 || (view == 3 && beforeAdView == 2)) && selected >= start && selected < start + 64) continue;
                pendingDrawTab = slot == 0 ? 1 : 3;
                pendingDrawJson = json; pendingDrawTexture = source.drawTextures[slot]; pendingDrawDownload = null;
                drawReadyToCommit = true; TryCommitDraw();
                appliedSharedDrawJson[slot] = json; changed = true;
            }
            if (drawTextures[slot] != source.drawTextures[slot]) { drawTextures[slot] = source.drawTextures[slot]; changed = true; }
            drawErrors[slot] = source.drawErrors[slot];
        }
        if (changed) Render();
    }

    private void RetireDraw(IVRCImageDownload download)
    {
        ReleaseUnusedDraws();
        for (int i = 0; i < retiredDrawDownloads.Length; i++)
        {
            if (retiredDrawDownloads[i] != null) continue;
            retiredDrawDownloads[i] = download; return;
        }
        var expanded = new IVRCImageDownload[retiredDrawDownloads.Length + 4];
        for (int i = 0; i < retiredDrawDownloads.Length; i++) expanded[i] = retiredDrawDownloads[i];
        expanded[retiredDrawDownloads.Length] = download; retiredDrawDownloads = expanded;
    }

    private void ReleaseUnusedDraws()
    {
        for (int i = 0; i < retiredDrawDownloads.Length; i++)
        {
            var download = retiredDrawDownloads[i];
            if (download == null) continue;
            var texture = download.Result;
            bool used = false;
            if (sharedDisplays != null) for (int j = 0; j < sharedDisplays.Length; j++)
            {
                var display = sharedDisplays[j];
                if (display == null) continue;
                if (display.drawTextures[0] == texture || display.drawTextures[1] == texture) used = true;
            }
            if (!used) { download.Dispose(); retiredDrawDownloads[i] = null; }
        }
    }


    private bool V3TelemetryAllowed()
    {
        if (v3Maintenance || v3RecoveryPending || !ready || imageLoading || !sponsorStatusValid || v3RequestKind != 0 || v3DrawCategory >= 0 ||
            Time.realtimeSinceStartup < v3NextStringAt || Time.realtimeSinceStartup < v3ServerWaitUntil ||
            Time.realtimeSinceStartup >= v3NextStatus) return false;
        for (int i = 0; i < v3WireUrls.Length; i++) if (!string.IsNullOrEmpty(v3WireUrls[i])) return false;
        return true;
    }

    private bool V3TrackStringRequest(string url)
    {
        int free = -1;
        for (int i = 0; i < v3WireUrls.Length; i++)
        {
            // At ten minutes an abandoned current/status body is older than
            // its maximum five-minute validity. Immutable set responses still
            // must match the currently requested bank/generation/set exactly.
            // Reclaim orphaned slots so a lost SDK callback cannot deadlock us.
            if (v3WireAbandoned[i] && Time.realtimeSinceStartup-v3WireStarted[i] >= 600f)
            { v3WireUrls[i] = ""; v3WireAbandoned[i] = false; }
            if (v3WireUrls[i] == url) return false;
            if (string.IsNullOrEmpty(v3WireUrls[i])) free = i;
        }
        if (free < 0) return false;
        v3WireUrls[free] = url; v3WireAbandoned[free] = false; v3WireStarted[free] = Time.realtimeSinceStartup;
        return true;
    }

    private void V3AbandonStringRequest()
    {
        if ((v3RequestUrl == null || string.IsNullOrEmpty(v3RequestUrl.Get()))) return;
        for (int i = 0; i < v3WireUrls.Length; i++)
            if (v3WireUrls[i] == v3RequestUrl.Get()) v3WireAbandoned[i] = true;
    }

    private bool V3TakeStringResponse(string url)
    {
        for (int i = 0; i < v3WireUrls.Length; i++)
        {
            if (v3WireUrls[i] != url) continue;
            bool abandoned = v3WireAbandoned[i];
            v3WireUrls[i] = ""; v3WireAbandoned[i] = false;
            if (!abandoned && v3RequestKind != 0 && v3RequestInFlight && !(v3RequestUrl == null || string.IsNullOrEmpty(v3RequestUrl.Get())) && v3RequestUrl.Get() == url) return true;
            staleResponseCount++; return false;
        }
        staleResponseCount++; return false;
    }

    private void V3CancelRequest()
    {
        V3AbandonStringRequest();
        v3RequestKind = 0; v3RequestInFlight = false; v3RequestAttempts = 0;
        sponsorStatusLoading = false;
    }

    private bool V3RequestContextValid()
    {
        int kind = v3RequestKind;
        if (kind == 1)
        {
            if (v3RequestBaseSelection != v3SharedBaseGeneration) return false;
            if (string.IsNullOrEmpty(v3RequestGeneration)) return V3Owner() && v3RequestOwnerTerm == v3OwnerTerm;
            return v3RequestGeneration == v3SharedBaseGeneration && v3RequestBank == v3SharedBaseBank;
        }
        if (kind == 2 || kind == 3 || kind == 6)
        {
            if (v3DrawCategory < 0 || v3RequestCategory != v3DrawCategory) return false;
            if (v3UserDraw && (!V3Owner() || v3RequestOwnerTerm != v3OwnerTerm || v3RequestSelectionSequence != v3SelectionSequence)) return false;
            if (kind == 2) return v3AwaitCatalog && V3Owner();
            if (v3RequestGeneration != v3TargetGeneration || v3RequestBank != v3TargetBank || v3RequestSet != v3TargetSet) return false;
            if (!v3UserDraw && v3DrawCategory < 2)
                return V3SharedGeneration(v3DrawCategory) == v3TargetGeneration && V3SharedBank(v3DrawCategory) == v3TargetBank && V3SharedSet(v3DrawCategory) == v3TargetSet;
        }
        return true;
    }

    private bool V3WaitResponse(DataDictionary d)
    {
        if (d == null || Str(d,"schema") != "metarea-guide-wait-v1" || !Flag(d,"waiting")) return false;
        bool busy = Str(d,"reason") == "busy";
        float wait = busy ? Mathf.Max(Mathf.Clamp(Integer(d,"retryAfter",5),5,300),V3Backoff(v3RequestAttempts)) : Mathf.Clamp(Integer(d,"retryAfter",60),10,300)+Random.Range(0f,10f);
        float until = Time.realtimeSinceStartup+wait;
        if (busy) v3JsonBusyUntil = until;
        else v3ServerWaitUntil = until;
        v3RequestInFlight = false; sponsorStatusLoading = false;
        v3RequestRetryAt = until;
        serverWaitCount++;
        // Waiting consumes an attempt too. Repeated HTTP 200 wait responses
        // cannot renew an unfinished draw or metadata transaction forever.
        if (v3RequestAttempts >= 4)
        {
            v3BusyResponse = busy;
            V3RequestFailed(true);
            v3BusyResponse = false;
        }
        v3Version++;
        return true;
    }

    private bool V3Owner()
    { return Networking.LocalPlayer == null || Networking.IsOwner(gameObject); }

    private double V3Now()
    {
        double now = v3ServerTime > 0 ? v3ServerTime + Time.realtimeSinceStartup - v3ServerReceived : 0;
        // Network UTC protects the first response too, before an HTTP clock
        // anchor exists. Editor fixtures intentionally have no LocalPlayer.
        if (Networking.LocalPlayer != null)
        {
            double network = (Networking.GetNetworkDateTime().Ticks - 621355968000000000L) / 10000000d;
            if (network > now) now = network;
        }
        return now;
    }

    private void V3AdvanceClock(double server)
    {
        double now = V3Now();
        v3ServerTime = server > now ? server : now;
        v3ServerReceived = Time.realtimeSinceStartup;
    }

    private bool V3Hex(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length != 16) return false;
        for (int i = 0; i < 16; i++) if ("0123456789abcdef".IndexOf(value[i]) < 0) return false;
        return true;
    }

    private DataDictionary V3Object(DataDictionary d, string name)
    { DataToken t; return d != null && d.TryGetValue(name, TokenType.DataDictionary, out t) ? t.DataDictionary : null; }

    private DataList V3List(DataDictionary d, string name)
    { DataToken t; return d != null && d.TryGetValue(name, TokenType.DataList, out t) ? t.DataList : null; }

    private DataDictionary V3Json(string json, int maxLength)
    {
        DataToken t;
        if (string.IsNullOrEmpty(json) || json.Length > maxLength || !VRCJson.TryDeserializeFromJson(json, out t) || t.TokenType != TokenType.DataDictionary) return null;
        return t.DataDictionary;
    }

    private string[] V3Strings(DataList list, int maximum)
    {
        if (list == null || list.Count > maximum) return new string[0];
        string[] values = new string[list.Count];
        for (int i = 0; i < list.Count; i++)
        { if (list[i].TokenType != TokenType.String || list[i].String.Length > 128) return new string[0]; values[i] = list[i].String; }
        return values;
    }

    private string V3SharedGeneration(int category)
    { return category == 0 ? v3SharedRecommendedGeneration : v3SharedRandomGeneration; }
    private int V3SharedBank(int category)
    { return category == 0 ? v3SharedRecommendedBank : v3SharedRandomBank; }
    private int V3SharedSet(int category)
    { return category == 0 ? v3SharedRecommendedSet : v3SharedRandomSet; }
    private string V3SelectionKey(string generation, int set)
    { return generation + ":" + set; }

    private bool V3BankUsable(int bank, string generation)
    {
        if (bank < 0 || bank >= 32 || !V3Hex(generation)) return false;
        if (v3BankGenerations[bank] != generation) return false;
        return v3BankExpiry[bank] == 0 || V3Now() < v3BankExpiry[bank];
    }

    private bool V3Descriptor(DataDictionary a)
    {
        if (a == null) return false;
        int id = Integer(a, "assetId", -1), w = Integer(a, "width", -1), h = Integer(a, "height", -1);
        if (id < 0 || id > 31 || w < 64 || w > 2048 || h < 16 || h > 2048 || !V3Hex(Str(a, "imageToken"))) return false;
        string ext = id == 0 || (id >= 26 && id <= 30) ? "png" : "jpg";
        if (Str(a, "format") != ext || Str(a, "path") != "images/" + id + "." + ext) return false;
        DataDictionary m = V3Object(a, "marker");
        if (m == null) return false;
        int x = Integer(m,"x",-1), y = Integer(m,"y",-1), mw = Integer(m,"width",-1), mh = Integer(m,"height",-1);
        return x >= 0 && y >= 0 && mw >= 64 && mw % 64 == 0 && mh >= 1 && x + mw <= w && y + mh <= h;
    }

    private bool V3RegisterAsset(int bank, string generation, DataDictionary asset)
    {
        if (bank < 0 || bank >= 32 || !V3Hex(generation) || !V3Descriptor(asset)) return false;
        int key = bank * 32 + Integer(asset,"assetId",-1);
        if (v3Assets[key] == null || Str(v3Assets[key],"imageToken") != Str(asset,"imageToken"))
        { v3ImageAttempts[key] = 0; v3ImageRetryAt[key] = 0; }
        v3Assets[key] = asset; v3AssetGenerations[key] = generation;
        return true;
    }

    private bool V3RegisterManifest(DataDictionary d)
    {
        int bank = Integer(d,"bank",-1);
        string generation = Str(d,"generationId");
        DataList assets = V3List(d,"assets");
        if (Str(d,"schema") != (v4Protocol ? "metarea-guide-v4" : "metarea-guide-v3") || Integer(d,"layoutVersion",-1) != (v4Protocol ? 2 : 1) || bank < 0 || bank >= 32 || !V3Hex(generation) || assets == null || assets.Count < 1 || assets.Count > 32) return false;
        bool[] seen = new bool[32];
        for (int i = 0; i < assets.Count; i++)
        {
            if (assets[i].TokenType != TokenType.DataDictionary || !V3Descriptor(assets[i].DataDictionary)) return false;
            int id = Integer(assets[i].DataDictionary,"assetId",-1); if (seen[id]) return false; seen[id] = true;
        }
        if (!seen[0] || Integer(d,"recommendedSets",-1) != 8 || Integer(d,"randomSets",-1) != 16 || Integer(d,"pageSize",-1) != 4) return false;
        double server = Number(d,"serverTime"), until = Number(d,"validUntil"), expires = Number(d,"expiresAtUnix");
        if (server <= 0 || expires < 0 || (expires != 0 && expires <= server)) return false;
        double clock = V3Now();
        bool localCopy = sharedDataSource != null && sharedDataSource.ready && sharedDataSource.v3BaseGeneration == generation && sharedDataSource.v3BaseBank == bank;
        if (until <= server || (!localCopy && until <= clock) || until-server > 300) return false;
        if (expires != 0 && expires <= clock) return false;
        if (clock > 0 && server > clock+120) return false;
        for (int i = 0; i < assets.Count; i++) V3RegisterAsset(bank,generation,assets[i].DataDictionary);
        v3BankGenerations[bank] = generation; v3BankExpiry[bank] = expires;
        V3AdvanceClock(server);
        return true;
    }

    private bool V3RefValid(DataDictionary reference, int bank, string generation)
    {
        if (reference == null) return true;
        int id = Integer(reference,"assetId",-1);
        if (bank < 0 || bank >= 32 || id < 0 || id > 31) return false;
        int key = bank * 32 + id;
        DataDictionary a = v3Assets[key], r = V3Object(reference,"rect");
        if (a == null || r == null || v3AssetGenerations[key] != generation) return false;
        int x = Integer(r,"x",-1), y = Integer(r,"y",-1), w = Integer(r,"width",-1), h = Integer(r,"height",-1);
        if (x < 0 || y < 0 || w < 1 || h < 1 || x+w > Integer(a,"width",0) || y+h > Integer(a,"height",0)) return false;
        DataDictionary m = V3Object(a,"marker");
        int mx = Integer(m,"x",0), my = Integer(m,"y",0), mw = Integer(m,"width",0), mh = Integer(m,"height",0);
        return x+w <= mx || x >= mx+mw || y+h <= my || y >= my+mh;
    }

    private bool V3WorldsValid(DataList list, int bank, string generation, int maximum)
    {
        if (list == null || list.Count > maximum) return false;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].TokenType != TokenType.DataDictionary) return false;
            DataDictionary w = list[i].DataDictionary;
            if (!ValidWorldId(Str(w,"id")) || string.IsNullOrEmpty(Str(w,"name")) || Str(w,"name").Length > 256 || !V3RefValid(V3Object(w,"image"),bank,generation)) return false;
            string portal = Str(w,"portalWorldId"); if (!string.IsNullOrEmpty(portal) && !ValidWorldId(portal)) return false;
            for (int j = 0; j < i; j++) if (Str(list[j].DataDictionary,"id") == Str(w,"id")) return false;
        }
        return true;
    }

    private void V3RememberInitial(DataList worlds)
    {
        if (worlds == null) return;
        for (int i = 0; i < worlds.Count && v3InitialWorldCount < 192; i++)
        {
            DataDictionary w = worlds[i].DataDictionary;
            DataDictionary r = V3Object(w,"image");
            if (r == null || Integer(r,"assetId",-1) != 0) continue;
            bool found = false;
            for (int j = 0; j < v3InitialWorldCount; j++) if (v3InitialWorldIds[j] == Str(w,"id")) found = true;
            if (found) continue;
            v3InitialWorldIds[v3InitialWorldCount] = Str(w,"id");
            v3InitialImages[v3InitialWorldCount++] = r;
        }
    }

    private bool V3ApplyBootstrap(string json)
    {
        int savedTab = tab, savedView = view, savedPage = page;
        DataDictionary d = V3Json(json,300000);
        if (d == null || !V3RegisterManifest(d)) return false;
        int bank = Integer(d,"bank",-1); string generation = Str(d,"generationId");
        if (!V3WorldsValid(V3List(d,"worlds"),bank,generation,64) || !V3WorldsValid(V3List(d,"latestInitial"),bank,generation,8) || !V3WorldsValid(V3List(d,"recommendedInitial"),bank,generation,8) || !V3WorldsValid(V3List(d,"randomInitial"),bank,generation,8)) return false;
        v4Layout = v4Protocol;
        v4TopReady = false;
        if (!Parse(json)) return false;
        v4TopReady = false;
        v4TopUsed = 0; v4BannerUsed = 0; v4LastTop = -1; v4LastBanner = -1;
        v4FirstTopCycle = true; v4FirstBannerCycle = true;
        v4PreviewImages = new DataDictionary[16];
        v3Bootstrap = json; v3BaseBank = bank; v3BaseGeneration = generation;
        v3InitialWorldCount = 0; v3LatestJson = ""; v3LatestComplete = false; v3LatestFailed = false;
        DataList worlds = V3List(d,"worlds");
        for (int i = 0; i < worlds.Count; i++) v3WorldImages[i] = V3Object(worlds[i].DataDictionary,"image");
        V3RememberInitial(worlds); V3RememberInitial(V3List(d,"latestInitial"));
        V3RememberInitial(V3List(d,"recommendedInitial")); V3RememberInitial(V3List(d,"randomInitial"));
        V3ApplyWorlds(2,V3List(d,"latestInitial"),bank,generation,0,false,V3InitialCount(d,"latestCount",V3List(d,"latestInitial")));
        DataList ads = V3List(d,"ads");
        for (int i = 0; ads != null && i < ads.Count; i++)
        {
            if (ads[i].TokenType != TokenType.DataDictionary) continue;
            DataDictionary a = ads[i].DataDictionary;
            for (int j = 0; j < adCount; j++) if (adIds[j] == Str(a,"id"))
            {
                v3BannerImages[j] = V3Object(a,"bannerImage"); v3QrImages[j] = V3Object(a,"qrImage");
                v3DetailImages[j] = V3Object(a,"detailImage"); v3TopImages[j] = V3Object(a,v4Layout ? "cardImage" : "topImage");
                v4PreviewImages[j] = V3Object(a,"previewImage");
                if (!V3RefValid(v4PreviewImages[j],bank,generation)) v4PreviewImages[j] = null;
                if (!V3RefValid(v3BannerImages[j],bank,generation)) v3BannerImages[j] = null;
                if (!V3RefValid(v3QrImages[j],bank,generation)) v3QrImages[j] = null;
                if (!V3RefValid(v3DetailImages[j],bank,generation)) v3DetailImages[j] = null;
                if (!V3RefValid(v3TopImages[j],bank,generation)) v3TopImages[j] = null;
            }
        }
        v3InitialTopIds = V3Strings(V3List(d,v4Layout ? "initialCardIds" : "initialTopIds"),v4Layout ? 6 : 4);
        v3InitialBannerIds = V3Strings(V3List(d,"initialBannerIds"),v4Layout ? 3 : 2);
        v3AdRotation = 0;
        for (int c = 0; c < 2; c++)
        {
            v3DrawComplete[c] = false; v3LocalDrawJson[c] = "";
            v3DrawGenerations[c] = ""; v3DrawBanks[c] = -1; v3DrawSets[c] = -1;
            if (V3SharedGeneration(c) == generation && V3SharedBank(c) == bank && V3SharedSet(c) == 0)
                V3ApplyWorlds(c,V3List(d,c == 0 ? "recommendedInitial" : "randomInitial"),bank,generation,0,false,V3InitialCount(d,c == 0 ? "recommendedCount" : "randomCount",V3List(d,c == 0 ? "recommendedInitial" : "randomInitial")));
        }
        imageFailed = false; imageLoading = V3Texture(bank,generation,0) == null;
        v3NextStatus = 0; sponsorStatusValid = false;
        if (savedView == 1 && !favoriteTabSelected)
        { tab = savedTab; view = savedView; BuildOrder(); page = Mathf.Clamp(savedPage,0,Mathf.Max(0,(PageItemCount()-1)/4)); }
        v3Version++; loading = false; failed = false; Render();
        return true;
    }

    private void V3ApplyWorlds(int category, DataList list, int bank, string generation, int set, bool complete, int total)
    {
        bool sameSelection = category == 2 ? complete && !v3LatestComplete : v3DrawGenerations[category] == generation && v3DrawBanks[category] == bank && v3DrawSets[category] == set;
        v3ListCounts[category] = complete ? list.Count : Mathf.Clamp(total,list.Count,64);
        int start = 64 + category * 64;
        for (int i = 0; i < 64; i++)
        {
            int index = start+i;
            ids[index] = ""; portalIds[index] = ""; titles[index] = ""; authors[index] = ""; descriptions[index] = "";
            pcSupport[index] = 0; androidSupport[index] = 0; iosSupport[index] = 0; pc[index] = false; android[index] = false; recommended[index] = false; sponsorOnly[index] = false; pictureIndices[index] = -1; v3WorldImages[index] = null;
            if (i >= list.Count) continue;
            DataDictionary w = list[i].DataDictionary;
            ids[index] = Str(w,"id"); titles[index] = Str(w,"name"); authors[index] = Str(w,"authorName"); descriptions[index] = Str(w,"description");
            ReadPlatforms(w, index); pc[index] = Flag(w,"pc"); android[index] = Flag(w,"android"); recommended[index] = Flag(w,"recommended");
            portalIds[index] = string.IsNullOrEmpty(Str(w,"portalWorldId")) ? ids[index] : Str(w,"portalWorldId");
            v3WorldImages[index] = V3Object(w,"image"); pictureIndices[index] = v3WorldImages[index] == null ? -1 : i;
        }
        if (category == 2)
        {
            v3LatestCount = list.Count; v3LatestComplete = complete; v3LatestFailed = false; cachedTabs[2] = false;
            if (tab == 2) { if (!sameSelection) page = 0; BuildOrder(); page = Mathf.Clamp(page,0,Mathf.Max(0,(PageItemCount()-1)/4)); }
            return;
        }
        v3DrawGenerations[category] = generation; v3DrawBanks[category] = bank; v3DrawSets[category] = set;
        v3DrawComplete[category] = complete; drawCounts[category] = list.Count; drawTotalCandidates[category] = total > 0 ? total : list.Count;
        drawHasResult[category] = true; drawErrors[category] = "";
        int targetTab = category == 0 ? 1 : 3;
        cachedTabs[targetTab] = false;
        if (tab == targetTab) { if (!sameSelection) page = 0; BuildOrder(); page = Mathf.Clamp(page,0,Mathf.Max(0,(PageItemCount()-1)/4)); }
    }

    private int V3CacheSlot(string token)
    {
        if (!V3Hex(token)) return -1;
        for (int i = 0; i < v3CacheTokens.Length; i++) if (v3CacheTokens[i] == token && v3CacheTextures[i] != null) return i;
        return -1;
    }

    private Texture2D V3Texture(int bank, string generation, int asset)
    {
        if (sharedDataSource != null && sharedDataSource != this) return sharedDataSource.V3Texture(bank,generation,asset);
        if (bank < 0 || bank >= 32 || asset < 0 || asset > 31) return null;
        int key = bank * 32 + asset;
        if (v3AssetGenerations[key] != generation || v3Assets[key] == null) return null;
        int slot = V3CacheSlot(Str(v3Assets[key],"imageToken"));
        if (slot < 0) return null;
        v3CacheUsed[slot] = Time.realtimeSinceStartup;
        return v3CacheTextures[slot];
    }

    private DataDictionary V3WorldRef(int world)
    {
        if (world < 0 || world >= count) return null;
        string generation = V3WorldGeneration(world);
        if (generation == v3BaseGeneration)
            for (int i = 0; i < v3InitialWorldCount; i++) if (v3InitialWorldIds[i] == ids[world]) return v3InitialImages[i];
        return v3WorldImages[world];
    }

    private Rect V3Uv(DataDictionary reference, Texture texture)
    {
        DataDictionary r = V3Object(reference,"rect");
        if (r == null || texture == null) return new Rect(0,0,1,1);
        float x = Integer(r,"x",0), y = Integer(r,"y",0), w = Integer(r,"width",1), h = Integer(r,"height",1);
        return new Rect((x+.5f)/texture.width,(texture.height-y-h+.5f)/texture.height,(w-1)/texture.width,(h-1)/texture.height);
    }

    private void V3Paint(RawImage image, DataDictionary reference, int bank, string generation, bool crop)
    {
        if (image == null) return;
        int asset = reference == null ? -1 : Integer(reference,"assetId",-1);
        Texture2D texture = V3Texture(bank,generation,asset);
        if (texture == null)
        {
            int state = reference == null ? 1 : 0;
            MetaReaWallGuide source = sharedDataSource != null ? sharedDataSource : this;
            if (bank >= 0 && bank < 32 && asset >= 0 && asset <= 31 && source.v3ImageAttempts[bank*32+asset] >= 4) state = 2;
            SetPlaceholder(image,state); return;
        }
        Rect uv = V3Uv(reference,texture);
        if (crop)
        {
            DataDictionary r = V3Object(reference,"rect");
            float aspect = (float)Integer(r,"width",1)/Integer(r,"height",1);
            float target = image.rectTransform.rect.width/Mathf.Max(1,image.rectTransform.rect.height);
            if (target > aspect) { float h = aspect/target; uv = new Rect(uv.x,uv.y+uv.height*(1-h)/2,uv.width,uv.height*h); }
            else { float w = target/aspect; uv = new Rect(uv.x+uv.width*(1-w)/2,uv.y,uv.width*w,uv.height); }
        }
        image.texture = texture; image.color = Color.white; image.uvRect = uv;
    }

    private void V3PaintWorld(RawImage image, int world)
    {
        if (world < 0 || world >= count) { SetPlaceholder(image,ready ? 1 : failed ? 2 : 0); return; }
        int category = world < 128 ? 0 : 1;
        V3Paint(image,V3WorldRef(world),V3WorldBank(world),V3WorldGeneration(world),true);
    }

    private bool V3ReferenceUses(DataDictionary reference, int bank, string generation, string token)
    {
        if (reference == null || bank < 0 || bank >= 32) return false;
        int id = Integer(reference,"assetId",-1);
        if (id < 0 || id > 31) return false;
        int key = bank*32+id;
        return v3Assets[key] != null && v3AssetGenerations[key] == generation && Str(v3Assets[key],"imageToken") == token;
    }

    private bool V3VisibleImage(RawImage image, Texture texture)
    { return image != null && image.gameObject.activeInHierarchy && image.texture == texture; }

    private bool V3VisibleImages(RawImage[] images, Texture texture)
    {
        if (images != null) for (int i = 0; i < images.Length; i++) if (V3VisibleImage(images[i],texture)) return true;
        return false;
    }

    private bool V3DisplayUses(string token)
    {
        if (!ready || !gameObject.activeInHierarchy) return false;
        // Keep the small initial sheet available for instant navigation.
        if (v3BaseBank >= 0)
        {
            DataDictionary a = v3Assets[v3BaseBank*32];
            if (a != null && Str(a,"imageToken") == token) return true;
        }
        MetaReaWallGuide source = sharedDataSource != null ? sharedDataSource : this;
        int slot = source.V3CacheSlot(token);
        if (slot < 0) return false;
        Texture texture = source.v3CacheTextures[slot];
        return V3VisibleImages(topPictures,texture) || V3VisibleImages(rowPictures,texture) ||
            V3VisibleImages(manyPictures,texture) || V3VisibleImages(sponsoredWorldPictures,texture) ||
            V3VisibleImage(detailPicture,texture) || V3VisibleImage(adPicture,texture) ||
            V3VisibleImage(bannerPicture,texture) || V3VisibleImage(sponsorQrPicture,texture);
    }

    private bool V3CachePinned(int slot)
    {
        // Let displays pull a newly committed image before another demand can
        // replace it. Active UI references remain pinned after this grace time.
        if (Time.realtimeSinceStartup-v3CacheUsed[slot] < 1f) return true;
        string token = v3CacheTokens[slot];
        if (V3DisplayUses(token)) return true;
        if (sharedDisplays != null) for (int i = 0; i < sharedDisplays.Length; i++)
            if (sharedDisplays[i] != null && sharedDisplays[i].V3DisplayUses(token)) return true;
        return false;
    }

    private int V3EvictionSlot()
    {
        int candidate = -1; float oldest = float.MaxValue;
        for (int i = 0; i < v3CacheTokens.Length; i++)
            if (v3CacheTextures[i] != null && !V3CachePinned(i) && v3CacheUsed[i] < oldest)
            { oldest = v3CacheUsed[i]; candidate = i; }
        return candidate;
    }

    private int V3FreeCacheSlot()
    {
        for (int i = 0; i < v3CacheTokens.Length; i++) if (v3CacheTextures[i] == null) return i;
        return V3EvictionSlot();
    }

    private void V3Evict(int slot)
    {
        if (slot < 0 || slot >= v3CacheTextures.Length) return;
        if (v3CacheDownloads[slot] != null) v3CacheDownloads[slot].Dispose();
        residentImageBytes = Mathf.Max(0,residentImageBytes-v3CacheSizes[slot]);
        v3CacheSizes[slot] = 0; v3CacheTokens[slot] = "";
        v3CacheTextures[slot] = null; v3CacheDownloads[slot] = null;
        imageEvictionCount++;
    }

    private bool V3EnsureImageRoom(int bytes, bool evict)
    {
        if (bytes <= 0 || bytes > v3MemoryLimit) return false;
        // At most 12 releases; never loop waiting for pinned textures to vanish.
        for (int n = 0; n <= v3CacheTextures.Length; n++)
        {
            bool vacant = false;
            for (int i = 0; i < v3CacheTextures.Length; i++) if (v3CacheTextures[i] == null) vacant = true;
            if (vacant && residentImageBytes <= v3MemoryLimit-bytes) return true;
            if (!evict) return false;
            int slot = V3EvictionSlot();
            if (slot < 0) return false;
            V3Evict(slot);
        }
        return false;
    }

    private bool V3WantImage(int bank, string generation, int id)
    {
        if (v3ImageKey >= 0 || imageRequestInFlight || v3Verifying || v3ReadbackPending || Time.realtimeSinceStartup < nextImageRequestAt || Time.realtimeSinceStartup < v3ServerWaitUntil) return false;
        if (!V3BankUsable(bank,generation) || id < 0 || id > 31) return false;
        int key = bank*32+id;
        DataDictionary a = v3Assets[key];
        if (a == null || v3AssetGenerations[key] != generation || V3CacheSlot(Str(a,"imageToken")) >= 0 || v3ImageAttempts[key] >= 4 || Time.realtimeSinceStartup < v3ImageRetryAt[key]) return false;
        if (v3ImageUrls == null || key >= v3ImageUrls.Length || (v3ImageUrls[key] == null || string.IsNullOrEmpty(v3ImageUrls[key].Get())) || V3FreeCacheSlot() < 0) return false;
        int reserved = Integer(a,"width",0)*Integer(a,"height",0)*4;
        if (!V3EnsureImageRoom(reserved,!v3PassiveImages)) return false;
        v3ReservedBytes = reserved;
        peakImageBytes = Mathf.Max(peakImageBytes,residentImageBytes+reserved);
        v3ImageKey = key; v3ImageToken = Str(a,"imageToken"); v3ImageGeneration = generation;
        v3ImageAttempts[key]++;
        if (imageDownloader == null) imageDownloader = new VRCImageDownloader();
        TextureInfo info = new TextureInfo(); info.GenerateMipMaps = false; info.FilterMode = FilterMode.Bilinear;
        info.WrapModeU = TextureWrapMode.Clamp; info.WrapModeV = TextureWrapMode.Clamp;
        MarkImageRequest();
        v3ImageDownload = imageDownloader.DownloadImage(v3ImageUrls[key],null,(IUdonEventReceiver)this,info);
        return true;
    }

    private bool V3WantRef(DataDictionary reference, int bank, string generation)
    { return reference != null && V3WantImage(bank,generation,Integer(reference,"assetId",-1)); }

    private bool V3WantWorld(int world)
    {
        if (world < 0 || world >= count) return false;
        int c = world < 128 ? 0 : 1;
        return V3WantRef(V3WorldRef(world),V3WorldBank(world),V3WorldGeneration(world));
    }

    private bool V3WantDisplay(MetaReaWallGuide display, bool details)
    {
        if (display == null || !display.ready || !display.gameObject.activeInHierarchy) return false;
        if (details)
        {
            if (display.view == 3 && display.openedAd >= 0 && display.openedAd < display.adCount)
            {
                int ad = display.openedAd;
                if (V3WantRef(display.v3DetailImages[ad],display.v3BaseBank,display.v3BaseGeneration)) return true;
                if (V3WantRef(display.v3QrImages[ad],display.v3BaseBank,display.v3BaseGeneration)) return true;
            }
            if (display.view == 2)
            {
                int w = display.selected;
                int c = w < 128 ? 0 : 1;
                return w >= 0 && w < display.count && V3WantRef(display.V3WorldRef(w),display.V3WorldBank(w),display.V3WorldGeneration(w));
            }
            return false;
        }
        if (display.view == 1)
            for (int i = 0; i < 4; i++)
            {
                int p = display.page*4+i;
                if (p >= display.orderCount) break;
                int w = display.order[p], c = w < 128 ? 0 : 1;
                if (V3WantRef(display.V3WorldRef(w),display.V3WorldBank(w),display.V3WorldGeneration(w))) return true;
            }
        if (display.view == 0 && display.manyVersion)
        {
            display.BuildManyWorlds();
            for (int i = 0; i < 4; i++)
            {
                int world = display.manyWorlds[i];
                if (display.TopWorldAvailable(world) && V3WantRef(display.V3WorldRef(world),display.V3WorldBank(world),display.V3WorldGeneration(world))) return true;
            }
        }
        return false;
    }

    private void V3PumpImages()
    {
        if (v3Maintenance || Time.realtimeSinceStartup < v3ServerWaitUntil) return;
        v3PassiveImages = false;
        if (v3ImageKey >= 0 || imageRequestInFlight || v3Verifying || Time.realtimeSinceStartup < nextImageRequestAt) return;
        if (!ready) { if (v4PendingBank >= 0) V3WantImage(v4PendingBank,v4PendingGeneration,0); return; }
        if (sharedDisplays != null)
            for (int i = 0; i < sharedDisplays.Length; i++) if (V3WantDisplay(sharedDisplays[i],true)) return;
        if (V3WantDisplay(this,true)) return;
        if (v4PendingBank >= 0 && V3WantImage(v4PendingBank,v4PendingGeneration,0)) return;
        if (V3WantImage(v3BaseBank,v3BaseGeneration,0)) return;
        if (!string.IsNullOrEmpty(v3PendingJson) && v3DrawCategory >= 0)
        {
            int asset = v3DrawCategory == 2 ? 1 : v3DrawCategory == 0 ? 2+v3TargetSet : 10+v3TargetSet;
            if (V3WantImage(v3TargetBank,v3TargetGeneration,asset)) return;
        }
        if (sharedDisplays != null)
            for (int i = 0; i < sharedDisplays.Length; i++) if (V3WantDisplay(sharedDisplays[i],false)) return;
        if (V3WantDisplay(this,false)) return;
        // Preserve bandwidth for visible images while any request is recovering.
        if (v3RecoveryPending || v3RequestAttempts > 0 || Time.realtimeSinceStartup < v3JsonBusyUntil) return;
        for (int i = 0; i < v3ImageAttempts.Length; i++)
            if (v3ImageAttempts[i] > 0 && Time.realtimeSinceStartup < v3ImageRetryAt[i]) return;
        if (Time.realtimeSinceStartup < v3NextPrefetchAt) return;
        v3NextPrefetchAt = Time.realtimeSinceStartup+5f;
        v3PassiveImages = true;
        // Prefetch does not evict active screens and follows list order.
        if (v3DrawComplete[0] && V3WantImage(v3DrawBanks[0],v3DrawGenerations[0],2+v3DrawSets[0])) return;
        if (V3WantImage(v3BaseBank,v3BaseGeneration,1)) return;
        if (v3DrawComplete[1] && V3WantImage(v3DrawBanks[1],v3DrawGenerations[1],10+v3DrawSets[1])) return;
        for (int ad = 0; ad < adCount; ad++)
        {
            if (!IsAdActive(ad)) continue;
            if (V3WantRef(v3BannerImages[ad],v3BaseBank,v3BaseGeneration)) return;
            if (V3WantRef(v3TopImages[ad],v3BaseBank,v3BaseGeneration)) return;
            if (v4Layout && !V4DetailReady(ad) && V3WantRef(v4PreviewImages[ad],v3BaseBank,v3BaseGeneration)) return;
        }
        if (V3WantImage(v3BaseBank,v3BaseGeneration,31)) return;
        if (!v4Layout) for (int ad = 0; ad < adCount; ad++)
            if (IsAdActive(ad) && V3WantRef(v3DetailImages[ad],v3BaseBank,v3BaseGeneration)) return;
    }

    private void V3ImageFailed()
    {
        int key = v3ImageKey;
        if (v3ImageDownload != null) v3ImageDownload.Dispose();
        v3ImageDownload = null; v3ImageTexture = null; v3Verifying = false;
        v3ImageKey = -1; imageRequestInFlight = false; v3ReservedBytes = 0;
        if (key >= 0)
        {
            v3ImageRetryAt[key] = Time.realtimeSinceStartup + V3Backoff(v3ImageAttempts[key]);
            if (v3ImageAttempts[key] >= 4) { v3ManualRetryAt = Mathf.Max(v3ManualRetryAt,Time.realtimeSinceStartup+60f); V3ArmRecovery(); }
            if (key == v3BaseBank*32 && v3ImageAttempts[key] >= 4) { imageFailed = true; imageLoading = false; }
        }
        v3Version++;
    }

    private void V3ImageSuccess(IVRCImageDownload result)
    {
        if (result != v3ImageDownload || v3ImageKey < 0) { result.Dispose(); return; }
        DataDictionary a = v3Assets[v3ImageKey];
        if (result.Result == null || a == null || result.Result.width != Integer(a,"width",0) || result.Result.height != Integer(a,"height",0) || Str(a,"imageToken") != v3ImageToken || v3AssetGenerations[v3ImageKey] != v3ImageGeneration || !V3BankUsable(v3ImageKey/32,v3ImageGeneration))
        { imageVerificationFailures++; V3ImageFailed(); return; }
        int actual = result.SizeInMemoryBytes;
        if (actual <= 0 || actual > v3ReservedBytes || residentImageBytes > v3MemoryLimit-actual)
        { V3ImageFailed(); return; }
        v3ReservedBytes = actual;
        DataDictionary m = V3Object(a,"marker");
        int width = Integer(m,"width",0);
        v3MarkerPixels = new Color32[width]; v3ImageTexture = result.Result; v3Verifying = true; v3ReadbackPending = true;
        int y = result.Result.height - Integer(m,"y",0) - Integer(m,"height",0)/2 - 1;
        VRCAsyncGPUReadback.Request(v3ImageTexture,0,Integer(m,"x",0),width,y,1,0,1,TextureFormat.RGBA32,(IUdonEventReceiver)this);
    }

    private void V3Readback(VRCAsyncGPUReadbackRequest request)
    {
        v3ReadbackPending = false;
        if (!v3Verifying || v3ImageKey < 0) return;
        if (request.hasError || !request.TryGetData(v3MarkerPixels)) { imageVerificationFailures++; V3ImageFailed(); return; }
        if (v3AssetGenerations[v3ImageKey] != v3ImageGeneration || !V3BankUsable(v3ImageKey/32,v3ImageGeneration)) { staleResponseCount++; V3ImageFailed(); return; }
        int cell = v3MarkerPixels.Length/64;
        for (int digit = 0; digit < 16; digit++)
        {
            int value = 0;
            for (int bit = 0; bit < 4; bit++)
            {
                Color32 p = v3MarkerPixels[(digit*4+bit)*cell+cell/2];
                if ((p.r > 64 && p.r < 192) || Mathf.Abs((int)p.r-p.g) > 24 || Mathf.Abs((int)p.r-p.b) > 24) { imageVerificationFailures++; V3ImageFailed(); return; }
                value = value*2+(p.r >= 192 ? 1 : 0);
            }
            if (value != "0123456789abcdef".IndexOf(v3ImageToken[digit])) { imageVerificationFailures++; V3ImageFailed(); return; }
        }
        int slot = V3FreeCacheSlot();
        if (slot < 0 || !V3EnsureImageRoom(v3ReservedBytes,false)) { V3ImageFailed(); return; }
        if (v3CacheTextures[slot] != null) V3Evict(slot);
        v3CacheSizes[slot] = v3ReservedBytes; residentImageBytes += v3ReservedBytes; v3ReservedBytes = 0;
        v3CacheTokens[slot] = v3ImageToken; v3CacheTextures[slot] = v3ImageTexture;
        v3CacheDownloads[slot] = v3ImageDownload; v3CacheUsed[slot] = Time.realtimeSinceStartup;
        // A later cache eviction starts a new transaction, not the fifth retry
        // of an image that already succeeded and passed verification.
        v3ImageAttempts[v3ImageKey] = 0; v3ImageRetryAt[v3ImageKey] = 0;
        v3ImageDownload = null; v3ImageTexture = null; v3ImageKey = -1;
        imageRequestInFlight = false; v3Verifying = false;
        imageLoading = V3Texture(v3BaseBank,v3BaseGeneration,0) == null; imageFailed = false;
        if (!imageLoading && v3AdRotation == 0) V3AdvanceSponsors();
        v3Version++; V4CommitBootstrap(); V3TryCommit();
    }

    private float V3Backoff(int attempts)
    { float delay = attempts <= 1 ? 5f : attempts == 2 ? 10f : 20f; return delay+Random.Range(0f,delay); }

    private void V3QueueRequest(int kind, VRCUrl url, int bank, string generation, int category, int set)
    {
        if (v3RequestKind != 0 || (url == null || string.IsNullOrEmpty(url.Get()))) return;
        v3RequestKind = kind; v3RequestUrl = url; v3RequestBank = bank; v3RequestGeneration = generation;
        v3RequestCategory = category; v3RequestSet = set; v3RequestAttempts = 0;
        v3RequestRetryAt = Time.realtimeSinceStartup; v3RequestQueuedAt = Time.realtimeSinceStartup;
        v3RequestOwnerTerm = v3OwnerTerm; v3RequestSelectionSequence = v3SelectionSequence;
        v3RequestBaseSelection = v3SharedBaseGeneration;
    }

    private void V3SendRequest()
    {
        if (v3RecoveryPending && Time.realtimeSinceStartup < v3RecoveryAt) return;
        if (v3RequestKind == 0 || v3RequestInFlight || Time.realtimeSinceStartup < v3RequestRetryAt || Time.realtimeSinceStartup < v3NextStringAt || Time.realtimeSinceStartup < v3ServerWaitUntil || Time.realtimeSinceStartup < v3JsonBusyUntil) return;
        if (Time.realtimeSinceStartup-v3RequestQueuedAt > 300f) { V3RequestFailed(true); return; }
        if (!V3RequestContextValid()) { V3CancelRequest(); V3FailDraw("共有結果が更新されました。新しい結果を取得します"); return; }
        if ((v3RequestKind == 3) && !V3BankUsable(v3RequestBank,v3RequestGeneration)) { V3RequestFailed(true); return; }
        if (!V3TrackStringRequest(v3RequestUrl.Get())) return;
        v3RequestInFlight = true; v3RequestStarted = Time.realtimeSinceStartup;
        v3NextStringAt = v3RequestStarted + 5.1f; v3RequestAttempts++;
        if (v3RequestKind == 4 || v3RequestKind == 5) { statusRequestCount++; sponsorStatusLoading = true; }
        else if (v3RequestKind == 3) drawRequestCount++;
        else feedRequestCount++;
        VRCStringDownloader.LoadUrl(v3RequestUrl,(IUdonEventReceiver)this);
    }

    private void V3RequestFailed(bool terminal)
    {
        int kind = v3RequestKind;
        V3AbandonStringRequest();
        v3RequestInFlight = false;
        if (!v3BusyResponse && kind == 5 && v4Protocol && !ready) v4ProbeFailures++;
        if (!v3BusyResponse && preferGuideV4 && v4Protocol && !ready && v4PendingBank < 0 && sharedGuideProtocol != 4 && (kind == 1 || kind == 5) && (terminal || v3RequestAttempts >= 4 || v4ProbeFailures >= 4))
        { V3CancelRequest(); V4SelectProtocol(false); v3BootstrapStopped = true; loading = true; failed = false; V3ArmRecovery(); v3NextStatus = v3RecoveryAt; return; }
        if (kind == 7 && (terminal || v3RequestAttempts >= 4))
        { v3RequestKind = 0; V3ArmRecovery(); v4PendingStatusAt = v3RecoveryAt; return; }
        if (kind == 5 && (terminal || v3RequestAttempts >= 4))
        {
            v3RequestKind = 0; v3RequestAttempts = 0;
            sponsorStatusLoading = false;
            V3ArmRecovery(); v3NextStatus = v3RecoveryAt;
            return;
        }
        if (kind == 4)
        {
            sponsorStatusValid = false; sponsorStatusLoading = false; sponsorStatusFailed = true;
            RefreshSponsorAvailability(); v3Version++;
        }
        if (!terminal && v3RequestAttempts < 4)
        { v3RequestRetryAt = Time.realtimeSinceStartup + V3Backoff(v3RequestAttempts); return; }
        v3RequestKind = 0;
        v3ManualRetryAt = Mathf.Max(v3ManualRetryAt,Time.realtimeSinceStartup+60f);
        if (kind == 1) { v3BootstrapStopped = true; loading = false; failed = !ready; V3ArmRecovery(); }
        else if (kind == 2 || kind == 3 || kind == 6) V3FailDraw("取得に失敗しました。再抽選ボタンで再試行できます");
        else if (kind == 4)
        {
            // A failed transaction has at most three retries. The normal status
            // poll is a separate, slow transaction and remains fail-closed.
            V3ArmRecovery(); v3NextStatus = v3RecoveryAt;
        }
        v3Version++;
    }

    private bool V3LoadStatus(DataDictionary d, string json)
    {
        if (Str(d,"schema") != (v4Protocol ? "metarea-guide-status-v4" : "metarea-guide-status-v3")) return false;
        double now = Number(d,"serverTime"), until = Number(d,"validUntil");
        DataList generations = V3List(d,"generations");
        DataDictionary current = V3Object(d,"current");
        if (now <= 0 || until <= now || until-now > 300 || generations == null || generations.Count > 32 || current == null) return false;
        double clock = V3Now();
        if (until <= clock || (clock > 0 && now > clock+120) || now < sponsorStatusUtc) return false;
        bool[] seen = new bool[32];
        for (int i = 0; i < generations.Count; i++)
        {
            if (generations[i].TokenType != TokenType.DataDictionary) return false;
            DataDictionary g = generations[i].DataDictionary;
            int bank = Integer(g,"bank",-1); double expiry = Number(g,"expiresAtUnix");
            if (bank < 0 || bank >= 32 || seen[bank] || !V3Hex(Str(g,"generationId")) || expiry < 0) return false;
            seen[bank] = true;
        }
        int latestBank = Integer(current,"bank",-1); string latestGeneration = Str(current,"generationId");
        if (latestBank < 0 || latestBank >= 32 || !V3Hex(latestGeneration)) return false;
        // Reuse the existing, strict contentKey/period validation. This data is
        // fetched directly by this client, never forwarded by the owner.
        DataDictionary legacy = d.ShallowClone();
        legacy.SetValue("schema",new DataToken("metarea-sponsor-status-v1"));
        legacy.SetValue("generatedAtUnix",new DataToken(now));
        DataToken serialized;
        if (!VRCJson.TrySerializeToJson(legacy,JsonExportType.Minify,out serialized) || !ApplySponsorStatus(serialized.String)) return false;
        V3AdvanceClock(now); v3StatusUntil = until;
        for (int b = 0; b < 32; b++) { v3BankExpiry[b] = now; }
        for (int i = 0; i < generations.Count; i++)
        {
            DataDictionary g = generations[i].DataDictionary;
            int bank = Integer(g,"bank",-1);
            v3BankGenerations[bank] = Str(g,"generationId"); v3BankExpiry[bank] = Number(g,"expiresAtUnix");
        }
        v3LatestBank = latestBank; v3LatestGeneration = latestGeneration;
        v3BlockedWorldIds = V3Strings(V3List(d,"blockedWorldIds"),4096);
        // A publication change can remove the first latest item. Invalidate
        // its cached order so the tab and four-card TOP advance together.
        if (ready)
        {
            cachedTabs[2] = false;
            if (tab == 2)
            { BuildOrder(); page = Mathf.Clamp(page,0,Mathf.Max(0,(PageItemCount()-1)/4)); }
        }
        sponsorStatusJson = json;
        v3NextStatus = Time.realtimeSinceStartup + 60f + Random.Range(0f,10f);
        if (v4Layout && V3Texture(v3BaseBank,v3BaseGeneration,0) != null) v4TopReady = true;
        v3Version++; return true;
    }

    private bool V3SetValid(DataDictionary d, int category, int bank, string generation, int set)
    {
        string kind = category == 2 ? "latest" : category == 0 ? "recommended" : "random";
        if (d == null || Str(d,"schema") != (v4Protocol ? "metarea-guide-set-v4" : "metarea-guide-set-v3") || Str(d,"kind") != kind || Integer(d,"layoutVersion",-1) != (v4Protocol ? 2 : 1) || Integer(d,"bank",-1) != bank || Str(d,"generationId") != generation || Integer(d,"setId",-1) != set) return false;
        DataDictionary a = V3Object(d,"asset");
        if (a == null || Integer(a,"assetId",-1) != (category == 2 ? 1 : (category == 0 ? 2 : 10)+set) || !V3RegisterAsset(bank,generation,a)) return false;
        DataList worlds = V3List(d,"worlds");
        if (!V3WorldsValid(worlds,bank,generation,64)) return false;
        for (int i = 0; i < worlds.Count; i++)
        {
            DataDictionary w = worlds[i].DataDictionary;
            if (Flag(w,"isSponsorOnly") || (category == 0 && !Flag(w,"recommended"))) return false;
            DataDictionary r = V3Object(w,"image");
            if (r != null && Integer(r,"assetId",-1) != Integer(a,"assetId",-1)) return false;
        }
        return true;
    }

    private void V3StringSuccess(IVRCStringDownload result)
    {
        if (!V3TakeStringResponse(result.Url.Get())) return;
        if (!V3RequestContextValid()) { staleResponseCount++; V3CancelRequest(); V3FailDraw("共有結果が更新されました。新しい結果を取得します"); return; }
        DataDictionary d = V3Json(result.Result,300000);
        if (d == null) { V3RequestFailed(false); return; }
        if (V3MaintenanceResponse(d) || V3WaitResponse(d)) return;
        int kind = v3RequestKind;
        bool accepted = false;
        if (kind == 1)
        {
            string generation = Str(d,"generationId"); int bank = Integer(d,"bank",-1);
            accepted = (string.IsNullOrEmpty(v3RequestGeneration) || (generation == v3RequestGeneration && bank == v3RequestBank)) && V3RegisterManifest(d);
            if (accepted)
            {
                if (V3Owner())
                {
                    sharedGuideProtocol = v4Protocol ? 4 : 3;
                    v3SharedBaseGeneration = generation; v3SharedBaseBank = bank;
                    for (int c = 0; c < 2; c++)
                    {
                        if (!string.IsNullOrEmpty(V3SharedGeneration(c)) && !V3SharedExpired(c)) continue;
                        if (c == 0) { v3SharedRecommendedGeneration = generation; v3SharedRecommendedBank = bank; v3SharedRecommendedSet = 0; v3SharedRecommendedUsed = 1; }
                        else { v3SharedRandomGeneration = generation; v3SharedRandomBank = bank; v3SharedRandomSet = 0; v3SharedRandomUsed = 1; }
                    }
                }
                accepted = v4Protocol ? V4StageBootstrap(d,result.Result) : V3ApplyBootstrap(result.Result);
                if (accepted) { v3RecoveryBootstrap = false; if (V3Owner()) RequestSerialization(); }
            }
        }
        else if (kind == 2)
        {
            accepted = V3RegisterManifest(d);
            if (accepted && v3DrawCategory >= 0 && V3Owner() && v3DrawOwnerTerm == v3OwnerTerm)
            {
                v3TargetBank = Integer(d,"bank",-1); v3TargetGeneration = Str(d,"generationId");
                v3LatestBank = v3TargetBank; v3LatestGeneration = v3TargetGeneration;
                v3TargetSet = V3ChooseSet(v3DrawCategory,v3TargetGeneration);
                v3AwaitCatalog = false;
            }
            else if (accepted) { V3FailDraw("管理役の変更により再抽選を中止しました"); }
        }
        else if (kind == 3)
        {
            accepted = v3DrawCategory == v3RequestCategory && v3TargetGeneration == v3RequestGeneration && v3TargetBank == v3RequestBank && v3TargetSet == v3RequestSet && V3SetValid(d,v3RequestCategory,v3RequestBank,v3RequestGeneration,v3RequestSet);
            if (accepted) v3PendingJson = result.Result;
        }
        else if (kind == 6)
        {
            // A valid different generation proves that this bank was recycled.
            // Record that fact but never apply its worlds as the requested set.
            bool registered = Integer(d,"bank",-1) == v3RequestBank && V3RegisterManifest(d);
            accepted = registered && Str(d,"generationId") == v3RequestGeneration;
        }
        else if (kind == 7) { accepted = V4StatusValid(d); if (accepted) { v4PendingStatus = result.Result; v4PendingStatusAt = Time.realtimeSinceStartup+30f; } }
        else if (kind == 4) accepted = V3LoadStatus(d,result.Result);
        else if (kind == 5) accepted = V3ResumeStatus(d,result.Result);
        if (!accepted) { V3RequestFailed(false); return; }
        v3RequestKind = 0; v3RequestInFlight = false; v3RequestAttempts = 0;
        v3Version++; V4CommitBootstrap(); V3TryCommit();
    }

    private int V3ChooseSet(int category, string generation)
    {
        int count = category == 0 ? 8 : 16;
        int mask = generation == V3SharedGeneration(category) ? (category == 0 ? v3SharedRecommendedUsed : v3SharedRandomUsed) : 0;
        int last = generation == V3SharedGeneration(category) ? V3SharedSet(category) : -1;
        if ((mask & ((1 << count)-1)) == (1 << count)-1) mask = 0;
        int available = 0;
        for (int i = 0; i < count; i++) if ((mask & (1 << i)) == 0 && i != last) available++;
        int pick = Random.Range(0,Mathf.Max(1,available));
        for (int i = 0; i < count; i++)
        {
            if ((mask & (1 << i)) != 0 || i == last) continue;
            if (pick-- == 0) return i;
        }
        return (last+1)%count;
    }

    private bool V3MaintenanceResponse(DataDictionary d)
    {
        if (d == null || Str(d,"schema") != "metarea-guide-maintenance-v1" || !Flag(d,"maintenance")) return false;
        v3Maintenance = true;
        v3RequestKind = 0; v3RequestInFlight = false; v3RequestAttempts = 0;
        v3NextStatus = Time.realtimeSinceStartup + Mathf.Clamp(Integer(d,"retryAfter",60),60,300) + Random.Range(0f,10f);
        // Preserve committed selections, current pages and every cached texture.
        // Only unfinished work is cancelled. The next status poll resumes it.
        if (v3DrawCategory >= 0 && v3DrawCategory < 2) drawErrors[v3DrawCategory] = "";
        v3DrawCategory = -1; v3PendingJson = ""; v3UserDraw = false; v3AwaitCatalog = false;
        if (V3Owner() && sharedDrawBusyTab >= 0)
        { sharedDrawBusyTab = -1; sharedDrawBusyEnd = 0; RequestSerialization(); }
        sponsorStatusLoading = false;
        loading = !ready; failed = false;
        v3Version++;
        return true;
    }

    private bool V3ResumeStatus(DataDictionary d, string json)
    {
        if (d == null || Str(d,"schema") != (v4Protocol ? "metarea-guide-status-v4" : "metarea-guide-status-v3")) return false;
        double now = Number(d,"serverTime"), until = Number(d,"validUntil");
        DataDictionary current = V3Object(d,"current");
        if (now <= 0 || until <= now || until-now > 300 || current == null || !V3Hex(Str(current,"generationId")) || Integer(current,"bank",-1) < 0 || Integer(current,"bank",-1) >= 32) return false;
        double clock = V3Now();
        if (until <= clock || (clock > 0 && now > clock+120)) return false;
        if (ready && !V3LoadStatus(d,json)) return false;
        if (!ready) V3AdvanceClock(now);
        bool resuming = v3Maintenance;
        v3Maintenance = false; sponsorStatusLoading = false;
        v3NextStatus = Time.realtimeSinceStartup + 60f + Random.Range(0f,10f);
        if (resuming)
        {
            v3BootstrapStopped = false; failed = false;
            v3LatestFailed = false;
            v3StoppedSelections[0] = ""; v3StoppedSelections[1] = "";
            for (int i = 0; i < v3ImageAttempts.Length; i++)
                if (v3ImageAttempts[i] >= 4) { v3ImageAttempts[i] = 0; v3ImageRetryAt[i] = 0; }
            imageFailed = false;
            v3NextStringAt = Time.realtimeSinceStartup + 5.1f + Random.Range(0f,5f);
            nextImageRequestAt = Mathf.Max(nextImageRequestAt,Time.realtimeSinceStartup+Random.Range(0f,5f));
            v3Version++;
        }
        return true;
    }

    private void V3BeginDraw(int category, bool user)
    {
        if (Time.realtimeSinceStartup < v3JsonBusyUntil || (v3RecoveryPending && Time.realtimeSinceStartup < v3RecoveryAt)) return;
        if (v3Maintenance || Time.realtimeSinceStartup < v3ServerWaitUntil || Time.realtimeSinceStartup < v3DrawRetryAt) return;
        if (!V3Owner() || !ready || category < 0 || category > 1 || v3DrawCategory >= 0 || V3SelectionPending(0) || V3SelectionPending(1) || SharedRemaining(sharedDrawBusyEnd) > 0 || SharedRemaining(sharedDrawCooldownEnd) > 0) return;
        v3DrawCategory = category; v3UserDraw = true; v3AwaitCatalog = true;
        v3DrawRetryAt = Time.realtimeSinceStartup+15f;
        v3DrawOwnerTerm = v3OwnerTerm; v3DrawSequence = v3SelectionSequence;
        v3PendingJson = ""; v3DrawDeadline = Time.realtimeSinceStartup + 300f;
        sharedDrawBusyTab = category == 0 ? 1 : 3; sharedDrawBusyEnd = TimeNow()+300;
        drawErrors[category] = ""; RequestSerialization(); v3Version++;
    }

    private void V3ReceiveSelection()
    {
        if (!ready && preferGuideV4 && (sharedGuideProtocol == 3 || sharedGuideProtocol == 4) && v4Protocol != (sharedGuideProtocol == 4))
        {
            V3CancelRequest(); v4PendingBank = -1; v4PendingBootstrap = ""; v4PendingStatus = "";
            V4SelectProtocol(sharedGuideProtocol == 4); v3BootstrapStopped = false; v3NextStatus = 0;
        }
        if (v3Maintenance) return;
        if (!ready) return;
        if (v3DrawCategory >= 0)
        {
            int active = v3DrawCategory;
            if (!v3UserDraw && active < 2 && (V3SharedGeneration(active) != v3TargetGeneration || V3SharedBank(active) != v3TargetBank || V3SharedSet(active) != v3TargetSet))
            {
                if (v3RequestKind == 3 || v3RequestKind == 6) V3CancelRequest();
                v3DrawCategory = -1; v3PendingJson = ""; v3AwaitCatalog = false;
            }
            else return;
        }
        // Match sheet loading order: pickup -> latest -> random.
        for (int step = 0; step < 3; step++)
        {
            int c = step == 0 ? 0 : step == 1 ? 2 : 1;
            if (c == 2)
            {
                if (!v3LatestComplete && !v3LatestFailed && V3BankUsable(v3BaseBank,v3BaseGeneration))
                {
                    v3DrawCategory = 2; v3UserDraw = false; v3AwaitCatalog = false;
                    v3TargetBank = v3BaseBank; v3TargetGeneration = v3BaseGeneration; v3TargetSet = 0;
                    v3PendingJson = ""; v3DrawDeadline = Time.realtimeSinceStartup+300f;
                    return;
                }
                continue;
            }
            string generation = V3SharedGeneration(c); int bank = V3SharedBank(c), set = V3SharedSet(c);
            if (!V3Hex(generation) || bank < 0 || bank >= 32 || set < 0 || set >= (c == 0 ? 8 : 16)) continue;
            if (v3DrawComplete[c] && v3DrawGenerations[c] == generation && v3DrawBanks[c] == bank && v3DrawSets[c] == set) continue;
            if (v3StoppedSelections[c] == V3SelectionKey(generation,set)) continue;
            v3DrawCategory = c; v3UserDraw = false; v3AwaitCatalog = false;
            v3TargetBank = bank; v3TargetGeneration = generation; v3TargetSet = set;
            v3PendingJson = ""; v3DrawDeadline = Time.realtimeSinceStartup+300f;
            v3DrawOwnerTerm = v3OwnerTerm; v3DrawSequence = v3SelectionSequence;
            v3NextStringAt = Mathf.Max(v3NextStringAt,Time.realtimeSinceStartup+Random.Range(.1f,1.5f));
            return;
        }
    }

    private void V3FailDraw(string message)
    {
        int c = v3DrawCategory;
        if (c < 0) return;
        if (v3RequestKind == 2 || v3RequestKind == 3 || v3RequestKind == 6) V3CancelRequest();
        v3DrawRetryAt = Mathf.Max(v3DrawRetryAt,Time.realtimeSinceStartup+30f);
        if (c < 2) drawErrors[c] = message;
        else v3LatestFailed = true;
        if (!v3UserDraw && c < 2) v3StoppedSelections[c] = V3SelectionKey(v3TargetGeneration,v3TargetSet);
        if (V3Owner() && (v3UserDraw || v3AwaitCatalog))
        { sharedDrawBusyTab = -1; sharedDrawBusyEnd = 0; RequestSerialization(); }
        V3FinishSharedDraw(c); V3ArmRecovery();
        v3DrawCategory = -1; v3PendingJson = ""; v3AwaitCatalog = false; v3UserDraw = false;
        v3NextMigrationAttempt = Time.realtimeSinceStartup+300f;
        v3Version++;
    }

    private void V3TryCommit()
    {
        if (v3Maintenance) return;
        if (v3DrawCategory < 0 || string.IsNullOrEmpty(v3PendingJson)) return;
        int c = v3DrawCategory;
        if (!V3BankUsable(v3TargetBank,v3TargetGeneration)) { V3FailDraw("この結果の保持期限が終了しました。再抽選してください"); return; }
        if (v3UserDraw && (!V3Owner() || v3DrawOwnerTerm != v3OwnerTerm || v3DrawSequence != v3SelectionSequence)) { V3FailDraw("再抽選の管理役が変更されました"); return; }
        if (!v3UserDraw && c < 2 && (V3SharedGeneration(c) != v3TargetGeneration || V3SharedBank(c) != v3TargetBank || V3SharedSet(c) != v3TargetSet)) { V3FailDraw("新しい共有結果を取得します"); return; }
        DataDictionary d = V3Json(v3PendingJson,100000);
        if (d == null) { V3FailDraw("結果を読み込めませんでした"); return; }
        DataList worlds = V3List(d,"worlds");
        bool bootstrapSet = !v3UserDraw && (c == 2 || !V3SelectionPending(c)) && v3TargetGeneration == v3BaseGeneration && v3TargetSet == 0;
        V3PublishSelection();
        int asset = c == 2 ? 1 : (c == 0 ? 2 : 10)+v3TargetSet;
        if (worlds.Count > 0 && !bootstrapSet && V3Texture(v3TargetBank,v3TargetGeneration,asset) == null)
        {
            if (v3ImageAttempts[v3TargetBank*32+asset] >= 4) V3FailDraw("画像取得に失敗しました。再試行できます");
            return;
        }
        if (V3DetailPinsCategory(c)) return;
        string json = v3PendingJson;
        V3ApplyWorlds(c,worlds,v3TargetBank,v3TargetGeneration,v3TargetSet,true,Integer(d,"totalCandidates",worlds.Count));
        if (c == 2) v3LatestJson = json;
        else { v3LocalDrawJson[c] = json; v3StoppedSelections[c] = ""; }
        V3FinishSharedDraw(c);
        v3DrawCategory = -1; v3PendingJson = ""; v3UserDraw = false; v3AwaitCatalog = false;
        v3Version++;
    }

    private bool V3DetailPinsCategory(int category)
    {
        return !isSharedDataSource && (view == 2 || (view == 3 && beforeAdView == 2)) && selected >= 64+category*64 && selected < 128+category*64;
    }

    private bool V3WorldAllowed(int world)
    {
        if (world < 0 || world >= count || string.IsNullOrEmpty(ids[world])) return false;
        for (int i = 0; i < v3BlockedWorldIds.Length; i++) if (v3BlockedWorldIds[i] == ids[world] || v3BlockedWorldIds[i] == portalIds[world]) return false;
        return true;
    }

    private void V3Initialize()
    {
        if (sharedDataSource != null) { V3Pull(); return; }
        if (!v3Initialized)
        {
            v3Initialized = true;
            if (preferGuideV4) V4SelectProtocol(sharedGuideProtocol != 3);
            v3NextStringAt = Mathf.Max(v3NextStringAt,Time.realtimeSinceStartup+Random.Range(.1f,.75f));
            nextImageRequestAt = Mathf.Max(nextImageRequestAt,Time.realtimeSinceStartup+Random.Range(.1f,.75f));
            v3NextPrefetchAt = Time.realtimeSinceStartup+15f+Random.Range(0f,10f);
        }
        loading = !ready; v3Version++;
    }

    private void V3Retry()
    {
        if (sharedDataSource != null) { sharedDataSource.V3Retry(); return; }
        if (v3Maintenance) return;
        if (Time.realtimeSinceStartup < v3NextStringAt || Time.realtimeSinceStartup < v3ManualRetryAt || Time.realtimeSinceStartup < v3ServerWaitUntil || Time.realtimeSinceStartup < v3JsonBusyUntil || (v3RecoveryPending && Time.realtimeSinceStartup < v3RecoveryAt) || v3RequestKind != 0) return;
        v3ManualRetryAt = Time.realtimeSinceStartup+60f;
        v3NextStringAt = Time.realtimeSinceStartup+Random.Range(.5f,5f);
        v3BootstrapStopped = false; v3NextStatus = 0; v3LatestFailed = false;
        v3RecoveryPending = false; v3RecoveryBootstrap = V3Owner() && !ready;
        for (int i = 0; i < v3ImageAttempts.Length; i++) if (v3ImageAttempts[i] >= 4) { v3ImageAttempts[i] = 0; v3ImageRetryAt[i] = 0; }
        v3StoppedSelections[0] = ""; v3StoppedSelections[1] = "";
        imageFailed = false; failed = false; v3Version++;
    }

    private bool V3NearExpiry(int bank, string generation)
    {
        if (bank < 0 || bank >= 32 || v3BankGenerations[bank] != generation) return true;
        double expiry = v3BankExpiry[bank];
        return expiry > 0 && V3Now() >= expiry-3600;
    }

    private void V3ScheduleMetadata()
    {
        if (v3RecoveryPending && Time.realtimeSinceStartup < v3RecoveryAt) return;
        if (v4PendingBank >= 0 && !v3Maintenance)
        {
            if (v3RequestKind != 0) { V3SendRequest(); return; }
            if (Time.realtimeSinceStartup >= v4PendingStatusAt)
            { V3QueueRequest(7,sponsorStatusUrl,-1,"",-1,0); V3SendRequest(); }
            return;
        }
        if (v3RequestKind != 0) { V3SendRequest(); return; }
        if (v3Maintenance)
        {
            if (Time.realtimeSinceStartup >= v3NextStatus)
            { V3QueueRequest(5,sponsorStatusUrl,-1,"",-1,0); V3SendRequest(); }
            return;
        }
        string expected = v3SharedBaseGeneration;
        if (expected != v3BootstrapAttemptGeneration)
        { v3BootstrapAttemptGeneration = expected; v3BootstrapStopped = false; }
        if ((!ready || v3BaseGeneration != expected || v3RecoveryBootstrap) && !v3BootstrapStopped)
        {
            if (string.IsNullOrEmpty(expected) || v3RecoveryBootstrap)
            {
                if (V3Owner()) V3QueueRequest(1,feedUrl,-1,"",-1,0);
                else if (Time.realtimeSinceStartup >= v3NextStatus) V3QueueRequest(5,sponsorStatusUrl,-1,"",-1,0);
            }
            else if (v3SharedBaseBank >= 0 && v3SharedBaseBank < 32 && v3ManifestUrls != null && v3ManifestUrls.Length == 32)
                V3QueueRequest(1,v3ManifestUrls[v3SharedBaseBank],v3SharedBaseBank,expected,-1,0);
            V3SendRequest(); return;
        }
        if (!ready) return;
        if (Time.realtimeSinceStartup >= v3NextStatus)
        { V3QueueRequest(4,sponsorStatusUrl,-1,"",-1,0); V3SendRequest(); return; }
        if (v3DrawCategory >= 0)
        {
            if (v3AwaitCatalog) V3QueueRequest(2,feedUrl,-1,"",v3DrawCategory,0);
            else if (!V3BankUsable(v3TargetBank,v3TargetGeneration)) V3QueueRequest(6,v3ManifestUrls[v3TargetBank],v3TargetBank,v3TargetGeneration,v3DrawCategory,v3TargetSet);
            else if (string.IsNullOrEmpty(v3PendingJson) && v3SetUrls != null && v3SetUrls.Length == 768)
            {
                if (v3DrawCategory == 2)
                { V3QueueRequest(3,v3LatestUrls[v3TargetBank],v3TargetBank,v3TargetGeneration,2,0); V3SendRequest(); return; }
                int setIndex = v3TargetBank*24+(v3DrawCategory == 0 ? 0 : 8)+v3TargetSet;
                if (setIndex >= 0 && setIndex < v3SetUrls.Length)
                    V3QueueRequest(3,v3SetUrls[setIndex],v3TargetBank,v3TargetGeneration,v3DrawCategory,v3TargetSet);
            }
            V3SendRequest(); return;
        }
        if (V3Owner() && Time.realtimeSinceStartup >= v3NextMigrationAttempt && v3LatestGeneration != "" && sponsorStatusValid)
        {
            if ((v4Layout || V3NearExpiry(v3BaseBank,v3BaseGeneration)) && v3BaseGeneration != v3LatestGeneration && !v3BootstrapStopped)
            {
                v3NextMigrationAttempt = Time.realtimeSinceStartup+300;
                V3QueueRequest(1,feedUrl,-1,"",-1,0); V3SendRequest(); return;
            }
            for (int c = 0; c < 2; c++)
                if (V3NearExpiry(V3SharedBank(c),V3SharedGeneration(c)) && V3SharedGeneration(c) != v3LatestGeneration)
                { V3BeginDraw(c,false); return; }
        }
    }

    private void V3Tick()
    {
        if (sharedDataSource == null && !v3Initialized) V3Initialize();
        if (sharedDataSource != null)
        {
            V3Pull();
            UpdateLoadingIndicators(); UpdateDrawUi(); RenderFavorites(); V4UpdateUi();
            QueueBoardReady();
            if (portalButton != null)
            {
                int wait = Mathf.CeilToInt(PortalWait());
                portalButton.interactable = ready && CanUseWorld(selected) && (!sponsoredDetail || IsSponsoredWorldActive(selected)) && wait == 0;
                adPortalButton.interactable = ready && wait == 0 && view == 3 && TopPortalAvailable();
                portalButtonLabel.text = wait > 0 ? "あと " + wait + " 秒" : "PORTAL\nこの世界へ";
                adPortalLabel.text = wait > 0 ? "あと " + wait + " 秒" : "PORTAL  この世界へ";
            }
            return;
        }
        if (v3SerializationPending && V3Owner() && Time.realtimeSinceStartup >= v3SerializationRetryAt)
        { v3SerializationRetryAt = Time.realtimeSinceStartup+5f; RequestSerialization(); }
        if (v3RequestInFlight && Time.realtimeSinceStartup-v3RequestStarted > 30f) { metadataTimeoutCount++; V3RequestFailed(false); }
        if (v3ImageKey >= 0 && Time.realtimeSinceStartup-imageRequestStarted > 60f) V3ImageFailed();
        if (v3DrawCategory >= 0 && Time.realtimeSinceStartup > v3DrawDeadline && (string.IsNullOrEmpty(v3PendingJson) || !V3DetailPinsCategory(v3DrawCategory))) V3FailDraw("取得がタイムアウトしました。自動で再試行します");
        if (v3UserDraw && (!V3Owner() || v3DrawOwnerTerm != v3OwnerTerm))
        {
            if (v3RequestKind == 2 || v3RequestKind == 3 || v3RequestKind == 6) V3CancelRequest();
            V3FailDraw("管理役が変わりました。再試行できます");
        }
        V3Recover();
        if (v4PendingBank >= 0) { V4CommitBootstrap(); V3PumpImages(); }
        if (ready)
        {
            if (sponsorStatusValid && V3Now() >= v3StatusUntil) { sponsorStatusValid = false; sponsorStatusFailed = true; v3Version++; }
            if (RefreshSponsorAvailability()) v3Version++;
            V3ReceiveSelection(); V3TryCommit();
            V3PumpImages();
            if (view != 3) UpdateSponsorRotation();
        }
        V3ScheduleMetadata();
        if (!v3Maintenance && Time.realtimeSinceStartup >= v3ServerWaitUntil && Time.realtimeSinceStartup >= v3JsonBusyUntil) { PumpAdClicks(); PumpStatistics(); }
        if (!isSharedDataSource) { Render(); UpdateLoadingIndicators(); UpdateDrawUi(); V4UpdateUi(); QueueBoardReady(); }
    }

    private void V3Pull()
    {
        MetaReaWallGuide source = sharedDataSource;
        if (source == null || !source.isSharedDataSource) return;
        v4Protocol = source.v4Protocol;
        bool maintenanceChanged = v3Maintenance != source.v3Maintenance;
        v3Maintenance = source.v3Maintenance; v3ServerWaitUntil = source.v3ServerWaitUntil;
        v3RecoveryPending = source.v3RecoveryPending; v3RecoveryAt = source.v3RecoveryAt;
        if (!source.ready) { loading = source.loading; failed = source.failed; Render(); return; }
        bool detailOpen = view == 2 || view == 3 || (view == 3 && beforeAdView == 2);
        if (v3Bootstrap != source.v3Bootstrap && !(ready && detailOpen))
        {
            v3SharedRecommendedGeneration = source.v3SharedRecommendedGeneration;
            v3SharedRandomGeneration = source.v3SharedRandomGeneration;
            v3SharedRecommendedBank = source.v3SharedRecommendedBank; v3SharedRandomBank = source.v3SharedRandomBank;
            v3SharedRecommendedSet = source.v3SharedRecommendedSet; v3SharedRandomSet = source.v3SharedRandomSet;
            if (!V3ApplyBootstrap(source.v3Bootstrap)) return;
        }
        bool changed = maintenanceChanged || v3DisplayVersion != source.v3Version;
        v3DisplayVersion = source.v3Version;
        loading = source.loading; failed = source.failed; imageLoading = source.imageLoading; imageFailed = source.imageFailed;
        v3LatestFailed = source.v3LatestFailed;
        // Definitions are shared but textures remain owned by the coordinator.
        v3Assets = source.v3Assets; v3AssetGenerations = source.v3AssetGenerations;
        v3BankGenerations = source.v3BankGenerations; v3BankExpiry = source.v3BankExpiry;
        v3ServerTime = source.v3ServerTime; v3ServerReceived = source.v3ServerReceived; v3StatusUntil = source.v3StatusUntil;
        v3BlockedWorldIds = source.v3BlockedWorldIds;
        if (appliedSponsorStatusVersion != source.sponsorStatusVersion)
        {
            DataDictionary status = V3Json(source.sponsorStatusJson,300000);
            if (status != null) V3LoadStatus(status,source.sponsorStatusJson);
            appliedSponsorStatusVersion = source.sponsorStatusVersion; changed = true;
        }
        sponsorStatusValid = source.sponsorStatusValid; sponsorStatusFailed = source.sponsorStatusFailed;
        if (v3BaseGeneration == source.v3BaseGeneration) v4TopReady = source.v4TopReady;
        sponsorStatusUtc = source.sponsorStatusUtc; sponsorStatusReceived = source.sponsorStatusReceived;
        if (RefreshSponsorAvailability()) changed = true;
        if (v3BaseGeneration == source.v3BaseGeneration)
        {
            if (currentAd != source.currentAd || sponsorLeft != source.sponsorLeft || sponsorRight != source.sponsorRight) changed = true;
            currentAd = source.currentAd; sponsorLeft = source.sponsorLeft; sponsorRight = source.sponsorRight;
        }
        drawCooldownUntil = Time.realtimeSinceStartup+source.SharedRemaining(source.sharedDrawCooldownEnd);
        for (int c = 0; c < 2; c++)
        {
            if (drawErrors[c] != source.drawErrors[c]) { drawErrors[c] = source.drawErrors[c]; changed = true; }
            string json = source.v3LocalDrawJson[c];
            if (string.IsNullOrEmpty(json) || v3LocalDrawJson[c] == json) continue;
            int start = 64+c*64;
            if ((view == 2 || (view == 3 && beforeAdView == 2)) && selected >= start && selected < start+64) continue;
            DataDictionary d = V3Json(json,100000);
            if (d == null) continue;
            V3ApplyWorlds(c,V3List(d,"worlds"),source.v3DrawBanks[c],source.v3DrawGenerations[c],source.v3DrawSets[c],true,Integer(d,"totalCandidates",0));
            v3LocalDrawJson[c] = json; changed = true;
        }
        if (v3BaseGeneration == source.v3BaseGeneration && v3LatestJson != source.v3LatestJson && !string.IsNullOrEmpty(source.v3LatestJson) && !(view == 2 && selected >= 192))
        {
            DataDictionary d = V3Json(source.v3LatestJson,100000);
            if (d != null) { V3ApplyWorlds(2,V3List(d,"worlds"),v3BaseBank,v3BaseGeneration,0,true,0); v3LatestJson = source.v3LatestJson; changed = true; }
        }
        if (changed) Render();
    }

    private int V3InitialTop(int n)
    {
        if (n >= 0 && n < v3InitialTopIds.Length)
            for (int i = 0; i < topCount; i++) if (topIds[i] == v3InitialTopIds[n] && IsTopActive(i)) return i;
        return -1;
    }

    private int V3InitialBanner(int n)
    {
        if (n >= 0 && n < v3InitialBannerIds.Length)
            for (int i = 0; i < adCount; i++) if (adIds[i] == v3InitialBannerIds[n] && IsAdActive(i) && adShowInBanner[i]) return i;
        return -1;
    }

    private bool V3RefLoaded(DataDictionary reference)
    { return reference == null || V3Texture(v3BaseBank,v3BaseGeneration,Integer(reference,"assetId",-1)) != null; }

    private bool V3TopLoaded(int top)
    {
        if (v4Layout) return top >= 0 && top < topCount && topAds[top] >= 0 && V4Loaded(v3TopImages[topAds[top]]) && V4DetailReady(topAds[top]);
        if (top < 0 || top >= topCount) return true;
        if (topAds[top] >= 0 && v3TopImages[topAds[top]] != null) return V3RefLoaded(v3TopImages[topAds[top]]);
        if (topTypes[top] == 0) return V3RefLoaded(V3WorldRef(topWorlds[top]));
        return topAds[top] < 0 || V3RefLoaded(v3BannerImages[topAds[top]]);
    }

    private void V3AdvanceSponsors()
    {
        if (v4Layout) { V4AdvanceSponsors(); return; }
        int banner = -1, left = -1, right = -1;
        if (v3AdRotation < 2)
        {
            banner = V3InitialBanner(v3AdRotation);
            left = V3InitialTop(v3AdRotation*2); right = V3InitialTop(v3AdRotation*2+1);
        }
        // If prefetched placements were removed, select only verified loaded
        // replacements; never replace a good ad with a loading placeholder.
        if (banner < 0)
            for (int n = 0; n < activeAdCount; n++)
            {
                if (adCursor >= activeAdCount) { ShuffleAds(); adCursor = 0; }
                int candidate = adOrder[adCursor++];
                if (IsAdActive(candidate) && V3RefLoaded(v3BannerImages[candidate])) { banner = candidate; break; }
            }
        if (left < 0)
            for (int n = 0; n < 16; n++) { int candidate = PickWeightedTop(-1); if (candidate < 0 || V3TopLoaded(candidate)) { left = candidate; break; } }
        if (right < 0)
            for (int n = 0; n < 16; n++) { int candidate = PickWeightedTop(left); if (candidate < 0 || V3TopLoaded(candidate)) { right = candidate; break; } }
        if (banner >= 0 && V3RefLoaded(v3BannerImages[banner])) currentAd = banner;
        else if (!IsAdActive(currentAd)) currentAd = -1;
        if (left >= 0 && V3TopLoaded(left)) sponsorLeft = left;
        else if (!IsTopActive(sponsorLeft)) sponsorLeft = -1;
        if (right >= 0 && right != sponsorLeft && V3TopLoaded(right)) sponsorRight = right;
        else if (!IsTopActive(sponsorRight) || sponsorRight == sponsorLeft) sponsorRight = -1;
        if (currentAd >= 0 || sponsorLeft >= 0 || sponsorRight >= 0) v3AdRotation++;
        sponsorRotationReady = false; nextSponsorRotation = Time.time+Mathf.Max(5f,sponsorRotationSeconds);
        v3Version++;
    }

    private void V3DrawUi()
    {
        if (isSharedDataSource || favoriteTabSelected || (tab != 1 && tab != 3) || reshuffleButton == null) return;
        MetaReaWallGuide source = sharedDataSource != null ? sharedDataSource : this;
        if (source.v3Maintenance)
        {
            reshuffleButton.interactable = false;
            if (reshuffleLabel == null) reshuffleLabel = reshuffleButton.GetComponentInChildren<Text>(true);
            if (reshuffleLabel != null)
            {
                if (v3NormalDrawFontSize == 0) v3NormalDrawFontSize = reshuffleLabel.fontSize;
                reshuffleLabel.fontSize = 16; reshuffleLabel.text = "メンテナンス中";
            }
            if (statusLabel != null) statusLabel.text = "復旧を自動で確認しています";
            return;
        }
        int c = tab == 1 ? 0 : 1;
        float retryAt = Mathf.Max(Mathf.Max(source.v3DrawRetryAt,source.v3ServerWaitUntil),source.v3JsonBusyUntil);
        if (source.v3RecoveryPending) retryAt = Mathf.Max(retryAt,source.v3RecoveryAt);
        int wait = Mathf.CeilToInt(Mathf.Max(source.SharedRemaining(source.sharedDrawCooldownEnd),retryAt-Time.realtimeSinceStartup));
        bool busy = source.SharedDrawBusy() || source.V3SelectionPending(0) || source.V3SelectionPending(1);
        reshuffleButton.interactable = ready && !busy && wait == 0;
        if (reshuffleLabel == null) reshuffleLabel = reshuffleButton.GetComponentInChildren<Text>(true);
        if (reshuffleLabel != null && v3NormalDrawFontSize > 0) reshuffleLabel.fontSize = v3NormalDrawFontSize;
        if (reshuffleLabel != null) reshuffleLabel.text = busy ? "抽選中…" : wait > 0 ? "あと " + wait + " 秒" : "再抽選";
        if (statusLabel != null)
        {
            if (source.v3RecoveryPending) statusLabel.text = "接続できません。自動再接続を待っています";
            else if (busy) statusLabel.text = "全員共通の候補を読み込んでいます。現在の結果はそのまま閲覧できます";
            else if (!string.IsNullOrEmpty(drawErrors[c])) statusLabel.text = drawErrors[c];
            else if (drawHasResult[c]) statusLabel.text = v3DrawComplete[c] ? "全 " + drawTotalCandidates[c] + " 件から " + drawCounts[c] + " 件を表示しています" : "最初のページを表示しています。続きの情報を取得中です";
        }
        if (orderCount == 0 && pageLabel != null) pageLabel.text = drawHasResult[c] ? "対象ワールドなし" : "読み込み中…";
    }

    private int V3WorldBank(int world)
    { return world < 64 || world >= 192 ? v3BaseBank : v3DrawBanks[world < 128 ? 0 : 1]; }
    private string V3WorldGeneration(int world)
    { return world < 64 || world >= 192 ? v3BaseGeneration : v3DrawGenerations[world < 128 ? 0 : 1]; }

    private void V4SelectProtocol(bool modern)
    {
        if (ready || v4Protocol == modern) return;
        if (v4LegacyFeed == null) { v4LegacyFeed = feedUrl; v4LegacyStatus = sponsorStatusUrl; }
        VRCUrl[] swap = v3ManifestUrls; v3ManifestUrls = v4ManifestUrls; v4ManifestUrls = swap;
        swap = v3LatestUrls; v3LatestUrls = v4LatestUrls; v4LatestUrls = swap;
        swap = v3SetUrls; v3SetUrls = v4SetUrls; v4SetUrls = swap;
        swap = v3ImageUrls; v3ImageUrls = v4ImageUrls; v4ImageUrls = swap;
        feedUrl = modern ? v4FeedUrl : v4LegacyFeed;
        sponsorStatusUrl = modern ? v4StatusUrl : v4LegacyStatus;
        v4Protocol = modern; v4ProbeFailures = 0;
    }

    private bool V4StageBootstrap(DataDictionary d, string json)
    {
        int bank = Integer(d,"bank",-1); string generation = Str(d,"generationId");
        if (!V3WorldsValid(V3List(d,"worlds"),bank,generation,64) ||
            !V3WorldsValid(V3List(d,"latestInitial"),bank,generation,8) ||
            !V3WorldsValid(V3List(d,"recommendedInitial"),bank,generation,8) ||
            !V3WorldsValid(V3List(d,"randomInitial"),bank,generation,8)) return false;
        DataList ads = V3List(d,"ads");
        if (ads == null || ads.Count > 16) return false;
        string[] refs = new string[] { "cardImage", "bannerImage", "previewImage", "detailImage", "qrImage" };
        for (int i = 0; i < ads.Count; i++)
        {
            if (ads[i].TokenType != TokenType.DataDictionary) return false;
            DataDictionary ad = ads[i].DataDictionary;
            if (string.IsNullOrEmpty(Str(ad,"id")) || Str(ad,"contentKey").Length != 64 || !ValidPeriod(ad)) return false;
            for (int j = 0; j < refs.Length; j++) if (!V3RefValid(V3Object(ad,refs[j]),bank,generation)) return false;
        }
        v4PendingBootstrap = json; v4PendingBank = bank; v4PendingGeneration = generation;
        v4PendingUntil = Number(d,"validUntil"); v4PendingStatus = ""; v4PendingStatusAt = 0;
        loading = !ready; failed = false; return true;
    }

    private bool V4StatusValid(DataDictionary d)
    {
        if (d == null || Str(d,"schema") != "metarea-guide-status-v4" || Integer(d,"layoutVersion",-1) != 2) return false;
        double now = Number(d,"serverTime"), until = Number(d,"validUntil"), clock = V3Now();
        if (now <= 0 || until <= now || until-now > 300 || until <= clock || now < sponsorStatusUtc || (clock > 0 && now > clock+120)) return false;
        DataList ads = V3List(d,"ads"), tops = V3List(d,"topPlacements"), worlds = V3List(d,"sponsoredWorlds"), generations = V3List(d,"generations");
        if (ads == null || tops == null || worlds == null || !ValidStatusList(ads) || !ValidStatusList(tops) || !ValidStatusList(worlds) || generations == null || generations.Count > 32) return false;
        DataDictionary current = V3Object(d,"current");
        if (current == null || !V3Hex(Str(current,"generationId")) || Integer(current,"bank",-1) < 0 || Integer(current,"bank",-1) >= 32) return false;
        bool found = false; bool[] seen = new bool[32];
        for (int i = 0; i < generations.Count; i++)
        {
            if (generations[i].TokenType != TokenType.DataDictionary) return false;
            DataDictionary g = generations[i].DataDictionary;
            int bank = Integer(g,"bank",-1); double expiry = Number(g,"expiresAtUnix");
            if (bank < 0 || bank >= 32 || seen[bank] || !V3Hex(Str(g,"generationId")) || expiry < 0) return false;
            seen[bank] = true;
            if (bank == v4PendingBank && Str(g,"generationId") == v4PendingGeneration && (expiry == 0 || expiry > clock)) found = true;
        }
        return found;
    }

    private void V4CommitBootstrap()
    {
        if (v4PendingBank < 0 || v3Maintenance) return;
        if ((!string.IsNullOrEmpty(v3SharedBaseGeneration) && (v3SharedBaseGeneration != v4PendingGeneration || v3SharedBaseBank != v4PendingBank)) || V3Now() >= v4PendingUntil)
        {
            v4PendingBank = -1; v4PendingBootstrap = ""; v4PendingStatus = "";
            v3BootstrapStopped = false; v3RecoveryBootstrap = true; return;
        }
        if (!V3BankUsable(v4PendingBank,v4PendingGeneration)) return;
        if (V3Texture(v4PendingBank,v4PendingGeneration,0) == null) return;
        DataDictionary status = V3Json(v4PendingStatus,300000);
        if (!V4StatusValid(status)) { v4PendingStatus = ""; return; }
        if (!isSharedDataSource && ready && (view == 2 || view == 3)) return;
        string json = v4PendingBootstrap, statusJson = v4PendingStatus;
        if (!V3ApplyBootstrap(json)) return;
        if (!V3LoadStatus(status,statusJson)) return;
        v4PendingBank = -1; v4PendingBootstrap = ""; v4PendingStatus = "";
        v4TopReady = true; v3RecoveryBootstrap = false;
        RefreshSponsorAvailability(); if (v3AdRotation == 0) V4AdvanceSponsors(); v3Version++;
        if (V3Owner()) RequestSerialization();
    }

    private bool V4Loaded(DataDictionary reference)
    { return reference != null && V3Texture(v3BaseBank,v3BaseGeneration,Integer(reference,"assetId",-1)) != null; }

    private bool V4DetailReady(int ad)
    { return ad >= 0 && ad < adCount && (V4Loaded(v4PreviewImages[ad]) || V4Loaded(v3DetailImages[ad])); }

    private bool V4InitialMember(bool top, int candidate)
    {
        string id = top ? topIds[candidate] : adIds[candidate];
        string[] selectedIds = top ? v3InitialTopIds : v3InitialBannerIds;
        for (int i = 0; i < selectedIds.Length; i++) if (selectedIds[i] == id) return true;
        return false;
    }

    private bool V4Eligible(bool top, int candidate)
    {
        if (top) return IsTopActive(candidate) && V3TopLoaded(candidate);
        return IsAdActive(candidate) && adShowInBanner[candidate] && V4Loaded(v3BannerImages[candidate]) && V4DetailReady(candidate);
    }

    private int V4Pick(bool top, int exclude)
    {
        int total = top ? topCount : adCount;
        int used = top ? v4TopUsed : v4BannerUsed;
        int last = top ? v4LastTop : v4LastBanner;
        bool first = top ? v4FirstTopCycle : v4FirstBannerCycle;
        for (int pass = 0; pass < 2; pass++)
        {
            int available = 0;
            for (int i = 0; i < total; i++)
                if (i != exclude && (used & (1 << i)) == 0 && (!first || V4InitialMember(top,i)) && V4Eligible(top,i)) available++;
            if (available > 0)
            {
                // Avoid immediate repeats when another unused candidate exists.
                bool skipLast = available > 1;
                int count = 0;
                for (int i = 0; i < total; i++)
                    if (i != exclude && (!skipLast || i != last) && (used & (1 << i)) == 0 && (!first || V4InitialMember(top,i)) && V4Eligible(top,i)) count++;
                int pick = Random.Range(0,count);
                for (int i = 0; i < total; i++)
                {
                    if (i == exclude || (skipLast && i == last) || (used & (1 << i)) != 0 || (first && !V4InitialMember(top,i)) || !V4Eligible(top,i)) continue;
                    if (pick-- != 0) continue;
                    if (top) { v4TopUsed = used | (1 << i); v4LastTop = i; v4FirstTopCycle = first; }
                    else { v4BannerUsed = used | (1 << i); v4LastBanner = i; v4FirstBannerCycle = first; }
                    return i;
                }
            }
            used = 0; first = false;
        }
        return -1;
    }

    private void V4AdvanceSponsors()
    {
        if (!StatusUsable()) { currentAd = -1; sponsorLeft = -1; sponsorRight = -1; return; }
        if (!v4TopReady) return;
        int banner = V4Pick(false,-1), left = V4Pick(true,-1), right = V4Pick(true,left);
        currentAd = banner; sponsorLeft = left; sponsorRight = right;
        v3AdRotation++; sponsorRotationReady = false;
        nextSponsorRotation = Time.time+Mathf.Max(5f,sponsorRotationSeconds); v3Version++;
    }

    private string V4DownloadLabel(DataDictionary reference, string prefix)
    {
        if (reference == null) return prefix+"を取得できません";
        if (V4Loaded(reference)) return "";
        MetaReaWallGuide source = sharedDataSource != null ? sharedDataSource : this;
        int id = Integer(reference,"assetId",-1), key = v3BaseBank*32+id;
        if (id < 0 || key < 0 || key >= source.v3ImageAttempts.Length) return prefix+"を取得できません";
        string dots = (int)(Time.unscaledTime*2f)%3 == 0 ? "." : (int)(Time.unscaledTime*2f)%3 == 1 ? ".." : "...";
        if (source.v3ImageKey == key && source.v3ImageGeneration == v3BaseGeneration) return prefix+"DL中"+dots;
        if (source.v3ImageAttempts[key] >= 4 || (source.v3ImageAttempts[key] > 0 && Time.realtimeSinceStartup < source.v3ImageRetryAt[key])) return prefix+"を再取得予定";
        return prefix+"DL待機中"+dots;
    }

    private void V4UpdateUi()
    {
        bool modern = v4Protocol && onlineMode && guideV3;
        bool gated = modern && !v4TopReady;
        if (initialTopGroup != null)
        { initialTopGroup.alpha = gated ? 0 : 1; initialTopGroup.interactable = !gated; initialTopGroup.blocksRaycasts = !gated; }
        if (initialLoadingPanel != null) initialLoadingPanel.SetActive(gated && view == 0);
        if (initialLoadingLabel != null && gated) initialLoadingLabel.text = failed || imageFailed ? "取得できませんでした。自動で再試行します…" : "TOPを読み込んでいます…";
        bool detail = modern && v4Layout && view == 3 && IsAdActive(openedAd);
        if (sponsorQrPicture != null) sponsorQrPicture.enabled = !detail || V4Loaded(v3QrImages[openedAd]);
        if (adDownloadStatus != null)
        {
            adDownloadStatus.gameObject.SetActive(detail);
            adDownloadStatus.text = detail ? V4DownloadLabel(v3DetailImages[openedAd],"高画質版") : "";
        }
        if (adQrStatus != null)
        {
            adQrStatus.gameObject.SetActive(detail && !V4Loaded(v3QrImages[openedAd]));
            adQrStatus.text = detail ? V4DownloadLabel(v3QrImages[openedAd],"QR").Replace("DL","読込") : "";
        }
        if (v4Layout && currentAd < 0 && activeAdCount > 0) SetPlaceholder(bannerPicture,0);
    }

}
