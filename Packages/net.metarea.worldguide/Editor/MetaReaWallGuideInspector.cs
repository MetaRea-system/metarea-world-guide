using UnityEditor;
using UnityEngine;
using UdonSharpEditor;

[CustomEditor(typeof(MetaReaWallGuide))]
public class MetaReaWallGuideInspector : Editor
{
    bool customColors;
    public override void OnInspectorGUI()
    {
        if (UdonSharpEditorUtility.GetBackingUdonBehaviour((MetaReaWallGuide)target) == null)
        { UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target); return; }
        serializedObject.Update();
        EditorGUILayout.LabelField("MetaRea WORLD GUIDE",EditorStyles.boldLabel);
        EditorGUILayout.LabelField("配色プリセット",EditorStyles.boldLabel);
        EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
        EditorGUI.BeginChangeCheck();
        int preset = MetaReaColorPresets.Match(serializedObject);
        int chosen = EditorGUILayout.Popup("配色", preset, MetaReaColorPresets.Names);
        if (chosen != preset && chosen < 8) MetaReaColorPresets.Apply(serializedObject, chosen);
        customColors = EditorGUILayout.Foldout(customColors, "色を個別に調整", true);
        if (customColors)
        {
        EditorGUILayout.PropertyField(serializedObject.FindProperty("headingColor"),new GUIContent("見出し・ワールド名"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("bodyColor"),new GUIContent("紹介文"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("secondaryColor"),new GUIContent("作者名・補足・ページ番号"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("accentColor"),new GUIContent("ブランド・小見出し・矢印"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("buttonTextColor"),new GUIContent("操作ボタンの文字"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("backgroundColor"),new GUIContent("背景の板（画面全体）"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("cardColor"),new GUIContent("カード・通常ボタンの背景"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("selectedTabColor"),new GUIContent("選択中タブの背景"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("portalColor"),new GUIContent("ポータルボタンの背景"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("borderColor"),new GUIContent("枠線・仕切り線"));
        }
        bool colorsChanged=EditorGUI.EndChangeCheck();
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.HelpBox("Play停止中に色を変更すると画面へ即時反映します。シーンを保存してください。写真・広告画像の色は変更しません。",MessageType.None);
        EditorGUILayout.Space();
        var second=serializedObject.FindProperty("secondSlotIsChill");
        second.boolValue=EditorGUILayout.Popup("TOPの2枠目",second.boolValue?1:0,new[]{"ランダムワールド②","ランダムチルワールド"})==1;
        EditorGUILayout.PropertyField(serializedObject.FindProperty("portalAnchor"),new GUIContent("ポータル出現位置"));
        var anchor=serializedObject.FindProperty("portalAnchor").objectReferenceValue as Transform;
        if(anchor!=null && GUILayout.Button("ポータル位置を選択して移動"))
        {Selection.activeGameObject=anchor.gameObject;if(SceneView.lastActiveSceneView!=null)SceneView.lastActiveSceneView.FrameSelected();}
        var cooldown=serializedObject.FindProperty("portalCooldownSeconds");
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
        if (settingsChanged && !EditorApplication.isPlaying) { UdonSharpEditorUtility.CopyProxyToUdon((MetaReaWallGuide)target); EditorUtility.SetDirty(target); }
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

