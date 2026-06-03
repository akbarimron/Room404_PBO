using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[InitializeOnLoad]
public static class GhostPrefabFixer
{
    static GhostPrefabFixer()
    {
        EditorApplication.delayCall += () => {
            ConfigureAnimationLoops();
            ResetOverrides();
        };
    }

    [MenuItem("Tools/Configure Ghost Animation Loops")]
    public static void ConfigureAnimationLoops()
    {
        string[] fbxPaths = new string[]
        {
            "Assets/NewAssets/Animation/Zombie Idle.fbx",
            "Assets/NewAssets/Animation/Injured Run.fbx",
            "Assets/NewAssets/Animation/Walking.fbx"
        };

        foreach (var path in fbxPaths)
        {
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[GhostPrefabFixer] Could not find asset at {path}");
                continue;
            }

            ModelImporterClipAnimation[] clips = importer.clipAnimations;
            if (clips == null || clips.Length == 0)
            {
                clips = importer.defaultClipAnimations;
            }

            if (clips != null && clips.Length > 0)
            {
                bool modified = false;
                for (int i = 0; i < clips.Length; i++)
                {
                    if (!clips[i].loopTime)
                    {
                        clips[i].loopTime = true;
                        clips[i].loop = true;
                        modified = true;
                    }
                }

                if (modified)
                {
                    importer.clipAnimations = clips;
                    importer.SaveAndReimport();
                    Debug.Log($"[GhostPrefabFixer] Successfully set loopTime = true on {path}");
                }
            }
            else
            {
                Debug.LogWarning($"[GhostPrefabFixer] No clips found in {path}");
            }
        }
    }

    [MenuItem("Tools/Reset Ghost Prefab Bone Overrides")]
    public static void ResetOverrides()
    {
        GhostAI ghost = Object.FindFirstObjectByType<GhostAI>();
        if (ghost == null)
        {
            Debug.LogWarning("[GhostPrefabFixer] GhostAI not found in active scene.");
            return;
        }

        GameObject go = ghost.gameObject;
        if (!PrefabUtility.IsPartOfAnyPrefab(go))
        {
            Debug.LogWarning("[GhostPrefabFixer] Ghost GameObject is not part of a prefab.");
            return;
        }

        GameObject prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
        var modifications = PrefabUtility.GetPropertyModifications(prefabRoot);
        if (modifications == null) return;

        var keptModifications = new List<PropertyModification>();
        int revertedCount = 0;

        foreach (var mod in modifications)
        {
            if (mod.target == null) continue;

            // Keep modifications on the outermost prefab root itself, or root component settings
            if (mod.target == prefabRoot.transform || 
                mod.target == prefabRoot ||
                mod.propertyPath.Contains("m_Avatar") || 
                mod.propertyPath.Contains("m_Controller") || 
                mod.propertyPath.Contains("m_AnimatePhysics") || 
                mod.propertyPath.Contains("m_ApplyRootMotion") ||
                mod.target.GetType().Name == "GhostAI" ||
                mod.target.GetType().Name == "NavMeshAgent")
            {
                keptModifications.Add(mod);
            }
            else
            {
                revertedCount++;
            }
        }

        if (revertedCount > 0)
        {
            PrefabUtility.SetPropertyModifications(prefabRoot, keptModifications.ToArray());
            EditorUtility.SetDirty(prefabRoot);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(prefabRoot.scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            Debug.Log($"[GhostPrefabFixer] Successfully reverted {revertedCount} bone/mesh overrides on the ghost prefab in the scene.");
        }
    }
}
