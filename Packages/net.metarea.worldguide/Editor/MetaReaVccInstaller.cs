using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

// This is the only code kept in the VCC package. All guide assets and scripts
// are installed below Assets, with their original GUIDs.
[InitializeOnLoad]
public static class MetaReaVccInstaller
{
    const string PackageRoot = "Packages/net.metarea.worldguide";
    public const string AssetRoot = "Assets/MetaRea_World_GUIDE";
    const string StatePath = "ProjectSettings/MetaReaWorldGuideInstall.json";
    const string Entry = "MetaRea World GUIDE.prefab";
    static bool queued;
    static bool force;

    [Serializable] public class FileRecord { public string path; public string sha256; }
    [Serializable] public class Manifest { public string version; public string payloadSha256; public FileRecord[] files; }
    [Serializable] public class InstallState { public string version; public string payloadSha256; public FileRecord[] files; }

    static MetaReaVccInstaller() { Schedule(); }
    static void Schedule()
    {
        if (queued) return;
        queued = true;
        EditorApplication.delayCall += InstallWhenReady;
    }

    static void InstallWhenReady()
    {
        queued = false;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        { Schedule(); return; }
        string manifestPath = PackageRoot + "/Payload~/manifest.json";
        if (!File.Exists(manifestPath)) return; // Authoring checkout, before packaging.
        try
        {
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(manifestPath));
            var state = File.Exists(StatePath) ? JsonUtility.FromJson<InstallState>(File.ReadAllText(StatePath)) : null;
            if (!force && state != null && state.payloadSha256 == manifest.payloadSha256 &&
                File.Exists(AssetRoot + "/Scripts/MetaReaWallGuide.cs") && File.Exists(AssetRoot + "/" + Entry)) return;
            force = false;
            AssetDatabase.StartAssetEditing();
            string backup;
            try { backup = InstallFiles(Directory.GetCurrentDirectory(), PackageRoot + "/Payload~/files.zip", manifest); }
            finally { AssetDatabase.StopAssetEditing(); }
            AssetDatabase.Refresh();
            Debug.Log("MetaRea World GUIDE " + manifest.version + ": " + AssetRoot + " に配置しました。" +
                (string.IsNullOrEmpty(backup) ? "" : " 変更前のファイル: " + backup));
        }
        catch (Exception error)
        {
            Debug.LogError("MetaRea World GUIDE: Assetsへの配置を完了できませんでした。" + error.Message +
                "\n原因を解消後、MetaRea → Assetsへの配置を再試行 を実行してください。");
        }
    }

    [MenuItem("MetaRea/Assetsへの配置を再試行")]
    public static void Retry() { force = true; Schedule(); }

    public static string HashFile(string path)
    {
        using (var hash = SHA256.Create())
        using (var stream = File.OpenRead(path))
            return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
    }

    static string SafePath(string root, string relative)
    {
        if (string.IsNullOrEmpty(relative) || relative.Contains("\\") || Path.IsPathRooted(relative) ||
            relative.Split('/').Any(p => p == ".." || p == "." || p.Length == 0) ||
            relative.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) || relative.IndexOf(':') >= 0)
            throw new IOException("不正な配布パス: " + relative);
        string fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string target = Path.GetFullPath(Path.Combine(root, relative));
        if (!target.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase)) throw new IOException("配置先が範囲外です。");
        for (string parent = Path.GetDirectoryName(target); parent != null && parent.Length >= fullRoot.Length - 1; parent = Path.GetDirectoryName(parent))
            if (Directory.Exists(parent) && (File.GetAttributes(parent) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("リンクされた配置先には書き込めません: " + parent);
        if (File.Exists(target) && (File.GetAttributes(target) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("リンクされたファイルには書き込めません: " + target);
        return target;
    }

    static bool PreserveEntry(string relative) { return relative == Entry || relative == Entry + ".meta"; }

    // Filesystem-only core, also exercised against an isolated test project.
    // Validate the entire payload before touching Assets. Back up changes and
    // roll them back if a write fails; publish the state only after success.
    public static string InstallFiles(string projectRoot, string zipPath, Manifest manifest)
    {
        projectRoot = Path.GetFullPath(projectRoot);
        if (manifest == null || manifest.files == null || string.IsNullOrEmpty(manifest.version) ||
            !string.Equals(HashFile(zipPath), manifest.payloadSha256, StringComparison.OrdinalIgnoreCase))
            throw new IOException("配布ファイルの整合性チェックに失敗しました。");
        string root = Path.Combine(projectRoot, AssetRoot);
        string statePath = Path.Combine(projectRoot, StatePath);
        var old = File.Exists(statePath) ? JsonUtility.FromJson<InstallState>(File.ReadAllText(statePath)) : null;
        string stage = Path.Combine(projectRoot, "Library/MetaReaWorldGuide/" + Guid.NewGuid().ToString("N"));
        string backup = Path.Combine(projectRoot, "MetaReaWorldGuideBackups/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8));
        var changed = new List<string>();
        var created = new List<string>();
        bool backedUp = false;
        Directory.CreateDirectory(stage);
        try
        {
            var expected = new Dictionary<string, FileRecord>(StringComparer.OrdinalIgnoreCase);
            foreach (var record in manifest.files)
            {
                SafePath(root, record.path);
                if (expected.ContainsKey(record.path)) throw new IOException("重複する配布パスです。");
                expected.Add(record.path, record);
            }
            using (var zip = ZipFile.OpenRead(zipPath))
            {
                if (zip.Entries.Sum(e => e.Length) > 256L * 1024 * 1024) throw new IOException("配布ファイルが大きすぎます。");
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var entry in zip.Entries)
                {
                    if (!expected.ContainsKey(entry.FullName) || !seen.Add(entry.FullName)) throw new IOException("配布内容が一覧と一致しません。");
                    string destination = SafePath(stage, entry.FullName);
                    Directory.CreateDirectory(Path.GetDirectoryName(destination));
                    entry.ExtractToFile(destination);
                    if (!string.Equals(HashFile(destination), expected[entry.FullName].sha256, StringComparison.OrdinalIgnoreCase))
                        throw new IOException("配布ファイルが破損しています: " + entry.FullName);
                }
                if (seen.Count != expected.Count) throw new IOException("配布ファイルが不足しています。");
            }
            // A legacy entry is migrated by the Assets-side helper after import.
            // Preserve its prefab before installing a new default at the same path.
            string legacy = Path.Combine(projectRoot, "Assets/MetaRea World GUIDE");
            if (Directory.Exists(legacy) && !Directory.Exists(root)) Directory.Move(legacy, root);
            string legacyMeta = legacy + ".meta";
            if (!Directory.Exists(legacy) && File.Exists(legacyMeta) && !File.Exists(root + ".meta"))
                File.Move(legacyMeta, root + ".meta");
            if (Directory.Exists(legacy) && File.Exists(Path.Combine(legacy, Entry)) && !File.Exists(Path.Combine(root, Entry)))
            {
                Directory.CreateDirectory(root);
                File.Move(Path.Combine(legacy, Entry), Path.Combine(root, Entry));
                if (File.Exists(Path.Combine(legacy, Entry + ".meta")) && !File.Exists(Path.Combine(root, Entry + ".meta")))
                    File.Move(Path.Combine(legacy, Entry + ".meta"), Path.Combine(root, Entry + ".meta"));
            }
            foreach (var record in manifest.files)
            {
                string target = SafePath(root, record.path);
                if (File.Exists(target) && PreserveEntry(record.path)) continue;
                // Existing document GUIDs may have been generated by 0.8.x.
                if (File.Exists(target) && (record.path.EndsWith(".txt.meta") || record.path.EndsWith(".md.meta"))) continue;
                if (File.Exists(target) && HashFile(target) == record.sha256) continue;
                if (File.Exists(target))
                {
                    string copy = SafePath(backup, record.path);
                    Directory.CreateDirectory(Path.GetDirectoryName(copy));
                    File.Copy(target, copy);
                    changed.Add(record.path); backedUp = true;
                }
                else created.Add(record.path);
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Copy(SafePath(stage, record.path), target, true);
            }
            // Only remove files tracked by our previous manifest. Back up even
            // modified obsolete scripts so stale classes cannot break compilation.
            if (old != null && old.files != null) foreach (var record in old.files)
            {
                if (expected.ContainsKey(record.path) || PreserveEntry(record.path)) continue;
                string target = SafePath(root, record.path);
                if (!File.Exists(target)) continue;
                string copy = SafePath(backup, record.path);
                Directory.CreateDirectory(Path.GetDirectoryName(copy));
                File.Copy(target, copy);
                changed.Add(record.path); backedUp = true;
                File.Delete(target);
            }
            Directory.CreateDirectory(Path.GetDirectoryName(statePath));
            string nextState = statePath + ".tmp";
            File.WriteAllText(nextState, JsonUtility.ToJson(new InstallState { version = manifest.version,
                payloadSha256 = manifest.payloadSha256, files = manifest.files }, true));
            if (File.Exists(statePath)) File.Replace(nextState, statePath, null);
            else File.Move(nextState, statePath);
            return backedUp ? backup : "";
        }
        catch
        {
            foreach (string relative in created) { string target = SafePath(root, relative); if (File.Exists(target)) File.Delete(target); }
            foreach (string relative in changed) File.Copy(SafePath(backup, relative), SafePath(root, relative), true);
            throw;
        }
        finally { if (Directory.Exists(stage)) Directory.Delete(stage, true); }
    }
}
