using System;
using System.IO;
using UnityEngine;
using UnityEditor;
class EditorCommand {
static object Execute(){
string root=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
var files=new[]{"TagManager.asset","DynamicsManager.asset"};
var prior=Array.ConvertAll(files,f=>File.ReadAllBytes(Path.Combine(root,"ProjectSettings",f)));
var report=MetaReaBuildEnvironment.Inspect();
for(int i=0;i<files.Length;i++)if(!System.Linq.Enumerable.SequenceEqual(prior[i],File.ReadAllBytes(Path.Combine(root,"ProjectSettings",files[i]))))throw new Exception("Read-only diagnostic modified "+files[i]);
if(!report.layersReady||!report.collisionReady)throw new Exception("SDK settings are still invalid");
if(!MetaReaBuildEnvironment.ContainsNonAscii("G:/VR関係/VCC/Test")||!MetaReaBuildEnvironment.ContainsNonAscii("C:/Users/一生/World")||MetaReaBuildEnvironment.ContainsNonAscii("C:/VRCProjects/WorldGuide"))throw new Exception("Path classification failed");
if(MetaReaBuildEnvironment.ApplySdkSetup()!=null)throw new Exception("Valid setup is not a no-op");
for(int i=0;i<files.Length;i++)if(!System.Linq.Enumerable.SequenceEqual(prior[i],File.ReadAllBytes(Path.Combine(root,"ProjectSettings",files[i]))))throw new Exception("No-op changed "+files[i]);
string output="C:/Users/一生/Documents/Codex/2026-10-25/t/outputs/build-issue-0173/development-diagnostic.json";
File.WriteAllText(output,JsonUtility.ToJson(report,true));
return new{passed=8,report,output};
}}
