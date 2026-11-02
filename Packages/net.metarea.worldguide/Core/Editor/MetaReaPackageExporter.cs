using System;
using System.Linq;
using UnityEditor;
using UdonSharp;

public static class MetaReaPackageExporter
{
    // Developer utility. Public distribution is through VCC only.
    public static void Export()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
        string path = EditorUtility.SaveFilePanel("MetaRea 配布パッケージ", "", "MetaRea_World_GUIDE-v0.8.2", "unitypackage");
        if (string.IsNullOrEmpty(path)) return;
        UdonSharpProgramAsset.CompileAllCsPrograms(true, false);
        if (UdonSharpProgramAsset.AnyUdonSharpScriptHasError()) throw new InvalidOperationException("Resolve Udon compile errors before exporting.");
        ExportTo(path);
    }

    public static void ExportTo(string path)
    {
        AssetDatabase.SaveAssets();
        string root = MetaReaPrototypeBuilder.Root;
        var assets = AssetDatabase.GetAllAssetPaths().Where(p =>
            (p == root || p.StartsWith(root + "/")) &&
            p != root + "/Scenes" && !p.StartsWith(root + "/Scenes/") &&
            p != root + "/Prefabs" && !p.StartsWith(root + "/Prefabs/")).ToArray();
        // Explicit paths only: do not pull SDK utilities or unrelated Udon programs into the package.
        AssetDatabase.ExportPackage(assets, path, ExportPackageOptions.Default);
    }
}
