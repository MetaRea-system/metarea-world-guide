using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class MetaReaFavoritesInspector
{
    public static string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";
        input = input.Trim();
        var match = Regex.Match(input, @"wrld_[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");
        return match.Success ? match.Value.ToLowerInvariant() : input;
    }

    public static void Draw(SerializedObject serialized)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("お気に入りワールド", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("このPrefabの登録一覧を、ワールドの全利用者に表示します。1ページ8件。行を押すと詳細ページを開かず共有ポータルを出します。", MessageType.Info);
        EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
        var ids = serialized.FindProperty("favoriteWorldIds");
        var titles = serialized.FindProperty("favoriteWorldTitles");
        var authors = serialized.FindProperty("favoriteWorldAuthors");
        titles.arraySize = ids.arraySize; authors.arraySize = ids.arraySize;
        EditorGUILayout.LabelField("登録数", ids.arraySize + " 件 / " + Mathf.Max(1, (ids.arraySize + 7) / 8) + " ページ");
        var seen = new HashSet<string>();
        for (int i = 0; i < ids.arraySize; i++)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            string name = titles.GetArrayElementAtIndex(i).stringValue;
            EditorGUILayout.LabelField((i + 1) + ". " + (string.IsNullOrEmpty(name) ? "新しいワールド" : name), EditorStyles.boldLabel);
            int move = 0; bool remove;
            using (new EditorGUI.DisabledScope(i == 0)) if (GUILayout.Button("↑", GUILayout.Width(30))) move = -1;
            using (new EditorGUI.DisabledScope(i + 1 == ids.arraySize)) if (GUILayout.Button("↓", GUILayout.Width(30))) move = 1;
            remove = GUILayout.Button("削除", GUILayout.Width(46));
            EditorGUILayout.EndHorizontal();
            var id = ids.GetArrayElementAtIndex(i);
            EditorGUI.BeginChangeCheck();
            string entered = EditorGUILayout.TextField(new GUIContent("ワールドURL / ID", "VRChatのワールドURLまたはwrld_で始まるIDを貼り付けてください。"), id.stringValue);
            if (EditorGUI.EndChangeCheck()) id.stringValue = Normalize(entered);
            EditorGUILayout.PropertyField(titles.GetArrayElementAtIndex(i), new GUIContent("タイトル"));
            EditorGUILayout.PropertyField(authors.GetArrayElementAtIndex(i), new GUIContent("作者名"));
            if (!Regex.IsMatch(id.stringValue, @"^wrld_[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$"))
                EditorGUILayout.HelpBox("有効なワールドURLまたはIDを入力してください。この行は入力が完了するまで表示されません。", MessageType.Warning);
            else if (!seen.Add(id.stringValue.ToLowerInvariant())) EditorGUILayout.HelpBox("同じIDが上の行にあります。一覧には最初の1件だけを表示します。", MessageType.Warning);
            if (string.IsNullOrWhiteSpace(titles.GetArrayElementAtIndex(i).stringValue) || titles.GetArrayElementAtIndex(i).stringValue.Length > 256)
                EditorGUILayout.HelpBox("タイトルを1〜256文字で入力してください。", MessageType.Warning);
            EditorGUILayout.EndVertical();
            if (remove)
            {
                ids.DeleteArrayElementAtIndex(i); titles.DeleteArrayElementAtIndex(i); authors.DeleteArrayElementAtIndex(i); break;
            }
            if (move != 0)
            {
                ids.MoveArrayElement(i, i + move); titles.MoveArrayElement(i, i + move); authors.MoveArrayElement(i, i + move); break;
            }
        }
        if (GUILayout.Button("＋ ワールドを追加"))
        {
            int i = ids.arraySize;
            ids.arraySize++; titles.arraySize++; authors.arraySize++;
            ids.GetArrayElementAtIndex(i).stringValue = "";
            titles.GetArrayElementAtIndex(i).stringValue = "";
            authors.GetArrayElementAtIndex(i).stringValue = "";
        }
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.Space();
    }
}
