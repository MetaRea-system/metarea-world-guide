using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

// Layout 3 includes Core in the VCC archive: never remove or rewrite runtime
// assets during an Editor callback. Layout 2's migrator remains for old payloads.
[InitializeOnLoad]
public static class MetaReaVccInstaller
{
    public const string PackageRoot = "Packages/net.metarea.worldguide";
    public const string AssetRoot = "Assets/MetaRea_World_GUIDE";
    const string LegacyRoot = "Assets/MetaRea World GUIDE";
    public const string CoreRoot = PackageRoot + "/Core";
    const string StatePath = "ProjectSettings/MetaReaWorldGuideInstall.json";
    const string JournalPath = "ProjectSettings/MetaReaWorldGuideMigration.json";
    static bool queued, force;
    [Serializable] public class FileRecord { public string path; public string sha256; }
    [Serializable] public class Manifest { public int layout; public string version; public string payloadSha256; public FileRecord[] files; public FileRecord[] legacyFiles; }
    [Serializable] public class InstallState { public int layout; public string version; public string payloadSha256; public FileRecord[] files; }
    [Serializable] public class UndoRecord { public string path; public bool existed; public string backupFile; }
    [Serializable] public class Journal { public string backup; public List<UndoRecord> changes = new List<UndoRecord>(); }

    static MetaReaVccInstaller() { Schedule(); }
    static void Schedule() { if (queued) return; queued = true; EditorApplication.delayCall += InstallWhenReady; }
    static void InstallWhenReady()
    {
        queued = false;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) { Schedule(); return; }
        string path = PackageRoot + "/Payload~/manifest.json";
        if (!File.Exists(path)) return;
        try
        {
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(path));
            if (manifest != null && manifest.layout == 3)
            {
                CompleteDirectInstall(Directory.GetCurrentDirectory(), manifest);
                force = false;
                return;
            }
            var state = File.Exists(StatePath) ? JsonUtility.FromJson<InstallState>(File.ReadAllText(StatePath)) : null;
            if (!force && !File.Exists(JournalPath) && state != null && state.layout == 2 && state.payloadSha256 == manifest.payloadSha256 && manifest.files.All(f => File.Exists(f.path))) return;
            force = false; string backup;
            AssetDatabase.StartAssetEditing();
            try { backup = InstallFiles(Directory.GetCurrentDirectory(), PackageRoot + "/Payload~/files.zip", manifest); }
            finally { AssetDatabase.StopAssetEditing(); }
            AssetDatabase.Refresh();
            Debug.Log("MetaRea World GUIDE " + manifest.version + ": 本体はPackages、設置用プレハブとUserContentはAssetsです。変更前のバックアップ: " + backup);
        }
        catch (Exception e) { Debug.LogError("MetaRea World GUIDE: パッケージの確認・設定を完了できませんでした。\n" + e.Message + "\n原因を解消後、Unityを開き直してください。"); }
    }

    public static void Retry() { force = true; Schedule(); }
    // VCC verifies the archive hash before extraction. Unity may reserialize
    // compiled Udon assets on import, so do not compare/rewrite Core bytes here.
    // Only the managed manual and the installation record may be changed.
    public static void CompleteDirectInstall(string projectRoot, Manifest manifest)
    {
        if (manifest == null || manifest.layout != 3 || manifest.files == null || manifest.files.Length == 0)
            throw new IOException("直接配置の配布情報が不正です。");
        if (File.Exists(SafePath(projectRoot, JournalPath)))
            throw new IOException("旧版の移行が中断されています。バックアップを保全して旧版で移行を完了してから更新してください。");
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in manifest.files)
        {
            if (!ManagedTarget(f.path) || !seen.Add(f.path)) throw new IOException("配布対象が不正です: " + f.path);
            string target = SafePath(projectRoot, f.path);
            if (f.path.StartsWith(CoreRoot + "/", StringComparison.Ordinal) || f.path == CoreRoot + ".meta")
            {
                if (!File.Exists(target)) throw new IOException("本体ファイルが不足しています。VCCで再導入してください: " + f.path);
            }
            else
            {
                string source = SafePath(projectRoot, PackageRoot + "/Documentation~/" + Path.GetFileName(f.path));
                if (!File.Exists(source) || HashFile(source) != f.sha256) throw new IOException("説明書の検証に失敗しました。");
            }
        }
        // Old Assets-based installations must first use 0.17.0's backed-up
        // migration. Never delete user-edited legacy assets on this code path.
        if (manifest.legacyFiles != null) foreach (var f in manifest.legacyFiles)
        {
            if (!f.path.StartsWith("Scripts/", StringComparison.Ordinal) || !f.path.EndsWith(".cs", StringComparison.Ordinal)) continue;
            foreach (string root in new[] { AssetRoot, LegacyRoot })
                if (File.Exists(SafePath(projectRoot, root + "/" + f.path)))
                    throw new IOException("Assets内に旧本体があります。先に0.17.0で移行を完了してから更新してください。");
        }
        string statePath = SafePath(projectRoot, StatePath);
        string stateJson = JsonUtility.ToJson(new InstallState { layout = 3, version = manifest.version, payloadSha256 = manifest.payloadSha256, files = manifest.files }, true);
        var journal = new Journal { backup = "MetaReaWorldGuideBackups/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0,8) };
        try
        {
            foreach (var f in manifest.files)
            {
                if (!f.path.StartsWith(AssetRoot + "/README.txt", StringComparison.Ordinal)) continue;
                string target = SafePath(projectRoot, f.path);
                if (File.Exists(target) && (f.path.EndsWith(".meta", StringComparison.Ordinal) || HashFile(target) == f.sha256)) continue;
                BeforeChange(projectRoot, journal, f.path);
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Copy(SafePath(projectRoot, PackageRoot + "/Documentation~/" + Path.GetFileName(f.path)), target, true);
            }
            if (!File.Exists(statePath) || File.ReadAllText(statePath) != stateJson)
            {
                BeforeChange(projectRoot, journal, StatePath);
                Directory.CreateDirectory(Path.GetDirectoryName(statePath));
                File.WriteAllText(statePath, stateJson);
            }
            string pending = SafePath(projectRoot, JournalPath);
            if (File.Exists(pending)) File.Delete(pending);
        }
        catch { Recover(projectRoot); throw; }
    }
    public static string HashFile(string path)
    {
        using (var hash = SHA256.Create()) using (var stream = File.OpenRead(path))
            return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
    }
    static string SafePath(string root, string relative)
    {
        if (string.IsNullOrEmpty(relative) || relative.Contains("\\") || Path.IsPathRooted(relative) || relative.IndexOf(':') >= 0 || relative.Split('/').Any(p => p == ".." || p == "." || p.Length == 0)) throw new IOException("不正な配置パス: " + relative);
        string fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string target = Path.GetFullPath(Path.Combine(root, relative));
        if (!target.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase)) throw new IOException("配置先が範囲外です。");
        for (string p = target; p != null && p.Length >= fullRoot.Length - 1; p = Path.GetDirectoryName(p))
            if ((Directory.Exists(p) || File.Exists(p)) && (File.GetAttributes(p) & FileAttributes.ReparsePoint) != 0) throw new IOException("リンクされた配置先には書き込めません: " + p);
        return target;
    }
    static bool ManagedTarget(string path)
    {
        return path.StartsWith(CoreRoot + "/", StringComparison.Ordinal) || path == CoreRoot + ".meta" || path == AssetRoot + "/README.txt" || path == AssetRoot + "/README.txt.meta";
    }
    static void BeforeChange(string project, Journal journal, string relative)
    {
        if (journal.changes.Any(c => c.path == relative)) return;
        string target = SafePath(project, relative); bool exists = File.Exists(target);
        string backupFile = journal.changes.Count.ToString();
        if (exists)
        {
            string saved = SafePath(SafePath(project, journal.backup), backupFile);
            Directory.CreateDirectory(Path.GetDirectoryName(saved)); File.Copy(target, saved, true);
        }
        journal.changes.Add(new UndoRecord { path = relative, existed = exists, backupFile = backupFile });
        string path = SafePath(project, JournalPath); Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonUtility.ToJson(journal, true)); // Before mutation.
        string backupIndex = SafePath(SafePath(project,journal.backup),"index.json");
        Directory.CreateDirectory(Path.GetDirectoryName(backupIndex));
        File.WriteAllText(backupIndex,JsonUtility.ToJson(journal,true));
    }
    public static void Recover(string project)
    {
        string path = SafePath(project, JournalPath); if (!File.Exists(path)) return;
        var journal = JsonUtility.FromJson<Journal>(File.ReadAllText(path));
        if (journal == null || journal.backup == null || !journal.backup.StartsWith("MetaReaWorldGuideBackups/", StringComparison.Ordinal)) throw new IOException("移行記録が不正です。");
        foreach (var change in journal.changes.AsEnumerable().Reverse())
        {
            if (!(change.path.StartsWith(AssetRoot + "/", StringComparison.Ordinal) || change.path.StartsWith(LegacyRoot + "/", StringComparison.Ordinal) || change.path.StartsWith(CoreRoot + "/", StringComparison.Ordinal) || change.path == CoreRoot + ".meta" || change.path == StatePath)) throw new IOException("復元対象が範囲外です。");
            string target = SafePath(project, change.path);
            if (change.existed)
            {
                string source = SafePath(SafePath(project, journal.backup), string.IsNullOrEmpty(change.backupFile) ? change.path : change.backupFile);
                Directory.CreateDirectory(Path.GetDirectoryName(target)); File.Copy(source, target, true);
            }
            else if (File.Exists(target)) File.Delete(target);
        }
        File.Delete(path);
    }
    // Filesystem-only core, tested against isolated project fixtures.
    public static string InstallFiles(string projectRoot, string zipPath, Manifest manifest)
    {
        projectRoot = Path.GetFullPath(projectRoot); Recover(projectRoot);
        if (manifest == null || manifest.layout != 2 || manifest.files == null || manifest.files.Length == 0 || HashFile(zipPath) != manifest.payloadSha256) throw new IOException("配布データの検証に失敗しました。");
        var expected = new Dictionary<string, FileRecord>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in manifest.files)
        {
            SafePath(projectRoot, f.path);
            if (!ManagedTarget(f.path) || f.path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) || expected.ContainsKey(f.path)) throw new IOException("配布対象が不正です: " + f.path);
            expected.Add(f.path, f);
        }
        string stage = SafePath(projectRoot, "Library/MRGStage/" + Guid.NewGuid().ToString("N").Substring(0,8));
        Directory.CreateDirectory(stage);
        using (var archive = ZipFile.OpenRead(zipPath))
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in archive.Entries)
            {
                if (!expected.ContainsKey(item.FullName) || !seen.Add(item.FullName)) throw new IOException("ZIP内に不正なファイルがあります。");
                string output = SafePath(stage, Array.IndexOf(manifest.files,expected[item.FullName]).ToString()); Directory.CreateDirectory(Path.GetDirectoryName(output)); item.ExtractToFile(output);
                if (HashFile(output) != expected[item.FullName].sha256) throw new IOException("ファイルのハッシュが一致しません: " + item.FullName);
            }
            if (seen.Count != expected.Count) throw new IOException("配布ファイルが不足しています。");
        }
        string statePath = SafePath(projectRoot, StatePath);
        var old = File.Exists(statePath) ? JsonUtility.FromJson<InstallState>(File.ReadAllText(statePath)) : null;
        var journal = new Journal { backup = "MetaReaWorldGuideBackups/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0,8) };
        try
        {
            var legacy = new Dictionary<string, FileRecord>(StringComparer.OrdinalIgnoreCase);
            if (manifest.legacyFiles != null) foreach (var f in manifest.legacyFiles) legacy[f.path] = f;
            if (old != null && old.layout < 2 && old.files != null) foreach (var f in old.files) legacy[f.path] = f;
            foreach (var f in legacy.Values)
            {
                if (f.path == "MetaRea World GUIDE.prefab" || f.path == "MetaRea World GUIDE.prefab.meta" || f.path == "README.txt" || f.path == "README.txt.meta" || f.path.StartsWith("UserContent/", StringComparison.Ordinal)) continue;
                foreach (string root in new[] { AssetRoot, LegacyRoot })
                {
                    string relative = root + "/" + f.path; string target = SafePath(projectRoot, relative);
                    if (!File.Exists(target)) continue;
                    BeforeChange(projectRoot, journal, relative); File.Delete(target);
                }
            }
            if (old != null && old.layout == 2 && old.files != null) foreach (var f in old.files)
            {
                if (expected.ContainsKey(f.path) || !ManagedTarget(f.path)) continue;
                string target = SafePath(projectRoot, f.path); if (!File.Exists(target)) continue;
                BeforeChange(projectRoot, journal, f.path); File.Delete(target);
            }
            foreach (var f in manifest.files)
            {
                string target = SafePath(projectRoot, f.path);
                if (f.path == AssetRoot + "/README.txt.meta" && File.Exists(target)) continue;
                if (File.Exists(target) && HashFile(target) == f.sha256) continue;
                BeforeChange(projectRoot, journal, f.path); Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Copy(SafePath(stage, Array.IndexOf(manifest.files,f).ToString()), target, true);
            }
            Directory.CreateDirectory(SafePath(projectRoot, AssetRoot + "/UserContent"));
            string stateJson = JsonUtility.ToJson(new InstallState { layout = 2, version = manifest.version, payloadSha256 = manifest.payloadSha256, files = manifest.files }, true);
            if (!File.Exists(statePath) || File.ReadAllText(statePath) != stateJson)
            {
                BeforeChange(projectRoot, journal, StatePath);
                File.WriteAllText(statePath, stateJson);
            }
            if (File.Exists(SafePath(projectRoot, JournalPath))) File.Delete(SafePath(projectRoot, JournalPath));
            return journal.changes.Count == 0 ? "" : SafePath(projectRoot, journal.backup);
        }
        catch { Recover(projectRoot); throw; }
        finally
        {
            if (Directory.Exists(stage) && stage.StartsWith(Path.GetFullPath(Path.Combine(projectRoot,"Library","MRGStage")) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) Directory.Delete(stage, true);
        }
    }
}
