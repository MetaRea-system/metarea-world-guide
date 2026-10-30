using UnityEditor;
using UdonSharpEditor;

[CustomEditor(typeof(MetaReaFavoritesSettings))]
public class MetaReaFavoritesSettingsInspector : Editor
{
    public override void OnInspectorGUI()
    {
        if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;
        serializedObject.Update();
        MetaReaFavoritesInspector.Draw(serializedObject);
        if (serializedObject.ApplyModifiedProperties() && !EditorApplication.isPlaying)
        {
            UdonSharpEditorUtility.CopyProxyToUdon((MetaReaFavoritesSettings)target);
            EditorUtility.SetDirty(target);
            var settings = (MetaReaFavoritesSettings)target;
            if (settings.gameObject.scene.IsValid()) UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(settings.gameObject.scene);
        }
    }
}
