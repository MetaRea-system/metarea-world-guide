using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using UdonSharpEditor;

public static class MetaReaSocialLayout
{
    public static void Apply(MetaReaWallGuide guide)
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play mode first.");
        var canvas = guide.topPanel.GetComponentInParent<Canvas>().transform;
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(MetaReaPrototypeBuilder.Root + "/UI/metarea-x-orbit.png");
        if (texture == null) throw new System.InvalidOperationException("MetaRea X icon is missing.");
        var udon = UdonSharpEditorUtility.GetBackingUdonBehaviour(guide);
        var iconRect = Rect(canvas, "Official X", 708, 16, 76, 76);
        var icon = iconRect.GetComponent<RawImage>() ?? iconRect.gameObject.AddComponent<RawImage>();
        icon.texture = texture; icon.color = Color.white; icon.raycastTarget = true;
        var open = iconRect.GetComponent<Button>() ?? iconRect.gameObject.AddComponent<Button>();
        open.targetGraphic = icon; open.navigation = new Navigation { mode = Navigation.Mode.None };
        open.onClick = new Button.ButtonClickedEvent();
        UnityEventTools.AddStringPersistentListener(open.onClick, udon.SendCustomEvent, "_ToggleX");

        var overlay = Rect(canvas, "Official X URL overlay", 0, 0, 800, 1350);
        var shade = overlay.GetComponent<Image>() ?? overlay.gameObject.AddComponent<Image>();
        shade.color = new Color(0, 0, 0, .6f); shade.raycastTarget = true;
        var outside = overlay.GetComponent<Button>() ?? overlay.gameObject.AddComponent<Button>();
        outside.targetGraphic = shade; outside.transition = Selectable.Transition.None;
        outside.navigation = new Navigation { mode = Navigation.Mode.None };
        outside.onClick = new Button.ButtonClickedEvent();
        UnityEventTools.AddStringPersistentListener(outside.onClick, udon.SendCustomEvent, "_CloseX");

        var panel = Rect(overlay, "Card", 60, 160, 680, 236);
        var background = panel.GetComponent<Image>() ?? panel.gameObject.AddComponent<Image>();
        background.color = guide.backgroundColor;
        var border = panel.GetComponent<Outline>() ?? panel.gameObject.AddComponent<Outline>();
        border.effectColor = guide.borderColor; border.effectDistance = new Vector2(2, -2);
        var consume = panel.GetComponent<Button>() ?? panel.gameObject.AddComponent<Button>();
        consume.targetGraphic = background; consume.transition = Selectable.Transition.None;
        consume.navigation = new Navigation { mode = Navigation.Mode.None };
        consume.onClick = new Button.ButtonClickedEvent();
        Label(panel, "X heading", "MetaRea の X", guide.detailTitle.font, 24, 20, 540, 42, 29, guide.headingColor);

        var closeRect = Rect(panel, "Close", 606, 12, 58, 58);
        var closeImage = closeRect.GetComponent<Image>() ?? closeRect.gameObject.AddComponent<Image>(); closeImage.color = guide.cardColor;
        var close = closeRect.GetComponent<Button>() ?? closeRect.gameObject.AddComponent<Button>(); close.targetGraphic = closeImage;
        close.navigation = new Navigation { mode = Navigation.Mode.None };
        close.onClick = new Button.ButtonClickedEvent();
        UnityEventTools.AddStringPersistentListener(close.onClick, udon.SendCustomEvent, "_CloseX");
        Label(closeRect, "Label", "×", guide.detailTitle.font, 0, 0, 58, 58, 34, guide.buttonTextColor).alignment = TextAnchor.MiddleCenter;

        var fieldRect = Rect(panel, "X URL", 24, 90, 632, 66);
        var fieldImage = fieldRect.GetComponent<Image>() ?? fieldRect.gameObject.AddComponent<Image>(); fieldImage.color = guide.cardColor;
        var field = fieldRect.GetComponent<InputField>() ?? fieldRect.gameObject.AddComponent<InputField>();
        field.targetGraphic = fieldImage; field.readOnly = true; field.lineType = InputField.LineType.SingleLine;
        field.navigation = new Navigation { mode = Navigation.Mode.None };
        field.textComponent = Label(fieldRect, "Text", "", guide.detailTitle.font, 14, 0, 604, 66, 26, guide.bodyColor);
        field.textComponent.alignment = TextAnchor.MiddleLeft;
        field.text = guide.xProfileUrl;
        Label(panel, "X help", "URLをブラウザーに入力してご覧ください。", guide.detailTitle.font, 24, 178, 632, 30, 20, guide.secondaryColor);
        guide.xUrlPanel = overlay.gameObject; guide.xUrlField = field;
        iconRect.SetAsLastSibling(); overlay.SetAsLastSibling(); overlay.gameObject.SetActive(false);
        UdonSharpEditorUtility.CopyProxyToUdon(guide); EditorUtility.SetDirty(guide);
    }

    static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
    {
        var child = parent.Find(name);
        var rect = child == null ? new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>() : child.GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.gameObject.layer = parent.gameObject.layer;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(w, h);
        return rect;
    }

    static Text Label(Transform parent, string name, string value, Font font, float x, float y, float w, float h, int size, Color color)
    {
        var r = Rect(parent, name, x, y, w, h); var t = r.GetComponent<Text>() ?? r.gameObject.AddComponent<Text>();
        t.font = font; t.fontSize = size; t.text = value; t.color = color; t.raycastTarget = false; t.supportRichText = false;
        t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate;
        return t;
    }
}
