using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using UdonSharpEditor;

public static class MetaReaHeaderLayout
{
    public static void Apply(MetaReaWallGuide guide)
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play mode first.");
        var canvas = guide.topPanel.GetComponentInParent<Canvas>().transform;
        string ui = MetaReaPrototypeBuilder.Root + "/UI/";
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(ui + "metarea-title-selected.png");
        var material = AssetDatabase.LoadAssetAtPath<Material>(ui + "MetaReaLogoMask.mat");
        if (material == null)
        {
            material = new Material(Shader.Find("MetaRea/UI Logo Alpha"));
            AssetDatabase.CreateAsset(material, ui + "MetaReaLogoMask.mat");
        }
        // Two independent UI objects use separate regions of the original PNG.
        // The alpha-only material permits any tint without multiplying its navy ink.
        guide.brandTitleImage = Logo(canvas, "MetaRea title", texture, material, 48, 35, 299.375f, 50.625f,
            new Rect(335f/1536, 851f/1024, 479f/1536, 81f/1024));
        guide.guideTitleImage = Logo(canvas, "WORLD GUIDE title", texture, material, 355.5f, 40.625f, 394.375f, 45.625f,
            new Rect(827f/1536, 850f/1024, 631f/1536, 73f/1024));
        canvas.Find("Brand").gameObject.SetActive(false);
        canvas.Find("Title").gameObject.SetActive(false);

        var iconTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(ui + "metarea-archive-wordmark.png");
        Rect(canvas, "Official X", 634, 108, 28, 28);
        var iconRect = Rect(canvas, "World Archive", 670, 108, 114, 28);
        var icon = iconRect.GetComponent<RawImage>() ?? iconRect.gameObject.AddComponent<RawImage>();


        icon.texture = iconTexture; icon.uvRect = new Rect(52f/2012, 108f/781, 1933f/2012, 481f/781); icon.material = material; icon.color = guide.archiveIconColor; icon.raycastTarget = true;
        guide.archiveIconImage = icon;
        guide.archiveIconTintMaterial = material;
        guide.xIconImage = canvas.Find("Official X").GetComponent<RawImage>();
        guide.xIconImage.material = material;
        guide.xIconImage.color = guide.xIconColor;
        var button = iconRect.GetComponent<Button>() ?? iconRect.gameObject.AddComponent<Button>();
        button.targetGraphic = icon; button.navigation = new Navigation { mode = Navigation.Mode.None };
        var udon = UdonSharpEditorUtility.GetBackingUdonBehaviour(guide);
        Wire(button, udon, "_ToggleArchive");
        var overlayTransform = canvas.Find("World Archive URL overlay");
        if (overlayTransform == null)
        {
            var clone = Object.Instantiate(guide.xUrlPanel, canvas, false);
            clone.name = "World Archive URL overlay"; overlayTransform = clone.transform;
        }
        guide.archiveUrlPanel = overlayTransform.gameObject;
        guide.archiveUrlField = overlayTransform.Find("Card/X URL").GetComponent<InputField>();
        guide.archiveUrlField.text = guide.archiveUrl;
        var heading = overlayTransform.Find("Card/X heading").GetComponent<Text>();
        heading.text = "MetaRea WORLD ARCHIVE"; heading.fontSize = 27;
        overlayTransform.Find("Card/X help").GetComponent<Text>().text = "ワールドアーカイブのURLです。";
        Wire(overlayTransform.GetComponent<Button>(), udon, "_CloseArchive");
        Wire(overlayTransform.Find("Card/Close").GetComponent<Button>(), udon, "_CloseArchive");
        iconRect.SetAsLastSibling();
        guide.xUrlPanel.transform.SetAsLastSibling(); overlayTransform.SetAsLastSibling();
        overlayTransform.gameObject.SetActive(false);
        guide._ApplyAppearance();
        UdonSharpEditorUtility.CopyProxyToUdon(guide);
        EditorUtility.SetDirty(guide);
    }

    static void Wire(Button button, VRC.Udon.UdonBehaviour udon, string method)
    {
        button.onClick = new Button.ButtonClickedEvent();
        UnityEventTools.AddStringPersistentListener(button.onClick, udon.SendCustomEvent, method);
    }

    static RawImage Logo(Transform parent, string name, Texture texture, Material material, float x, float y, float w, float h, Rect uv)
    {
        var rect = Rect(parent, name, x, y, w, h);
        var image = rect.GetComponent<RawImage>() ?? rect.gameObject.AddComponent<RawImage>();
        image.texture = texture; image.material = material; image.uvRect = uv; image.raycastTarget = false;
        return image;
    }

    static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
    {
        var t = parent.Find(name);
        var r = t == null ? new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>() : (RectTransform)t;
        r.SetParent(parent, false); r.gameObject.layer = parent.gameObject.layer;
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1);
        r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h);
        return r;
    }
}
