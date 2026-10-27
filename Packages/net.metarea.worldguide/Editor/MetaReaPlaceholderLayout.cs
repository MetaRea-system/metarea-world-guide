using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Editor preview content is intentionally generic. Runtime data always replaces it.
public static class MetaReaPlaceholderLayout
{
    public const string Root = "Packages/net.metarea.worldguide/Runtime/UI/Placeholders/";
    static Texture Load(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>(Root + name + ".png");

    public static void Apply(MetaReaWallGuide g)
    {
        g.loadingPicture = Load("loading");
        g.unavailablePicture = Load("unavailable");
        g.failedPicture = Load("failed");
        var photos = new List<RawImage>();
        if (g.topPictures != null) photos.AddRange(g.topPictures);
        if (g.rowPictures != null) photos.AddRange(g.rowPictures);
        if (g.detailPicture != null) photos.Add(g.detailPicture);
        if (g.adPicture != null) photos.Add(g.adPicture);
        if (g.manyPictures != null) photos.AddRange(g.manyPictures);
        if (g.sponsoredWorldPictures != null) photos.AddRange(g.sponsoredWorldPictures);
        photos.RemoveAll(p => p == null);
        var indicators = new List<Text>();
        foreach (var photo in photos)
        {
            var child = photo.transform.Find("Loading indicator");
            if (child == null)
            {
                var go = new GameObject("Loading indicator", typeof(RectTransform), typeof(Text));
                go.transform.SetParent(photo.transform, false);
                go.layer = photo.gameObject.layer;
                child = go.transform;
            }
            var text = child.GetComponent<Text>();
            var rect = text.rectTransform;
            rect.anchorMin = new Vector2(.2f, .16f); rect.anchorMax = new Vector2(.8f, .30f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            text.font = g.topTitles[0].font;
            text.fontSize = 18; text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 8; text.resizeTextMaxSize = 18;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(.35f, .85f, 1f);
            text.raycastTarget = false; text.supportRichText = false;
            text.text = "●  ○  ○"; text.enabled = false;
            indicators.Add(text);
        }
        g.loadingTargets = photos.ToArray(); g.loadingIndicators = indicators.ToArray();
        if (g.samplePictures != null)
            for (int i = 0; i < g.samplePictures.Length; i++) g.samplePictures[i] = Load(i == 0 ? "daily-1" : i == 2 ? "daily-2" : "world");
        for (int i = 0; i < g.topPictures.Length; i++)
        {
            Photo(g.topPictures[i], Load("daily-" + (i + 1)), true);
            g.topTitles[i].text = "ワールド名";
            g.topSummaries[i].text = "ワールドの説明文";
            if (g.topAuthors != null && i < g.topAuthors.Length) g.topAuthors[i].text = "作者名";
            g.topCaptions[i].text = i == 0 ? "今日のランダムワールド①" : g.secondSlotIsChill ? "今日のランダムチルワールド" : "今日のランダムワールド②";
        }
        for (int i = 0; i < g.rowPictures.Length; i++)
        {
            Photo(g.rowPictures[i], Load("world"), true);
            g.rowTitles[i].text = "ワールド名"; g.rowSummaries[i].text = "ワールドの説明文";
        }
        Photo(g.detailPicture, Load("world"), true);
        g.detailTitle.text = "ワールド名"; g.detailDescription.text = "ワールドの説明文";
        if (g.detailAuthor != null) g.detailAuthor.text = "作者名";
        if (g.manyPictures != null && g.manyPictures.Length == 4)
        {
            var names = new[] { "many-daily-1", "many-daily-2", "many-latest", "many-recommended" };
            var captions = new[] { "今日のランダムワールド①", "今日のランダムワールド②", "紹介最新ワールド", "おすすめランダムワールド" };
            for (int i = 0; i < 4; i++)
            {
                Photo(g.manyPictures[i], Load(names[i]), true);
                g.manyTitles[i].text = "ワールド名"; g.manyAuthors[i].text = "作者名";
                g.manyCaptions[i].text = captions[i];
            }
        }
        foreach (var label in g.GetComponentsInChildren<Text>(true))
            if (label.text == "ランダム\n全てから") label.text = "ランダム\nワールド";
        g.statusLabel.text = "ワールドをアップロードすると表示されます";
        EditorUtility.SetDirty(g);
    }

    static void Photo(RawImage image, Texture texture, bool crop)
    {
        if (image == null) return;
        image.texture = texture; image.color = Color.white;
        image.uvRect = new Rect(0, 0, 1, 1);
        if (texture == null || !crop || image.rectTransform.rect.height <= 0) return;
        float source = (float)texture.width / texture.height;
        float target = image.rectTransform.rect.width / image.rectTransform.rect.height;
        if (target > source) { float h = source / target; image.uvRect = new Rect(0, (1-h)/2, 1, h); }
        else { float w = target / source; image.uvRect = new Rect((1-w)/2, 0, w, 1); }
    }
}
