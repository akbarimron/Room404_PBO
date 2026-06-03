using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using System.Text;
using System.IO;

[InitializeOnLoad]
public static class GhostDiagnosis
{
    static GhostDiagnosis()
    {
        EditorApplication.delayCall += Diagnose;
    }

    [MenuItem("Tools/Diagnose Ghost")]
    public static void Diagnose()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== GHOST DIAGNOSIS ===");

        // Find the ghost GameObject
        GhostAI ghost = Object.FindFirstObjectByType<GhostAI>();
        if (ghost == null)
        {
            sb.AppendLine("ERROR: GhostAI component not found in the active scene!");
            SaveLog(sb.ToString());
            return;
        }

        GameObject go = ghost.gameObject;
        sb.AppendLine($"Ghost GameObject Name: {go.name}");
        sb.AppendLine($"Active Self: {go.activeSelf}");
        sb.AppendLine($"Layer: {LayerMask.LayerToName(go.layer)}");
        sb.AppendLine($"Tag: {go.tag}");
        sb.AppendLine($"World Position: {go.transform.position}");
        sb.AppendLine($"Local Position: {go.transform.localPosition}");
        sb.AppendLine($"World Scale: {go.transform.lossyScale}");
        sb.AppendLine($"Local Scale: {go.transform.localScale}");

        sb.AppendLine("\n--- COMPONENTS ---");
        Component[] components = go.GetComponents<Component>();
        foreach (var comp in components)
        {
            if (comp == null) continue;
            sb.AppendLine($"- {comp.GetType().Name}");

            if (comp is NavMeshAgent agent)
            {
                sb.AppendLine($"  - Enabled: {agent.enabled}");
                sb.AppendLine($"  - Height: {agent.height}");
                sb.AppendLine($"  - Radius: {agent.radius}");
                sb.AppendLine($"  - Base Offset: {agent.baseOffset}");
                sb.AppendLine($"  - Speed: {agent.speed}");
                sb.AppendLine($"  - Acceleration: {agent.acceleration}");
                sb.AppendLine($"  - Is On NavMesh: {agent.isOnNavMesh}");
            }
            else if (comp is Animator animator)
            {
                sb.AppendLine($"  - Enabled: {animator.enabled}");
                sb.AppendLine($"  - Avatar: {(animator.avatar != null ? animator.avatar.name : "NULL")}");
                sb.AppendLine($"  - Avatar Is Valid: {(animator.avatar != null ? animator.avatar.isValid.ToString() : "N/A")}");
                sb.AppendLine($"  - Avatar Is Human: {(animator.avatar != null ? animator.avatar.isHuman.ToString() : "N/A")}");
                sb.AppendLine($"  - Apply Root Motion: {animator.applyRootMotion}");
                sb.AppendLine($"  - Controller: {(animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "NULL")}");
            }
            else if (comp is CapsuleCollider capsule)
            {
                sb.AppendLine($"  - Enabled: {capsule.enabled}");
                sb.AppendLine($"  - Radius: {capsule.radius}");
                sb.AppendLine($"  - Height: {capsule.height}");
                sb.AppendLine($"  - Center: {capsule.center}");
                sb.AppendLine($"  - Is Trigger: {capsule.isTrigger}");
            }
            else if (comp is CharacterController cc)
            {
                sb.AppendLine($"  - Enabled: {cc.enabled}");
                sb.AppendLine($"  - Height: {cc.height}");
                sb.AppendLine($"  - Center: {cc.center}");
            }
            else if (comp is Rigidbody rb)
            {
                sb.AppendLine($"  - Is Kinematic: {rb.isKinematic}");
                sb.AppendLine($"  - Use Gravity: {rb.useGravity}");
                sb.AppendLine($"  - Constraints: {rb.constraints}");
            }
        }

        sb.AppendLine("\n--- HIERARCHY AND LOCAL POSITIONS ---");
        DumpTransform(go.transform, "", sb);

        SaveLog(sb.ToString());
    }

    private static void DumpTransform(Transform t, string indent, StringBuilder sb)
    {
        sb.AppendLine($"{indent}- {t.name} (Local Pos: {t.localPosition}, Local Scale: {t.localScale})");
        for (int i = 0; i < t.childCount; i++)
        {
            DumpTransform(t.GetChild(i), indent + "  ", sb);
        }
    }

    private static void SaveLog(string content)
    {
        string logPath = "d:/Project/unity/My project/ghost_diag.txt";
        File.WriteAllText(logPath, content);
        Debug.Log($"[GhostDiagnosis] Saved diagnosis log to {logPath}");
    }
}
