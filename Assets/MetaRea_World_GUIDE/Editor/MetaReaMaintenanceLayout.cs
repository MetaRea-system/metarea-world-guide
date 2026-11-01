using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class MetaReaMaintenanceLayout
{
    static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
    {
        var child = parent.Find(name);
        if (child == null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer; go.transform.SetParent(parent,false); child = go.transform;
        }
        var r = (RectTransform)child;
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0,1);
        r.anchoredPosition = new Vector2(x,-y); r.sizeDelta = new Vector2(w,h);
        return r;
    }
    static Text Label(Transform parent,string name,float x,float y,float w,float h,int size,Font font,Color color,string value)
    {
        var r=Rect(parent,name,x,y,w,h);var t=r.GetComponent<Text>();if(t==null)t=r.gameObject.AddComponent<Text>();
        t.font=font;t.fontSize=size;t.alignment=TextAnchor.MiddleCenter;t.color=color;t.text=value;
        t.supportRichText=false;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;
        t.resizeTextForBestFit=false;return t;
    }
    static Image Background(RectTransform rect,Color color,Color border)
    {
        var image=rect.GetComponent<Image>();if(image==null)image=rect.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=true;
        var outline=rect.GetComponent<Outline>();if(outline==null)outline=rect.gameObject.AddComponent<Outline>();outline.effectColor=border;outline.effectDistance=new Vector2(1,-1);return image;
    }
    public static void Apply(MetaReaWallGuide g)
    {
        if(g==null||g.topPanel==null||g.statusLabel==null||g.sponsorBannerButton==null)return;
        var canvas=g.statusLabel.transform.parent;var content=(RectTransform)g.topPanel.transform.parent;
        var banner=(RectTransform)g.sponsorBannerButton.transform;
        float top=-content.anchoredPosition.y, bottom=-banner.anchoredPosition.y+banner.rect.height;
        float height=bottom-top;
        var full=Rect(canvas,"Maintenance waiting",content.anchoredPosition.x,top,content.rect.width,height);
        Background(full,g.backgroundColor,g.borderColor);
        var font=g.topTitles[0].font;
        g.maintenanceMessage=Label(full,"Message",30,height*.27f,content.rect.width-60,height*.30f,34,font,g.headingColor,"ただいまメンテナンス中です。\n５分程度で終了します。\nしばらくお待ちください。");
        g.maintenanceMessage.lineSpacing=1.5f;
        g.maintenanceHint=Label(full,"Help",35,height*.64f,content.rect.width-70,100,22,font,g.secondaryColor,"お気に入りワールドは引き続き利用できます。\n終了後、自動で読み込みを再開します。");
        var line=Rect(full,"Divider",80,height*.61f,content.rect.width-160,1);var stroke=line.GetComponent<Image>();if(stroke==null)stroke=line.gameObject.AddComponent<Image>();stroke.color=g.borderColor;stroke.raycastTarget=false;
        var notice=Rect(canvas,"Maintenance notice",banner.anchoredPosition.x,-banner.anchoredPosition.y,banner.rect.width,banner.rect.height);
        Background(notice,g.cardColor,g.borderColor);
        g.maintenanceNoticeText=Label(notice,"Message",16,8,banner.rect.width-32,banner.rect.height-16,22,font,g.bodyColor,g.maintenanceMessage.text);
        g.maintenanceNoticeText.lineSpacing=1.1f;
        g.maintenancePanel=full.gameObject;g.maintenanceNotice=notice.gameObject;
        full.SetAsLastSibling();notice.SetAsLastSibling();
        full.gameObject.SetActive(false);notice.gameObject.SetActive(false);
        EditorUtility.SetDirty(g);
    }
}
