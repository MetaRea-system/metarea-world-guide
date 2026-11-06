using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UdonSharpEditor;

public static class MetaReaAdvertisingLayout
{
    public static void Apply(MetaReaWallGuide guide)
    {
        if (guide == null || guide.adPanel == null || guide.topPanel == null) return;
        guide.adDownloadStatus = Label(guide.adPanel.transform,"HQ download status",guide.adTitle.font,360,575,390,24,16);
        guide.adDownloadStatus.alignment = TextAlignmentOptions.Right;
        guide.adQrStatus = Label(guide.adPanel.transform,"QR download status",guide.adTitle.font,664,675,96,96,14);
        guide.adQrStatus.alignment = TextAlignmentOptions.Center;
        guide.initialTopGroup = guide.topPanel.GetComponent<CanvasGroup>();
        if (guide.initialTopGroup == null) guide.initialTopGroup = guide.topPanel.AddComponent<CanvasGroup>();
        var canvas = guide.topPanel.GetComponentInParent<Canvas>();
        var panel = Rect(canvas.transform,"Initial TOP loading",20,267,760,1050);
        var background = panel.GetComponent<Image>();
        if (background == null) background = panel.gameObject.AddComponent<Image>();
        background.color = guide.backgroundColor; background.raycastTarget = true;
        guide.initialLoadingPanel = panel.gameObject;
        guide.initialLoadingLabel = Label(panel,"Status",guide.adTitle.font,20,410,720,120,24);
        guide.initialLoadingLabel.alignment = TextAlignmentOptions.Center;
        guide.initialLoadingLabel.text = "TOPを読み込んでいます…";
        guide.initialLoadingPanel.SetActive(false);
        guide.adDownloadStatus.gameObject.SetActive(false); guide.adQrStatus.gameObject.SetActive(false);
        guide.adTitle.enableAutoSizing = true; guide.adTitle.fontSizeMin = 20; guide.adTitle.fontSizeMax = 28;
        guide.adDescription.fontSize = 18; guide.adDescription.enableAutoSizing = false;
        UdonSharpEditorUtility.CopyProxyToUdon(guide); EditorUtility.SetDirty(guide);
    }
    static RectTransform Rect(Transform parent,string name,float x,float y,float width,float height)
    {
        var child=parent.Find(name);
        var rect=child==null?new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>():child.GetComponent<RectTransform>();
        rect.SetParent(parent,false); rect.gameObject.layer=0;
        rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
        rect.anchoredPosition=new Vector2(x,-y); rect.sizeDelta=new Vector2(width,height); return rect;
    }
    static TMP_Text Label(Transform parent,string name,TMP_FontAsset font,float x,float y,float width,float height,int size)
    {
        var rect=Rect(parent,name,x,y,width,height); var label=rect.GetComponent<TMP_Text>();
        if(label==null)label=rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font=font;label.fontSize=size;label.text="";label.color=new Color(.64f,.77f,.86f);
        label.richText=false;label.raycastTarget=false;label.enableWordWrapping = true;
        label.overflowMode = TextOverflowModes.Truncate;return label;
    }
}

