using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UdonSharp;
using UdonSharpEditor;
using VRC.Udon;
using VRC.SDK3.Components;
using Newtonsoft.Json.Linq;

public static class MetaReaPrototypeBuilder
{
    public const string Root = "Packages/net.metarea.worldguide/Runtime";
    public const string ScenePath = Root + "/Scenes/MetaReaPrototype.unity";
    static Font font;
    static MetaReaWallGuide guide;
    static UdonBehaviour udon;
    static Color Ink = new Color(.88f,.94f,1f);
    static Color Muted = new Color(.64f,.77f,.86f);
    static Color Navy = new Color(.016f,.036f,.068f);
    static Color Panel = new Color(.022f,.064f,.11f);
    static Color Cyan = new Color(.18f,.73f,.95f);

    // Developer utility only; scene generation is not exposed in the install menu.
    public static void Create()
    {
        if (EditorApplication.isPlaying) throw new Exception("Stop Play mode first.");
        var original=SceneManager.GetActiveScene();
        if (original.isDirty) throw new Exception("Save the current scene before creating the prototype.");
        if (File.Exists(ScenePath) && (original.path != ScenePath || GameObject.Find("MetaRea Wall Guide") != null))
            throw new Exception("Prototype scene already exists. Open it instead of overwriting it.");
        Directory.CreateDirectory(Root+"/Scenes");
        Directory.CreateDirectory(Root+"/Generated");
        Directory.CreateDirectory(Root+"/Prefabs");
        if (!File.Exists(ScenePath) && !EditorSceneManager.SaveScene(original,ScenePath,true)) throw new Exception("Cannot copy source scene.");
        var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
        var program=AssetDatabase.LoadAssetAtPath<UdonSharpProgramAsset>(Root+"/Runtime/MetaReaWallGuide.asset");
        if(program==null){program=ScriptableObject.CreateInstance<UdonSharpProgramAsset>();program.sourceCsScript=AssetDatabase.LoadAssetAtPath<MonoScript>(Root+"/Runtime/MetaReaWallGuide.cs");AssetDatabase.CreateAsset(program,Root+"/Runtime/MetaReaWallGuide.asset");AssetDatabase.SaveAssets();}
        UdonSharpProgramAsset.CompileAllCsPrograms(true);
        if (UdonSharpProgramAsset.AnyUdonSharpScriptHasError()) throw new Exception("Resolve Udon compile errors before creating the scene.");
        font=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Fonts/NotoSansCJKjp-Regular.otf");
        if(font==null)throw new Exception("Japanese font is missing.");
        var root=new GameObject("MetaRea Wall Guide");
        root.transform.position=new Vector3(0,0,1.6f);
        guide=root.AddUdonSharpComponent<MetaReaWallGuide>();
        udon=UdonSharpEditorUtility.GetBackingUdonBehaviour(guide);
        guide.sampleFeed=AssetDatabase.LoadAssetAtPath<TextAsset>(Root+"/Data/prototype-feed.json");
        var data=JObject.Parse(guide.sampleFeed.text);
        guide.samplePictures=new Texture[8];
        guide.pictureWorldIds=new string[8];
        for(int i=0;i<8;i++)
        {
            string path=Root+"/Pictures/world-"+i+".jpg";
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer!=null){importer.maxTextureSize=1024;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Compressed;importer.SaveAndReimport();}
            guide.samplePictures[i]=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            guide.pictureWorldIds[i]=(string)data["worlds"][i]["id"];
        }

        var metal=Material("Frame",new Color(.08f,.12f,.17f));
        var wallMat=Material("Wall",new Color(.12f,.13f,.14f));
        var glow=Material("Cyan",Cyan,true);
        Cube("Thin frame",root.transform,new Vector3(0,.774f,.015f),new Vector3(.777f,1.286f,.035f),metal,false);
        Cube("Top cyan trim",root.transform,new Vector3(0,1.416f,-.008f),new Vector3(.755f,.0025f,.004f),glow,false).SetActive(false);
        Cube("Bottom cyan trim",root.transform,new Vector3(0,.131f,-.008f),new Vector3(.755f,.0025f,.004f),glow,false).SetActive(false);
        var canvasObject=new GameObject("Wall newspaper",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster),typeof(VRCUiShape),typeof(BoxCollider));
        canvasObject.layer=0;
        var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;
        var cr=canvasObject.GetComponent<RectTransform>();cr.SetParent(root.transform,false);cr.pivot=new Vector2(.5f,0);cr.sizeDelta=new Vector2(800,1350);cr.localScale=Vector3.one*.000925f;cr.localPosition=new Vector3(0,.15f,-.01f);
        var col=canvasObject.GetComponent<BoxCollider>();col.size=new Vector3(800,1350,8);col.center=new Vector3(0,675,0);
        var bg=canvasObject.AddComponent<Image>();bg.color=Navy;
        Label(cr,"Brand","MetaRea",0,5,800,52,32,Cyan,TextAnchor.MiddleCenter);
        Label(cr,"Title","WORLD GUIDE",0,52,800,62,40,Ink,TextAnchor.MiddleCenter);
        Label(cr,"Tagline","Discover Worlds Worth Visiting.",0,111,800,29,18,Muted,TextAnchor.MiddleCenter);
        Label(cr,"AccentLeft","◇",20,24,60,70,45,Cyan,TextAnchor.MiddleCenter);
        Label(cr,"AccentRight","◇",720,24,60,70,45,Cyan,TextAnchor.MiddleCenter);
        string[] navNames={"TOP\nトップ","おすすめ\nピックアップ","LATEST\n紹介最新順","ランダム\nワールド"};
        string[] navEvents={"_Top","_Recommended","_Latest","_Random"};
        guide.navImages=new Image[4];
        for(int i=0;i<4;i++)guide.navImages[i]=Button(cr,"Navigation "+i,navNames[i],20+i*191,156,185,92,navEvents[i],24).GetComponent<Image>();

        var content=Rect(cr,"Content",20,267,760,858);
        var top=Rect(content,"TOP",0,0,760,858);guide.topPanel=top.gameObject;
        Label(top,"Section","DAILY DISCOVERY",0,0,760,34,22,Muted);
        guide.topTitles=new Text[2];guide.topSummaries=new Text[2];guide.topCaptions=new Text[2];guide.topPictures=new RawImage[2];guide.topButtons=new Button[2];
        for(int i=0;i<2;i++)
        {
            var b=Button(top,"Daily "+i,"",0,44+i*401,760,388,i==0?"_DailyOne":"_DailyTwo");guide.topButtons[i]=b;
            var r=(RectTransform)b.transform;
            guide.topPictures[i]=Photo(r,"Photo",6,6,748,218);
            guide.topCaptions[i]=Label(r,"Caption",i==0?"今日のランダムワールド①":"今日のランダムワールド②",20,231,720,32,21,Cyan);
            guide.topTitles[i]=Label(r,"Name","読込待ち",20,268,704,51,28,Ink);
            guide.topTitles[i].resizeTextForBestFit=true;guide.topTitles[i].resizeTextMinSize=20;guide.topTitles[i].resizeTextMaxSize=28;
            guide.topSummaries[i]=Label(r,"Summary","",20,322,704,55,21,Muted);
        }

        var list=Rect(content,"List",0,0,760,858);guide.listPanel=list.gameObject;
        guide.sectionLabel=Label(list,"Section","LATEST WORLDS",0,0,500,40,22,Muted);
        guide.reshuffleButton=Button(list,"Shuffle","再抽選",620,0,140,40,"_Reshuffle",21);
        guide.rows=new GameObject[4];guide.rowTitles=new Text[4];guide.rowSummaries=new Text[4];guide.rowPictures=new RawImage[4];
        for(int i=0;i<4;i++)
        {
            var b=Button(list,"World row "+i,"",0,51+i*176,760,165,"_Row"+i);guide.rows[i]=b.gameObject;
            var r=(RectTransform)b.transform;
            guide.rowPictures[i]=Photo(r,"Photo",6,6,260,153);
            guide.rowTitles[i]=Label(r,"Name","",285,10,415,66,26,Ink);
            guide.rowTitles[i].resizeTextForBestFit=true;guide.rowTitles[i].resizeTextMinSize=20;guide.rowTitles[i].resizeTextMaxSize=26;
            guide.rowSummaries[i]=Label(r,"Summary","",285,83,415,69,20,Muted);
            Label(r,"Arrow","›",711,48,38,65,43,Cyan,TextAnchor.MiddleCenter);
        }
        guide.previousPage=Button(list,"Previous page","‹",190,771,78,66,"_PreviousPage",37);
        guide.pageLabel=Label(list,"Page","1 / 2",280,771,200,66,26,Ink,TextAnchor.MiddleCenter);
        guide.nextPage=Button(list,"Next page","›",492,771,78,66,"_NextPage",37);

        var detail=Rect(content,"Detail",0,0,760,858);guide.detailPanel=detail.gameObject;
        guide.detailPicture=Photo(detail,"Large photo",0,0,760,428);
        guide.detailTitle=Label(detail,"Name","",10,441,740,79,34,Ink,TextAnchor.MiddleCenter);
        guide.detailTitle.resizeTextForBestFit=true;guide.detailTitle.resizeTextMinSize=24;guide.detailTitle.resizeTextMaxSize=34;
        // A scrollable description has a fixed viewport, so buttons never move.
        var viewport=Rect(detail,"Description viewport",16,530,728,114);var mask=viewport.gameObject.AddComponent<RectMask2D>();
        var text=Label(viewport,"Description","",0,0,728,114,23,Muted,TextAnchor.UpperCenter);
        text.verticalOverflow=VerticalWrapMode.Overflow;
        var fitter=text.gameObject.AddComponent<ContentSizeFitter>();fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=text.rectTransform;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;
        var scrollSurface=viewport.gameObject.AddComponent<Image>();scrollSurface.color=new Color(0,0,0,.001f);
        guide.detailDescription=text;
        guide.platformNotice=Label(detail,"Platform notice","",5,650,750,48,18,new Color(1,.79f,.4f),TextAnchor.MiddleCenter);
        guide.previousWorld=Button(detail,"Previous world","◀ 前へ",0,707,195,77,"_PreviousWorld",23);
        guide.portalButton=Button(detail,"Portal","PORTAL\nこの世界へ",207,696,346,98,"_Portal",29,true);
        guide.portalButtonLabel=guide.portalButton.GetComponentInChildren<Text>();
        guide.nextWorld=Button(detail,"Next world","次へ ▶",565,707,195,77,"_NextWorld",23);
        guide.returnLabel=Button(detail,"Back","一覧に戻る",0,807,760,50,"_Back",23).GetComponentInChildren<Text>();

        var ad=Rect(content,"Advertisement detail",0,0,760,858);guide.adPanel=ad.gameObject;
        Label(ad,"Disclosure","PR",0,0,760,40,22,Muted);
        guide.adPicture=Photo(ad,"Ad photo",0,50,760,428);
        guide.adTitle=Label(ad,"Ad title","",15,493,730,80,31,Ink,TextAnchor.MiddleCenter);
        guide.adDescription=Label(ad,"Ad description","",20,590,720,115,22,Muted,TextAnchor.MiddleCenter);
        guide.adSponsor=Label(ad,"Sponsor","",20,710,720,36,19,Muted,TextAnchor.MiddleCenter);
        guide.adPortalButton=Button(ad,"Ad portal","PORTAL  この世界へ",130,753,500,62,"_AdPortal",25,true);
        guide.adPortalLabel=guide.adPortalButton.GetComponentInChildren<Text>();
        Button(ad,"Ad back","元の画面に戻る",0,823,760,35,"_AdBack",21);

        Image(cr,"Ad divider",0,1148,800,2,new Color(.32f,.43f,.51f));
        Label(cr,"Sponsored","PR",20,1162,760,28,18,Muted);
        var banner=Button(cr,"Sponsored banner","",20,1199,760,115,"_Ad");
        guide.bannerPicture=Photo((RectTransform)banner.transform,"Banner image",5,5,202,105);
        guide.bannerTitle=Label((RectTransform)banner.transform,"Title","次に行く世界を、ここから。",227,13,488,50,26,Ink);
        Label((RectTransform)banner.transform,"Brand","MetaRea WORLD ARCHIVE  ›",227,68,488,30,20,Cyan);
        guide.statusLabel=Label(cr,"Status","試作データを読み込みます",20,1320,565,25,15,Muted);
        guide.retryButton=Button(cr,"Retry","再試行",622,1318,158,28,"_Retry",17);

        var anchor=new GameObject("Portal Position - 移動して位置と向きを調整");anchor.transform.SetParent(root.transform,false);anchor.transform.localPosition=new Vector3(-1.3f,0,-.45f);guide.portalAnchor=anchor.transform;
        var portal=new GameObject("Shared Portal",typeof(BoxCollider),typeof(VRCPortalMarker));portal.transform.SetParent(anchor.transform,false);
        var trigger=portal.GetComponent<BoxCollider>();trigger.isTrigger=true;trigger.size=new Vector3(1,2,1);trigger.center=Vector3.up;
        guide.portalMarker=portal.GetComponent<VRCPortalMarker>();guide.portalMarker.useDefaultPresentation=true;
        portal.SetActive(false);
        var preview=new GameObject("Editor-only Portal Preview");preview.transform.SetParent(anchor.transform,false);guide.editorPortalPreview=preview;
        var ring=new GameObject("Ring",typeof(MeshFilter),typeof(MeshRenderer));ring.transform.SetParent(preview.transform,false);ring.transform.localPosition=Vector3.up*.85f;
        ring.GetComponent<MeshFilter>().sharedMesh=RingMesh();ring.GetComponent<MeshRenderer>().sharedMaterial=glow;
        var portalCanvas=new GameObject("Destination label",typeof(RectTransform),typeof(Canvas));portalCanvas.transform.SetParent(preview.transform,false);portalCanvas.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
        var pr=(RectTransform)portalCanvas.transform;pr.sizeDelta=new Vector2(650,160);pr.localScale=Vector3.one*.001f;pr.localPosition=new Vector3(0,1.88f,0);
        guide.portalDestinationLabel=Label(pr,"World","共有ポータル",0,0,650,100,32,Ink,TextAnchor.MiddleCenter);
        Label(pr,"Preview disclaimer","Editor Preview / 実移動はVRChatで確認",0,105,650,50,20,Cyan,TextAnchor.MiddleCenter);
        preview.SetActive(false);

        var room=new GameObject("Prototype room - 配布Prefabには含まれません");
        Cube("Display wall",room.transform,new Vector3(0,.85f,1.68f),new Vector3(1.2f,1.7f,.12f),wallMat,true);
        var floor=GameObject.Find("Floor");if(floor!=null)floor.GetComponent<Renderer>().sharedMaterial=Material("Floor",new Color(.18f,.19f,.21f));
        var camera=Camera.main;if(camera!=null){camera.transform.position=new Vector3(.05f,1.08f,.1f);camera.transform.LookAt(new Vector3(-.18f,.82f,1.6f));camera.fieldOfView=65;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.065f,.075f,.09f);}
        var descriptor=UnityEngine.Object.FindObjectOfType<VRCSceneDescriptor>();if(descriptor!=null)descriptor.transform.position=new Vector3(0,0,-.5f);
        guide.portalCooldownSeconds=5f;guide.secondSlotIsChill=false;
        MetaReaSponsorLayout.Apply(guide);
        MetaReaPresentationLayout.Apply(guide);
        MetaReaOnlineSetup.Configure(guide,MetaReaOnlineSetup.FeedUrl,MetaReaOnlineSetup.AtlasTemplate);
        UdonSharpEditorUtility.CopyProxyToUdon(guide);
        UdonSharpProgramAsset.CompileAllCsPrograms(true);
        PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/MetaReaWallGuide.prefab");
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject=root;
        if(SceneView.lastActiveSceneView!=null)SceneView.lastActiveSceneView.LookAt(new Vector3(-.3f,.9f,1.5f),Quaternion.Euler(5,0,0),2.2f);
        Debug.Log("MetaRea prototype created. Height: 1.25m, lower edge: 0.15m; avatar reference: 1.2m.");
    }

    static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
    {
        var go=new GameObject(name,typeof(RectTransform));go.layer=0;
        var r=(RectTransform)go.transform;r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;
    }
    static Image Image(Transform p,string n,float x,float y,float w,float h,Color c)
    {var r=Rect(p,n,x,y,w,h);var im=r.gameObject.AddComponent<Image>();im.color=c;im.raycastTarget=false;return im;}
    static Text Label(Transform p,string n,string value,float x,float y,float w,float h,int size,Color color,TextAnchor align=TextAnchor.UpperLeft)
    {var r=Rect(p,n,x,y,w,h);var t=r.gameObject.AddComponent<Text>();t.font=font;t.fontSize=size;t.text=value;t.color=color;t.alignment=align;t.supportRichText=false;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;return t;}
    static RawImage Photo(Transform p,string n,float x,float y,float w,float h)
    {var r=Rect(p,n,x,y,w,h);var image=r.gameObject.AddComponent<RawImage>();image.color=Color.white;image.raycastTarget=false;return image;}
    static Button Button(Transform p,string n,string label,float x,float y,float w,float h,string method,int size=24,bool primary=false)
    {
        var r=Rect(p,n,x,y,w,h);var im=r.gameObject.AddComponent<Image>();im.color=primary?new Color(0,.43f,.66f):Panel;
        var outline=r.gameObject.AddComponent<Outline>();outline.effectColor=primary?Cyan:new Color(.11f,.28f,.4f);outline.effectDistance=new Vector2(1,-1);
        var b=r.gameObject.AddComponent<Button>();b.targetGraphic=im;var colors=b.colors;colors.highlightedColor=new Color(1.15f,1.15f,1.15f);colors.pressedColor=new Color(.7f,.85f,1);colors.disabledColor=new Color(.4f,.4f,.4f,.6f);b.colors=colors;
        if(!string.IsNullOrEmpty(label))Label(r,"Label",label,4,2,w-8,h-4,size,Ink,TextAnchor.MiddleCenter);
        UnityEventTools.AddStringPersistentListener(b.onClick,udon.SendCustomEvent,method);return b;
    }
    static Material Material(string name,Color color,bool unlit=false)
    {
        var mat=new Material(Shader.Find(unlit?"Unlit/Color":"Standard"));mat.name=name;mat.color=color;
        if(!unlit){mat.SetFloat("_Metallic",.12f);mat.SetFloat("_Glossiness",.3f);}
        AssetDatabase.CreateAsset(mat,Root+"/Generated/"+name+".mat");return mat;
    }
    static GameObject Cube(string name,Transform p,Vector3 pos,Vector3 scale,Material mat,bool collider)
    {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(p,false);go.transform.localPosition=pos;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=mat;if(!collider)UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());return go;}
    static Mesh RingMesh()
    {
        const int segments=64;var vertices=new Vector3[segments*2];var triangles=new int[segments*6];
        for(int i=0;i<segments;i++){float a=i*Mathf.PI*2/segments;vertices[i*2]=new Vector3(Mathf.Cos(a)*.46f,Mathf.Sin(a)*.79f,0);vertices[i*2+1]=new Vector3(Mathf.Cos(a)*.43f,Mathf.Sin(a)*.76f,0);int j=(i+1)%segments;int t=i*6;triangles[t]=i*2;triangles[t+1]=j*2+1;triangles[t+2]=j*2;triangles[t+3]=i*2;triangles[t+4]=i*2+1;triangles[t+5]=j*2+1;}
        var mesh=new Mesh();mesh.name="Portal preview ring";mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();AssetDatabase.CreateAsset(mesh,Root+"/Generated/PortalRing.asset");return mesh;
    }
}

