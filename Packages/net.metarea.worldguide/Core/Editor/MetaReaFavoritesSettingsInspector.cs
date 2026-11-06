using UnityEditor;
using UnityEngine;
using UdonSharpEditor;

[CustomEditor(typeof(MetaReaFavoritesSettings))]
public class MetaReaFavoritesSettingsInspector : Editor
{
    public override void OnInspectorGUI()
    {
        if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;
        DrawSettings((MetaReaFavoritesSettings)target);
    }

    public static void DrawSettings(MetaReaFavoritesSettings settings)
    {
        if (!MetaReaFavoritesStorage.CanStore(settings))
        {
            EditorGUILayout.HelpBox("シーンに配置したボードの「お気に入りワールド設定」で登録してください。登録データはパッケージの更新対象外に保存されます。", MessageType.Info);
            return;
        }
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            var profile = EditorApplication.isPlaying ? MetaReaFavoritesStorage.Load(settings) : MetaReaFavoritesStorage.Ensure(settings);
            var chosen = (MetaReaFavoritesProfile)EditorGUILayout.ObjectField("保存データ", profile, typeof(MetaReaFavoritesProfile), false);
            if (chosen != null && chosen != profile)
            { MetaReaFavoritesStorage.Bind(settings, chosen); profile = chosen; }
            EditorGUILayout.HelpBox("登録一覧はAssets/MetaRea_World_GUIDE/UserContentに保存します。更新後やボードの再配置時も、この保存データを選ぶと引き継げます。同じデータを選んだボードは一覧を共有します。", MessageType.Info);
            if (profile == null)
            {
                EditorGUILayout.HelpBox("保存データが見つかりません。既存のデータを選び直してください。", MessageType.Error);
                return;
            }
            if (GUILayout.Button("保存データを複製して別の一覧にする"))
            {
                var clone = Instantiate(profile);
                AssetDatabase.CreateAsset(clone, AssetDatabase.GenerateUniqueAssetPath(MetaReaFavoritesStorage.Folder + "/Favorite Worlds.asset"));
                MetaReaFavoritesStorage.Bind(settings, clone);
                AssetDatabase.SaveAssetIfDirty(clone);
                profile = clone;
            }
            var data = new SerializedObject(profile);
            data.Update();
            MetaReaFavoritesInspector.Draw(data);
            if (data.ApplyModifiedProperties())
            {
                AssetDatabase.SaveAssetIfDirty(profile);
                MetaReaFavoritesStorage.PrepareAll();
            }
        }
    }
}
