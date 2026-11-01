using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using VRC.SDK3.Data;
using VRC.SDK3.StringLoading;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;
using UdonSharpEditor;
class EditorCommand {
static BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
static object G(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
static void S(object o,string n,object v)=>o.GetType().GetField(n,F).SetValue(o,v);
static object C(object o,string n,params object[] a){try{return o.GetType().GetMethod(n,F).Invoke(o,a);}catch(TargetInvocationException e){throw e.InnerException;}}
static List<string> checks=new List<string>();
static void Check(bool b,string n){if(!b)throw new Exception(n);checks.Add(n);}
static string fixture="C:/Users/一生/Documents/Codex/2026-10-25/t/work/v010/fixtures/";
static DataDictionary D(string json){DataToken t;if(!VRCJson.TryDeserializeFromJson(json,out t))throw new Exception("JSON");return t.DataDictionary;}
static string J(DataDictionary d){DataToken t;VRCJson.TrySerializeToJson(d,JsonExportType.Minify,out t);return t.String;}
class Download:IVRCStringDownload {public void StartDownload(){}public void CancelDownload(){}public string Result{get;set;}public byte[] ResultBytes=>System.Text.Encoding.UTF8.GetBytes(Result);public string Error=>"test";public int ErrorCode=>503;public VRCUrl Url{get;set;}public IUdonEventReceiver UdonBehaviour=>null;}
static void Arm(MetaReaWallGuide g,int kind,VRCUrl url){C(g,"V3CancelRequest");C(g,"V3QueueRequest",kind,url,-1,"",-1,0);Check((bool)C(g,"V3TrackStringRequest",url.Get()),"track request "+checks.Count);S(g,"v3RequestInFlight",true);S(g,"v3RequestAttempts",1);}
static object Execute(){
checks.Clear();var scene=EditorSceneManager.NewPreviewScene();var textures=new List<Texture2D>();
try {
var go=new GameObject("stability source");SceneManager.MoveGameObjectToScene(go,scene);var g=UdonSharpUndo.AddComponent<MetaReaWallGuide>(go);
g.isSharedDataSource=true;g.guideV3=true;g.onlineMode=true;g.anonymousStatistics=false;MetaReaOnlineSetup.ConfigureV3Urls(g);
string boot=File.ReadAllText(fixture+"bootstrap.json"), status=File.ReadAllText(fixture+"status-fixture.json");var b=D(boot);int bank=(int)b["bank"].Double;string gen=b["generationId"].String;
g.v3SharedBaseGeneration=gen;g.v3SharedBaseBank=bank;g.v3SharedRecommendedGeneration=gen;g.v3SharedRecommendedBank=bank;g.v3SharedRandomGeneration=gen;g.v3SharedRandomBank=bank;
Check((bool)C(g,"V3ApplyBootstrap",boot),"bootstrap accepted");
C(g,"V3Initialize");float start=Time.realtimeSinceStartup;float first=(float)G(g,"v3NextStringAt");Check(first>start&&first<=start+5.1f,"first request jitter applied");C(g,"V3Initialize");Check((float)G(g,"v3NextStringAt")==first,"reinitialization does not reset jitter");
var d=D(status);double server=d["serverTime"].Double,until=d["validUntil"].Double;
Check((bool)C(g,"V3LoadStatus",d,status),"status accepted");S(g,"v3ServerReceived",Time.realtimeSinceStartup-40f);
double before=(double)C(g,"V3Now");Check((bool)C(g,"V3LoadStatus",d,status),"same unexpired status accepted");Check((double)C(g,"V3Now")>=before,"repeated status does not rewind clock");Check((double)G(g,"v3StatusUntil")==until,"repeated status retains absolute deadline");
Check(!(bool)C(g,"InPeriod",0d,server+20),"ad period does not extend on repeated status");
S(g,"v3ServerTime",until+1);S(g,"v3ServerReceived",Time.realtimeSinceStartup);
Check(!(bool)C(g,"V3LoadStatus",d,status),"expired status replay rejected");Check(!(bool)C(g,"StatusUsable"),"expired advertisement permission fails closed");
Check(!(bool)C(g,"V3ResumeStatus",d,status),"expired status cannot end maintenance");
// A display can apply a validated local snapshot after its HTTP envelope TTL.
var displayObject=new GameObject("delayed display");SceneManager.MoveGameObjectToScene(displayObject,scene);var display=UdonSharpUndo.AddComponent<MetaReaWallGuide>(displayObject);display.isSharedDataSource=true;display.guideV3=true;display.onlineMode=true;display.sharedDataSource=g;
S(display,"v3ServerTime",until+10);S(display,"v3ServerReceived",Time.realtimeSinceStartup);
Check((bool)C(display,"V3ApplyBootstrap",boot),"trusted local snapshot survives short HTTP TTL");
S(g,"v3ServerTime",server);S(g,"v3ServerReceived",Time.realtimeSinceStartup);Check((bool)C(g,"V3LoadStatus",d,status),"fresh test clock restored");
// Timeouts retire their physical URL; another URL can still make progress.
var u1=new VRCUrl("https://example.invalid/current.json");var u2=new VRCUrl("https://example.invalid/status.json");
Arm(g,4,u1);C(g,"V3RequestFailed",false);
Check(!(bool)C(g,"V3TrackStringRequest",u1.Get()),"unresolved URL cannot be issued twice");
Check((bool)C(g,"V3TrackStringRequest",u2.Get()),"different URL remains available while old request drains");
Check(!(bool)C(g,"V3TakeStringResponse",u1.Get()),"late timed-out result is discarded");
Check((bool)C(g,"V3TrackStringRequest",u1.Get()),"URL reusable only after old callback drains");
C(g,"V3AbandonStringRequest");C(g,"V3TakeStringResponse",u1.Get());C(g,"V3TakeStringResponse",u2.Get());C(g,"V3CancelRequest");
for(int i=0;i<8;i++) Check((bool)C(g,"V3TrackStringRequest","https://example.invalid/"+i),"bounded physical slot "+i);
Check(!(bool)C(g,"V3TrackStringRequest","https://example.invalid/overflow"),"outstanding transport requests have a hard cap");
for(int i=0;i<8;i++) C(g,"V3TakeStringResponse","https://example.invalid/"+i);
// Real success handler must reject a bootstrap started by an old owner term.
Arm(g,1,u1);g.v3OwnerTerm++;int version=(int)G(g,"v3Version");C(g,"V3StringSuccess",new Download{Url=u1,Result=boot});
Check((int)G(g,"v3RequestKind")==0&&g.staleResponseCount>0,"old owner bootstrap is discarded before mutation");
Check((string)G(g,"v3BaseGeneration")==gen,"old owner response cannot replace committed generation");
// Overload wait is not a maintenance claim and cannot create an unlimited loop.
Arm(g,4,u2);C(g,"V3StringSuccess",new Download{Url=u2,Result="{\"schema\":\"metarea-guide-wait-v1\",\"waiting\":true,\"retryAfter\":60}"});
float wake=(float)G(g,"v3ServerWaitUntil")-Time.realtimeSinceStartup;
Check(wake>=59&&wake<=70,"server wait honored with jitter");Check(!(bool)G(g,"v3Maintenance"),"overload wait distinct from maintenance");Check(g.serverWaitCount==1,"overload diagnostic recorded");
Check(!(bool)C(g,"V3TelemetryAllowed"),"nonessential telemetry suspended while overloaded");
int counts=g.feedRequestCount+g.statusRequestCount+g.imageRequestCount;C(g,"V3SendRequest");C(g,"V3PumpImages");
Check(counts==g.feedRequestCount+g.statusRequestCount+g.imageRequestCount,"no requests before server wait elapses");
S(g,"v3RequestAttempts",4);C(g,"V3WaitResponse",D("{\"schema\":\"metarea-guide-wait-v1\",\"waiting\":true,\"retryAfter\":60}"));Check((int)G(g,"v3RequestKind")==0,"wait response respects four-attempt transaction cap");
S(g,"v3ServerWaitUntil",0f);S(g,"v3NextStringAt",0f);S(g,"v3ManualRetryAt",0f);S(g,"v3BootstrapStopped",true);
C(g,"V3Retry");S(g,"v3BootstrapStopped",true);S(g,"v3NextStringAt",0f);C(g,"V3Retry");Check((bool)G(g,"v3BootstrapStopped"),"manual retry spam cannot reset failure state");
S(g,"v3ManualRetryAt",0f);S(g,"v3RequestKind",1);S(g,"v3RequestAttempts",4);C(g,"V3RequestFailed",true);C(g,"V3Retry");Check((bool)G(g,"v3BootstrapStopped"),"terminal failures enforce a manual retry cooldown");
// Latest failures stay retryable instead of falsely marking an empty page complete.
S(g,"v3DrawCategory",2);S(g,"v3LatestComplete",false);C(g,"V3FailDraw","fixture failure");Check((bool)G(g,"v3LatestFailed")&&!(bool)G(g,"v3LatestComplete"),"latest failure is not marked complete");
S(g,"v3ManualRetryAt",0f);S(g,"v3NextStringAt",0f);C(g,"V3Retry");Check(!(bool)G(g,"v3LatestFailed"),"latest failure can be retried after cooldown");
// Shared active textures are pinned even when their source selection changed.
var tokens=(string[])G(g,"v3CacheTokens");var ts=(Texture2D[])G(g,"v3CacheTextures");var sizes=(int[])G(g,"v3CacheSizes");var used=(float[])G(g,"v3CacheUsed");
for(int i=0;i<3;i++){ts[i]=new Texture2D(2,2);textures.Add(ts[i]);tokens[i]=(i+1).ToString("x16");sizes[i]=40;used[i]=Time.realtimeSinceStartup-10-i;}
g.residentImageBytes=120;S(g,"v3MemoryLimit",120);
var picture=new GameObject("visible shared image",typeof(RectTransform),typeof(RawImage));SceneManager.MoveGameObjectToScene(picture,scene);picture.transform.SetParent(displayObject.transform);display.detailPicture=picture.GetComponent<RawImage>();display.detailPicture.texture=ts[0];g.sharedDisplays=new[]{display};
Check((bool)C(g,"V3CachePinned",0),"visible texture on another board is pinned");
int evictions=g.imageEvictionCount;Check(!(bool)C(g,"V3EnsureImageRoom",40,false)&&g.imageEvictionCount==evictions,"prefetch never evicts to churn the cache");
Check((bool)C(g,"V3EnsureImageRoom",40,true),"visible demand may evict unused texture");Check(ts[0]!=null&&g.residentImageBytes==80,"eviction preserves active shared texture and updates bytes");
Check(!(bool)C(g,"V3EnsureImageRoom",121,true),"image exceeding budget rejected");
// With every remaining texture visible, memory pressure cannot destroy them.
var pics=new List<RawImage>();for(int i=0;i<3;i++)if(ts[i]!=null){var o=new GameObject("pin"+i,typeof(RectTransform),typeof(RawImage));SceneManager.MoveGameObjectToScene(o,scene);o.transform.SetParent(displayObject.transform);var ri=o.GetComponent<RawImage>();ri.texture=ts[i];pics.Add(ri);}display.rowPictures=pics.ToArray();
Check(!(bool)C(g,"V3EnsureImageRoom",100,true),"all-visible cache refuses over-budget allocation");Check(g.residentImageBytes==80,"pinned cache byte count remains unchanged");
displayObject.SetActive(false);Check((bool)C(g,"V3EnsureImageRoom",100,true),"hidden boards release their residency pins");
// GPU callback identity is protected by serializing readbacks even after timeout.
S(g,"v3ReadbackPending",true);S(g,"v3ImageKey",-1);S(g,"imageRequestInFlight",false);S(g,"v3Verifying",false);S(g,"nextImageRequestAt",0f);
Check(!(bool)C(g,"V3WantImage",bank,gen,1),"late GPU readback blocks overlapping verification");
// Accelerated sustained-use simulation, not a device soak test.
for(int i=0;i<ts.Length;i++) C(g,"V3Evict",i);
S(g,"v3MemoryLimit",120);bool bounded=true;
var reusable=new Texture2D(2,2);textures.Add(reusable);
for(int n=0;n<2000;n++){
 if(!(bool)C(g,"V3EnsureImageRoom",40,true)){bounded=false;break;}
 int free=(int)C(g,"V3FreeCacheSlot");ts[free]=reusable;sizes[free]=40;tokens[free]=(n+10).ToString("x16");used[free]=Time.realtimeSinceStartup-10;g.residentImageBytes+=40;
 if(g.residentImageBytes>120){bounded=false;break;}
}
Check(bounded&&g.residentImageBytes<=120,"2000 simulated cache insertions remain within byte budget");
for(int i=0;i<ts.Length;i++) C(g,"V3Evict",i);
Check(g.residentImageBytes==0,"sustained cache simulation fully releases accounted residency");
S(g,"v3WireUrls",new string[8]);S(g,"v3WireAbandoned",new bool[8]);bool transport=true;
for(int n=0;n<2000;n++){string url="https://example.invalid/loop";if(!(bool)C(g,"V3TrackStringRequest",url)){transport=false;break;}C(g,"V3TakeStringResponse",url);}
Check(transport,"2000 delayed-response drain cycles do not leak transport slots");
S(g,"v3ServerReceived",Time.realtimeSinceStartup-21600f);double afterSixHours=(double)C(g,"V3Now");C(g,"V3AdvanceClock",afterSixHours-21600);
Check((double)C(g,"V3Now")>=afterSixHours&&!(bool)C(g,"StatusUsable"),"six-hour simulated elapsed time cannot revive old advertisement permission");

Check(g.feedRequestCount+g.imageRequestCount+g.statusRequestCount==0,"regressions do not contact production");
Check(!UdonSharp.UdonSharpProgramAsset.AnyUdonSharpScriptHasError(),"Udon compile clean");
Directory.CreateDirectory("C:/Users/一生/Documents/Codex/2026-10-25/t/outputs/stability");File.WriteAllLines("C:/Users/一生/Documents/Codex/2026-10-25/t/outputs/stability/runtime-checks.txt",checks);
return new{count=checks.Count,checks};
} finally {EditorSceneManager.ClosePreviewScene(scene);foreach(var t in textures)if(t!=null)UnityEngine.Object.DestroyImmediate(t);}
}
}
