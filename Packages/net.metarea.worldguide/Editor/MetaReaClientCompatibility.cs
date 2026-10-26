using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using VRC.SDK3.Components;

public static class MetaReaClientCompatibility
{
    [MenuItem("MetaRea/選択した壁新聞の撮影・操作設定を修復")]
    public static void RepairSelected()
    {
        var g = Selection.activeGameObject == null ? null : Selection.activeGameObject.GetComponentInParent<MetaReaWallGuide>();
        if (g == null) throw new System.InvalidOperationException("HierarchyでMetaRea Wall Guideを選択してください。");
        Apply(g);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(g.gameObject.scene);
    }

    public static void Apply(MetaReaWallGuide g)
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play mode first.");
        foreach (var canvas in g.GetComponentsInChildren<Canvas>(true))
        {
            canvas.renderMode = RenderMode.WorldSpace;
            // Layer 5 hides world UI from VRChat's normal pointer and photo camera.
            foreach (var t in canvas.GetComponentsInChildren<Transform>(true))
            { Undo.RecordObject(t.gameObject, "Repair MetaRea UI layer"); t.gameObject.layer = 0; EditorUtility.SetDirty(t.gameObject); }
            if (canvas.GetComponent<VRCUiShape>() != null)
            {
                var raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (raycaster == null) raycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();
                var collider = canvas.GetComponent<BoxCollider>();
                if (collider == null) collider = canvas.gameObject.AddComponent<BoxCollider>();
                var rect = ((RectTransform)canvas.transform).rect;
                collider.center = new Vector3(rect.center.x, rect.center.y, 0);
                collider.size = new Vector3(rect.width, rect.height, 8);
                EditorUtility.SetDirty(collider);
            }
            EditorUtility.SetDirty(canvas);
        }
        foreach (var selectable in g.GetComponentsInChildren<Selectable>(true))
        { selectable.navigation = new Navigation { mode = Navigation.Mode.None }; EditorUtility.SetDirty(selectable); }
    }
}
