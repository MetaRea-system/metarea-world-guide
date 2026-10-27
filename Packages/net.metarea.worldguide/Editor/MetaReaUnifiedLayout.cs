using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using UdonSharpEditor;

// One transport and one detail screen; only the local TOP presentation changes.
public static class MetaReaUnifiedLayout
{
    const float ExtraHeight = 0f;
    static BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

    public static void Apply(MetaReaWallGuide g)
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
        bool keepFavorites = !g.unifiedDisplay || g.favoritesEnabled;
        g.unifiedDisplay = true;
        g.favoritesEnabled = true;
        if (g.favoritesSettings == null)
        {
            var settings = new GameObject("お気に入りワールド設定");
            settings.transform.SetParent(g.transform, false);
            g.favoritesSettings = UdonSharpUndo.AddComponent<MetaReaFavoritesSettings>(settings);
            UdonSharpEditorUtility.CopyProxyToUdon(g.favoritesSettings);
        }
        MetaReaManyLayout.Apply(g);
        MetaReaFavoritesLayout.Apply(g);
        var canvas = g.topPanel.GetComponentInParent<Canvas>();
        var canvasRect = (RectTransform)canvas.transform;
        canvasRect.sizeDelta = new Vector2(800, 1350 + ExtraHeight);
        var collider = canvas.GetComponent<BoxCollider>();
        if (collider != null) { collider.size = new Vector3(800, 1350 + ExtraHeight, 8); collider.center = new Vector3(0, (1350 + ExtraHeight) / 2, 0); }
        var frame = g.transform.Find("Thin frame");
        if (frame != null) { frame.localPosition = new Vector3(0, .774f + ExtraHeight * .000925f / 2, .015f); frame.localScale = new Vector3(.777f, 1.286f + ExtraHeight * .000925f, .035f); }
        var trim = g.transform.Find("Top cyan trim");
        if (trim != null) trim.localPosition = new Vector3(0, 1.416f + ExtraHeight * .000925f, -.008f);
        Place((RectTransform)g.topPanel.transform.parent, 20, 267 + ExtraHeight, 760, 858);
        Place((RectTransform)g.sponsorBannerButton.transform, 20, 1190 + ExtraHeight, 760, 760f/6f);
        if (g.sponsorDisclosure != null) Place((RectTransform)g.sponsorDisclosure.transform, 20, 1158 + ExtraHeight, 760, 30);
        Place(g.statusLabel.rectTransform, 20, 1320 + ExtraHeight, 565, 25);
        Place((RectTransform)g.retryButton.transform, 622, 1318 + ExtraHeight, 158, 28);
        var divider = canvas.transform.Find("Ad divider") as RectTransform;
        if (divider != null) Place(divider, 0, 1148 + ExtraHeight, 800, 2);
        if (g.xUrlPanel != null) ((RectTransform)g.xUrlPanel.transform).sizeDelta = new Vector2(800, 1350 + ExtraHeight);
        if (g.archiveUrlPanel != null) ((RectTransform)g.archiveUrlPanel.transform).sizeDelta = new Vector2(800, 1350 + ExtraHeight);
        // Favorites shares the original navigation row; no runtime layout selector.
        var oldBar = canvas.transform.Find("Display modes");
        if (oldBar != null) UnityEngine.Object.DestroyImmediate(oldBar.gameObject);
        canvas.transform.Find("Navigation 4").gameObject.SetActive(true);
        g.buttonTexts = g.buttonTexts.Where(t => t != null).ToArray();
        g.cardImages = g.cardImages.Where(t => t != null).ToArray();
        g.borderOutlines = g.GetComponentsInChildren<Outline>(true);
        g.favoritesEnabled = keepFavorites;
        Preview(g);
        if (g.xUrlPanel != null) g.xUrlPanel.transform.SetAsLastSibling();
        if (g.archiveUrlPanel != null) g.archiveUrlPanel.transform.SetAsLastSibling();
        UdonSharpEditorUtility.CopyProxyToUdon(g); EditorUtility.SetDirty(g);
    }

    public static void Preview(MetaReaWallGuide g)
    {

        typeof(MetaReaWallGuide).GetMethod("InitializeDisplayMode", Flags).Invoke(g, null);
        typeof(MetaReaWallGuide).GetMethod("LayoutDailyCards", Flags).Invoke(g, null);
        bool favorites = (bool)typeof(MetaReaWallGuide).GetField("favoriteTabSelected", Flags).GetValue(g);
        g.topPanel.SetActive(!favorites); g.listPanel.SetActive(false); g.detailPanel.SetActive(false); g.adPanel.SetActive(false);
        if (g.favoritesSettings != null)
        {
            g.favoriteWorldIds = g.favoritesSettings.favoriteWorldIds;
            g.favoriteWorldTitles = g.favoritesSettings.favoriteWorldTitles;
            g.favoriteWorldAuthors = g.favoritesSettings.favoriteWorldAuthors;
        }
        typeof(MetaReaWallGuide).GetMethod("BuildFavorites", Flags).Invoke(g, null);
        typeof(MetaReaWallGuide).GetMethod("RenderFavorites", Flags).Invoke(g, null);
        MetaReaPlaceholderLayout.Apply(g);
        g._ApplyAppearance();
        EditorUtility.SetDirty(g);
    }

    static void Place(RectTransform r, float x, float y, float w, float h)
    { r.anchorMin = r.anchorMax = new Vector2(0,1); r.pivot = new Vector2(0,1); r.anchoredPosition = new Vector2(x,-y); r.sizeDelta = new Vector2(w,h); }
    static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
    { var child=parent.Find(name); var r=child!=null?(RectTransform)child:new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent,false); r.gameObject.layer=parent.gameObject.layer; Place(r,x,y,w,h); return r; }
}
