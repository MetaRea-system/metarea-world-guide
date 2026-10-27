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
    [Min(1f)] public float portalCooldownSeconds = 5f;
    public Transform portalAnchor;
    public VRCPortalMarker portalMarker;
    public GameObject editorPortalPreview;
    public Text portalDestinationLabel;

    [Header("外部データ配信")]
    public bool onlineMode;
    public VRCUrl feedUrl = VRCUrl.Empty;
    // A separate instance is required: sharing VRCUrl.Empty aliases the Udon heap slots.
    public VRCUrl sponsorStatusUrl = new VRCUrl("https://metarea.net/api/udon/status");
    [Tooltip("Editorで生成する不変画像URL。番号は再利用しません。")]
    public VRCUrl[] atlasUrls;
    public VRCUrl[] sponsorAtlasUrls;
    public VRCUrl[] sponsorExtraAtlasUrls;
    public VRCUrl[] sponsorDetailAtlasUrls;
    [Header("サイト全件からの抽選")]
    public VRCUrl recommendedDrawUrl = new VRCUrl("https://metarea.net/api/udon/draw/recommended");
    public VRCUrl randomDrawUrl = new VRCUrl("https://metarea.net/api/udon/draw/random");
    public VRCUrl[] drawAtlasUrls;
    [Header("公式X")]
    public string xProfileUrl = "https://x.com/metametarea";
    public GameObject xUrlPanel;
    public InputField xUrlField;
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

    public Texture[] samplePictures;
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
    [HideInInspector] public int drawRequestCount;
    [HideInInspector] public int statusRequestCount;
    [HideInInspector] public int imageRequestCount;
    [HideInInspector] public RawImage xIconImage;
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
    private int[] adBannerPages;
    private int[] adDetailCells;
    private int extraSponsorPages;
    private int detailSponsorPages;
    // Slot 0: extra banner page; slots 1..3: detail pages.
    private Texture2D[] extraSponsorTextures = new Texture2D[4];
    private IVRCImageDownload[] extraSponsorDownloads = new IVRCImageDownload[4];
    private bool[] extraSponsorLoading = new bool[4];
    private bool[] extraSponsorFailed = new bool[4];
    private float[] extraSponsorAttempts = new float[] { -999f, -999f, -999f, -999f };
    private int adCount;
    private int activeAdCount;
    private string[] adIds;
    private string[] adKeys;
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

    private void Start()
    {
        if (isSharedDataSource) { SendCustomEventDelayedSeconds(nameof(_Initialize), 1f); return; }
        if (archiveUrlPanel != null) archiveUrlPanel.SetActive(false);
        if (reshuffleButton != null) reshuffleLabel = reshuffleButton.GetComponentInChildren<Text>(true);
        if (xUrlPanel != null) xUrlPanel.SetActive(false);
        if (favoritesEnabled && favoritesSettings != null)
        {
            favoriteWorldIds = favoritesSettings.favoriteWorldIds;
            favoriteWorldTitles = favoritesSettings.favoriteWorldTitles;
            favoriteWorldAuthors = favoritesSettings.favoriteWorldAuthors;
        }
        BuildFavorites();
        _ApplyAppearance();
        if (portalMarker != null) portalMarker.gameObject.SetActive(false);
        if (editorPortalPreview != null) editorPortalPreview.SetActive(false);
        Render();
        SendCustomEventDelayedSeconds(nameof(_Initialize), 1f);
    }

    public void _Initialize()
    {
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
            if (!ApplySponsorStatus(result.Result)) FailSponsorStatus();
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
        else failed = true;
        Render();
    }

    public override void OnStringLoadError(IVRCStringDownload result)
    {
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
        if (sharedDataSource != null) { sharedDataSource._Retry(); return; }
        if (isSharedDataSource) { attemptedSharedDrawJson[0] = ""; attemptedSharedDrawJson[1] = ""; }
        if (!string.IsNullOrEmpty(pinnedSnapshot))
        {
            ApplySnapshot(); imageFailed = false; sponsorImageFailed = false;
            for (int i = 0; i < 4; i++) extraSponsorFailed[i] = false;
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
        if (sharedDataSource == null) ApplySnapshot();
        if (isSharedDataSource) ReceiveSharedDraw();
        else ApplyPortal();
    }

    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        if (sharedDataSource != null) return;
        if (isSharedDataSource && player.isLocal)
        {
            sharedDrawBusyTab = -1; sharedDrawBusyEnd = 0;
            RequestSerialization();
        }
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
        if (string.IsNullOrEmpty(json) || json.Length > 60000) return false;
        DataToken root;
        if (!VRCJson.TryDeserializeFromJson(json, out root) || root.TokenType != TokenType.DataDictionary) return false;
        DataDictionary d = root.DataDictionary;
        if (Str(d, "schema") != "metarea-wall-v1") return false;
        int incomingSnapshot = Integer(d, "snapshotIndex", -1);
        if (onlineMode && (incomingSnapshot < 0 || atlasUrls == null || incomingSnapshot >= atlasUrls.Length)) return false;
        DataToken wt;
        if (!d.TryGetValue("worlds", TokenType.DataList, out wt)) return false;
        DataList list = wt.DataList;
        int n = list.Count;
        if (n < 1 || n > 64) return false;
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
        baseWorldCount = n; count = 192;
        ids = new string[count]; titles = new string[count]; authors = new string[count]; descriptions = new string[count];
        recommended = new bool[count]; pc = new bool[count]; android = new bool[count]; pictureIndices = new int[count];
        portalIds = new string[count]; sponsorOnly = new bool[count];
        for (int i = 0; i < count; i++) pictureIndices[i] = -1;
        for (int i = 0; i < n; i++)
        {
            DataDictionary w = list[i].DataDictionary;
            ids[i] = Str(w, "id"); titles[i] = Str(w, "name"); descriptions[i] = Str(w, "description");
            authors[i] = Str(w, "authorName");
            recommended[i] = Flag(w, "recommended"); pc[i] = Flag(w, "pc"); android[i] = Flag(w, "android");
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
        if (onlineMode) { BeginSponsorStatusDownload(); PumpImageDownloads(); }
        return true;
    }

    private void ReadSponsors(DataDictionary d)
    {
        adCount = 0; activeAdCount = 0; currentAd = -1; openedAd = -1; sponsoredCount = 0; sponsoredDetail = false;
        placementCount = 0; sponsorLeft = -1; sponsorRight = -1;
        sponsorStatusProtocol = Integer(d, "sponsorStatusVersion", 0) == 1;
        sponsorStatusValid = false; sponsorStatusLoading = false; sponsorStatusFailed = false;
        sponsorStatusStarted = -999f; nextSponsorStatusAttempt = 0;
        bannerAtlasV2 = Str(d, "sponsorAtlasLayout") == "banner-v2";
        extraSponsorPages = Mathf.Clamp(Integer(d, "sponsorBannerPages", 1) - 1, 0, 1);
        detailSponsorPages = Str(d, "sponsorDetailAtlasLayout") == "detail-v1" ? Mathf.Clamp(Integer(d, "sponsorDetailPages", 0), 0, 3) : 0;
        DataToken token;
        if (d.TryGetValue("ads", TokenType.DataList, out token))
        {
            DataList list = token.DataList;
            int capacity = Mathf.Min(10, list.Count);
            adNames = new string[capacity]; adDescriptions = new string[capacity]; adSponsors = new string[capacity]; adUrls = new string[capacity];
            adCells = new int[capacity]; adQrCells = new int[capacity]; adWorlds = new int[capacity];
            adBannerRows = new int[capacity];
            adBannerPages = new int[capacity]; adDetailCells = new int[capacity];
            adIds = new string[capacity]; adKeys = new string[capacity];
            adStarts = new double[capacity]; adEnds = new double[capacity];
            adAllowed = new bool[capacity]; adActive = new bool[capacity];
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
                if (bannerPage < 0 || bannerPage > extraSponsorPages || detailCell >= detailSponsorPages * 4 || detailCell > 9) continue;
                if (bannerAtlasV2 && (row < 0 || row > 7)) continue;
                adNames[adCount] = Str(a, "title"); adDescriptions[adCount] = Str(a, "description");
                adSponsors[adCount] = Str(a, "sponsor"); adUrls[adCount] = url;
                adCells[adCount] = cell; adQrCells[adCount] = qr; adWorlds[adCount] = FindId(Str(a, "worldId"));
                adBannerRows[adCount] = row;
                adBannerPages[adCount] = bannerPage; adDetailCells[adCount] = detailCell;
                adIds[adCount] = Str(a, "id"); adKeys[adCount] = Str(a, "contentKey");
                adStarts[adCount] = Number(a, "startsAtUnix"); adEnds[adCount] = Number(a, "endsAtUnix");
                adCount++;
            }
        }
        sponsoredWorlds = new int[10]; sponsorOrder = new int[10];
        placementIds = new string[10]; placementKeys = new string[10]; placementWorlds = new int[10];
        placementStarts = new double[10]; placementEnds = new double[10]; placementAllowed = new bool[10]; placementActive = new bool[10];
        if (sponsorStatusProtocol && d.TryGetValue("sponsoredWorlds", TokenType.DataList, out token))
        {
            DataList list = token.DataList;
            for (int i = 0; i < list.Count && placementCount < 10 - adCount; i++)
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
        topIds = new string[10]; topKeys = new string[10]; topAdTitles = new string[10];
        topTypes = new int[10]; topWorlds = new int[10]; topAds = new int[10];
        topStarts = new double[10]; topEnds = new double[10]; topAllowed = new bool[10]; topActive = new bool[10];
        DataToken token;
        topPlacementProtocol = d.TryGetValue("topPlacements", out token);
        if (!topPlacementProtocol || token.TokenType != TokenType.DataList || !sponsorStatusProtocol) return;
        DataList list = token.DataList;
        for (int i = 0; i < list.Count && topCount < 10; i++)
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
            if ((kind != 1 && world < 0) || (kind != 0 && ad < 0)) continue;
            topIds[topCount] = id; topKeys[topCount] = Str(p, "contentKey"); topTypes[topCount] = kind;
            topWorlds[topCount] = world; topAds[topCount] = ad;
            topAdTitles[topCount] = Str(p, "title");
            if (string.IsNullOrEmpty(topAdTitles[topCount])) topAdTitles[topCount] = kind == 0 ? titles[world] : adNames[ad];
            topStarts[topCount] = Number(p, "startsAtUnix"); topEnds[topCount] = Number(p, "endsAtUnix");
            topCount++;
        }
    }

    private bool IsTopActive(int index)
    {
        if (index < 0 || index >= topCount || !StatusUsable() || !topAllowed[index] || !InPeriod(topStarts[index], topEnds[index])) return false;
        return topTypes[index] == 0 || IsAdActive(topAds[index]);
    }

    private bool TopPortalAvailable()
    { return topPlacementProtocol && IsTopActive(openedTop) && topTypes[openedTop] == 2 && topAds[openedTop] == openedAd && CanUseWorld(topWorlds[openedTop]); }

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
    { return sponsorStatusValid && Time.realtimeSinceStartup - sponsorStatusReceived < 300f; }

    private bool InPeriod(double start, double end)
    {
        double now = sponsorStatusUtc + Time.realtimeSinceStartup - sponsorStatusReceived;
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
        return false;
    }

    private bool CanUseWorld(int world)
    { return world >= 0 && world < count && !string.IsNullOrEmpty(ids[world]) && (!sponsorOnly[world] || IsSponsoredWorldActive(world)); }

    private void BeginSponsorStatusDownload()
    {
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
        if (list.Count > 10) return false;
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
        if (!ready || !sponsorStatusProtocol || string.IsNullOrEmpty(json) || json.Length > 60000) return false;
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
            for (int j = 0; j < ads.Count; j++)
            {
                DataDictionary item = ads[j].DataDictionary;
                if (Str(item, "id") != adIds[i] || Str(item, "contentKey") != adKeys[i]) continue;
                adAllowed[i] = true; adStarts[i] = Number(item, "startsAtUnix"); adEnds[i] = Number(item, "endsAtUnix");
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
            for (int j = 0; j < tops.Count; j++)
            {
                DataDictionary item = tops[j].DataDictionary;
                if (Str(item, "id") != topIds[i] || Str(item, "contentKey") != topKeys[i]) continue;
                topAllowed[i] = true; topStarts[i] = Number(item, "startsAtUnix"); topEnds[i] = Number(item, "endsAtUnix");
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
            for (int i = 0; i < adCount; i++) if (adActive[i]) adOrder[activeAdCount++] = i;
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
        if (activeAdCount > 0) { if (adCursor >= activeAdCount) { ShuffleAds(); adCursor = 0; } currentAd = adOrder[adCursor++]; }
        else currentAd = -1;
        if (sponsoredCount > 0)
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
        nextSponsorRotation = Time.time + Mathf.Max(5f, sponsorRotationSeconds);
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
            if (topTypes[world] == 0) { RequestPortal(topWorlds[world]); return; }
            CancelSponsorFade(); beforeAdView = 0; openedTop = world; openedAd = topAds[world]; view = 3; PumpImageDownloads(); Render(); return;
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
        int index = snapshotIndex * 3 + slot - 1;
        return sponsorDetailAtlasUrls != null && index < sponsorDetailAtlasUrls.Length ? sponsorDetailAtlasUrls[index] : null;
    }

    private int ExtraSponsorSlot(string url)
    {
        for (int slot = 0; slot < 4; slot++)
        { var candidate = ExtraSponsorUrl(slot); if (candidate != null && candidate.Get() == url) return slot; }
        return -1;
    }

    private void BeginExtraSponsorImages()
    {
        if (!onlineMode || !ready || adCount == 0) return;
        for (int slot = 0; slot < 4; slot++)
        {
            bool needed = slot == 0 ? extraSponsorPages > 0 : view == 3 && IsAdActive(openedAd) && adDetailCells[openedAd] >= 0 && slot == adDetailCells[openedAd] / 4 + 1;
            if (isSharedDataSource && slot > 0 && sharedDisplays != null)
                for (int d = 0; d < sharedDisplays.Length; d++)
                {
                    var display = sharedDisplays[d];
                    if (display == null || !display.gameObject.activeInHierarchy || display.view != 3) continue;
                    int ad = display.openedAd;
                    if (IsAdActive(ad) && adDetailCells[ad] >= 0 && slot == adDetailCells[ad] / 4 + 1) needed = true;
                }
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
    { for (int i = 0; i < 4; i++) if (extraSponsorLoading[i]) return true; return false; }
    private bool ExtraSponsorImagesFailed()
    { for (int i = 0; i < 4; i++) if (extraSponsorFailed[i] && !extraSponsorLoading[i]) return true; return false; }

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

    private void PumpImageDownloads()
    {
        if (sharedDataSource != null) return;
        if (!onlineMode || !ready || imageRequestInFlight || drawVerifying || Time.realtimeSinceStartup < nextImageRequestAt) return;
        // Do not flood VRChat's random-order queue. First show the banner/QR atlas.
        BeginSponsorImageDownload(); if (imageRequestInFlight) return;
        if (currentAd >= 0 && adBannerPages[currentAd] == 1 && extraSponsorTextures[0] == null)
        { BeginExtraSponsorImages(); if (imageRequestInFlight) return; }
        BeginImageDownload(); if (imageRequestInFlight) return;
        BeginDrawImage(); if (imageRequestInFlight) return;
        BeginExtraSponsorImages();
    }

    public override void OnImageLoadSuccess(IVRCImageDownload result)
    {
        if (result.Url.Get().Contains("/draw-atlas/")) { VerifyDrawImage(result); return; }
        int extraSlot = ExtraSponsorSlot(result.Url.Get());
        if (extraSlot >= 0)
        {
            if (result != extraSponsorDownloads[extraSlot]) { result.Dispose(); return; }
            imageRequestInFlight = false;
            extraSponsorLoading[extraSlot] = false;
            int height = extraSlot == 0 ? 2048 : 1728;
            if (result.Result == null || result.Result.width != 2048 || result.Result.height != height)
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
        Texture texture = Picture(worldIndex);
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
        if (sharedDataSource != null) { if (!initial) sharedDataSource._RequestSharedDraw(targetTab); return; }
        if (isSharedDataSource && Networking.LocalPlayer != null && !Networking.IsOwner(gameObject)) return;
        if (!onlineMode || !ready || (targetTab != 1 && targetTab != 3) || pendingDrawTab >= 0 || (!initial && Time.realtimeSinceStartup < drawCooldownUntil)) return;
        int slot = targetTab == 1 ? 0 : 1;
        initialDrawAttempted[slot] = true;
        var url = DrawUrl(targetTab);
        if (url == null || string.IsNullOrEmpty(url.Get()) || drawAtlasUrls == null || drawAtlasUrls.Length == 0)
        { drawErrors[slot] = "抽選URLが未設定です。最新版を設定してください"; Render(); return; }
        pendingDrawTab = targetTab; pendingDrawAtlas = -1; pendingDrawJson = ""; pendingDrawGeneration = "";
        drawStartedAt = Time.realtimeSinceStartup; if (!initial) drawCooldownUntil = drawStartedAt + 30f;
        drawLoadingMetadata = true; drawImageRequested = false; drawVerifying = false; drawReadyToCommit = false;
        drawErrors[slot] = "";
        receivingSharedDraw = false; sharedImageUnavailable = false;
        if (isSharedDataSource)
        {
            sharedDrawBusyTab = targetTab; sharedDrawBusyEnd = TimeNow() + 90;
            if (!initial) sharedDrawCooldownEnd = TimeNow() + 30;
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
            sharedDrawCooldownEnd = TimeNow() + 5; RequestSerialization();
        }
        drawErrors[pendingDrawTab == 1 ? 0 : 1] = message;
        if (drawImageRequested && !drawVerifying && !drawReadyToCommit) imageRequestInFlight = false;
        if (pendingDrawDownload != null) { pendingDrawDownload.Dispose(); pendingDrawDownload = null; }
        pendingDrawTexture = null; pendingDrawJson = ""; pendingDrawTab = -1; pendingDrawAtlas = -1;
        drawLoadingMetadata = false; drawImageRequested = false; drawVerifying = false; drawReadyToCommit = false;
        drawCooldownUntil = Time.realtimeSinceStartup + 5f;
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
            pc[index] = false; android[index] = false; recommended[index] = false; sponsorOnly[index] = false; pictureIndices[index] = -1;
            if (i >= worlds.Count) continue;
            var w = worlds[i].DataDictionary;
            ids[index] = Str(w, "id"); titles[index] = Str(w, "name"); authors[index] = Str(w, "authorName"); descriptions[index] = Str(w, "description");
            pc[index] = Flag(w, "pc"); android[index] = Flag(w, "android"); recommended[index] = Flag(w, "recommended"); pictureIndices[index] = Integer(w, "imageIndex", -1);
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
        if (onlineMode && (tab == 1 || tab == 3))
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
    public void _Top() { SetTab(0); }
    public void _ToggleArchive()
    {
        if (archiveUrlPanel == null) return;
        if (archiveUrlField != null) archiveUrlField.text = archiveUrl;
        if (xUrlPanel != null) xUrlPanel.SetActive(false);
        archiveUrlPanel.SetActive(!archiveUrlPanel.activeSelf);
    }
    public void _CloseArchive() { if (archiveUrlPanel != null) archiveUrlPanel.SetActive(false); }
    public void _ToggleX()
    {
        if (archiveUrlPanel != null) archiveUrlPanel.SetActive(false);
        if (xUrlPanel == null) return;
        if (xUrlField != null) xUrlField.text = xProfileUrl;
        xUrlPanel.SetActive(!xUrlPanel.activeSelf);
    }
    public void _CloseX() { if (xUrlPanel != null) xUrlPanel.SetActive(false); }
    public void _Recommended() { SetTab(1); }
    public void _Latest() { SetTab(2); }
    public void _Random() { SetTab(3); }
    public void _Reshuffle() { if (!ready) return; if (onlineMode) BeginDraw(); else { BuildOrder(); page = 0; Render(); } }
    public void _PreviousPage() { if (page > 0) page--; Render(); }
    public void _NextPage() { if ((page + 1) * 4 < orderCount) page++; Render(); }
    public void _DailyOne() { Open(dailyOne); }
    public void _DailyTwo() { Open(secondSlotIsChill ? dailyChill : dailyTwo); }
    public void _Row0() { Open(rowMap[0]); }
    public void _Row1() { Open(rowMap[1]); }
    public void _Row2() { Open(rowMap[2]); }
    public void _Row3() { Open(rowMap[3]); }
    private void Open(int i) { if (!ready || !CanUseWorld(i) || sponsorOnly[i]) return; selected = i; sponsoredDetail = false; view = 2; Render(); }
    public void _Back() { view = tab == 0 ? 0 : 1; TryCommitDraw(); Render(); }
    public void _Ad() { if (onlineMode && !IsAdActive(currentAd)) return; CancelSponsorFade(); if (view != 3) beforeAdView = view; openedTop = -1; openedAd = currentAd; view = 3; PumpImageDownloads(); Render(); }
    public void _AdBack() { view = beforeAdView == 3 ? 0 : beforeAdView; nextSponsorRotation = Time.time + Mathf.Max(5f, sponsorRotationSeconds); Render(); }
    private int Position()
    { for (int i = 0; i < orderCount; i++) if (order[i] == selected) return i; return -1; }
    public void _PreviousWorld()
    { if (sponsoredDetail) { int s = SponsoredPosition(); if (s > 0) OpenSponsored(sponsoredWorlds[s - 1]); } else if (tab == 0) Open(dailyOne); else { int p = Position(); if (p > 0) Open(order[p - 1]); } }
    public void _NextWorld()
    { if (sponsoredDetail) { int s = SponsoredPosition(); if (s >= 0 && s + 1 < sponsoredCount) OpenSponsored(sponsoredWorlds[s + 1]); } else if (tab == 0) Open(secondSlotIsChill ? dailyChill : dailyTwo); else { int p = Position(); if (p >= 0 && p + 1 < orderCount) Open(order[p + 1]); } }

    public void _Portal() { if (!sponsoredDetail || IsSponsoredWorldActive(selected)) RequestPortal(selected); }
    public void _AdPortal() { if (!onlineMode) RequestPortal(adWorld); else if (view == 3 && TopPortalAvailable()) RequestPortal(topWorlds[openedTop]); }
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
    private void RequestPortal(int i)
    {
        if (!ready || !CanUseWorld(i) || PortalWait() > 0 || Time.realtimeSinceStartup - localPortalRequestTime < 1) return;
        localPortalRequestTime = Time.realtimeSinceStartup;
        if (onlineMode && i >= 64)
        {
            if (Networking.LocalPlayer == null || Networking.IsOwner(gameObject)) _DrawPortalOnOwner(portalIds[i], titles[i]);
            else SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(_DrawPortalOnOwner), portalIds[i], titles[i]);
            return;
        }
        if (Networking.LocalPlayer == null || Networking.IsOwner(gameObject)) _PortalOnOwner(ids[i]);
        else SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(_PortalOnOwner), ids[i]);
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
        RequestSerialization(); ApplyPortal();
    }

    private void BuildFavorites()
    {
        favoriteCount = 0;
        if (!favoritesEnabled || favoriteWorldIds == null || favoriteWorldTitles == null) return;
        int length = Mathf.Min(favoriteWorldIds.Length, favoriteWorldTitles.Length);
        favoriteOrder = new int[length];
        for (int i = 0; i < length; i++)
        {
            if (!ValidWorldId(favoriteWorldIds[i]) || string.IsNullOrEmpty(favoriteWorldTitles[i]) || string.IsNullOrEmpty(favoriteWorldTitles[i].Trim()) || favoriteWorldTitles[i].Length > 256) continue;
            bool duplicate = false;
            for (int j = 0; j < favoriteCount; j++) if (favoriteWorldIds[favoriteOrder[j]] == favoriteWorldIds[i]) duplicate = true;
            if (!duplicate) favoriteOrder[favoriteCount++] = i;
        }
    }

    public void _Favorites()
    {
        if (!favoritesEnabled) return;
        favoriteTabSelected = true; view = 4; CancelSponsorFade(); Render();
    }
    public void _FavoritePrevious() { if (favoritePage > 0) favoritePage--; RenderFavorites(); }
    public void _FavoriteNext() { if ((favoritePage + 1) * 8 < favoriteCount) favoritePage++; RenderFavorites(); }
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
        if (!favoritesEnabled || favoritesPanel == null) return;
        favoritesPanel.SetActive(view == 4);
        int pages = Mathf.Max(1, (favoriteCount + 7) / 8);
        favoritePage = Mathf.Clamp(favoritePage, 0, pages - 1);
        if (favoritePageLabel != null) favoritePageLabel.text = favoriteCount == 0 ? "登録なし" : (favoritePage + 1) + " / " + pages + "  ・  " + favoriteCount + " 件";
        if (favoritePrevious != null) favoritePrevious.interactable = favoritePage > 0;
        if (favoriteNext != null) favoriteNext.interactable = favoritePage + 1 < pages;
        float wait = PortalWait();
        if (favoriteHelp != null) favoriteHelp.text = favoriteCount == 0 ? "UnityのInspectorでお気に入りワールドを登録してください。" : wait > 0 ? "ポータルの再作成まで あと " + Mathf.CeilToInt(wait) + " 秒" : "ワールドを押すと、共有ポータルが20秒間出現します。";
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
        if (!favoritesEnabled || view != 4 || row < 0 || row >= 8 || PortalWait() > 0 || Time.realtimeSinceStartup - localPortalRequestTime < 1) return;
        int position = favoritePage * 8 + row;
        if (position >= favoriteCount || favoriteOrder == null || position >= favoriteOrder.Length) return;
        int entry = favoriteOrder[position];
        if (entry < 0 || favoriteWorldIds == null || entry >= favoriteWorldIds.Length) return;
        localPortalRequestTime = Time.realtimeSinceStartup;
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
                else for (int i = 0; i < 4; i++) if (extraSponsorLoading[i])
                { extraSponsorLoading[i] = false; extraSponsorFailed[i] = true; if (extraSponsorDownloads[i] != null) extraSponsorDownloads[i].Dispose(); extraSponsorDownloads[i] = null; }
                Render();
            }
            TryCommitDraw();
            if (pendingDrawTab < 0)
            {
                if (!initialDrawAttempted[0] && string.IsNullOrEmpty(sharedRecommendedDraw)) RequestDraw(1, true);
                else if (!initialDrawAttempted[1] && string.IsNullOrEmpty(sharedRandomDraw)) RequestDraw(3, true);
            }
            bool wasLoadingImage = imageRequestInFlight;
            PumpImageDownloads();
            if (wasLoadingImage != imageRequestInFlight) Render();
            UpdateDrawUi();
        }
        if (sharedDataSource == null && ready && onlineMode && sponsorStatusProtocol && (adCount > 0 || placementCount > 0 || topCount > 0))
        {
            bool failedNow = (sponsorStatusLoading && Time.realtimeSinceStartup - sponsorStatusStarted >= 15f) || (sponsorStatusValid && !StatusUsable());
            if (failedNow) FailSponsorStatus();
            if (Time.realtimeSinceStartup >= nextSponsorStatusAttempt) BeginSponsorStatusDownload();
            if (RefreshSponsorAvailability() || failedNow) Render();
        }
        if (sharedDataSource == null && ready && view != 3 && Time.time >= nextSponsorRotation) BeginSponsorFade();
        if (isSharedDataSource) return;
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
        statusLabel.text = loading ? "NOW LOADING…" : failed ? "情報を取得できません。URL許可を確認して再試行。" : ready ? (imageLoading || sponsorImageLoading || ExtraSponsorImagesLoading() ? "写真を読み込んでいます…" : imageFailed || sponsorImageFailed || ExtraSponsorImagesFailed() ? "写真を取得できません。URL許可を確認して再試行。" : onlineMode ? "入室時の情報を表示しています" : "試作データ: " + revision) : "初期データを待っています…";
        if (ready && sponsorStatusFailed) statusLabel.text = "スポンサーの掲載確認に失敗しました。再試行できます。";
        RenderFavorites();
        LayoutDailyCards();
        for (int k = 0; k < 2; k++)
        {
            int i = k == 0 ? dailyOne : secondSlotIsChill ? dailyChill : dailyTwo;
            topCaptions[k].text = k == 0 ? "今日のランダムワールド" : secondSlotIsChill ? "今日のランダムチルワールド" : "今日のランダムワールド ②";
            topButtons[k].interactable = ready && i >= 0;
            topTitles[k].text = !ready ? "読込待ち" : i >= 0 ? titles[i] : "本日の追加紹介はありません";
            if (topAuthors != null && k < topAuthors.Length && topAuthors[k] != null)
                topAuthors[k].text = ready && i >= 0 && !string.IsNullOrEmpty(authors[i]) ? "by " + authors[i] : "";
            topSummaries[k].text = ready && i >= 0 ? descriptions[i] : "";
            SetPhoto(topPictures[k], ready ? i : -1);
        }
        if (!ready)
        {
            if (sponsorBannerButton != null) sponsorBannerButton.gameObject.SetActive(!onlineMode);
            if (sponsorDisclosure != null) sponsorDisclosure.SetActive(!onlineMode);
            return;
        }
        sectionLabel.text = tab == 1 ? "PICKUP WORLDS" : tab == 2 ? "LATEST WORLDS" : "RANDOM DISCOVERY";
        reshuffleButton.gameObject.SetActive(tab == 1 || tab == 3);
        for (int r = 0; r < 4; r++)
        {
            int p = page * 4 + r; bool valid = p < orderCount;
            rows[r].SetActive(valid); rowMap[r] = valid ? order[p] : -1;
            if (!valid) continue;
            int i = rowMap[r]; rowTitles[r].text = titles[i]; rowSummaries[r].text = descriptions[i]; SetPhoto(rowPictures[r], i);
        }
        pageLabel.text = orderCount == 0 ? "掲載準備中" : (page + 1) + " / " + Mathf.CeilToInt(orderCount / 4f);
        previousPage.interactable = page > 0; nextPage.interactable = (page + 1) * 4 < orderCount;
        detailTitle.text = titles[selected].Replace("\r", " ").Replace("\n", " "); detailDescription.text = descriptions[selected]; SetPhoto(detailPicture, selected);
        if (detailAuthor != null) detailAuthor.text = string.IsNullOrEmpty(authors[selected]) ? "" : "by " + authors[selected];
        bool supported = pc[selected];
#if UNITY_ANDROID
        supported = android[selected];
#endif
        platformNotice.text = supported ? "" : "この端末の対応は未確認、または非対応です。ポータルは生成できます。";
        int second = secondSlotIsChill ? dailyChill : dailyTwo;
        previousWorld.interactable = sponsoredDetail ? SponsoredPosition() > 0 : tab == 0 ? selected != dailyOne && dailyOne >= 0 : Position() > 0;
        nextWorld.interactable = sponsoredDetail ? SponsoredPosition() + 1 < sponsoredCount : tab == 0 ? selected != second && second >= 0 : Position() >= 0 && Position() + 1 < orderCount;
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
    }

    private void Place(RectTransform r, float x, float y, float w, float h)
    {
        // Both prefabs share the standard page coordinates.
        float scale = 1f;
        r.anchoredPosition = new Vector2(x * scale, -y); r.sizeDelta = new Vector2(w * scale, h);
    }

    private void LayoutDailyCards()
    {
        bool compact = ready && sponsoredCount > 0;
        if (sponsoredWorldPanel != null) sponsoredWorldPanel.SetActive(compact);
        for (int i = 0; i < 2; i++)
        {
            Place((RectTransform)topButtons[i].transform, 0, compact ? 44 + i * 267 : 44 + i * 401, 760, compact ? 255 : 388);
            Place(topPictures[i].rectTransform, compact ? 8 : 6, compact ? 8 : 6, compact ? 310 : 748, compact ? 174 : 218);
            Place(topCaptions[i].rectTransform, compact ? 333 : 20, compact ? 15 : 231, compact ? 410 : 720, compact ? 55 : 32);
            Place(topTitles[i].rectTransform, compact ? 333 : 20, compact ? 80 : 268, compact ? 410 : 704, compact ? 62 : 34);
            if (topAuthors != null && i < topAuthors.Length && topAuthors[i] != null)
                Place(topAuthors[i].rectTransform, compact ? 333 : 20, compact ? 147 : 302, compact ? 410 : 704, compact ? 27 : 22);
            Place(topSummaries[i].rectTransform, compact ? 12 : 20, compact ? 190 : 322, compact ? 736 : 704, 55);
        }
    }

    public void _ApplyAppearance()
    {
        if (xIconImage != null) xIconImage.color = xIconColor;
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
        float width = bannerAtlasV2 ? 760f : 760f / 6f;
        Place(image.rectTransform, (760f - width) / 2f, footer ? 0 : 58, width, 760f / 6f);
        if (!bannerAtlasV2) { SponsorPhoto(image, adCells[ad]); return; }
        image.texture = adCells[ad] >= 0 ? (adBannerPages[ad] == 0 ? sponsorTexture : extraSponsorTextures[0]) : null;
        image.color = image.texture == null ? new Color(.04f, .12f, .19f, 1) : Color.white;
        image.uvRect = new Rect(.5f / 2048f, (7 - adBannerRows[ad]) / 8f + .5f / 2048f, .75f - 1f / 2048f, .125f - 1f / 2048f);
    }

    private void SponsorQr(int ad)
    {
        if (!bannerAtlasV2) { SponsorPhoto(sponsorQrPicture, adQrCells[ad]); return; }
        sponsorQrPicture.texture = adQrCells[ad] >= 0 ? (adBannerPages[ad] == 0 ? sponsorTexture : extraSponsorTextures[0]) : null;
        sponsorQrPicture.color = sponsorQrPicture.texture == null ? new Color(.04f, .12f, .19f, 1) : Color.white;
        sponsorQrPicture.uvRect = new Rect(.75f + .5f / 2048f, (7 - adBannerRows[ad]) / 8f + .5f / 2048f, .125f - 1f / 2048f, .125f - 1f / 2048f);
    }

    private void SponsorDetail(int ad)
    {
        bool premium = TopPortalAvailable();
        float detailHeight = premium ? 566f : 641.25f;
        int cell = adDetailCells[ad];
        if (cell >= 0 && cell < 10 && extraSponsorTextures[1 + cell / 4] != null)
        {
            float detailWidth = detailHeight * 1024f / 864f;
            Place(adPicture.rectTransform, (760f - detailWidth) * .5f, 58, detailWidth, detailHeight);
            adPicture.texture = extraSponsorTextures[1 + cell / 4]; adPicture.color = Color.white;
            int local = cell % 4;
            adPicture.uvRect = new Rect((local % 2) * .5f + .5f / 2048f, (1 - local / 2) * .5f + .5f / 1728f, .5f - 1f / 2048f, .5f - 1f / 1728f);
            return;
        }
        // Old feeds and a failed detail download keep the correct banner visible.
        SponsorBanner(adPicture, ad);
        float width = bannerAtlasV2 ? 760f : 760f / 6f;
        Place(adPicture.rectTransform, (760f - width) * .5f, 58 + (detailHeight - 760f / 6f) * .5f, width, 760f / 6f);
    }

    private void RenderSponsors()
    {
        if (sponsoredWorldButtons != null && sponsoredWorldButtons.Length == 2)
        {
            for (int s = 0; s < 2; s++)
            {
                int world = s == 0 ? sponsorLeft : sponsorRight;
                sponsoredWorldButtons[s].gameObject.SetActive(world >= 0);
                if (world >= 0)
                {
                    RawImage photo = sponsoredWorldPictures[s];
                    if (topPlacementProtocol)
                    {
                        sponsoredWorldButtons[s].interactable = IsTopActive(world);
                        sponsoredWorldTitles[s].text = topAdTitles[world];
                        if (topTypes[world] == 0) { Place(photo.rectTransform,8,8,356,133); SetPhoto(photo,topWorlds[world]); }
                        else { Place(photo.rectTransform,8,8+(133f-356f/6f)*.5f,356,356f/6f); SponsorBanner(photo,topAds[world]); }
                    }
                    else { sponsoredWorldTitles[s].text = titles[world]; Place(photo.rectTransform,8,8,356,133); SetPhoto(photo,world); }
                }
            }
        }
        if (sponsorBannerButton != null) { sponsorBannerButton.gameObject.SetActive(!onlineMode || activeAdCount > 0); sponsorBannerButton.interactable = !onlineMode || IsAdActive(currentAd); }
        if (sponsorDisclosure != null) sponsorDisclosure.SetActive(!onlineMode || activeAdCount > 0);
        if (sponsorLinkHelp != null) sponsorLinkHelp.gameObject.SetActive(activeAdCount > 0);
        if (activeAdCount == 0)
        {
            if (sponsorQrPicture != null) sponsorQrPicture.gameObject.SetActive(false);
            if (sponsorUrlField != null) sponsorUrlField.gameObject.SetActive(false);
            adPortalButton.gameObject.SetActive(!onlineMode && adWorld >= 0);
            return;
        }
        int a = view == 3 && IsAdActive(openedAd) ? openedAd : currentAd;
        bannerTitle.text = adNames[currentAd]; SponsorBanner(bannerPicture, currentAd);
        adTitle.text = adNames[a]; adDescription.text = adDescriptions[a]; adSponsor.text = "広告主: " + adSponsors[a];
        SponsorDetail(a);
        bool premium = view == 3 && TopPortalAvailable();
        float linkY = premium ? 635f : 710f;
        if (sponsorQrPicture != null) Place(sponsorQrPicture.rectTransform,664,linkY,96,96);
        if (sponsorUrlField != null) Place((RectTransform)sponsorUrlField.transform,0,linkY,650,96);
        if (sponsorLinkHelp != null) Place(sponsorLinkHelp.rectTransform,10,linkY+66,630,25);
        Place((RectTransform)adPortalButton.transform,130,746,500,62);
        if (sponsorQrPicture != null) { sponsorQrPicture.gameObject.SetActive(true); SponsorQr(a); }
        if (sponsorUrlField != null) { sponsorUrlField.gameObject.SetActive(true); sponsorUrlField.text = adUrls[a]; }
        adPortalButton.gameObject.SetActive(premium);
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
    { return pendingDrawTab >= 0 || (sharedDrawBusyTab >= 0 && SharedRemaining(sharedDrawBusyEnd) > 0); }

    public void _RequestSharedDraw(int targetTab)
    {
        if (!isSharedDataSource || (targetTab != 1 && targetTab != 3) || SharedDrawBusy() || SharedRemaining(sharedDrawCooldownEnd) > 0) return;
        if (Networking.LocalPlayer == null || Networking.IsOwner(gameObject)) _SharedDrawOnOwner(targetTab);
        else SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(_SharedDrawOnOwner), targetTab);
    }

    [NetworkCallable(maxEventsPerSecond: 1)]
    public void _SharedDrawOnOwner(int targetTab)
    {
        if (!isSharedDataSource || (Networking.LocalPlayer != null && !Networking.IsOwner(gameObject))) return;
        if (!ready || (targetTab != 1 && targetTab != 3) || SharedDrawBusy() || SharedRemaining(sharedDrawCooldownEnd) > 0) return;
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
        for (int i = 0; i < 4; i++)
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

}
