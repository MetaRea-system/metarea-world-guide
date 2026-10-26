using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using UdonSharpEditor;

public static class MetaReaFavoritesLayout
{
    public const string PrefabPath = "Packages/net.metarea.worldguide/Runtime/MetaRea World GUIDE Favorites.prefab";

    public static void Apply(MetaReaWallGuide g)
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
        if (!g.favoritesEnabled) return;
        var standard = AssetDatabase.LoadAssetAtPath<GameObject>(MetaReaPrototypeBuilder.Root + "/MetaRea World GUIDE.prefab");
        if (standard == null) throw new InvalidOperationException("The standard MetaRea prefab is missing.");
        // Rebuild from the standard coordinates so repeated updates do not accumulate scaling.
        foreach (var rect in g.GetComponentsInChildren<RectTransform>(true))
        {
            string path = AnimationUtility.CalculateTransformPath(rect, g.transform);
            var source = standard.transform.Find(path) as RectTransform;
            if (source == null) continue;
            rect.anchorMin = source.anchorMin; rect.anchorMax = source.anchorMax; rect.pivot = source.pivot;
            rect.anchoredPosition = new Vector2(source.anchoredPosition.x, source.anchoredPosition.y);
            rect.sizeDelta = new Vector2(source.sizeDelta.x, source.sizeDelta.y);
        }
        foreach (string name in new[] { "Thin frame", "Top cyan trim", "Bottom cyan trim" })
        {
            var source = standard.transform.Find(name); var target = g.transform.Find(name);
            if (source != null && target != null) target.localScale = new Vector3(source.localScale.x, source.localScale.y, source.localScale.z);
        }
        var canvas = g.topPanel.GetComponentInParent<Canvas>().transform;
        var udon = UdonSharpEditorUtility.GetBackingUdonBehaviour(g);
        var nav = new Image[5];
        for (int i = 0; i < 4; i++)
        {
            nav[i] = canvas.Find("Navigation " + i).GetComponent<Image>();
            Place(nav[i].rectTransform, 20 + i * 153, 156, 148, 92);
            var label = nav[i].GetComponentInChildren<Text>(true);
            Place(label.rectTransform, 0, 0, 148, 92); label.fontSize = 20;
        }
        var fifth = canvas.Find("Navigation 4");
        if (fifth == null)
        {
            var clone = UnityEngine.Object.Instantiate(nav[3].gameObject, canvas, false);
            clone.name = "Navigation 4"; fifth = clone.transform;
        }
        var fifthButton = fifth.GetComponent<Button>();
        fifthButton.onClick = new Button.ButtonClickedEvent();
        UnityEventTools.AddStringPersistentListener(fifthButton.onClick, udon.SendCustomEvent, "_Favorites");
        nav[4] = fifth.GetComponent<Image>(); Place(nav[4].rectTransform, 632, 156, 148, 92);
        var fifthLabel = fifth.GetComponentInChildren<Text>(true);
        fifthLabel.text = "お気に入り\nワールド"; Place(fifthLabel.rectTransform, 0, 0, 148, 92); fifthLabel.fontSize = 20;
        g.navImages = nav;

        var panel = Rect(g.topPanel.transform.parent, "Favorites", 0, 0, 760, 858);
        g.favoritesPanel = panel.gameObject;
        Label(panel, "Section", "FAVORITE WORLDS", g, 0, 0, 350, 48, 23, g.secondaryColor);
        g.favoritePageLabel = Label(panel, "Page", "登録なし", g, 350, 0, 240, 48, 21, g.secondaryColor);
        g.favoritePageLabel.alignment = TextAnchor.MiddleCenter;
        g.favoritePrevious = Button(panel, "Previous", "＜", "_FavoritePrevious", g, 596, 0, 76, 48);
        g.favoriteNext = Button(panel, "Next", "＞", "_FavoriteNext", g, 684, 0, 76, 48);
        g.favoriteHelp = Label(panel, "Help", "UnityのInspectorでお気に入りワールドを登録してください。", g, 0, 49, 760, 30, 19, g.secondaryColor);
        g.favoriteButtons = new Button[8]; g.favoriteTitles = new Text[8]; g.favoriteAuthors = new Text[8];
        for (int i = 0; i < 8; i++)
        {
            var button = Button(panel, "Favorite " + i, "", "_Favorite" + i, g, 0, 82 + i * 96, 760, 88);
            g.favoriteButtons[i] = button;
            g.favoriteTitles[i] = Label(button.transform, "Name", "ワールド名", g, 20, 2, 580, 58, 20, g.headingColor);
            g.favoriteTitles[i].alignment = TextAnchor.MiddleLeft;
            g.favoriteAuthors[i] = Label(button.transform, "Author", "作者名", g, 20, 61, 580, 26, 18, g.secondaryColor);
            var arrow = Label(button.transform, "Arrow", "PORTAL  ›", g, 620, 20, 120, 48, 19, g.accentColor);
            arrow.alignment = TextAnchor.MiddleCenter;
            button.gameObject.SetActive(false);
        }
        panel.gameObject.SetActive(false);
        MetaReaClientCompatibility.Apply(g);
        g.headingTexts = g.headingTexts.Concat(g.favoriteTitles).Distinct().ToArray();
        g.secondaryTexts = g.secondaryTexts.Concat(g.favoriteAuthors).Concat(new[] { g.favoritePageLabel, g.favoriteHelp }).Distinct().ToArray();
        g.buttonTexts = g.buttonTexts.Concat(fifth.GetComponentsInChildren<Text>(true)).Concat(new[] { g.favoritePrevious.GetComponentInChildren<Text>(), g.favoriteNext.GetComponentInChildren<Text>() }).Distinct().ToArray();
        g.accentTexts = g.accentTexts.Concat(panel.GetComponentsInChildren<Text>(true).Where(t => t.name == "Arrow")).Distinct().ToArray();
        g.cardImages = g.cardImages.Concat(panel.GetComponentsInChildren<Button>(true).Select(b => b.GetComponent<Image>())).Concat(new[] { nav[4] }).Distinct().ToArray();
        g.borderOutlines = g.GetComponentsInChildren<Outline>(true);
        g._ApplyAppearance();
        if (g.xUrlPanel != null) g.xUrlPanel.transform.SetAsLastSibling();
        UdonSharpEditorUtility.CopyProxyToUdon(g); EditorUtility.SetDirty(g);
    }

    static void Place(RectTransform r, float x, float y, float w, float h)
    { r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1); r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h); }
    static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
    {
        var child = parent.Find(name);
        var r = child == null ? new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>() : (RectTransform)child;
        r.SetParent(parent, false); r.gameObject.layer = 0; Place(r, x, y, w, h); return r;
    }
    static Text Label(Transform parent, string name, string text, MetaReaWallGuide g, float x, float y, float w, float h, int size, Color color)
    {
        var r = Rect(parent, name, x, y, w, h); var t = r.GetComponent<Text>() ?? r.gameObject.AddComponent<Text>();
        t.text = text; t.font = g.detailTitle.font; t.fontSize = size; t.color = color; t.supportRichText = false; t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate; t.alignment = TextAnchor.MiddleLeft; return t;
    }
    static Button Button(Transform parent, string name, string text, string method, MetaReaWallGuide g, float x, float y, float w, float h)
    {
        var r = Rect(parent, name, x, y, w, h); var image = r.GetComponent<Image>() ?? r.gameObject.AddComponent<Image>(); image.color = g.cardColor;
        var outline = r.GetComponent<Outline>() ?? r.gameObject.AddComponent<Outline>(); outline.effectColor = g.borderColor; outline.effectDistance = new Vector2(1, -1);
        var b = r.GetComponent<Button>() ?? r.gameObject.AddComponent<Button>(); b.targetGraphic = image; b.navigation = new Navigation { mode = Navigation.Mode.None };
        b.onClick = new Button.ButtonClickedEvent(); UnityEventTools.AddStringPersistentListener(b.onClick, UdonSharpEditorUtility.GetBackingUdonBehaviour(g).SendCustomEvent, method);
        var label = Label(r, "Label", text, g, 0, 0, w, h, 26, g.buttonTextColor); label.alignment = TextAnchor.MiddleCenter; return b;
    }
}


