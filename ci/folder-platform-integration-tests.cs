using System; using System.IO; using System.Reflection; using System.Collections.Generic;
using UnityEngine; using UnityEngine.UI; using UnityEditor; using UnityEditor.SceneManagement; using UnityEngine.SceneManagement;
using VRC.SDK3.Data; using VRC.SDK3.StringLoading; using VRC.SDKBase; using VRC.Udon.Common.Interfaces; using UdonSharpEditor;
class EditorCommand {
static BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
static object G(object x,string n)=>x.GetType().GetField(n,F).GetValue(x);
static void S(object x,string n,object v)=>x.GetType().GetField(n,F).SetValue(x,v);
static object C(object x,string n,params object[] a){try{return x.GetType().GetMethod(n,F).Invoke(x,a);}catch(TargetInvocationException e){throw e.InnerException;}}
static List<string> checks=new List<string>(); static List<Texture2D> textures=new List<Texture2D>();
static string fixture="C:/Users/一生/Documents/Codex/2026-10-12/files-mentioned-by-the-user-codex/patch045-work/fixtures/guide-v4/";
static string output="C:/Users/一生/Documents/Codex/2026-10-25/t/outputs/v015/integration";
static DataDictionary boot,status;static string gen;static int bank;static double now;static Scene scene;
static void Check(bool b,string n){if(!b)throw new Exception("CHECK FAILED: "+n);checks.Add(n);}
static DataDictionary D(string json){DataToken t;if(!VRCJson.TryDeserializeFromJson(json,out t))throw new Exception("json");return t.DataDictionary;}
static string J(DataDictionary d){DataToken t;VRCJson.TrySerializeToJson(d,JsonExportType.Minify,out t);return t.String;}
class Download:IVRCStringDownload{public void StartDownload(){}public void CancelDownload(){}public string Result{get;set;}public byte[] ResultBytes=>System.Text.Encoding.UTF8.GetBytes(Result);public string Error=>"fixture";public int ErrorCode=>503;public VRCUrl Url{get;set;}public IUdonEventReceiver UdonBehaviour=>null;}
static void Reply(MetaReaWallGuide g,int kind,string json){C(g,"V3CancelRequest");var url=new VRCUrl("https://example.invalid/"+kind+".json");C(g,"V3QueueRequest",kind,url,-1,"",-1,0);C(g,"V3TrackStringRequest",url.Get());S(g,"v3RequestInFlight",true);S(g,"v3RequestAttempts",1);C(g,"V3StringSuccess",new Download{Url=url,Result=json});}
static MetaReaWallGuide Make(){var go=new GameObject("v4 source");SceneManager.MoveGameObjectToScene(go,scene);var g=UdonSharpUndo.AddComponent<MetaReaWallGuide>(go);g.isSharedDataSource=true;g.onlineMode=true;g.guideV3=true;g.anonymousStatistics=false;g.feedUrl=new VRCUrl(MetaReaOnlineSetup.FeedUrl);g.sponsorStatusUrl=new VRCUrl(MetaReaOnlineSetup.SponsorStatusUrl);MetaReaOnlineSetup.ConfigureV3Urls(g);C(g,"V3Initialize");S(g,"v3NextStringAt",Time.realtimeSinceStartup+10000);S(g,"nextImageRequestAt",Time.realtimeSinceStartup+10000);return g;}
static void Image(MetaReaWallGuide g,int asset,int slot){var a=((DataDictionary[])G(g,"v3Assets"))[bank*32+asset];var t=new Texture2D(2,2,TextureFormat.RGBA32,false);textures.Add(t);string ext=(asset==0||asset>=26&&asset<=30)?"png":"jpg";ImageConversion.LoadImage(t,File.ReadAllBytes(fixture+"banks/0/images/"+asset+"."+ext));((string[])G(g,"v3CacheTokens"))[slot]=a["imageToken"].String;((Texture2D[])G(g,"v3CacheTextures"))[slot]=t;}
static void Capture(MetaReaWallGuide g,string name){
var canvas=g.topPanel.GetComponentInParent<Canvas>();var r=(RectTransform)canvas.transform;var corners=new Vector3[4];r.GetWorldCorners(corners);var center=(corners[0]+corners[2])*.5f;
var go=new GameObject("QA camera");SceneManager.MoveGameObjectToScene(go,scene);var cam=go.AddComponent<Camera>();var rt=new RenderTexture(900,1500,24);var tex=new Texture2D(900,1500,TextureFormat.RGB24,false);var prev=RenderTexture.active;
try{cam.overrideSceneCullingMask=EditorSceneManager.GetSceneCullingMask(scene);cam.transform.position=center-r.forward*5;cam.transform.rotation=Quaternion.LookRotation(r.forward,r.up);cam.orthographic=true;cam.orthographicSize=(corners[1]-corners[0]).magnitude*.53f;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.04f,.05f,.07f);cam.nearClipPlane=.01f;cam.farClipPlane=10;cam.targetTexture=rt;Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,900,1500),0,0);tex.Apply();File.WriteAllBytes(output+"/"+name+".png",tex.EncodeToPNG());}
finally{RenderTexture.active=prev;cam.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}}
static object Execute(){checks.Clear();textures.Clear();Directory.CreateDirectory(output);scene=EditorSceneManager.NewPreviewScene();try{
boot=D(File.ReadAllText(fixture+"current.json"));status=D(File.ReadAllText(fixture+"status.json"));gen=boot["generationId"].String;bank=(int)boot["bank"].Double;now=(DateTime.UtcNow-new DateTime(1970,1,1)).TotalSeconds;
foreach(var d in new[]{boot,status}){d["serverTime"]=new DataToken(now);d["validUntil"]=new DataToken(now+240);}
boot["expiresAtUnix"]=new DataToken(0d);boot["retiredAtUnix"]=new DataToken(0d);
var gs=status["generations"].DataList;for(int i=0;i<gs.Count;i++)gs[i].DataDictionary["expiresAtUnix"]=new DataToken(0d);
// Synthetic advertising periods are all open for the UI fixtures.
foreach(var d in new[]{boot,status})foreach(string key in new[]{"ads","topPlacements","sponsoredWorlds"})if(d.ContainsKey(key)){var list=d[key].DataList;for(int i=0;i<list.Count;i++){list[i].DataDictionary["startsAtUnix"]=new DataToken(0d);list[i].DataDictionary["endsAtUnix"]=new DataToken(0d);}}
var g=Make();Check((bool)G(g,"v4Protocol"),"new installation selects v4 URL set");Check(g.v3ImageUrls[0].Get().Contains("guide-v4"),"bank images use matching protocol");
Reply(g,1,J(boot));Check(!(bool)G(g,"ready")&&(int)G(g,"v4PendingBank")==bank,"bootstrap alone does not expose partial TOP");
Reply(g,7,J(status));Check(!(bool)G(g,"ready"),"publication status alone does not expose partial TOP");
Image(g,0,0);C(g,"V4CommitBootstrap");Check((bool)G(g,"ready")&&(bool)G(g,"v4TopReady"),"verified initial image and status commit TOP atomically");
Check((int)G(g,"adCount")==16,"all 16 unified ads including world/campaign are parsed");
Check(((string[])G(g,"v3InitialTopIds")).Length==6&&((string[])G(g,"v3InitialBannerIds")).Length==3,"six cards and three banners are reserved");
Check(((int[])G(g,"drawCounts"))[0]==8&&((int[])G(g,"drawCounts"))[1]==8&&(int)G(g,"v3LatestCount")==8,"each initial list has two complete pages");
var cards=new HashSet<int>();var banners=new HashSet<int>();
for(int round=0;round<3;round++){int l=(int)G(g,"sponsorLeft"),r=(int)G(g,"sponsorRight"),b=(int)G(g,"currentAd");Check(l>=0&&r>=0&&l!=r,"distinct visible cards round "+round);Check(cards.Add(l)&&cards.Add(r),"initial card cycle has no repeats round "+round);Check(banners.Add(b),"initial banner cycle has no repeats round "+round);if(round<2)C(g,"V4AdvanceSponsors");}
Check(cards.Count==6&&banners.Count==3,"first cycle covers all initial cards and banners");
int initialAd=(int)G(g,"currentAd");Check((bool)C(g,"V4DetailReady",initialAd),"initial banner detail is ready without HQ");
var ads=(string[])G(g,"adIds");int noninitial=-1;for(int i=0;i<16;i++)if(!(bool)C(g,"V4DetailReady",i)){noninitial=i;break;}Check(noninitial>=0,"noninitial ads initially await detail images");
Check(!(bool)C(g,"V4Eligible",false,noninitial),"unprepared additional ad cannot enter rotation");
Image(g,30,1);Image(g,29,2);Image(g,26,3);Check((bool)C(g,"V4DetailReady",noninitial),"additional preview satisfies detail-ready condition");
var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(MetaReaAssetsEntry.PrefabPath),scene);
var u=Array.Find(root.GetComponentsInChildren<VRC.Udon.UdonBehaviour>(true),x=>UdonSharpEditorUtility.GetUdonSharpBehaviourType(x)==typeof(MetaReaWallGuide));var display=(MetaReaWallGuide)UdonSharpEditorUtility.GetProxyBehaviour(u);
display.sharedDataSource=g;display.onlineMode=true;display.guideV3=true;display.anonymousStatistics=false;g.sharedDisplays=new[]{display};C(display,"InitializeDisplayMode");C(display,"V3Pull");C(display,"V4UpdateUi");
Check(!display.initialLoadingPanel.activeSelf&&display.initialTopGroup.alpha==1,"ready shared display reveals TOP together");for(int i=0;i<2;i++){
var photo=display.sponsoredWorldPictures[i].rectTransform;var title=display.sponsoredWorldTitles[i];
Check(Mathf.Abs(photo.rect.width-356)<.01f&&Mathf.Abs(photo.rect.height-200.25f)<.01f,"wide 16:9 card dimensions "+i);
Check(Mathf.Abs(photo.rect.width/photo.rect.height-16f/9f)<.001f,"card aspect retained "+i);
Check(-photo.anchoredPosition.y+photo.rect.height < -title.rectTransform.anchoredPosition.y,"title below image "+i);
Check(-title.rectTransform.anchoredPosition.y+title.rectTransform.rect.height<=252,"title fits unchanged card "+i);
}
Check(((RectTransform)display.sponsorBannerButton.transform).anchoredPosition.y==-1135,"bottom banner position unchanged");
Check(((string)C(display,"FitSingleLine",display.sponsoredWorldTitles[0],"あいうえおかきくけこさしすせそたちつてと",8)).EndsWith("…"),"long Japanese title ellipsis");
Check(display.sponsoredWorldPictures[0].texture==((Texture2D[])G(g,"v3CacheTextures"))[0],"enlarged card still reads first sheet");
display.initialDisplayMode=1;C(display,"InitializeDisplayMode");C(display,"Render");
for(int i=0;i<4;i++){
var p=display.manyPictures[i].rectTransform;var t=display.manyTitles[i].rectTransform;var summary=display.manySummaries[i].rectTransform;
Check(Mathf.Abs(p.rect.height-138)<.01f,"four-card photo height "+i);
Check(-p.anchoredPosition.y+p.rect.height<=-display.manyCaptions[i].rectTransform.anchoredPosition.y,"caption does not overlap photo "+i);
Check(-t.anchoredPosition.y+t.rect.height<=-summary.anchoredPosition.y,"title does not overlap summary "+i);
Check(-summary.anchoredPosition.y+summary.rect.height<=-display.manyAuthors[i].rectTransform.anchoredPosition.y,"summary does not overlap author "+i);
}
Capture(display,"01-top");
var measure=display.manyTitles[0];
string[] samples={"Short world","Late Night Movie DREAM - 真夜中の映画館",new string('i',90),new string('W',90),new string('あ',40),"English と 日本語が混ざるタイトルの表示を確認します","<b>literal</b> title",""};
var measured=new List<string>();foreach(string sample in samples){
measure.text=sample;float full=measure.preferredWidth;
string result=(string)C(display,"FitSingleLine",measure,sample,0);measure.text=result;
Check(measure.preferredWidth<=350.01f,"measured line fits "+measured.Count);
if(full<=350)Check(result==sample,"fitting text preserved "+measured.Count);
else{
Check(result.EndsWith("…"),"overflow uses ellipsis "+measured.Count);
int end=result.Length-1;bool longerFits=false;
for(int n=end+1;n<sample.Length;n++){measure.text=sample.Substring(0,n)+"…";if(measure.preferredWidth<=350)longerFits=true;}
Check(!longerFits,"longest fitting prefix "+measured.Count);
}
measure.text=result;measured.Add(result);
Check((string)C(display,"FitSingleLine",measure,sample,0)==result,"cache repeat "+measured.Count);
}
Check(measured[2].Length>measured[3].Length,"narrow i fits more than wide W");
Check(measured[1].Length>17,"mixed English title uses space beyond old character cap");
measure.rectTransform.sizeDelta=new Vector2(180,30);
string narrow=(string)C(display,"FitSingleLine",measure,samples[1],0);measure.text=narrow;
Check(measure.preferredWidth<=178.01f&&narrow.Length<measured[1].Length,"width change invalidates cache");
measure.rectTransform.sizeDelta=new Vector2(352,30);measure.fontSize=16;
string small=(string)C(display,"FitSingleLine",measure,samples[1],0);measure.text=small;
Check(measure.preferredWidth<=350.01f&&small.Length>=measured[1].Length,"font size change invalidates cache");
measure.fontSize=20;
string unicode="日本語😀e\u0301と絵文字を含む長いタイトルをここに追加して省略を確認";
for(int width=1;width<120;width+=3){
measure.rectTransform.sizeDelta=new Vector2(width,30);string clipped=(string)C(display,"FitSingleLine",measure,unicode,0);measure.text=clipped;
Check(measure.preferredWidth<=Mathf.Max(0,width-2)+.01f,"small width "+width);
if(clipped.EndsWith("…")&&clipped.Length>1)Check(!char.IsHighSurrogate(clipped[clipped.Length-2]),"surrogate intact "+width);
}
measure.rectTransform.sizeDelta=new Vector2(352,30);
C(display,"Render");
File.WriteAllLines(output+"/width-examples.txt",measured);

string[] examples={"桜の花びらが舞う湖畔でゆっくり過ごすワールド","忘風の神殿","Late Night Movie DREAM - 真夜中の映画館","Past times"};
string[] summaries={"桜満開の自然豊かな和風ワールド","水上に建つ遺跡群のワールド","映画の中に入って体験する落ち着いた雰囲気の美しいワールド","光り輝く惑星と地球のワールド"};
for(int i=0;i<4;i++){
display.manyTitles[i].text=(string)C(display,"FitSingleLine",display.manyTitles[i],examples[i],i);
display.manySummaries[i].text=(string)C(display,"FitSingleLine",display.manySummaries[i],summaries[i],4+i);
display.manyAuthors[i].text="by 作者名";
var texture=new Texture2D(2,2);textures.Add(texture);ImageConversion.LoadImage(texture,File.ReadAllBytes("C:/Users/一生/Documents/Codex/2026-10-25/t/work/downloaded-photos/preview-"+i+".png"));
display.manyPictures[i].texture=texture;
float crop=138f/356f*((float)texture.width/texture.height);
display.manyPictures[i].uvRect=new Rect(0,(1-crop)/2,1,crop);
}
Check(display.manyTitles[0].text.EndsWith("…"),"long world title ellipsis");
Check(display.manySummaries[2].text.EndsWith("…"),"long summary ellipsis");
Canvas.ForceUpdateCanvases();
for(int i=0;i<4;i++){
var title=display.manyTitles[i];Check(title.preferredHeight<=title.rectTransform.rect.height+.1f,"single-line Japanese title fits "+i);
var summary=display.manySummaries[i];Check(summary.preferredHeight<=summary.rectTransform.rect.height+.1f,"single-line Japanese summary fits "+i);
}
Capture(display,"02-layout-preview");
int savedSponsors=(int)G(display,"sponsoredCount");
S(display,"sponsoredCount",0);C(display,"RenderMany");
for(int i=0;i<4;i++){
var cap=display.manyCaptions[i].rectTransform;var title=display.manyTitles[i].rectTransform;var summary=display.manySummaries[i].rectTransform;
Check(Mathf.Abs(-title.anchoredPosition.y-(-cap.anchoredPosition.y+cap.rect.height)-10)<.01f,"ad-free caption/title 10px "+i);
Check(Mathf.Abs(-summary.anchoredPosition.y-(-title.anchoredPosition.y+title.rect.height)-10)<.01f,"ad-free title/summary 10px "+i);
Check(display.manyPictures[i].rectTransform.rect.height==230 && display.manyTitles[i].fontSize==20,"ad-free image and font preserved "+i);
Check(display.manyAuthors[i].rectTransform.anchoredPosition.y==-358,"ad-free author remains at bottom "+i);
}
for(int i=0;i<4;i++)Check(-display.manySummaries[i].rectTransform.anchoredPosition.y+display.manySummaries[i].rectTransform.rect.height<=-display.manyAuthors[i].rectTransform.anchoredPosition.y,"enlarged image text fits above author "+i);
Capture(display,"03-no-ads-spacing");
S(display,"sponsoredCount",savedSponsors);C(display,"RenderMany");
for(int i=0;i<4;i++)Check(display.manyPictures[i].rectTransform.rect.height==138 && display.manyTitles[i].rectTransform.anchoredPosition.y==-172 && display.manySummaries[i].rectTransform.anchoredPosition.y==-204 && display.manyAuthors[i].rectTransform.anchoredPosition.y==-228,"advertising layout unchanged after switch "+i);

// Exercise actual local/owner acceptance paths against a ready v4 fixture.
g.anonymousStatistics=true;display.anonymousStatistics=true;g.statisticsUrls=MetaReaOnlineSetup.StatisticsUrls(MetaReaOnlineSetup.FeedUrl,"fixture","test");
S(g,"statisticsCount",0);S(g,"statisticsHead",0);S(g,"statisticsBoardSeen",false);
C(display,"QueueBoardReady");C(display,"QueueBoardReady");Check((int)G(g,"statisticsCount")==1,"ready board counted once across repeated renders");
S(g,"statisticsCount",0);S(g,"statisticsHead",0);
int portalWorld=-1;for(int i=0;i<(int)G(display,"baseWorldCount");i++)if((bool)C(display,"CanUseWorld",i)){portalWorld=i;break;}
Check(portalWorld>=0,"fixture provides a valid portal world");
S(display,"portalCooldownStarted",false);S(display,"localPortalRequestTime",Time.realtimeSinceStartup-2f);C(display,"RequestPortal",portalWorld);
Check((int)G(g,"statisticsCount")==2,"normal portal records local and owner acceptance");Check(((int[])G(g,"statisticsQueue"))[0]==6&&((int[])G(g,"statisticsQueue"))[1]==10,"v4 local and owner events are distinct");
C(display,"RequestPortal",portalWorld);Check((int)G(g,"statisticsCount")==2,"cooldown rejects duplicate portal without an event");C(display,"ApplyPortal");Check((int)G(g,"statisticsCount")==2,"portal rendering does not duplicate owner event");
S(display,"portalCooldownStarted",false);C(display,"_DrawPortalOnOwner","invalid-world","Invalid");Check((int)G(g,"statisticsCount")==2,"invalid owner request excluded");
string validId=((string[])G(display,"ids"))[portalWorld];C(display,"_DrawPortalOnOwner",validId,"Fixture draw");Check((int)G(g,"statisticsCount")==3,"draw owner acceptance counted");
display.favoritesEnabled=true;display.favoriteWorldIds=new[]{validId};display.favoriteWorldTitles=new[]{"Fixture favorite"};S(display,"favoriteFolder",0);C(display,"BuildFavorites");S(display,"view",4);S(display,"portalCooldownStarted",false);S(display,"localPortalRequestTime",Time.realtimeSinceStartup-2f);C(display,"RequestFavorite",0);Check((int)G(g,"statisticsCount")==5,"favorite request and owner acceptance both counted");
C(display,"RequestFavorite",0);Check((int)G(g,"statisticsCount")==5,"favorite cooldown duplicate excluded");
C(g,"PumpStatistics");Check((int)G(g,"statisticsSent")==0,"fixture never sends production telemetry");
g.anonymousStatistics=false;display.anonymousStatistics=false;display.sharedPortalWorldId="";S(display,"view",0);S(display,"portalCooldownStarted",false);C(display,"Render");
int left=(int)G(display,"sponsorLeft");C(display,"OpenSponsored",left);C(display,"V4UpdateUi");int ad=(int)G(display,"openedAd");Check((int)G(display,"view")==3&&ad>=0,"card opens common advertising detail");Check(display.adPicture.texture==((Texture2D[])G(g,"v3CacheTextures"))[0],"click immediately paints initial-sheet preview");
Check(display.adDownloadStatus.text.StartsWith("高画質版DL待機中"),"unstarted HQ request is labelled waiting");Capture(display,"02-preview-waiting");
var detail=((DataDictionary[])G(display,"v3DetailImages"))[ad];int asset=(int)detail["assetId"].Double;S(g,"v3ImageKey",bank*32+asset);S(g,"v3ImageGeneration",gen);C(display,"V4UpdateUi");Check(display.adDownloadStatus.text.StartsWith("高画質版DL中"),"active HQ request is labelled downloading");Capture(display,"03-preview-downloading");
S(g,"v3ImageKey",-1);((int[])G(g,"v3ImageAttempts"))[bank*32+asset]=4;C(display,"V4UpdateUi");Check(display.adDownloadStatus.text=="高画質版を再取得予定","exhausted HQ attempts do not animate fake download");
Check(display.adPicture.texture!=null,"preview survives HQ failure");Image(g,asset,4);C(display,"Render");C(display,"V4UpdateUi");Check(display.adDownloadStatus.text==""&&display.adPicture.texture==((Texture2D[])G(g,"v3CacheTextures"))[4],"verified HQ replaces preview and clears label");Capture(display,"04-high-quality");
var back=(RectTransform)display.adPanel.transform.Find("Ad back");var portal=(RectTransform)display.adPortalButton.transform;
Check(!display.adPortalButton.gameObject.activeSelf || back.anchoredPosition.x+back.sizeDelta.x<=portal.anchoredPosition.x,"portal and back controls never overlap");
Check(display.adTitle.gameObject.activeSelf&&display.adDescription.gameObject.activeSelf,"title and description are live text outside preview");
S(display,"view",0);C(display,"Render");C(display,"V4UpdateUi");Check(!display.adDownloadStatus.gameObject.activeSelf&&!display.adQrStatus.gameObject.activeSelf,"closing details hides download status");
// Direct world cards request a portal; the banner still provides the detail screen.
int worldCard=-1;var types=(int[])G(display,"topTypes");for(int i=0;i<(int)G(display,"topCount");i++)if(types[i]==0){worldCard=i;break;}
Check(worldCard>=0,"fixture includes a world-only card");S(display,"portalCooldownStarted",false);S(display,"localPortalRequestTime",Time.realtimeSinceStartup-2f);C(display,"OpenSponsored",worldCard);Check((int)G(display,"view")==0&&!string.IsNullOrEmpty(display.sharedPortalWorldId),"world-only card directly requests a portal");
S(display,"currentAd",((int[])G(display,"topAds"))[worldCard]);display._Ad();Check((int)G(display,"view")==3,"banner can still open the world advertisement detail");
if((bool)C(display,"TopPortalAvailable")){
g.anonymousStatistics=true;display.anonymousStatistics=true;int prior=(int)G(g,"statisticsCount");S(display,"portalCooldownStarted",false);S(display,"localPortalRequestTime",Time.realtimeSinceStartup-2f);C(display,"_AdPortal");Check((int)G(g,"statisticsCount")==prior+2,"advertising portal records local and owner acceptance");g.anonymousStatistics=false;display.anonymousStatistics=false;
}else throw new Exception("Advertising portal fixture unavailable");
((string[])G(g,"v3CacheTokens"))[3]="";C(display,"Render");C(display,"V4UpdateUi");Check(display.adQrStatus.text.StartsWith("QR読込待機中"),"missing QR is labelled waiting independently of preview");Check(display.adPicture.texture!=null,"QR wait does not hide detail image");Capture(display,"05-qr-waiting");
// Availability is independent of cached pixels.
var stopped=D(J(status));stopped["ads"]=new DataToken(new DataList());stopped["topPlacements"]=new DataToken(new DataList());Check((bool)C(g,"V3LoadStatus",stopped,J(stopped)),"empty live allowlist accepted");C(g,"RefreshSponsorAvailability");Check((int)G(g,"currentAd")==-1&&(int)G(g,"sponsorLeft")==-1,"stopped ads disappear despite cached entrance images");
// Fresh session fallback cannot mix v4 metadata and v3 image URLs.
var fallback=Make();S(fallback,"v3RequestKind",1);S(fallback,"v3RequestAttempts",4);C(fallback,"V3RequestFailed",false);Check(!(bool)G(fallback,"v4Protocol")&&fallback.v3ImageUrls[0].Get().Contains("guide-v3"),"unavailable v4 falls back to a complete v3 URL set");
var probe=Make();for(int i=0;i<4;i++){S(probe,"v3RequestKind",5);S(probe,"v3RequestAttempts",1);C(probe,"V3RequestFailed",false);}Check(!(bool)G(probe,"v4Protocol"),"four independent follower probes permit v3 fallback");
var follower=Make();follower.sharedGuideProtocol=3;C(follower,"V3ReceiveSelection");Check(!(bool)G(follower,"v4Protocol")&&follower.v3ImageUrls[0].Get().Contains("guide-v3"),"follower adopts owner protocol before ready");
follower.sharedGuideProtocol=4;C(follower,"V3ReceiveSelection");Check((bool)G(follower,"v4Protocol")&&follower.v3ImageUrls[0].Get().Contains("guide-v4"),"follower can align to v4 owner before ready");
var expired=Make();Reply(expired,1,J(boot));S(expired,"v4PendingUntil",1d);C(expired,"V4CommitBootstrap");Check((int)G(expired,"v4PendingBank")==-1&&!(bool)G(expired,"ready"),"expired pending bootstrap cannot expose TOP");
var superseded=Make();Reply(superseded,1,J(boot));S(superseded,"v3SharedBaseGeneration","0000000000000006");C(superseded,"V4CommitBootstrap");Check((int)G(superseded,"v4PendingBank")==-1&&!(bool)G(superseded,"ready"),"superseded shared generation cannot commit stale TOP");
Check(g.feedRequestCount+g.statusRequestCount+g.imageRequestCount==0,"fixtures issue no production downloads");
var loadingSource=Make();display.sharedDataSource=loadingSource;S(display,"ready",false);S(display,"v4TopReady",false);S(display,"view",0);C(display,"V3Pull");C(display,"V4UpdateUi");Check(display.initialLoadingPanel.activeSelf&&display.initialTopGroup.alpha==0,"initial loading covers TOP until complete publication");Capture(display,"06-initial-loading");
File.WriteAllLines(output+"/checks.txt",checks);return new{checks=checks.Count,results=checks,output};
}finally{EditorSceneManager.ClosePreviewScene(scene);foreach(var t in textures)UnityEngine.Object.DestroyImmediate(t);}}
}
