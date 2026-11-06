using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

// The package carries its own font and shader. Only a missing project-wide TMP
// settings asset is created; existing projects retain their settings unchanged.
[InitializeOnLoad]
public static class MetaReaTextResources
{
    public const string Template = "Packages/net.metarea.worldguide/Core/Internal/TMP Settings Template.asset";
    public const string SettingsPath = "Assets/MetaRea_World_GUIDE/UserContent/TextMeshPro/Resources/TMP Settings.asset";
    static MetaReaTextResources() { EditorApplication.delayCall += Prepare; }
    static void Prepare()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += Prepare; return; }
        EnsureSettings();
    }
    public static void EnsureSettings()
    {
        if (Resources.Load<TMP_Settings>("TMP Settings") != null) return;
        // Asset import may not have populated Resources yet. Do not create a
        // second asset beside an existing project's configuration.
        foreach (string guid in AssetDatabase.FindAssets("t:TMP_Settings"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid).Replace('\\', '/');
            if (path.EndsWith("/Resources/TMP Settings.asset", StringComparison.OrdinalIgnoreCase)) return;
        }
        if (!File.Exists(Template)) return;
        if (File.Exists(SettingsPath)) return;
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
        AssetDatabase.Refresh();
        if (!AssetDatabase.CopyAsset(Template, SettingsPath)) throw new IOException("GUIDEの文字表示設定を作成できませんでした。");
        AssetDatabase.ImportAsset(SettingsPath, ImportAssetOptions.ForceSynchronousImport);
        TMP_Settings.LoadDefaultSettings();
    }
    public static void DrawFallbackNotice()
    {
        var settings = Resources.Load<TMP_Settings>("TMP Settings");
        if (settings == null) return;
        var serialized = new SerializedObject(settings);
        var fallback = serialized.FindProperty("m_fallbackFontAssets");
        if (fallback == null || fallback.arraySize == 0) return;
        EditorGUILayout.HelpBox("既存のTMP補完フォントが設定されています。VRChat内蔵の多言語補完を利用するにはTMP SettingsのFallback Font Assetsを空にする必要があります。他の文字表示への影響を確認してから変更してください。GUIDEは既存設定を自動変更しません。", MessageType.Warning);
        if (GUILayout.Button("文字の補完設定を確認")) { Selection.activeObject = settings; EditorGUIUtility.PingObject(settings); }
    }
}
