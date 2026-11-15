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
        guide.initialLoadingLabel = Label(panel,"Status",guide.adTitle.font,20,245,720,500,24);
        guide.initialLoadingLabel.alignment = TextAlignmentOptions.Center;
        guide.initialLoadingLabel.richText = true;
        guide.initialLoadingLabel.enableAutoSizing = false;
        guide.initialLoadingLabel.lineSpacing = 8;
        guide.initialLoadingLabel.text = "<size=28>TOPを読み込んでいます…</size>\n\n" + MetaReaWallGuide.InitialLoadingGuidance;
        guide.initialLoadingPanel.SetActive(false);
        guide.adDownloadStatus.gameObject.SetActive(false); guide.adQrStatus.gameObject.SetActive(false);
        var overlay=Rect(canvas.transform,"Poster detail overlay",0,155,800,958);
        var fill=overlay.GetComponent<Image>();if(fill==null)fill=overlay.gameObject.AddComponent<Image>();
        fill.color=guide.backgroundColor;fill.raycastTarget=true;
        overlay.SetAsLastSibling();guide.posterOverlay=overlay.gameObject;
        var detail=(RectTransform)guide.adPanel.transform;detail.SetParent(overlay,false);
        detail.anchorMin=detail.anchorMax=detail.pivot=new Vector2(0,1);
        detail.anchoredPosition=new Vector2(20,0);detail.sizeDelta=new Vector2(760,950);
        var disclosure=detail.Find("Disclosure") as RectTransform;
        if(disclosure!=null){disclosure.anchorMin=disclosure.anchorMax=disclosure.pivot=new Vector2(0,1);disclosure.anchoredPosition=Vector2.zero;disclosure.sizeDelta=new Vector2(80,24);}
        guide.sponsorUrlField.lineType=TMP_InputField.LineType.SingleLine;
        guide.sponsorUrlField.readOnly=true;
        guide.posterUrlText=guide.sponsorUrlField.textComponent;
        var inputRect=(RectTransform)guide.sponsorUrlField.transform; inputRect.anchorMin=inputRect.anchorMax=inputRect.pivot=new Vector2(0,1);
        var viewport=Rect(inputRect,"Viewport",0,0,650,46); viewport.anchorMin=Vector2.zero;viewport.anchorMax=Vector2.one;viewport.offsetMin=new Vector2(10,0);viewport.offsetMax=new Vector2(-10,0);
        if(viewport.GetComponent<RectMask2D>()==null)viewport.gameObject.AddComponent<RectMask2D>();
        guide.sponsorUrlField.textComponent.transform.SetParent(viewport,false);guide.sponsorUrlField.textViewport=viewport;
        guide.sponsorUrlField.textComponent.enableWordWrapping=false;
        guide.sponsorUrlField.textComponent.richText=false;
        var frameRect=Rect(detail,"Poster frame background",0,0,760,838);
        var frame=frameRect.GetComponent<Image>();if(frame==null)frame=frameRect.gameObject.AddComponent<Image>();
        frame.color=new Color(11f/255f,21f/255f,38f/255f,1f);frame.raycastTarget=false;
        int pictureIndex=guide.adPicture.transform.GetSiblingIndex(); if(frameRect.GetSiblingIndex()<pictureIndex)pictureIndex--; frameRect.SetSiblingIndex(pictureIndex);
        guide.posterFrameBackground=frame;frame.gameObject.SetActive(false);
        guide.adDownloadStatus.alignment=TextAlignmentOptions.Center;
        var noticeRect=Rect(detail,"Poster loading background",160,380,440,88);
        var noticeBackground=noticeRect.GetComponent<Image>();
        if(noticeBackground==null)noticeBackground=noticeRect.gameObject.AddComponent<Image>();
        noticeBackground.color=new Color(.015f,.04f,.065f,.9f);noticeBackground.raycastTarget=false;
        guide.posterNoticeBackground=noticeBackground;noticeBackground.gameObject.SetActive(false);
        guide.adTitle.gameObject.SetActive(false);guide.adDescription.gameObject.SetActive(false);
        guide.adSponsor.gameObject.SetActive(false);guide.sponsorQrPicture.gameObject.SetActive(false);
        guide.sponsorLinkHelp.gameObject.SetActive(false);guide.posterOverlay.SetActive(false);
        if(guide.xUrlPanel!=null)guide.xUrlPanel.transform.SetAsLastSibling();
        if(guide.archiveUrlPanel!=null)guide.archiveUrlPanel.transform.SetAsLastSibling();
        if(guide.maintenancePanel!=null)guide.maintenancePanel.transform.SetAsLastSibling();
        if(guide.maintenanceNotice!=null)guide.maintenanceNotice.transform.SetAsLastSibling();
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

