using UnityEngine;
using UnityEditor;

// Editor utility to quickly toggle Settings UI visibility in the Hierarchy (hides it from Game view)
public static class SettingsUIEditor
{
    // Menu item with hotkey Ctrl+Shift+H (Windows). Toggles active state for selected objects that have SettingsUI.
    [MenuItem("Tools/Toggle Settings Visibility %#h")]
    private static void ToggleSettingsVisibilityMenu()
    {
        foreach (var obj in Selection.gameObjects)
        {
            if (obj == null) continue;
            if (obj.GetComponent<SettingsUI>() == null) continue;

            Undo.RegisterFullObjectHierarchyUndo(obj, "Toggle Settings Visibility");
            obj.SetActive(!obj.activeSelf);
            EditorUtility.SetDirty(obj);
        }
    }

    // Validation so the menu is only enabled when a SettingsUI is selected
    [MenuItem("Tools/Toggle Settings Visibility %#h", true)]
    private static bool ToggleSettingsVisibilityMenuValidate()
    {
        foreach (var obj in Selection.gameObjects)
            if (obj != null && obj.GetComponent<SettingsUI>() != null)
                return true;
        return false;
    }

    // Context menu entry under GameObject for convenience
    [MenuItem("GameObject/Toggle Settings Visibility", false, 0)]
    private static void ToggleSettingsVisibilityContext()
    {
        var obj = Selection.activeGameObject;
        if (obj == null) return;
        if (obj.GetComponent<SettingsUI>() == null) return;

        Undo.RegisterFullObjectHierarchyUndo(obj, "Toggle Settings Visibility");
        obj.SetActive(!obj.activeSelf);
        EditorUtility.SetDirty(obj);
    }

    [MenuItem("GameObject/Toggle Settings Visibility", true)]
    private static bool ToggleSettingsVisibilityContextValidate()
    {
        var obj = Selection.activeGameObject;
        return obj != null && obj.GetComponent<SettingsUI>() != null;
    }
}
