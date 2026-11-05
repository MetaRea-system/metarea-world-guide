using System;using System.IO;using System.Collections.Generic;using UnityEngine;using UnityEditor;using VRC.SDK3.StringLoading;using VRC.SDKBase;using VRC.Udon.Common.Interfaces;
class EditorCommand {
static IUdonProgram p;static IUdonVM vm;static List<string> checks=new List<string>();
static object G(string n)=>p.Heap.GetHeapVariable(p.SymbolTable.GetAddressFromSymbol(n));
static void S(string n,object v){uint a=p.SymbolTable.GetAddressFromSymbol(n);p.Heap.SetHeapVariable(a,v,p.Heap.GetHeapVariableType(a));}
static void Run(string n){vm.SetProgramCounter(p.EntryPoints.GetAddressFromSymbol(n));uint result=vm.Interpret();if(result!=0)throw new Exception("VM failed "+n+" code "+result);}
static void Check(bool b,string n){if(!b)throw new Exception(n);checks.Add(n);}
class Download:IVRCStringDownload {public void StartDownload(){}public void CancelDownload(){}public string Result{get;set;}public byte[] ResultBytes=>System.Text.Encoding.UTF8.GetBytes(Result);public string Error=>"fixture";public int ErrorCode=>503;public VRCUrl Url{get;set;}public IUdonEventReceiver UdonBehaviour=>null;}
static void Arm(string url,int attempts=1){S("v3RequestKind",4);S("v3RequestUrl",new VRCUrl(url));S("v3RequestInFlight",true);S("v3RequestAttempts",attempts);S("v3WireUrls",new string[]{url,null,null,null,null,null,null,null});S("v3WireAbandoned",new bool[8]);}
static void Reply(string url,string json){S("onStringLoadSuccessIVRCStringDownload",new Download{Url=new VRCUrl(url),Result=json});Run("_onStringLoadSuccess");}
static object Execute(){
checks.Clear();var a=AssetDatabase.LoadAssetAtPath<UdonSharp.UdonSharpProgramAsset>("Packages/net.metarea.worldguide/Core/Scripts/MetaReaWallGuide.asset");p=a.SerializedProgramAsset.RetrieveProgram();vm=VRC.Udon.Editor.UdonEditorManager.Instance.ConstructUdonVM();Check(vm.LoadProgram(p),"compiled Udon bytecode loads into VM");
S("guideV3",true);S("onlineMode",true);S("isSharedDataSource",true);S("sharedDataSource",null);S("ready",true);S("sponsorStatusProtocol",true);S("v3DrawCategory",-1);S("anonymousStatistics",false);
// Local component references are not used by these transport/status paths.
string url="https://example.invalid/status.json";
Arm(url);Reply(url,"{\"schema\":\"metarea-guide-wait-v1\",\"waiting\":true,\"retryAfter\":60}");
Check((int)G("serverWaitCount")==1&&!(bool)G("v3RequestInFlight"),"VM accepts overload wait and releases logical request");
Check((float)G("v3ServerWaitUntil")>Time.realtimeSinceStartup+59,"VM wait does not retry immediately");
Check(!(bool)G("v3Maintenance"),"VM distinguishes overload from maintenance");
Arm(url);var abandoned=new bool[8];abandoned[0]=true;S("v3WireAbandoned",abandoned);Reply(url,"{}");Check((int)G("staleResponseCount")==1,"VM discards retired transport callback");
S("v3RequestKind",0);S("v3RequestInFlight",false);S("v3ServerWaitUntil",0f);S("v3NextStringAt",0f);S("v3ManualRetryAt",0f);S("v3BootstrapStopped",true);Run("_Retry");Check(!(bool)G("v3BootstrapStopped"),"VM manual retry resets stopped bootstrap once");
S("v3BootstrapStopped",true);S("v3NextStringAt",0f);Run("_Retry");Check((bool)G("v3BootstrapStopped"),"VM rejects retry button spam");
// Actual server fixture; no ads loaded in this minimal VM, but strict status parsing runs.
string status=File.ReadAllText("C:/Users/一生/Documents/Codex/2026-10-25/t/work/v010/fixtures/status-fixture.json");
Arm(url);Reply(url,status);Check((bool)G("sponsorStatusValid"),"VM accepts valid publication status");
double server=(double)G("v3ServerTime"),until=(double)G("v3StatusUntil");S("v3ServerReceived",Time.realtimeSinceStartup-40f);
Arm(url);Reply(url,status);Check((double)G("v3ServerTime")>=server+39.9,"VM repeated status never rolls clock backward");
Check((double)G("v3StatusUntil")==until,"VM absolute permission deadline unchanged on replay");
S("v3ServerTime",until+1);S("v3ServerReceived",Time.realtimeSinceStartup);Arm(url);Reply(url,status);Check(!(bool)G("sponsorStatusValid"),"VM expired permission fails closed");
// Compiled outage path: four failures, timed recovery, then a new bootstrap transaction.
S("ready",false);S("v3RequestKind",0);S("v3ServerWaitUntil",0f);S("v3NextStringAt",Time.realtimeSinceStartup+10000f);S("v3RecoveryPending",false);S("v3RecoveryRound",0);S("v3Maintenance",false);S("v3RequestUrl",new VRCUrl(url));Arm(url,4);S("v3RequestKind",1);S("onStringLoadErrorIVRCStringDownload",new Download{Url=new VRCUrl(url)});Run("_onStringLoadError");
Check((bool)G("v3BootstrapStopped")&&(bool)G("v3RecoveryPending"),"VM bootstrap exhaustion arms automatic recovery");
S("nextUiTick",0f);Run("_update");Check((bool)G("v3BootstrapStopped"),"VM does not bypass recovery delay");
S("v3RecoveryAt",0f);S("nextUiTick",0f);S("feedUrl",new VRCUrl("https://example.invalid/current.json"));Run("_update");
Check(!(bool)G("v3BootstrapStopped")&&(int)G("v3RequestKind")==1,"VM automatically queues new bootstrap after recovery timer");
Check(!(bool)G("v3RequestInFlight"),"VM recovery still respects paced transport admission");
Check((int)G("feedRequestCount")+(int)G("statusRequestCount")+(int)G("imageRequestCount")==0,"VM regression sends no network requests");

// New contract exercised through compiled Udon callback (no actual network).
S("v3ServerWaitUntil",0f);S("v3JsonBusyUntil",0f);S("v3RecoveryPending",false);S("v3RecoveryRound",0);S("ready",true);S("v3DrawDeadline",12345f);
for(int attempt=1;attempt<=3;attempt++){Arm(url,attempt);Reply(url,"{\"schema\":\"metarea-guide-wait-v1\",\"waiting\":true,\"reason\":\"busy\",\"retryAfter\":5}");float lo=attempt==1?5:attempt==2?10:20;float wait=(float)G("v3JsonBusyUntil")-Time.realtimeSinceStartup;Check(wait>=lo-.2f&&wait<=lo*2+.2f,"VM busy retry range "+attempt);Check((float)G("v3ServerWaitUntil")==0&&(float)G("v3DrawDeadline")==12345f,"VM busy preserves image access and draw deadline "+attempt);}
Arm(url,4);Reply(url,"{\"schema\":\"metarea-guide-wait-v1\",\"waiting\":true,\"reason\":\"busy\",\"retryAfter\":5}");Check((bool)G("v3RecoveryPending"),"VM fourth busy enters long recovery");
float recovery=(float)G("v3RecoveryAt");Check(recovery>=Time.realtimeSinceStartup+59&&recovery<=Time.realtimeSinceStartup+91,"VM recovery range sixty to ninety seconds");
S("v3RequestKind",0);S("v3NextStringAt",0f);S("v3ManualRetryAt",0f);S("v3JsonBusyUntil",0f);Run("_Retry");Check((bool)G("v3RecoveryPending")&&(float)G("v3RecoveryAt")==recovery,"VM manual retry preserves recovery clock");

File.WriteAllLines("C:/Users/一生/Documents/Codex/2026-10-25/t/outputs/v016/recovery/udon-vm-checks.txt",checks);return new{count=checks.Count,checks};
}
}
