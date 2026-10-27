using UnityEngine;
using UnityEngine.UI;

// Keep image/text sizes; remove gaps and use the saved height for a shorter board.
public static class MetaReaCompactLayout
{
    public const float Height = 1295f;
    public const float ContentHeight = 823f;

    public static void Apply(MetaReaWallGuide g)
    {
        var canvas = g.topPanel.GetComponentInParent<Canvas>();
        var cr = (RectTransform)canvas.transform;
        cr.sizeDelta = new Vector2(800, Height);
        var collider = canvas.GetComponent<BoxCollider>();
        if (collider != null) { collider.size = new Vector3(800, Height, 8); collider.center = new Vector3(0, Height / 2, 0); }
        var frame = g.transform.Find("Thin frame");
        if (frame != null) { frame.localPosition = new Vector3(0, .774f - 55f * .000925f / 2, .015f); frame.localScale = new Vector3(.777f, 1.286f - 55f * .000925f, .035f); }
        var trim = g.transform.Find("Top cyan trim");
        if (trim != null) trim.localPosition = new Vector3(0, 1.416f - 55f * .000925f, -.008f);
        Place(g.topPanel.transform.parent, 20, 267, 760, 840);
        foreach (var panel in new[] { g.topPanel, g.listPanel, g.detailPanel, g.adPanel, g.favoritesPanel })
            if (panel != null) Place(panel.transform, 0, 0, 760, ContentHeight);

        if (g.sponsoredWorldPanel != null)
        {
            Place(g.topPanel.transform, 0, 0, 760, 840);
            Place(g.sponsoredWorldPanel.transform, 0, 570, 760, 270);
            Disclosure(g.sponsoredWorldPanel.transform.Find("Heading"), 0, 0);
            for (int i = 0; i < g.sponsoredWorldButtons.Length; i++)
            {
                var card = g.sponsoredWorldButtons[i].transform;
                Place(card, i * 388, 18, 372, 252);
                var title = g.sponsoredWorldTitles[i];
                Place(title.transform, 10, 150, 352, 92);
                title.resizeTextForBestFit = true;
                title.resizeTextMinSize = 20;
                title.resizeTextMaxSize = 24;
                title.horizontalOverflow = HorizontalWrapMode.Wrap;
                title.verticalOverflow = VerticalWrapMode.Truncate;
                var gold = card.Find("Gold frame");
                Place(gold, 0, 0, 372, 252);
                if (gold != null) for (int edge = 0; edge < 4; edge++)
                    Place(gold.Find("Edge " + edge), edge == 3 ? 369 : 0,
                        edge == 1 ? 249 : 0, edge < 2 ? 372 : 3, edge < 2 ? 3 : 252);
            }
        }
        Place(canvas.transform.Find("Ad divider"), 0, 1113, 800, 2);
        Disclosure(g.sponsorDisclosure != null ? g.sponsorDisclosure.transform : null, 20, 1115);
        Place(g.sponsorBannerButton.transform, 20, 1135, 760, 760f / 6f);
        Place(g.statusLabel.transform, 20, 1265, 565, 25);
        Place(g.retryButton.transform, 622, 1263, 158, 28);

        // List rows and favorites retain their height; only inter-row gaps shrink.
        for (int i = 0; i < g.rows.Length; i++) Place(g.rows[i].transform, 0, 60 + i * 190, 760, 187);
        if (g.favoriteButtons != null) for (int i = 0; i < g.favoriteButtons.Length; i++)
            Place(g.favoriteButtons[i].transform, 0, 82 + i * 93, 760, 88);
        Place(g.detailDescription.transform.parent, 12, 515, 736, 95);
        Place(g.platformNotice.transform, 5, 615, 750, 40);
        Place(g.previousWorld.transform, 0, 672, 195, 77);
        Place(g.portalButton.transform, 207, 661, 346, 98);
        Place(g.nextWorld.transform, 565, 672, 195, 77);
        Place(g.returnLabel.transform.parent, 0, 772, 760, 50);

        Disclosure(g.adPanel.transform.Find("Disclosure"), 0, 0);
        Place(g.adPicture.transform, 0, 23, 760, 641.25f);
        Place(g.sponsorQrPicture.transform, 664, 675, 96, 96);
        Place(g.sponsorUrlField.transform, 0, 675, 650, 96);
        Place(g.sponsorLinkHelp.transform, 10, 741, 630, 25);
        Place(g.adPortalButton.transform, 130, 711, 500, 62);
        Place(g.adPanel.transform.Find("Ad back"), 0, 788, 760, 35);
        if (g.xUrlPanel != null) ((RectTransform)g.xUrlPanel.transform).sizeDelta = new Vector2(800, Height);
        if (g.archiveUrlPanel != null) ((RectTransform)g.archiveUrlPanel.transform).sizeDelta = new Vector2(800, Height);
    }

    static void Disclosure(Transform t, float x, float y)
    {
        if (t == null) return;
        Place(t, x, y, 760, 18);
        var label = t.GetComponent<Text>();
        if (label != null)
        {
            label.fontSize = 14; label.resizeTextForBestFit = false; label.alignment = TextAnchor.MiddleLeft;
            // CJK font line metrics exceed the visible PR glyph height.
            label.verticalOverflow = VerticalWrapMode.Overflow;
        }
    }
    static void Place(Transform t, float x, float y, float width, float height)
    {
        if (t == null) return;
        var r = (RectTransform)t;
        r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1);
        r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(width, height);
    }
}
