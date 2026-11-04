using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public static class MetaReaPlatformLayout
{
    public static void Apply(MetaReaWallGuide g)
    {
        g.topPlatforms = new Text[g.topButtons.Length];
        for (int i = 0; i < g.topPlatforms.Length; i++) g.topPlatforms[i] = Label(g.topButtons[i].transform, "Platforms", g, 20, 302, 276, 26, 16);
        g.manyPlatforms = new Text[g.manyButtons.Length];
        for (int i = 0; i < g.manyPlatforms.Length; i++) g.manyPlatforms[i] = Label(g.manyButtons[i].transform, "Platforms", g, 10, 358, 212, 26, 14);
        g.rowPlatforms = new Text[g.rows.Length]; g.rowAuthors = new Text[g.rows.Length];
        for (int i = 0; i < g.rows.Length; i++)
        {
            Place(g.rowTitles[i].rectTransform, 285, 10, 415, 64);
            Place(g.rowSummaries[i].rectTransform, 285, 78, 415, 64);
            g.rowTitles[i].fontSize = 24; g.rowTitles[i].resizeTextForBestFit = false;
            g.rowSummaries[i].fontSize = 22; g.rowSummaries[i].resizeTextForBestFit = false;
            g.rowTitles[i].lineSpacing = 1; g.rowSummaries[i].lineSpacing = 1;
            g.rowTitles[i].verticalOverflow = VerticalWrapMode.Truncate; g.rowSummaries[i].verticalOverflow = VerticalWrapMode.Truncate;
            g.rowPlatforms[i] = Label(g.rows[i].transform, "Platforms", g, 285, 151, 220, 28, 14);
            g.rowAuthors[i] = Label(g.rows[i].transform, "Author", g, 510, 151, 190, 28, 16);
            g.rowAuthors[i].alignment = TextAnchor.MiddleRight;
        }
        g.detailPlatforms = Label(g.detailPanel.transform, "Platforms", g, 12, 611, 736, 30, 18);
        Place(g.platformNotice.rectTransform, 5, 640, 750, 20);
        g.platformNotice.fontSize = 14;
        g.secondaryTexts = g.secondaryTexts.Concat(g.topPlatforms).Concat(g.manyPlatforms).Concat(g.rowPlatforms).Concat(g.rowAuthors).Concat(new[] { g.detailPlatforms }).Where(t => t != null).Distinct().ToArray();
        if (g.favoriteFolderButtons != null) for (int i = 0; i < g.favoriteFolderButtons.Length; i++) Place((RectTransform)g.favoriteFolderButtons[i].transform, (i % 2) * 388, 82 + (i / 2) * 185, 372, 176);
        g._ApplyAppearance();
    }
    static void Place(RectTransform r, float x, float y, float w, float h)
    { r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1); r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h); }
    static Text Label(Transform parent, string name, MetaReaWallGuide g, float x, float y, float w, float h, int size)
    {
        var child = parent.Find(name);
        var r = child != null ? (RectTransform)child : new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        r.SetParent(parent, false); r.gameObject.layer = parent.gameObject.layer; Place(r, x, y, w, h);
        var text = r.GetComponent<Text>() ?? r.gameObject.AddComponent<Text>();
        text.font = g.detailTitle.font; text.fontSize = size; text.supportRichText = false; text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow; text.verticalOverflow = VerticalWrapMode.Truncate;
        text.resizeTextForBestFit = false; text.alignment = TextAnchor.MiddleLeft; text.text = "";
        return text;
    }
}
