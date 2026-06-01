using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

// Editor utility to automatically generate a Humanoid Avatar from any imported GLTF/GLB or custom character model GameObject in the scene
public static class AvatarGenerator
{
    private static readonly Dictionary<string, string[]> boneNameKeywords = new Dictionary<string, string[]>()
    {
        { "Hips", new[] { "hips" } },
        { "Spine", new[] { "spine" } },
        { "Chest", new[] { "spine1", "chest" } },
        { "UpperChest", new[] { "spine2", "upperchest" } },
        { "Neck", new[] { "neck" } },
        { "Head", new[] { "head" } },
        { "LeftShoulder", new[] { "leftshoulder", "l_shoulder", "left_shoulder" } },
        { "LeftUpperArm", new[] { "leftarm", "l_uparm", "left_uparm", "leftupperarm" } },
        { "LeftLowerArm", new[] { "leftforearm", "l_forearm", "left_forearm", "leftlowerarm" } },
        { "LeftHand", new[] { "lefthand", "l_hand", "left_hand" } },
        { "RightShoulder", new[] { "rightshoulder", "r_shoulder", "right_shoulder" } },
        { "RightUpperArm", new[] { "rightarm", "r_uparm", "right_uparm", "rightupperarm" } },
        { "RightLowerArm", new[] { "rightforearm", "r_forearm", "right_forearm", "rightlowerarm" } },
        { "RightHand", new[] { "righthand", "r_hand", "right_hand" } },
        { "LeftUpperLeg", new[] { "leftupleg", "l_thigh", "left_thigh", "leftupleg" } },
        { "LeftLowerLeg", new[] { "leftleg", "l_calf", "left_calf", "leftshin", "leftlowerleg" } },
        { "LeftFoot", new[] { "leftfoot", "l_foot", "left_foot" } },
        { "LeftToes", new[] { "lefttoes", "lefttoe", "l_toe", "left_toe" } },
        { "RightUpperLeg", new[] { "rightupleg", "r_thigh", "right_thigh", "rightupleg" } },
        { "RightLowerLeg", new[] { "rightleg", "r_calf", "right_calf", "rightshin", "rightlowerleg" } },
        { "RightFoot", new[] { "rightfoot", "r_foot", "right_foot" } },
        { "RightToes", new[] { "righttoes", "righttoe", "r_toe", "right_toe" } }
    };

    [MenuItem("Tools/Generate Humanoid Avatar")]
    public static void GenerateAvatar()
    {
        GameObject root = Selection.activeGameObject;
        if (root == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select the imported character GameObject in the Hierarchy.", "OK");
            return;
        }

        Transform[] allTransforms = root.GetComponentsInChildren<Transform>();
        List<HumanBone> humanBones = new List<HumanBone>();
        List<SkeletonBone> skeletonBones = new List<SkeletonBone>();

        // Add root skeleton bone
        SkeletonBone rootBone = new SkeletonBone();
        rootBone.name = root.name;
        rootBone.position = root.transform.localPosition;
        rootBone.rotation = root.transform.localRotation;
        rootBone.scale = root.transform.localScale;
        skeletonBones.Add(rootBone);

        // Find matches for each humanoid bone
        foreach (var pair in boneNameKeywords)
        {
            string unityBoneName = pair.Key;
            string[] keywords = pair.Value;

            Transform matchedTransform = FindBestMatch(allTransforms, unityBoneName, keywords);
            if (matchedTransform != null)
            {
                HumanBone hb = new HumanBone();
                hb.boneName = matchedTransform.name;
                hb.humanName = unityBoneName;
                hb.limit = new HumanLimit();
                hb.limit.useDefaultValues = true;
                humanBones.Add(hb);
                
                Debug.Log($"Mapped Humanoid Bone '{unityBoneName}' -> '{matchedTransform.name}'");
            }
            else
            {
                Debug.LogWarning($"Could not find match for bone: {unityBoneName}");
            }
        }

        // Add all transforms to skeleton
        foreach (Transform t in allTransforms)
        {
            if (t == root.transform) continue;

            SkeletonBone sb = new SkeletonBone();
            sb.name = t.name;
            sb.position = t.localPosition;
            sb.rotation = t.localRotation;
            sb.scale = t.localScale;
            skeletonBones.Add(sb);
        }

        HumanDescription description = new HumanDescription();
        description.human = humanBones.ToArray();
        description.skeleton = skeletonBones.ToArray();
        description.upperArmTwist = 0.5f;
        description.lowerArmTwist = 0.5f;
        description.upperLegTwist = 0.5f;
        description.lowerLegTwist = 0.5f;
        description.armStretch = 0.05f;
        description.legStretch = 0.05f;
        description.feetSpacing = 0.0f;
        description.hasTranslationDoF = false;

        Avatar avatar = AvatarBuilder.BuildHumanAvatar(root, description);
        if (avatar != null && avatar.isValid)
        {
            string path = "Assets/" + root.name + "_Avatar.asset";
            AssetDatabase.CreateAsset(avatar, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Try to assign it directly if the GameObject has an Animator
            Animator animator = root.GetComponent<Animator>();
            if (animator != null)
            {
                animator.avatar = avatar;
                EditorUtility.SetDirty(animator);
            }

            EditorUtility.DisplayDialog("Success", $"Avatar generated and saved to {path}!\nIt has also been assigned to the Animator component.", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Error", "Failed to build a valid Humanoid Avatar. Please check console logs for errors. Ensure the bones are in a proper T-pose.", "OK");
        }
    }

    private static Transform FindBestMatch(Transform[] transforms, string unityBone, string[] keywords)
    {
        foreach (Transform t in transforms)
        {
            string nameLower = t.name.ToLower();
            foreach (string kw in keywords)
            {
                // Avoid matching "forearm" when searching for "arm"
                if (unityBone == "LeftUpperArm" || unityBone == "RightUpperArm")
                {
                    if (nameLower.Contains("forearm")) continue;
                }
                // Avoid matching "upleg" when searching for "leg"
                if (unityBone == "LeftLowerLeg" || unityBone == "RightLowerLeg")
                {
                    if (nameLower.Contains("upleg") || nameLower.Contains("thigh")) continue;
                }
                // Avoid matching finger bones when searching for Hand bones
                if (unityBone == "LeftHand" || unityBone == "RightHand")
                {
                    if (nameLower.Contains("index") || nameLower.Contains("middle") || nameLower.Contains("ring") || nameLower.Contains("pinky") || nameLower.Contains("thumb") || nameLower.Contains("finger") || nameLower.Contains("little"))
                    {
                        continue;
                    }
                }
                // Avoid matching toe bones when searching for Foot bones
                if (unityBone == "LeftFoot" || unityBone == "RightFoot")
                {
                    if (nameLower.Contains("toe"))
                    {
                        continue;
                    }
                }
                // Avoid matching "spine1/spine2" when looking for base "spine"
                if (unityBone == "Spine")
                {
                    if (nameLower.Contains("spine1") || nameLower.Contains("spine2") || nameLower.Contains("spine_") || nameLower.Contains("spine."))
                    {
                        if (kw == "spine" && (nameLower == "spine" || nameLower == "mixamorig:spine"))
                        {
                            return t;
                        }
                        continue;
                    }
                }

                if (nameLower.Contains(kw))
                {
                    bool isLeftBone = unityBone.StartsWith("Left");
                    bool isRightBone = unityBone.StartsWith("Right");

                    if (isLeftBone && (nameLower.Contains("left") || nameLower.Contains("_l") || nameLower.StartsWith("l_") || nameLower.Contains(".l") || nameLower.Contains(" l")))
                    {
                        return t;
                    }
                    if (isRightBone && (nameLower.Contains("right") || nameLower.Contains("_r") || nameLower.StartsWith("r_") || nameLower.Contains(".r") || nameLower.Contains(" r")))
                    {
                        return t;
                    }
                    if (!isLeftBone && !isRightBone)
                    {
                        return t;
                    }
                }
            }
        }
        return null;
    }

    [MenuItem("Tools/Generate Humanoid Avatar", true)]
    public static bool GenerateAvatarValidate()
    {
        return Selection.activeGameObject != null;
    }
}
