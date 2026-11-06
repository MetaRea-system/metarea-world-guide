using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Explicit editor-only checks. Importing GUIDE never rewrites a world's physics settings.
[InitializeOnLoad]
public sealed class MetaReaBuildEnvironment : EditorWindow
{
    [Serializable] public sealed class Report
    {
        public string projectPath;
        public bool layersReady, collisionReady, nonAsciiPath;
        public string[] assemblyErrors;
    }
    static MetaReaBuildEnvironment() { EditorApplication.delayCall += CheckInstalledEnvironment; }
    static void CheckInstalledEnvironment()
    {
        if (Application.isBatchMode) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        { EditorApplication.delayCall += CheckInstalledEnvironment; return; }
        // Only distributed installs: one notice per project/version on this computer.
        const string manifestPath = "Packages/net.metarea.worldguide/Payload~/manifest.json";
        if (!File.Exists(manifestPath)) return;
        string key = "MetaRea.BuildEnvironment." + Application.dataPath + "." + MetaReaOnlineSetup.GuideVersion;
        if (EditorPrefs.GetBool(key, false)) return;
        var status = Inspect();
        EditorPrefs.SetBool(key, true);
        if (!status.layersReady || !status.collisionReady || status.assemblyErrors.Length > 0) Open();
    }
    Report report;
    Vector2 scroll;
    string backup;

    public static bool ContainsNonAscii(string path)
    {
        foreach (char c in path ?? "") if (c > 127) return true;
        return false;
    }

    public static Report Inspect()
    {
        var errors = new List<string>();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try { assembly.GetName(); }
            catch (Exception ex) { errors.Add(assembly.FullName + ": " + ex.GetType().Name + " / " + ex.Message); }
        }
        string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        return new Report { projectPath = root, layersReady = UpdateLayers.AreLayersSetup(),
            collisionReady = UpdateLayers.IsCollisionLayerMatrixSetup(), nonAsciiPath = ContainsNonAscii(root), assemblyErrors = errors.ToArray() };
    }

    [MenuItem("MetaRea/ビルド環境を診断")]
    public static void Open()
    {
        var window = GetWindow<MetaReaBuildEnvironment>("GUIDE ビルド環境");
        window.minSize = new Vector2(480, 360);
        window.report = Inspect();
        window.Show();
    }

    // Called only after an explicit setup action; back up before invoking the SDK's own setup.
    public static string ApplySdkSetup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            throw new InvalidOperationException("Playとコンパイルが停止してから実行してください。");
        if (UpdateLayers.AreLayersSetup() && UpdateLayers.IsCollisionLayerMatrixSetup()) return null;
        string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string destination = Path.Combine(root, "MetaReaWorldGuideBackups", "BuildEnvironment-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(destination);
        foreach (string file in new[] { "TagManager.asset", "DynamicsManager.asset" })
            File.Copy(Path.Combine(root, "ProjectSettings", file), Path.Combine(destination, file));
        File.WriteAllText(Path.Combine(destination, "README.txt"), "VRChat SDK設定前のバックアップ。Unityを閉じて、必要なファイルをProjectSettingsへ戻すと設定を復元できます。\n" + root);
        if (!UpdateLayers.AreLayersSetup()) UpdateLayers.SetupEditorLayers();
        if (!UpdateLayers.IsCollisionLayerMatrixSetup()) UpdateLayers.SetupCollisionLayerMatrix();
        AssetDatabase.SaveAssets();
        if (!UpdateLayers.AreLayersSetup() || !UpdateLayers.IsCollisionLayerMatrixSetup())
            throw new InvalidOperationException("SDK設定を完了できませんでした。VRChat SDKのBuilderを確認してください。バックアップ: " + destination);
        return destination;
    }

    void OnGUI()
    {
        if (report == null) report = Inspect();
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.LabelField("VRChatビルド前の環境確認", EditorStyles.boldLabel);
        EditorGUILayout.SelectableLabel(report.projectPath, EditorStyles.wordWrappedLabel, GUILayout.Height(40));
        EditorGUILayout.HelpBox("必須レイヤー: " + (report.layersReady ? "設定済み" : "未設定・旧設定") + "\n衝突設定: " + (report.collisionReady ? "設定済み" : "未設定・旧設定"), report.layersReady && report.collisionReady ? MessageType.Info : MessageType.Error);
        if (!report.layersReady || !report.collisionReady)
        {
            EditorGUILayout.HelpBox("この状態ではVRChat SDKがビルドを停止します。SDK標準設定を適用すると、プロジェクト全体の必須レイヤー・衝突設定が変わります。", MessageType.Warning);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling))
            if (GUILayout.Button("バックアップしてSDK標準設定を適用"))
            {
                if (EditorUtility.DisplayDialog("VRChat SDKの必須設定", "プロジェクト全体のレイヤーと衝突設定をSDK標準に設定します。独自の物理設定に影響する場合があります。変更前の設定はMetaReaWorldGuideBackupsに保存します。", "適用", "キャンセル"))
                {
                    try { backup = ApplySdkSetup(); }
                    catch (Exception ex) { EditorUtility.DisplayDialog("設定を確認してください", ex.Message, "閉じる"); }
                    report = Inspect();
                }
            }
        }
        if (!string.IsNullOrEmpty(backup)) EditorGUILayout.HelpBox("設定を保存しました。バックアップ: " + backup, MessageType.Info);
        if (report.assemblyErrors.Length > 0)
        {
            EditorGUILayout.HelpBox("アセンブリ情報の取得に失敗しています。SDKのPlayモード検証にも影響する可能性があります。", MessageType.Error);
            foreach (string error in report.assemblyErrors) EditorGUILayout.LabelField(error, EditorStyles.wordWrappedLabel);
        }
        if (report.nonAsciiPath)
            EditorGUILayout.HelpBox("保存先に日本語などの文字が含まれます。必ず失敗するわけではありませんが、MonoのExecutionEngineExceptionが出る場合は、Unityを閉じてプロジェクトをバックアップし、親フォルダーも含め半角英数字の場所（例 C:/VRCProjects/WorldGuide）へコピーしてVCCに追加してください。GUIDEの再配置では直りません。", MessageType.Warning);
        EditorGUILayout.HelpBox("この診断はビルド成功を保証するものではありません。最後にVRChat SDKのBuilderで確認してください。", MessageType.None);
        if (GUILayout.Button("再診断")) report = Inspect();
        EditorGUILayout.EndScrollView();
    }
}
