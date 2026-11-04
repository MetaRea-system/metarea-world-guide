using System; using System.IO; using System.Linq; using System.Reflection; using System.Collections.Generic;
using UnityEngine; using UnityEditor; using UnityEditor.SceneManagement; using UnityEngine.SceneManagement; using UdonSharpEditor; using VRC.SDKBase;
class EditorCommand {
static BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
static object G(object x,string n)=>x.GetType().GetField(n,F).GetValue(x);
static void S(object x,string n,object v)=>x.GetType().GetField(n,F).SetValue(x,v);
static object C(object x,string n,params object[] a){try{return x.GetType().GetMethod(n,F).Invoke(x,a);}catch(TargetInvocationException e){throw e.InnerException;}}
static List<string> checks=new List<string>();static Scene scene;
static void Check(bool b,string n){if(!b)throw new Exception("CHECK FAILED: "+n);checks.Add(n);}
static MetaReaWallGuide Make(){var go=new GameObject("telemetry fixture");SceneManager.MoveGameObjectToScene(go,scene);var g=UdonSharpUndo.AddComponent<MetaReaWallGuide>(go);g.onlineMode=true;g.anonymousStatistics=true;g.guideV3=false;g.statisticsUrls=MetaReaOnlineSetup.StatisticsUrls(MetaReaOnlineSetup.FeedUrl,"site_1","test");g.feedUrl=new VRCUrl(MetaReaOnlineSetup.FeedUrl);g.recommendedDrawUrl=new VRCUrl(MetaReaOnlineSetup.RecommendedDrawUrl);g.randomDrawUrl=new VRCUrl(MetaReaOnlineSetup.RandomDrawUrl);S(g,"lastFeedAttempt",-100f);S(g,"sponsorStatusStarted",-100f);S(g,"nextStatisticsSend",0f);return g;}
static int Take(MetaReaWallGuide g){S(g,"nextStatisticsSend",0f);return (int)C(g,"TakeStatisticsEvent");}
static object Execute(){checks.Clear();scene=EditorSceneManager.NewPreviewScene();try{
var urls=MetaReaOnlineSetup.StatisticsUrls(MetaReaOnlineSetup.FeedUrl,"world-A_2","test");
Check(urls.Length==12,"all three events and four protocols baked");
foreach(var u in urls){var s=u.Get();Check(s.Contains("schema=2")&&s.Contains("version="+MetaReaOnlineSetup.GuideVersion)&&s.Contains("site=world-A_2")&&s.Contains("environment=test"),"schema2 context "+s.Split(new[]{"event="},StringSplitOptions.None)[1]);}
Check(MetaReaOnlineSetup.StatisticsSite("")=="unknown","blank site normalized unknown");
Check(MetaReaOnlineSetup.StatisticsEnvironment("")=="unknown","blank environment normalized unknown");
foreach(string invalid in new[]{"bad&site=x","日本語","has space",new string('a',65),"a/b"}){bool rejected=false;try{MetaReaOnlineSetup.StatisticsSite(invalid);}catch(ArgumentException){rejected=true;}Check(rejected,"invalid site rejected "+invalid.Length);}
bool badEnvironment=false;try{MetaReaOnlineSetup.StatisticsEnvironment("prod");}catch(ArgumentException){badEnvironment=true;}Check(badEnvironment,"invalid environment rejected");
Check(MetaReaOnlineSetup.StatisticsUrls("https://example.invalid/feed").Length==0,"custom feed has no official telemetry");
var g=Make();C(g,"QueueStatistics",0);C(g,"QueueStatistics",0);Check((int)G(g,"statisticsCount")==1,"board ready coalesced once locally");
C(g,"QueueStatistics",1);C(g,"QueueStatistics",1);C(g,"QueueStatistics",2);Check((int)G(g,"statisticsCount")==4,"accepted operations not coalesced within 60 seconds");
Check(Take(g)==0&&Take(g)==4&&Take(g)==4&&Take(g)==8,"queue preserves event order v2");Check(Take(g)==-1,"drained event not retried");
C(g,"QueueStatistics",1);S(g,"nextStatisticsSend",Time.realtimeSinceStartup+10f);Check((int)C(g,"TakeStatisticsEvent")==-1,"send pacing enforced");Check(Take(g)==4,"queued operation survives pacing");
var source=Make();var display=Make();display.sharedDataSource=source;C(display,"QueueStatistics",0);C(source,"QueueStatistics",0);C(display,"QueueStatistics",1);Check((int)G(source,"statisticsCount")==2&&(int)G(display,"statisticsCount")==0,"shared boards aggregate readiness and operations per client");
source.anonymousStatistics=false;C(display,"QueueStatistics",1);Check((int)G(source,"statisticsCount")==2,"disabled source rejects forwarded request");Check(Take(source)==-1&&(int)G(source,"statisticsCount")==0,"disabled telemetry discards pending queue");
var protocol=Make();protocol.guideV3=true;C(protocol,"QueueStatistics",1);S(protocol,"v3Initialized",true);S(protocol,"v4Protocol",false);C(protocol,"QueueStatistics",1);S(protocol,"v4Protocol",true);C(protocol,"QueueStatistics",1);Check(((int[])G(protocol,"statisticsQueue")).Take(3).SequenceEqual(new[]{7,5,6}),"protocol frozen at event time unknown then v3 then v4");
var capacity=Make();for(int i=0;i<65;i++)C(capacity,"QueueStatistics",1);Check((int)G(capacity,"statisticsCount")==64&&capacity.statisticsDropped==1,"64-entry queue drops overflow with diagnostic");
var expired=Make();C(expired,"QueueStatistics",1);((float[])G(expired,"statisticsQueueTimes"))[0]=Time.realtimeSinceStartup-301f;Check(Take(expired)==-1&&expired.statisticsDropped==1,"events older than 300 seconds expire");
var cap=Make();S(cap,"statisticsSent",128);C(cap,"QueueStatistics",1);Check((int)G(cap,"statisticsCount")==0&&cap.statisticsDropped==1,"128 dispatch session cap enforced");
var offline=Make();offline.onlineMode=false;C(offline,"QueueStatistics",1);Check((int)G(offline,"statisticsCount")==0,"offline mode does not queue telemetry");
var editor=Make();C(editor,"QueueStatistics",1);C(editor,"PumpStatistics");Check(!editor.statisticsRuntimeEnabled&&!(bool)C(editor,"StatisticsRuntimeAllowed")&&(int)G(editor,"statisticsSent")==0&&(int)G(editor,"statisticsCount")==1,"editor cannot dispatch even with queued events");
C(editor,"PumpAdClicks");Check((int)G(editor,"clickSent")==0,"editor cannot dispatch advertising clicks");
// Build-only gate is tested on an isolated scene, never the user's editing scene.
MetaReaBuildAssets.Prepare(scene);Check(editor.statisticsRuntimeEnabled&&!offline.statisticsRuntimeEnabled,"build copy enables online telemetry only");
Check((int)G(editor,"statisticsSent")==0,"build preparation emits no telemetry");
Directory.CreateDirectory("Temp/MetaReaTelemetryChecks");File.WriteAllLines("Temp/MetaReaTelemetryChecks/telemetry-checks.txt",checks);return new{checks=checks.Count,results=checks};
}finally{EditorSceneManager.ClosePreviewScene(scene);}}
}
