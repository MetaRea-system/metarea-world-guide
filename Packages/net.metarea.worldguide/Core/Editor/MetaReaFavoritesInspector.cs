using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class MetaReaFavoritesInspector
{
    static readonly Dictionary<string, int> destinations = new Dictionary<string, int>();
    static readonly string[] fields = { "favoriteWorldIds", "favoriteWorldTitles", "favoriteWorldAuthors" };
    static void Align(SerializedProperty array, int count)
    {
        int oldCount = array.arraySize; array.arraySize = count;
        for (int i = oldCount; i < count; i++) array.GetArrayElementAtIndex(i).stringValue = "";
    }
    static void Transfer(SerializedProperty source, int row, SerializedProperty target)
    {
        int targetCount = target.FindPropertyRelative(fields[0]).arraySize;
        Align(target.FindPropertyRelative(fields[1]), targetCount);
        Align(target.FindPropertyRelative(fields[2]), targetCount);
        foreach (var field in fields)
        {
            var from = source.FindPropertyRelative(field); var to = target.FindPropertyRelative(field);
            string value = row < from.arraySize ? from.GetArrayElementAtIndex(row).stringValue : "";
            int at = to.arraySize++; to.GetArrayElementAtIndex(at).stringValue = value;
            if (row < from.arraySize) from.DeleteArrayElementAtIndex(row);
        }
    }
    static int Destination(SerializedProperty folders, int current, string key)
    {
        var labels = new List<string>(); var indices = new List<int>();
        for (int i = 0; i < folders.arraySize; i++) if (i != current) { labels.Add((i + 1) + ". " + folders.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue); indices.Add(i); }
        if (indices.Count == 0) return -1;
        int selected; destinations.TryGetValue(key, out selected);
        selected = EditorGUILayout.Popup("移動先", Mathf.Clamp(selected, 0, indices.Count - 1), labels.ToArray());
        destinations[key] = selected; return indices[selected];
    }
    public static string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";
        input = input.Trim();
        var match = Regex.Match(input, @"wrld_[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");
        return match.Success ? match.Value.ToLowerInvariant() : input;
    }

    public static void Draw(SerializedObject serialized)
    {
        var folders = serialized.FindProperty("folders");
        for (int f = 0; f < folders.arraySize; f++)
        {
            var folder = folders.GetArrayElementAtIndex(f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            folder.isExpanded = EditorGUILayout.Foldout(folder.isExpanded, folder.FindPropertyRelative("name").stringValue + "  (" + folder.FindPropertyRelative(fields[0]).arraySize + "件)", true);
            int move = 0; bool remove = false; int destination = -1;
            if (folder.isExpanded)
            {
                EditorGUILayout.PropertyField(folder.FindPropertyRelative("name"), new GUIContent("フォルダ名"));
                EditorGUILayout.BeginHorizontal();
                using (new EditorGUI.DisabledScope(f == 0)) if (GUILayout.Button("↑")) move = -1;
                using (new EditorGUI.DisabledScope(f + 1 == folders.arraySize)) if (GUILayout.Button("↓")) move = 1;
                remove = GUILayout.Button("フォルダを削除");
                EditorGUILayout.EndHorizontal();
                destination = Destination(folders, f, serialized.targetObject.GetInstanceID() + ":folder:" + f);
                if (destination >= 0) EditorGUILayout.LabelField("削除時に中身を移す場合の移動先", EditorStyles.miniLabel);
                DrawWorlds(folder, folders, f);
            }
            EditorGUILayout.EndVertical();
            if (remove)
            {
                int choice = EditorUtility.DisplayDialogComplex("フォルダ削除", "中身を移動するか、中身ごと削除するか選んでください。", destination >= 0 ? "中身を移動して削除" : "キャンセル", "キャンセル", "中身ごと削除");
                if (choice == 2 || (choice == 0 && destination >= 0))
                {
                    if (choice == 0) while (folder.FindPropertyRelative(fields[0]).arraySize > 0) Transfer(folder, 0, folders.GetArrayElementAtIndex(destination));
                    folders.DeleteArrayElementAtIndex(f); break;
                }
            }
            if (move != 0) { folders.MoveArrayElement(f, f + move); break; }
        }
        if (GUILayout.Button("＋ フォルダを追加"))
        {
            int at = folders.arraySize++; var folder = folders.GetArrayElementAtIndex(at);
            folder.FindPropertyRelative("name").stringValue = "新しいフォルダ";
            foreach (var field in fields) folder.FindPropertyRelative(field).arraySize = 0;
            folder.isExpanded = true;
        }
    }
    static void DrawWorlds(SerializedProperty folder, SerializedProperty folders, int folderIndex)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("お気に入りワールド", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("この保存データの登録一覧を、ワールドの全利用者に表示します。1ページ8件。行を押すと詳細ページを開かず共有ポータルを出します。", MessageType.Info);
        EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
        var ids = folder.FindPropertyRelative("favoriteWorldIds");
        var titles = folder.FindPropertyRelative("favoriteWorldTitles");
        var authors = folder.FindPropertyRelative("favoriteWorldAuthors");
        Align(titles, ids.arraySize); Align(authors, ids.arraySize);
        EditorGUILayout.LabelField("登録数", ids.arraySize + " 件 / " + Mathf.Max(1, (ids.arraySize + 7) / 8) + " ページ");
        var seen = new HashSet<string>();
        for (int i = 0; i < ids.arraySize; i++)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            string name = titles.GetArrayElementAtIndex(i).stringValue;
            var id = ids.GetArrayElementAtIndex(i);
            bool duplicate = !seen.Add(id.stringValue.ToLowerInvariant());
            id.isExpanded = EditorGUILayout.Foldout(id.isExpanded, (i + 1) + ". " + (string.IsNullOrEmpty(name) ? "新しいワールド" : name), true);
            int move = 0; bool remove;
            using (new EditorGUI.DisabledScope(i == 0)) if (GUILayout.Button("↑", GUILayout.Width(30))) move = -1;
            using (new EditorGUI.DisabledScope(i + 1 == ids.arraySize)) if (GUILayout.Button("↓", GUILayout.Width(30))) move = 1;
            remove = GUILayout.Button("削除", GUILayout.Width(46));
            EditorGUILayout.EndHorizontal();
            int destination = -1; bool transfer = false;
            if (id.isExpanded)
            {
            EditorGUI.BeginChangeCheck();
            string entered = EditorGUILayout.TextField(new GUIContent("ワールドURL / ID", "VRChatのワールドURLまたはwrld_で始まるIDを貼り付けてください。"), id.stringValue);
            if (EditorGUI.EndChangeCheck()) id.stringValue = Normalize(entered);
            EditorGUILayout.PropertyField(titles.GetArrayElementAtIndex(i), new GUIContent("タイトル"));
            EditorGUILayout.PropertyField(authors.GetArrayElementAtIndex(i), new GUIContent("作者名"));
            destination = Destination(folders, folderIndex, folder.serializedObject.targetObject.GetInstanceID() + ":" + folderIndex + ":" + i);
            using (new EditorGUI.DisabledScope(destination < 0)) transfer = GUILayout.Button("選択したフォルダへ移動");
            if (!Regex.IsMatch(id.stringValue, @"^wrld_[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$"))
                EditorGUILayout.HelpBox("有効なワールドURLまたはIDを入力してください。この行は入力が完了するまで表示されません。", MessageType.Warning);
            else if (duplicate) EditorGUILayout.HelpBox("同じIDが上の行にあります。一覧には最初の1件だけを表示します。", MessageType.Warning);
            if (string.IsNullOrWhiteSpace(titles.GetArrayElementAtIndex(i).stringValue) || titles.GetArrayElementAtIndex(i).stringValue.Length > 256)
                EditorGUILayout.HelpBox("タイトルを1〜256文字で入力してください。", MessageType.Warning);
            }
            EditorGUILayout.EndVertical();
            if (transfer && destination >= 0) { Transfer(folder, i, folders.GetArrayElementAtIndex(destination)); break; }
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
            ids.GetArrayElementAtIndex(i).isExpanded = true;
            titles.GetArrayElementAtIndex(i).stringValue = "";
            authors.GetArrayElementAtIndex(i).stringValue = "";
        }
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.Space();
    }
}
