using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Events;
using UdonSharpEditor;

public static class MetaReaSponsorLayout
{
    static Font font;
    static Color ink = new Color(.88f,.94f,1f);
    static Color muted = new Color(.64f,.77f,.86f);
    static Color cyan = new Color(.18f,.73f,.95f);

    public static void Apply(MetaReaWallGuide g)
    {
        font = g.adTitle.font;
        var udon = UdonSharpEditorUtility.GetBackingUdonBehaviour(g);
        var panel = Rect(g.topPanel.transform,"Sponsored worlds",0,585,760,270);
        g.sponsoredWorldPanel = panel.gameObject;
        Label(panel,"Heading","PR",0,0,760,32,21,muted);
        g.sponsoredWorldButtons = new Button[2]; g.sponsoredWorldPictures = new RawImage[2]; g.sponsoredWorldTitles = new Text[2];
        g.sponsorWorldFades = new CanvasGroup[2];
        g.sponsorGoldFrames = new Image[2];
        for(int i=0;i<2;i++)
        {
            var card=Rect(panel,"Sponsor world "+i,i*388,38,372,220);
            var bg=card.GetComponent<Image>() ?? card.gameObject.AddComponent<Image>(); bg.color=new Color(.022f,.064f,.11f);
            var button=card.GetComponent<Button>();
            if(button==null) { button=card.gameObject.AddComponent<Button>(); UnityEventTools.AddStringPersistentListener(button.onClick,udon.SendCustomEvent,"_SponsoredWorld"+i); }
            g.sponsoredWorldButtons[i]=button;
            var fade=card.GetComponent<CanvasGroup>(); if(fade==null)fade=card.gameObject.AddComponent<CanvasGroup>(); g.sponsorWorldFades[i]=fade;
            g.sponsorWorldFades[i].alpha=1; g.sponsorWorldFades[i].interactable=true; g.sponsorWorldFades[i].blocksRaycasts=true;
            g.sponsoredWorldPictures[i]=Photo(card,"Photo",8,8,356,133);
            g.sponsoredWorldTitles[i]=Label(card,"Name","",10,150,352,60,24,ink);
            g.sponsoredWorldTitles[i].resizeTextForBestFit=true;
            g.sponsoredWorldTitles[i].resizeTextMinSize=20;
            g.sponsoredWorldTitles[i].resizeTextMaxSize=24;
            var frame = Rect(card,"Gold frame",0,0,372,220);
            var frameImage = frame.GetComponent<Image>() ?? frame.gameObject.AddComponent<Image>();
            frameImage.color = Color.clear; frameImage.raycastTarget = false;
            for (int edge=0; edge<4; edge++)
            {
                var border=Rect(frame,"Edge "+edge,edge==3?369:0,edge==1?217:0,edge<2?372:3,edge<2?3:220);
                var edgeImage=border.GetComponent<Image>() ?? border.gameObject.AddComponent<Image>();
                edgeImage.color=new Color(.9f,.68f,.23f); edgeImage.raycastTarget=false;
            }
            g.sponsorGoldFrames[i]=frameImage; frame.gameObject.SetActive(false);
        }
        panel.gameObject.SetActive(false);
        Place(g.adPicture.rectTransform,0,58,760,641.25f);
        g.sponsorQrPicture=Photo(g.adPanel.transform,"Sponsor QR",664,710,96,96);
        g.adTitle.gameObject.SetActive(false);
        g.adDescription.gameObject.SetActive(false);
        g.adSponsor.gameObject.SetActive(false);
        var inputRect=Rect(g.adPanel.transform,"Sponsor URL",0,710,650,96);
        var surface=inputRect.GetComponent<Image>() ?? inputRect.gameObject.AddComponent<Image>(); surface.color=new Color(.04f,.12f,.19f);
        var input=inputRect.GetComponent<InputField>() ?? inputRect.gameObject.AddComponent<InputField>();
        input.textComponent=Label(inputRect,"Text","",10,8,630,56,19,ink);
        input.readOnly=true; input.lineType=InputField.LineType.MultiLineNewline; input.navigation=new Navigation { mode=Navigation.Mode.None };
        g.sponsorUrlField=input;
        g.sponsorLinkHelp=Label(g.adPanel.transform,"URL help","URLをブラウザーに入力、または右のQRを読み取ってください。",10,776,630,25,16,muted);
        Place(g.adSponsor.rectTransform,20,718,720,30);
        g.sponsorBannerButton=g.bannerPicture.transform.parent.GetComponent<Button>();
        g.sponsorBannerFade=g.sponsorBannerButton.GetComponent<CanvasGroup>(); if(g.sponsorBannerFade==null)g.sponsorBannerFade=g.sponsorBannerButton.gameObject.AddComponent<CanvasGroup>();
        g.sponsorBannerFade.alpha=1; g.sponsorBannerFade.interactable=true; g.sponsorBannerFade.blocksRaycasts=true;
        Place((RectTransform)g.sponsorBannerButton.transform,20,1190,760,760f/6f);
        Place(g.bannerPicture.rectTransform,0,0,760,760f/6f);
        g.bannerTitle.gameObject.SetActive(false);
        var brand=g.bannerPicture.transform.parent.Find("Brand");
        if(brand!=null) brand.gameObject.SetActive(false);
        var disclosure=g.sponsorBannerButton.transform.parent.Find("Sponsored");
        if(disclosure!=null) { g.sponsorDisclosure=disclosure.gameObject; Place((RectTransform)disclosure,20,1158,760,30); var label=disclosure.GetComponent<Text>(); if(label!=null)label.fontSize=18; }
        UdonSharpEditorUtility.CopyProxyToUdon(g);
        EditorUtility.SetDirty(g);
    }

    static void Place(RectTransform r,float x,float y,float w,float h)
    { r.anchoredPosition=new Vector2(x,-y); r.sizeDelta=new Vector2(w,h); }
    static RectTransform Rect(Transform p,string name,float x,float y,float w,float h)
    {
        var existing=p.Find(name);
        var r=existing==null?new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>():existing.GetComponent<RectTransform>();
        r.gameObject.layer=0; r.SetParent(p,false); r.anchorMin=r.anchorMax=new Vector2(0,1); r.pivot=new Vector2(0,1); Place(r,x,y,w,h); return r;
    }
    static Text Label(Transform p,string n,string text,float x,float y,float w,float h,int size,Color color)
    {
        var r=Rect(p,n,x,y,w,h); var t=r.GetComponent<Text>() ?? r.gameObject.AddComponent<Text>();
        t.font=font;t.fontSize=size;t.text=text;t.color=color;t.supportRichText=false;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;
        return t;
    }
    static RawImage Photo(Transform p,string n,float x,float y,float w,float h)
    { var r=Rect(p,n,x,y,w,h); var im=r.GetComponent<RawImage>() ?? r.gameObject.AddComponent<RawImage>(); im.raycastTarget=false; return im; }
}

