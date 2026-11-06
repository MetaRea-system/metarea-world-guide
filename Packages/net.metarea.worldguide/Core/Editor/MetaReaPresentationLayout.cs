using TMPro;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UdonSharpEditor;

public static class MetaReaPresentationLayout
{

    public static void SelectGuide()
    {
        var g = Object.FindObjectOfType<MetaReaWallGuide>();
        if (g == null) throw new System.InvalidOperationException("シーンにMetaRea Wall Guideを配置してください。");
        Selection.activeGameObject = g.gameObject;
        EditorGUIUtility.PingObject(g.gameObject);
    }

    public static void UpgradeSelected()
    {
        var g = Selection.activeGameObject == null ? null : Selection.activeGameObject.GetComponentInParent<MetaReaWallGuide>();
        if (g == null) throw new System.InvalidOperationException("HierarchyでMetaRea Wall Guideを選択してください。");
        MetaReaSponsorLayout.Apply(g);
        Apply(g);
        MetaReaOnlineSetup.Configure(g, MetaReaOnlineSetup.FeedUrl, MetaReaOnlineSetup.AtlasTemplate);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(g.gameObject.scene);
    }

    public static void Apply(MetaReaWallGuide g)
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play mode first.");
        var canvas = g.topPanel.GetComponentInParent<Canvas>();
        foreach (string name in new[] { "AccentLeft", "AccentRight" })
        { var t = canvas.transform.Find(name); if (t != null) Undo.DestroyObjectImmediate(t.gameObject); }
        var brand = canvas.transform.Find("Brand").GetComponent<TMP_Text>();
        var heading = canvas.transform.Find("Title").GetComponent<TMP_Text>();
        brand.text = "MetaRea"; heading.text = "WORLD GUIDE";
        foreach (var t in new[] { brand, heading })
        {
            t.fontSize = 52; t.enableAutoSizing = false;
            t.alignment = TextAlignmentOptions.Left;
            t.enableWordWrapping = false;
            t.overflowMode = TextOverflowModes.Overflow;
        }
        float brandWidth = brand.preferredWidth, headingWidth = heading.preferredWidth;
        float headerX = (800f - brandWidth - headingWidth - 14f) * .5f;
        Place(brand.rectTransform, headerX, 22, brandWidth + 2, 80);
        Place(heading.rectTransform, headerX + brandWidth + 14, 22, headingWidth + 2, 80);
        foreach (var button in new[] { g.previousPage, g.nextPage })
        {
            var label = button.GetComponentInChildren<TMP_Text>(true);
            label.text = button == g.previousPage ? "＜" : "＞";
            label.fontSize = 40; label.fontStyle = FontStyles.Bold;
            label.enableAutoSizing = false; label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Overflow;
            Place(label.rectTransform, 0, 0, 78, 54);
        }
        Place((RectTransform)g.previousPage.transform, 0, 0, 78, 54);
        Place((RectTransform)g.nextPage.transform, 682, 0, 78, 54);
        Place(g.pageLabel.rectTransform, 280, 0, 200, 54);
        Place(g.sectionLabel.rectTransform, 94, 0, 175, 54);
        g.sectionLabel.fontSize = 16; g.sectionLabel.alignment = TextAlignmentOptions.Left;
        Place((RectTransform)g.reshuffleButton.transform, 520, 2, 144, 50);
        for (int i = 0; i < g.rows.Length; i++)
        {
            Place((RectTransform)g.rows[i].transform, 0, 66 + i * 197, 760, 187);
            Place(g.rowPictures[i].rectTransform, 6, 6, 260, 175);
            Place(g.rowTitles[i].rectTransform, 285, 10, 415, 64);
            Place(g.rowSummaries[i].rectTransform, 285, 80, 415, 98);
        }
        var titleClip = Rect(g.detailPanel.transform, "Title clip", 12, 434, 736, 48);
        if (titleClip.GetComponent<RectMask2D>() == null) titleClip.gameObject.AddComponent<RectMask2D>();
        g.detailTitle.transform.SetParent(titleClip, false);
        Place(g.detailTitle.rectTransform, 0, 0, 736, 48);
        g.detailTitle.alignment = TextAlignmentOptions.Left;
        g.detailTitle.enableAutoSizing = false; g.detailTitle.fontSize = 30;
        g.detailTitle.enableWordWrapping = false;
        g.detailTitle.overflowMode = TextOverflowModes.Truncate;
        g.detailAuthor = Label(g.detailPanel.transform, "Author", g.detailTitle.font, 12, 482, 736, 28, 19);
        g.detailAuthor.alignment = TextAlignmentOptions.Right;
        var viewport = (RectTransform)g.detailDescription.transform.parent;
        Place(viewport, 12, 515, 736, 130);
        Place(g.detailDescription.rectTransform, 0, 0, 736, 130);
        g.detailDescription.alignment = TextAlignmentOptions.TopLeft;
        g.detailDescription.lineSpacing = 0f;
        g.detailDescription.fontSize = 23;
        g.topAuthors = new TMP_Text[2];
        for (int i = 0; i < 2; i++)
        {
            g.topAuthors[i] = Label(g.topButtons[i].transform, "Author", g.detailTitle.font, 20, 302, 704, 22, 19);
            g.topAuthors[i].alignment = TextAlignmentOptions.Right;
            // Runtime switches between full and compact cards; this is the unloaded preview.
            Place(g.topTitles[i].rectTransform, 20, 268, 704, 34);
        }
        g.backgroundImage = canvas.GetComponent<Image>();
        var all = g.GetComponentsInChildren<TMP_Text>(true);
        g.bodyTexts = all.Where(t => t.name == "Summary" || t == g.detailDescription || t == g.adDescription).ToArray();
        g.headingTexts = all.Where(t => t.name == "Name" || t == g.detailTitle || t == g.adTitle || t == g.portalDestinationLabel || t.transform == canvas.transform.Find("Title")).ToArray();
        g.accentTexts = all.Where(t => t.name == "Caption" || t.name == "Arrow" || t.transform == canvas.transform.Find("Brand")).ToArray();
        g.buttonTexts = all.Where(t => t.name == "Label").ToArray();
        g.secondaryTexts = all.Except(g.bodyTexts).Except(g.headingTexts).Except(g.accentTexts).Except(g.buttonTexts).ToArray();
        g.portalImages = new[] {g.portalButton.GetComponent<Image>(),g.adPortalButton.GetComponent<Image>()};
        g.cardImages = g.GetComponentsInChildren<Selectable>(true).Select(s=>s.GetComponent<Image>()).Where(im=>im!=null && !g.portalImages.Contains(im)).Distinct().ToArray();
        g.borderOutlines = g.GetComponentsInChildren<Outline>(true);
        g.dividerImages = g.GetComponentsInChildren<Image>(true).Where(im=>im.name=="Ad divider").ToArray();
        MetaReaClientCompatibility.Apply(g);
        ApplyColors(g);
        UdonSharpEditorUtility.CopyProxyToUdon(g);
        EditorUtility.SetDirty(g);
    }

    public static void ApplyColors(MetaReaWallGuide g)
    {
        var graphics = g.GetComponentsInChildren<Graphic>(true);
        Undo.RecordObjects(graphics.Cast<Object>().ToArray(), "Change MetaRea colors");
        Undo.RecordObjects(g.GetComponentsInChildren<Outline>(true).Cast<Object>().ToArray(), "Change MetaRea borders");
        g._ApplyAppearance();
        foreach (var graphic in graphics) { EditorUtility.SetDirty(graphic); PrefabUtility.RecordPrefabInstancePropertyModifications(graphic); }
        foreach (var outline in g.GetComponentsInChildren<Outline>(true)) { EditorUtility.SetDirty(outline); PrefabUtility.RecordPrefabInstancePropertyModifications(outline); }
    }

    static void Place(RectTransform r, float x, float y, float w, float h)
    { r.anchorMin=r.anchorMax=new Vector2(0,1); r.pivot=new Vector2(0,1); r.anchoredPosition=new Vector2(x,-y); r.sizeDelta=new Vector2(w,h); }
    static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
    {
        var t=parent.Find(name);
        var r=t==null?new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>():(RectTransform)t;
        r.SetParent(parent,false);r.gameObject.layer=0;Place(r,x,y,w,h);return r;
    }
    static TMP_Text Label(Transform parent,string name,TMP_FontAsset font,float x,float y,float w,float h,int size)
    {
        var r=Rect(parent,name,x,y,w,h);var t=r.GetComponent<TMP_Text>();if(t==null)t=r.gameObject.AddComponent<TextMeshProUGUI>();
        t.font=font;t.fontSize=size;t.text="";t.raycastTarget=false;t.richText=false;
        t.enableWordWrapping = true;t.overflowMode = TextOverflowModes.Truncate;
        return t;
    }
}



