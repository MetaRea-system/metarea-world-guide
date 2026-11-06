using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class MetaReaPhotoOverlay
{
    public static void Apply(MetaReaWallGuide g)
    {
        if (g.manyPictures == null || g.manyPictures.Length != 4) return;
        const string path = "Packages/net.metarea.worldguide/Core/UI/PhotoCaptionRounded.asset";
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null)
        {
            texture = new Texture2D(32,32,TextureFormat.RGBA32,false);
            texture.name = "Photo Caption Rounded";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            AssetDatabase.CreateAsset(texture,path);
        }
        for (int y=0;y<32;y++) for(int x=0;x<32;x++)
        {
            float dx=Mathf.Max(Mathf.Abs(x+.5f-16f)-10f,0f);
            float dy=Mathf.Max(Mathf.Abs(y+.5f-16f)-10f,0f);
            float alpha=Mathf.Clamp01(6f-Mathf.Sqrt(dx*dx+dy*dy));
            texture.SetPixel(x,y,new Color(1,1,1,alpha));
        }
        texture.Apply();
        EditorUtility.SetDirty(texture);
        Sprite sprite=null;
        foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(path)) if(asset is Sprite) sprite=(Sprite)asset;
        if(sprite==null){sprite=Sprite.Create(texture,new Rect(0,0,32,32),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(8,8,8,8));sprite.name="Rounded Caption";AssetDatabase.AddObjectToAsset(sprite,path);}
        g.manyPhotoGradients = new Image[4];
        g.manyTitleBackplates = new Image[4];
        g.manyCaptionShadows = new Shadow[4];
        g.manyTitleShadows = new Shadow[4];
        for (int i=0;i<4;i++)
        {
            var photo=g.manyPictures[i];
            var existing=photo.transform.parent.Find("Photo Caption Gradient");
            var go=existing != null ? existing.gameObject : new GameObject("Photo Caption Gradient",typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage));
            go.transform.SetParent(photo.transform.parent,false);
            var image=Backplate(go,sprite);
            var rect=image.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
            go.transform.SetSiblingIndex(photo.transform.GetSiblingIndex()+1);
            var titleExisting=photo.transform.parent.Find("Title Backplate");
            var titleGo=titleExisting!=null?titleExisting.gameObject:new GameObject("Title Backplate",typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage));
            titleGo.transform.SetParent(photo.transform.parent,false);
            var titleImage=Backplate(titleGo,sprite);
            titleImage.rectTransform.anchorMin=titleImage.rectTransform.anchorMax=titleImage.rectTransform.pivot=new Vector2(0,1);
            titleGo.transform.SetSiblingIndex(go.transform.GetSiblingIndex()+1);
            g.manyTitleBackplates[i]=titleImage;titleGo.SetActive(false);
            g.manyCaptions[i].transform.SetAsLastSibling();
            g.manyTitles[i].transform.SetAsLastSibling();
            g.manyPhotoGradients[i]=image;
            g.manyCaptionShadows[i]=AddShadow(g.manyCaptions[i]);
            g.manyTitleShadows[i]=AddShadow(g.manyTitles[i]);
            go.SetActive(false);
        }
    }
    static Image Backplate(GameObject go,Sprite sprite)
    {
        var old=go.GetComponent<RawImage>();if(old!=null)Object.DestroyImmediate(old);
        var image=go.GetComponent<Image>();if(image==null)image=go.AddComponent<Image>();
        image.sprite=sprite;image.type=Image.Type.Sliced;image.color=new Color(0,0,0,.58f);image.raycastTarget=false;
        return image;
    }
    static Shadow AddShadow(TMP_Text text)
    {
        var shadow=text.GetComponent<Shadow>();
        if(shadow==null)shadow=text.gameObject.AddComponent<Shadow>();
        shadow.effectColor=new Color(0,0,0,.4f);shadow.effectDistance=new Vector2(1,-1);
        shadow.useGraphicAlpha=true;shadow.enabled=false;
        return shadow;
    }
}

