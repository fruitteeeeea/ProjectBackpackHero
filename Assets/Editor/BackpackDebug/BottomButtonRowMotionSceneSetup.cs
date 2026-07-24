using System.Collections.Generic;
using BackpackPrototype;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BackpackPrototypeEditor
{
    public static class BottomButtonRowMotionSceneSetup
    {
        private const string ScenePath =
            "Assets/Scenes/BackpackDebugScene.unity";

        private const float HiddenY = -140f;
        private const float VisibleY = 80f;

        [MenuItem(
            "Tools/Backpack/Setup Bottom Button Row Motion")]
        public static void Setup()
        {
            Scene scene = EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Single);

            GameObject row = FindSceneObject("BottomButtonRow");

            if (row == null ||
                row.GetComponent<RectTransform>() == null)
            {
                Debug.LogError(
                    "BackpackDebugScene缺少BottomButtonRow。");
                return;
            }

            RectTransform rect = row.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(
                rect.anchoredPosition.x,
                HiddenY);

            MMF_Player showPlayer = ConfigurePlayer(
                row.transform,
                "ShowFeedbacks",
                row,
                VisibleY,
                0.35f,
                MMTween.MMTweenCurve.EaseOutOverhead);

            MMF_Player hidePlayer = ConfigurePlayer(
                row.transform,
                "HideFeedbacks",
                row,
                HiddenY,
                0.25f,
                MMTween.MMTweenCurve.EaseInCubic);

            BottomButtonRowPhaseMotion motion =
                row.GetComponent<BottomButtonRowPhaseMotion>();

            if (motion == null)
            {
                motion =
                    row.AddComponent<BottomButtonRowPhaseMotion>();
            }

            SerializedObject motionObject =
                new SerializedObject(motion);
            motionObject.FindProperty("showFeedbacks")
                .objectReferenceValue = showPlayer;
            motionObject.FindProperty("hideFeedbacks")
                .objectReferenceValue = hidePlayer;
            motionObject.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(row);
            EditorUtility.SetDirty(rect);
            EditorUtility.SetDirty(motion);
            EditorUtility.SetDirty(showPlayer);
            EditorUtility.SetDirty(hidePlayer);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log(
                "BottomButtonRow FEEL阶段动效配置完成。");
        }

        private static MMF_Player ConfigurePlayer(
            Transform parent,
            string name,
            GameObject target,
            float targetY,
            float duration,
            MMTween.MMTweenCurve tween)
        {
            Transform existing = parent.Find(name);
            GameObject playerObject;

            if (existing != null)
            {
                playerObject = existing.gameObject;
            }
            else
            {
                playerObject = new GameObject(name);
                playerObject.transform.SetParent(parent, false);
            }

            MMF_Player player =
                playerObject.GetComponent<MMF_Player>();

            if (player == null)
            {
                player = playerObject.AddComponent<MMF_Player>();
            }

            player.FeedbacksList ??= new List<MMF_Feedback>();
            player.FeedbacksList.Clear();
            player.StopFeedbacksOnDisable = true;
            player.RestoreInitialValuesOnDisable = false;

            MMF_Position position =
                (MMF_Position)player.AddFeedback(
                    typeof(MMF_Position));

            position.Label = $"{name} Position";
            position.AnimatePositionTarget = target;
            position.Mode = MMF_Position.Modes.ToDestination;
            position.Space = MMF_Position.Spaces.RectTransform;
            position.MovementMode =
                MMF_Position.MovementModes.Duration;
            position.AnimatePositionDuration = duration;
            position.AnimatePositionTween =
                new MMTweenType(tween);
            position.RelativePosition = false;
            position.DeterminePositionsOnPlay = true;
            position.DestinationPosition =
                new Vector3(0f, targetY, 0f);

            return player;
        }

        private static GameObject FindSceneObject(string objectName)
        {
            foreach (GameObject root in
                     SceneManager.GetActiveScene()
                         .GetRootGameObjects())
            {
                Transform match =
                    FindChildRecursive(root.transform, objectName);

                if (match != null)
                {
                    return match.gameObject;
                }
            }

            return null;
        }

        private static Transform FindChildRecursive(
            Transform current,
            string objectName)
        {
            if (current.name == objectName)
            {
                return current;
            }

            foreach (Transform child in current)
            {
                Transform match =
                    FindChildRecursive(child, objectName);

                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }
    }
}
