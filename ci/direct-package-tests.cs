using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;
class EditorCommand {
static List<string> checks=new List<string>();
static void Check(bool b,string n){if(!b)throw new Exception(n);checks.Add(n);}
static string stage="C:/Users/一生/Documents/Codex/2026-10-25/t/outputs/v0176/distribution/package";
static string output="C:/Users/一生/Documents/Codex/2026-10-25/t/outputs/v0176/installer-tests";
static MetaReaVccInstaller.Manifest Manifest()=>JsonUtility.FromJson<MetaReaVccInstaller.Manifest>(File.ReadAllText(stage+"/Payload~/manifest.json"));
static string Fixture(string name){string root=output+"/"+name;Directory.CreateDirectory(root);foreach(string path in Directory.GetFiles(stage,"*",SearchOption.AllDirectories)){string target=root+"/Packages/net.metarea.worldguide/"+path.Substring(stage.Length+1);Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy(path,target);}return root;}
static void Write(string root,string path,string value){string target=root+"/"+path;Directory.CreateDirectory(Path.GetDirectoryName(target));File.WriteAllText(target,value);}
static void Reject(Action a,string name){bool failed=false;try{a();}catch(IOException){failed=true;}Check(failed,name);}
static object Execute(){
if(Directory.Exists(output))throw new Exception("Choose a new test output directory");Directory.CreateDirectory(output);checks.Clear();
foreach(string version in new[]{"fresh","0.16.0","0.17.0","0.17.3","0.17.4","0.17.5"}){
string r=Fixture(version);var m=Manifest();
string prefab="Assets/MetaRea_World_GUIDE/MetaRea World GUIDE.prefab", meta=prefab+".meta", favorite="Assets/MetaRea_World_GUIDE/UserContent/Favorites/custom.asset", scene="Assets/Scenes/Test.unity";
Write(r,prefab,"custom-position-and-colors");Write(r,meta,"guid: 85c6b170f9bbfde48b0585cfe49312f8");Write(r,favorite,"folders/descriptions/flat-mode/worlds");Write(r,scene,"scene overrides");
Write(r,"Assets/MetaRea_World_GUIDE/README.txt.meta","guid: user-manual-guid");
if(version!="fresh")Write(r,"ProjectSettings/MetaReaWorldGuideInstall.json",JsonUtility.ToJson(new MetaReaVccInstaller.InstallState{layout=(version=="0.17.3"||version=="0.17.4"||version=="0.17.5")?3:2,version=version,files=m.files}));
var core=m.files.Where(f=>f.path.StartsWith(MetaReaVccInstaller.CoreRoot)).ToDictionary(f=>f.path,f=>MetaReaVccInstaller.HashFile(r+"/"+f.path));
MetaReaVccInstaller.CompleteDirectInstall(r,m);
Check(core.All(f=>MetaReaVccInstaller.HashFile(r+"/"+f.Key)==f.Value),version+": Core never rewritten");
Check(File.ReadAllText(r+"/"+prefab)=="custom-position-and-colors"&&File.ReadAllText(r+"/"+meta).Contains("85c6b170"),version+": user prefab and GUID preserved");
Check(File.ReadAllText(r+"/"+favorite)=="folders/descriptions/flat-mode/worlds"&&File.ReadAllText(r+"/"+scene)=="scene overrides",version+": favorites and scene preserved");
Check(File.ReadAllText(r+"/Assets/MetaRea_World_GUIDE/README.txt.meta")=="guid: user-manual-guid",version+": manual GUID preserved");
Check(JsonUtility.FromJson<MetaReaVccInstaller.InstallState>(File.ReadAllText(r+"/ProjectSettings/MetaReaWorldGuideInstall.json")).layout==3,version+": direct layout recorded");
var before=Directory.GetFiles(r+"/MetaReaWorldGuideBackups","*",SearchOption.AllDirectories).Length;MetaReaVccInstaller.CompleteDirectInstall(r,m);Check(before==Directory.GetFiles(r+"/MetaReaWorldGuideBackups","*",SearchOption.AllDirectories).Length,version+": repeated callback is a no-op");
string asset=m.files.First(f=>f.path.EndsWith("MetaReaWallGuide.asset")).path;File.AppendAllText(r+"/"+asset,"\n# Unity reserialization\n");string hash=MetaReaVccInstaller.HashFile(r+"/"+asset);MetaReaVccInstaller.CompleteDirectInstall(r,m);Check(hash==MetaReaVccInstaller.HashFile(r+"/"+asset),version+": imported Udon serialization is not overwritten");
}
string missing=Fixture("missing");var manifest=Manifest();File.Delete(missing+"/"+manifest.files.First(f=>f.path.EndsWith("MetaRea World GUIDE.prefab")).path);Reject(()=>MetaReaVccInstaller.CompleteDirectInstall(missing,manifest),"missing runtime rejected before user writes");Check(!Directory.Exists(missing+"/Assets"),"missing runtime changes no user files");
string invalid=Fixture("invalid");var bad=Manifest();bad.files[0].path="../outside.cs";Reject(()=>MetaReaVccInstaller.CompleteDirectInstall(invalid,bad),"path traversal rejected");
string pending=Fixture("interrupted");Write(pending,"ProjectSettings/MetaReaWorldGuideMigration.json","old transaction");Reject(()=>MetaReaVccInstaller.CompleteDirectInstall(pending,Manifest()),"old interrupted migration is not replayed over new Core");
string legacy=Fixture("legacy");Write(legacy,"Assets/MetaRea_World_GUIDE/Scripts/MetaReaWallGuide.cs","user edited legacy");Reject(()=>MetaReaVccInstaller.CompleteDirectInstall(legacy,Manifest()),"Assets-era source requires staged migration");Check(File.ReadAllText(legacy+"/Assets/MetaRea_World_GUIDE/Scripts/MetaReaWallGuide.cs")=="user edited legacy","legacy source is never deleted");
string corrupt=Fixture("corrupt-doc");File.AppendAllText(corrupt+"/Packages/net.metarea.worldguide/Documentation~/README.txt","bad");Reject(()=>MetaReaVccInstaller.CompleteDirectInstall(corrupt,Manifest()),"bad manual hash rejected before writes");Check(!Directory.Exists(corrupt+"/Assets"),"corrupt documentation changes no user files");
File.WriteAllLines(output+"/checks.txt",checks);return new{passed=checks.Count,checks};
}}
