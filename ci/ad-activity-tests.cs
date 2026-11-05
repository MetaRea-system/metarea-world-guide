using System;using System.IO;using System.Reflection;using System.Collections.Generic;using UnityEngine;using UnityEditor;
class EditorCommand {
static BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
static object G(object x,string n)=>x.GetType().GetField(n,F).GetValue(x);static void S(object x,string n,object v)=>x.GetType().GetField(n,F).SetValue(x,v);static object C(object x,string n,params object[] a)=>x.GetType().GetMethod(n,F).Invoke(x,a);
static List<string> ok=new List<string>();static void Check(bool b,string s){if(!b)throw new Exception(s);ok.Add(s);}
static string feed=File.ReadAllText("ci/ad-fixtures/feed.json"), status=File.ReadAllText("ci/ad-fixtures/status.json");
static void Setup(MetaReaWallGuide g){g.onlineMode=true;g.guideV3=false;g.atlasUrls=new VRC.SDKBase.VRCUrl[4096];g.sharedDataSource=g;g.portalClickUrls=MetaReaOnlineSetup.PortalClickUrls(MetaReaOnlineSetup.FeedUrl,"test");g.statisticsUrls=MetaReaOnlineSetup.StatisticsUrls(MetaReaOnlineSetup.FeedUrl);g.clickUrls=MetaReaOnlineSetup.ClickUrls(MetaReaOnlineSetup.FeedUrl);C(g,"Parse",feed);C(g,"ApplySponsorStatus",status);}
static object Execute(){var root=PrefabUtility.LoadPrefabContents("Packages/net.metarea.worldguide/Core/Internal/MetaRea World GUIDE.prefab");GameObject otherRoot=null;
try{var g=root.GetComponent<MetaReaWallGuide>();Setup(g);S(g,"currentAd",0);g._Ad();
Check((int)G(g,"clickCount")==1,"first detail view queued");
Check(((int[])G(g,"clickQueueOrigins"))[0]==1,"first source attributed to banner");
C(g,"OpenSponsored",0);Check((int)G(g,"clickCount")==1,"same advertisement across TOP and banner deduplicated");
((int[])G(g,"adClickSlots"))[0]=100;((int[])G(g,"topClickSlots"))[0]=100;
((string[])G(g,"adKeys"))[0]=new string('b',64);((string[])G(g,"topKeys"))[0]=new string('b',64);
C(g,"QueueAdClick","top0",new string('b',64),100,0);Check((int)G(g,"clickCount")==1,"slot and key change does not bypass advertisement ID dedupe");
var times=(float[])G(g,"clickLastTimes");times[0]=Time.realtimeSinceStartup-1799;C(g,"QueueAdClick","top0",new string('b',64),100,0);Check((int)G(g,"clickCount")==1,"29m59s suppressed");
times[0]=Time.realtimeSinceStartup-1800;C(g,"QueueAdClick","top0",new string('b',64),100,0);Check((int)G(g,"clickCount")==2,"30-minute boundary permits new view");
Check(((int[])G(g,"clickQueueOrigins"))[1]==0,"new window attributed to first TOP view");
C(g,"Render");C(g,"NextDetailPrefetchSlot");Check((int)G(g,"clickCount")==2,"render and prefetch do not count");
C(g,"QueueAdClick","top15",new string('a',64),15,0);Check((int)G(g,"clickCount")==2,"direct world portal is not a detail view");
((int[])G(g,"adWorlds"))[1]=-1;S(g,"currentAd",1);g._Ad();Check((int)G(g,"clickCount")==2,"invalid world not counted");
((int[])G(g,"adDetailCells"))[2]=-1;S(g,"currentAd",2);g._Ad();Check((int)G(g,"clickCount")==2,"missing detail not counted");
g.anonymousStatistics=false;C(g,"QueueAdClick","top3",new string('a',64),3,0);Check((int)G(g,"clickCount")==2,"OFF suppresses queue");C(g,"TakeAdClick");Check((int)G(g,"clickCount")==0,"OFF clears unsent queue");g.anonymousStatistics=true;
Setup(g);S(g,"clickLastAdIds",new string[128]);S(g,"clickCount",0);S(g,"clickHead",0);
otherRoot=PrefabUtility.LoadPrefabContents("Packages/net.metarea.worldguide/Core/Internal/MetaRea World GUIDE.prefab");var other=otherRoot.GetComponent<MetaReaWallGuide>();Setup(other);g.sharedDataSource=null;other.sharedDataSource=g;
S(other,"currentAd",0);other._Ad();C(other,"OpenSponsored",0);Check((int)G(g,"clickCount")==1&&(int)G(other,"clickCount")==0,"multiple boards use one local dedupe record");
S(g,"imageFailed",true);S(g,"sponsorImageFailed",true);S(g,"extraSponsorFailed",new[]{true,true,true,true,true,true,true});S(g,"pendingDrawTab",-1);S(g,"lastFeedAttempt",-999f);S(g,"sponsorStatusStarted",-999f);S(g,"drawStartedAt",-999f);
Check((int)C(g,"TakeAdClick")==1,"request URL preserves first banner attribution");
C(g,"QueueAdClick","top3",new string('a',64),3,0);Check((int)C(g,"TakeAdClick")==-1,"send spacing retained");
S(g,"nextClickSend",0f);((float[])G(g,"clickQueueTimes"))[1]=Time.realtimeSinceStartup-121;Check((int)C(g,"TakeAdClick")==-1&&(int)G(g,"clickCount")==0,"expired event discarded without retry");
C(g,"QueueAdClick","top4",new string('a',64),4,0);((bool[])G(g,"topClickAllowed"))[4]=false;Check((int)C(g,"TakeAdClick")==-1,"revoked status drops queued event");
S(g,"clickSent",128);C(g,"QueueAdClick","top5",new string('a',64),5,0);Check((int)G(g,"clickCount")==0,"per-stay cap enforced");S(g,"clickSent",0);S(g,"clickCount",32);C(g,"QueueAdClick","top5",new string('a',64),5,0);Check((int)G(g,"clickCount")==32,"bounded queue");
Check((bool)C(g,"IsAdClickResponse","https://metarea.net/api/udon/ad-click/0/top"),"response isolated from feed pipeline");
Check(MetaReaOnlineSetup.DetailTrackingUrls(MetaReaOnlineSetup.FeedUrl).Length==0,"no tracking pixels");

Setup(g);g.sharedDataSource=null;S(g,"clickCount",0);S(g,"clickHead",0);S(g,"clickSent",0);S(g,"statisticsCount",0);S(g,"statisticsHead",0);S(g,"localPortalRequestTime",-999f);S(g,"portalCooldownStarted",false);S(g,"ready",true);
// Simulate a v4 world placement backed by the same sponsor metadata.
S(g,"v4Layout",true);((int[])G(g,"topAds"))[15]=0;g.portalMarker=null;S(g,"view",0);
C(g,"OpenSponsored",15);
Check((int)G(g,"view")==0,"v4 direct portal never opens ad detail");
Check((int)G(g,"clickCount")==1&&((int[])G(g,"clickQueueOrigins"))[0]==2,"accepted direct request queues portal attribution only");
Check((int)G(g,"statisticsCount")==2&&((int[])G(g,"statisticsQueue"))[0]==4&&((int[])G(g,"statisticsQueue"))[1]==8,"global request once plus distinct owner acceptance");
C(g,"OpenSponsored",15);Check((int)G(g,"clickCount")==1&&(int)G(g,"statisticsCount")==2,"cooldown rejection queues nothing");
S(g,"portalCooldownStarted",false);S(g,"localPortalRequestTime",-999f);C(g,"OpenSponsored",15);Check((int)G(g,"clickCount")==2,"accepted repeat portal is not detail-debounced");
S(g,"nextClickSend",0f);S(g,"imageFailed",true);S(g,"sponsorImageFailed",true);S(g,"extraSponsorFailed",new[]{true,true,true,true,true,true,true});S(g,"pendingDrawTab",-1);S(g,"lastFeedAttempt",-999f);S(g,"sponsorStatusStarted",-999f);S(g,"drawStartedAt",-999f);
Check((int)C(g,"TakeAdClick")==8207,"portal uses separate endpoint index");
var portalUrl=(VRC.SDKBase.VRCUrl)C(g,"AdEventUrl",8207);Check(portalUrl.Get()=="https://metarea.net/api/udon/ad-portal/15/top?version=0.17.0&environment=test","portal URL carries version/environment without user identity");
Check((bool)C(g,"IsAdClickResponse",portalUrl.Get()),"portal reply isolated from feed parser");
S(g,"nextClickSend",0f);((bool[])G(g,"topClickAllowed"))[15]=false;Check((int)C(g,"TakeAdClick")==-1&&(int)G(g,"clickCount")==0,"revoked portal attribution discarded");
S(g,"portalCooldownStarted",false);S(g,"localPortalRequestTime",-999f);g.anonymousStatistics=false;C(g,"OpenSponsored",15);Check((int)G(g,"clickCount")==0,"statistics disabled suppresses portal attribution");
Check(MetaReaOnlineSetup.PortalClickUrls("https://example.invalid/feed").Length==0,"custom feed has no portal beacons");
Check(MetaReaOnlineSetup.ClickUrls(MetaReaOnlineSetup.FeedUrl,"test")[0].Get().Contains("environment=test"),"detail context respects test environment");
File.WriteAllLines("C:/Users/一生/Documents/Codex/2026-10-25/t/outputs/v017/ad-checks.txt",ok);
return new{passed=ok.Count,checks=ok};
}finally{if(otherRoot!=null)PrefabUtility.UnloadPrefabContents(otherRoot);PrefabUtility.UnloadPrefabContents(root);}}}
