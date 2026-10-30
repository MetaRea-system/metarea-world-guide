using UnityEditor;
using UnityEngine;
using UdonSharpEditor;

[CustomEditor(typeof(MetaReaWallGuide))]
public class MetaReaWallGuideInspector : Editor
{
    bool customColors;
    readonly System.Collections.Generic.Dictionary<string, SerializedProperty> properties = new System.Collections.Generic.Dictionary<string, SerializedProperty>();
    void OnEnable() { properties.Clear(); }
    SerializedProperty Property(string name)
    {
        if (!properties.TryGetValue(name, out var property))
        {
            property = serializedObject.FindProperty(name);
            properties.Add(name, property);
        }
        return property;
    }
    public override void OnInspectorGUI()
    {
        if (UdonSharpEditorUtility.GetBackingUdonBehaviour((MetaReaWallGuide)target) == null)
        { UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target); return; }
        if (((MetaReaWallGuide)target).isSharedDataSource)
        {
            EditorGUILayout.LabelField("MetaRea 共通データ", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("各ボードが共有するデータ・画像を管理します。配置したボードから自動設定されます。", MessageType.Info);
            return;
        }
        serializedObject.Update();
        EditorGUILayout.LabelField("MetaRea WORLD GUIDE",EditorStyles.boldLabel);
        var currentGuide = (MetaReaWallGuide)target;
        bool displayChanged = false;
        if (currentGuide.unifiedDisplay)
        {
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
            EditorGUILayout.LabelField("表示モード", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            var initialMode = Property("initialDisplayMode");
            initialMode.intValue = EditorGUILayout.Popup("表示形式", initialMode.intValue == 1 ? 1 : 0, new[] { "2枠", "4枠" });
            var favorites = Property("favoritesEnabled");
            favorites.boolValue = EditorGUILayout.Popup("お気に入り", favorites.boolValue ? 0 : 1, new[] { "有り", "無し" }) == 0;
            displayChanged = EditorGUI.EndChangeCheck();
            EditorGUI.EndDisabledGroup();
        }
        EditorGUILayout.LabelField("配色プリセット",EditorStyles.boldLabel);
        EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
        EditorGUI.BeginChangeCheck();
        int preset = MetaReaColorPresets.Match(Property);
        int chosen = EditorGUILayout.Popup("配色", preset, MetaReaColorPresets.Names);
        if (chosen != preset && chosen < 8) MetaReaColorPresets.Apply(serializedObject, chosen);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("タイトルの色", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(Property("brandTitleColor"), new GUIContent("MetaRea"));
        EditorGUILayout.PropertyField(Property("guideTitleColor"), new GUIContent("WORLD GUIDE"));
        EditorGUILayout.LabelField("リンクアイコンの色", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(Property("xIconColor"), new GUIContent("X"));
        EditorGUILayout.PropertyField(Property("archiveIconUseOriginalColor"), new GUIContent("アーカイブの元の色を使う"));
        EditorGUI.BeginDisabledGroup(Property("archiveIconUseOriginalColor").boolValue);
        EditorGUILayout.PropertyField(Property("archiveIconColor"), new GUIContent("ワールドアーカイブ"));
        EditorGUI.EndDisabledGroup();
        customColors = EditorGUILayout.Foldout(customColors, "色を個別に調整", true);
        if (customColors)
        {
        EditorGUILayout.PropertyField(Property("headingColor"),new GUIContent("見出し・ワールド名"));
        EditorGUILayout.PropertyField(Property("bodyColor"),new GUIContent("紹介文"));
        EditorGUILayout.PropertyField(Property("secondaryColor"),new GUIContent("作者名・補足・ページ番号"));
        EditorGUILayout.PropertyField(Property("accentColor"),new GUIContent("小見出し・矢印"));
        EditorGUILayout.PropertyField(Property("buttonTextColor"),new GUIContent("操作ボタンの文字"));
        EditorGUILayout.PropertyField(Property("backgroundColor"),new GUIContent("背景の板（画面全体）"));
        EditorGUILayout.PropertyField(Property("cardColor"),new GUIContent("カード・通常ボタンの背景"));
        EditorGUILayout.PropertyField(Property("selectedTabColor"),new GUIContent("選択中タブの背景"));
        EditorGUILayout.PropertyField(Property("portalColor"),new GUIContent("ポータルボタンの背景"));
        EditorGUILayout.PropertyField(Property("borderColor"),new GUIContent("枠線・仕切り線"));
        }
        bool colorsChanged=EditorGUI.EndChangeCheck();
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.HelpBox("Play停止中に色を変更すると画面へ即時反映します。シーンを保存してください。写真・広告画像の色は変更しません。",MessageType.None);
        EditorGUILayout.Space();
        var second=Property("secondSlotIsChill");
        if (currentGuide.unifiedDisplay || !currentGuide.manyVersion) second.boolValue=EditorGUILayout.Popup("標準表示の2枠目",second.boolValue?1:0,new[]{"今日のランダムワールド②","ランダムチルワールド"})==1;
        EditorGUILayout.PropertyField(Property("portalAnchor"),new GUIContent("ポータル出現位置"));
        EditorGUILayout.PropertyField(Property("anonymousStatistics"), new GUIContent("匿名の操作統計を送る"));
        EditorGUILayout.HelpBox("読込結果・表示・操作の概数を送信します。個人IDやワールドIDは含めません。同じシーンの1台でも無効にすると操作統計は停止します。通常のFeed読出はサーバー側で集計されます。", MessageType.None);
        var anchor=Property("portalAnchor").objectReferenceValue as Transform;
        if(anchor!=null && GUILayout.Button("ポータル位置を選択して移動"))
        {Selection.activeGameObject=anchor.gameObject;if(SceneView.lastActiveSceneView!=null)SceneView.lastActiveSceneView.FrameSelected();}
        var cooldown=Property("portalCooldownSeconds");
        cooldown.floatValue=EditorGUILayout.Slider("共有待機時間（秒）",cooldown.floatValue,1,30);
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("画像や情報が表示されない場合は、VRChatの「Allow Untrusted URLs」を有効にしてください。", MessageType.None);
        if (((MetaReaWallGuide)target).favoritesEnabled)
        {
            EditorGUILayout.HelpBox("ワールド登録は子オブジェクト「お気に入りワールド設定」で行います。", MessageType.Info);
            var settings = ((MetaReaWallGuide)target).favoritesSettings;
            if (settings != null && GUILayout.Button("お気に入りワールド設定を選択")) Selection.activeGameObject = settings.gameObject;
        }
        bool settingsChanged = serializedObject.ApplyModifiedProperties();
        if (displayChanged && !EditorApplication.isPlaying) MetaReaUnifiedLayout.Preview(currentGuide);
        if (settingsChanged && !colorsChanged && !EditorApplication.isPlaying) { UdonSharpEditorUtility.CopyProxyToUdon((MetaReaWallGuide)target); EditorUtility.SetDirty(target); }
        if(colorsChanged && !EditorApplication.isPlaying)
        {
            var guide=(MetaReaWallGuide)target;
            MetaReaPresentationLayout.ApplyColors(guide);
            UdonSharpEditorUtility.CopyProxyToUdon(guide);
            EditorUtility.SetDirty(guide);
            if(guide.gameObject.scene.IsValid())UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(guide.gameObject.scene);
        }
    }
}

