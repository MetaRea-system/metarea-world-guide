using System; using System.IO; using System.Linq; using System.Reflection; using System.Collections.Generic;
using UnityEngine; using UnityEditor; using UnityEditor.SceneManagement; using UnityEngine.SceneManagement; using UdonSharpEditor; using VRC.SDK3.Data;
class EditorCommand {
static BindingFlags F=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
static object G(object x,string n)=>x.GetType().GetField(n,F).GetValue(x);
static void S(object x,string n,object v)=>x.GetType().GetField(n,F).SetValue(x,v);
static object C(object x,string n,params object[] a){try{return x.GetType().GetMethod(n,F).Invoke(x,a);}catch(TargetInvocationException e){throw e.InnerException;}}
static List<string> checks=new List<string>(); static void Check(bool ok,string name){if(!ok)throw new Exception(name);checks.Add(name);}
static string output="C:/Users/一生/Documents/Codex/2026-10-25/t/outputs/v017";
static void Capture(MetaReaWallGuide g,string file){
var root=g.gameObject;var canvas=g.topPanel.GetComponentInParent<Canvas>();var r=(RectTransform)canvas.transform;var corners=new Vector3[4];r.GetWorldCorners(corners);var center=(corners[0]+corners[2])*.5f;
var co=new GameObject("QA camera");SceneManager.MoveGameObjectToScene(co,root.scene);var camera=co.AddComponent<Camera>();var rt=new RenderTexture(900,1500,24);var texture=new Texture2D(900,1500,TextureFormat.RGB24,false);var previous=RenderTexture.active;
try{camera.overrideSceneCullingMask=EditorSceneManager.GetSceneCullingMask(root.scene);camera.transform.position=center-r.forward*5;camera.transform.rotation=Quaternion.LookRotation(r.forward,r.up);camera.orthographic=true;camera.orthographicSize=(corners[1]-corners[0]).magnitude*.53f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.04f,.05f,.07f);camera.nearClipPlane=.01f;camera.farClipPlane=10;camera.targetTexture=rt;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,900,1500),0,0);texture.Apply();File.WriteAllBytes(output+"/"+file+".png",texture.EncodeToPNG());}finally{RenderTexture.active=previous;camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(co);}}
static object Execute(){checks.Clear();Directory.CreateDirectory(output);var scene=EditorSceneManager.NewPreviewScene();var profile=ScriptableObject.CreateInstance<MetaReaFavoritesProfile>();try{
var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(MetaReaAssetsEntry.SourcePath),scene);var g=root.GetComponent<MetaReaWallGuide>();
foreach(var u in root.GetComponentsInChildren<VRC.Udon.UdonBehaviour>(true)) UdonSharpEditorUtility.GetProxyBehaviour(u);
g.favoritesEnabled=true;MetaReaUnifiedLayout.Apply(g);
profile.favoriteWorldIds=new[]{"wrld_00000000-0000-0000-0000-000000000001","wrld_00000000-0000-0000-0000-000000000002"}; profile.favoriteWorldTitles=new[]{"星の湖畔","海辺の街"};profile.favoriteWorldAuthors=new[]{"作者A","作者B"};
MetaReaFavoritesStorage.Migrate(profile);Check(profile.folders.Length==1&&profile.folders[0].name=="フォルダ1"&&profile.folders[0].favoriteWorldIds.SequenceEqual(profile.favoriteWorldIds)&&profile.folders[0].favoriteWorldAuthors.SequenceEqual(profile.favoriteWorldAuthors),"legacy metadata/order preserved");
profile.folders[0].name="景色・写真";MetaReaFavoritesStorage.Migrate(profile);Check(profile.folders[0].name=="景色・写真","migration idempotent");
var empty=ScriptableObject.CreateInstance<MetaReaFavoritesProfile>();MetaReaFavoritesStorage.Migrate(empty);Check(empty.folders.Length==1&&empty.folders[0].favoriteWorldIds.Length==0,"empty legacy gets folder1");UnityEngine.Object.DestroyImmediate(empty);
var folders=new MetaReaFavoriteFolder[10];for(int f=0;f<10;f++){folders[f]=new MetaReaFavoriteFolder{name=f==0?"景色・写真":f==1?"友人と遊ぶ":"フォルダ"+(f+1),favoriteWorldIds=new string[10],favoriteWorldTitles=new string[10],favoriteWorldAuthors=new string[10]};for(int i=0;i<10;i++){folders[f].favoriteWorldIds[i]="wrld_00000000-0000-0000-0000-"+(i+1).ToString("D12");folders[f].favoriteWorldTitles[i]="お気に入りワールド "+(i+1);folders[f].favoriteWorldAuthors[i]="作者 "+(i+1);}}
folders[0].description="星空や水辺をゆっくり楽しむ\n撮影におすすめのワールド";
profile.folders=folders;MetaReaFavoritesStorage.Apply(g.favoritesSettings,profile);g.favoriteFolderDescriptions=g.favoritesSettings.favoriteFolderDescriptions;g.favoriteFolderNames=g.favoritesSettings.favoriteFolderNames;g.favoriteFolderIndices=g.favoritesSettings.favoriteFolderIndices;g.favoriteWorldIds=g.favoritesSettings.favoriteWorldIds;g.favoriteWorldTitles=g.favoritesSettings.favoriteWorldTitles;g.favoriteWorldAuthors=g.favoritesSettings.favoriteWorldAuthors;g.onlineMode=false;
g._Favorites();Check(g.favoriteFolderButtons.Count(b=>b.gameObject.activeSelf)==8&&!g.favoriteButtons[0].gameObject.activeSelf,"folder grid eight visible");Check(((int[])G(g,"favoriteFolderCountsCache"))[0]==10&&((int[])G(g,"favoriteFolderCountsCache"))[1]==10,"duplicates allowed across folders");Check(g.favoriteFolderCounts[0].text==folders[0].description&&g.favoriteFolderCounts[1].text=="","folder descriptions replace counts, empty stays blank");Capture(g,"folders");g._FavoriteNext();g._FavoriteFolder0();Check((int)G(g,"favoriteFolder")==8&&(int)G(g,"favoriteCount")==10,"folder page selection");g._FavoriteNext();Check((int)G(g,"favoritePage")==1&&g.favoriteButtons.Count(b=>b.gameObject.activeSelf)==2,"world page eight plus two");g._FavoriteFolders();Check((int)G(g,"favoriteFolderPage")==1,"back restores folder page");g._FavoritePrevious();g._FavoriteFolder0();Capture(g,"folder-worlds");

Check(!profile.flatList && !g.favoriteFlatList,"folder mode is default");
Check(g.initialDisplayMode==1,"new prefab defaults to four cards");
profile.flatList=true;MetaReaFavoritesStorage.Apply(g.favoritesSettings,profile);MetaReaUnifiedLayout.Preview(g);g._Favorites();
Check((int)G(g,"favoriteCount")==10&&g.favoriteButtons.Count(b=>b.gameObject.activeSelf)==8,"flat mode deduplicates across ten folders");
Check(!g.favoriteBack.gameObject.activeSelf&&g.favoriteFolderButtons.All(b=>!b.gameObject.activeSelf),"flat mode hides folders and back button");
Capture(g,"favorites-flat");g._FavoriteNext();Check((int)G(g,"favoritePage")==1&&g.favoriteButtons.Count(b=>b.gameObject.activeSelf)==2,"flat mode next page");g._FavoritePrevious();Check((int)G(g,"favoritePage")==0,"flat mode previous page");
Check(profile.folders.Length==10&&profile.folders.All(f=>f.favoriteWorldIds.Length==10),"flat view preserves folder registrations");
profile.folders[0].description="説明文だけ変更";MetaReaFavoritesStorage.Apply(g.favoritesSettings,profile);Check(g.favoritesSettings.favoriteFolderDescriptions[0]=="説明文だけ変更","description-only update propagates");
profile.flatList=false;MetaReaFavoritesStorage.Apply(g.favoritesSettings,profile);MetaReaUnifiedLayout.Preview(g);g._Favorites();Check(g.favoriteFolderButtons[0].gameObject.activeSelf&&!g.favoriteButtons[0].gameObject.activeSelf,"switch back preserves folder view");
var marker=g.portalMarker;g.portalMarker=null;g._FavoritePortalOnOwner(0);Check(g.sharedPortalWorldId==g.favoriteWorldIds[0],"offline favorite portal resolves original index");g.portalMarker=marker;
DataToken token;VRCJson.TryDeserializeFromJson("{\"platformSupport\":{\"pc\":true,\"android\":false,\"ios\":null}}",out token);var world=token.DataDictionary;Check((int)C(g,"PlatformState",world,"pc")==1&&(int)C(g,"PlatformState",world,"android")==-1&&(int)C(g,"PlatformState",world,"ios")==0,"platform true false null");Check((int)C(g,"PlatformState",new DataDictionary(),"pc")==0,"missing platform stays unknown");
Check(g.favoriteButtons.All(b=>b.transform.Find("Platforms")==null),"favorites contain no platform labels");
Check(g.sampleFeed!=null,"sample feed present for visual QA");
VRCJson.TryDeserializeFromJson(g.sampleFeed.text,out token);var feed=token.DataDictionary;var worlds=feed["worlds"].DataList;
for(int i=0;i<worlds.Count;i++){var w=worlds[i].DataDictionary;w["platformSupport"]=world;w["platformSupport"]=world["platformSupport"];w["authorName"]="とても長い作者名の表示を確認するためのサンプル作者";}
VRCJson.TrySerializeToJson(feed,JsonExportType.Minify,out token);Check((bool)C(g,"Parse",token.String),"parse platform fixture");S(g,"favoriteTabSelected",false);S(g,"view",0);S(g,"tab",0);S(g,"ready",true);g.manyVersion=true;C(g,"Render");Capture(g,"cards-four");
Check(g.manyPlatforms.Any(t=>t.text.Contains("PC ✓")&&t.text.Contains("Android —")&&t.text.Contains("iOS ?")),"card tri-state labels");
S(g,"sponsoredCount",0);C(g,"Render");Capture(g,"cards-four-no-ad");g.manyVersion=false;C(g,"Render");Capture(g,"cards-two-no-ad");
C(g,"SetTab",1);Capture(g,"wide-list");S(g,"selected",0);S(g,"view",2);C(g,"Render");Capture(g,"detail");
Check(g.detailPlatforms.text.Contains("PC：対応")&&g.detailPlatforms.text.Contains("Android：非対応")&&g.detailPlatforms.text.Contains("iOS：未確認"),"detail explicit platform words");
File.WriteAllText(output+"/checks.txt",string.Join("\n",checks));return checks.ToArray();
}finally{UnityEngine.Object.DestroyImmediate(profile);EditorSceneManager.ClosePreviewScene(scene);}}
}
