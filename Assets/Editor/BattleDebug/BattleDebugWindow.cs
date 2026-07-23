using BackpackHero.Battle;
using UnityEditor;
using UnityEngine;

namespace BackpackHero.EditorTools
{
    public sealed class BattleDebugWindow : EditorWindow
    {
        private double nextRepaintTime;
        private Vector2 scrollPosition;

        private BattleBackpackOwner modifierTarget =
            BattleBackpackOwner.Player;

        private FighterDefinition modifierDefinition;

        private float modifierCooldown = 3f;
        
        [MenuItem("Tools/Battle/Battle Debug")]
        public static void ShowFromMenu()
        {
            OpenWindow();
        }

        internal static BattleDebugWindow OpenWindow()
        {
            BattleDebugWindow window =
                GetWindow<BattleDebugWindow>();

            window.titleContent =
                new GUIContent("Battle Debug");

            window.minSize =
                new Vector2(520f, 620f);

            window.Show();
            return window;
        }

        internal static void CloseAllWindows()
        {
            BattleDebugWindow[] windows =
                Resources.FindObjectsOfTypeAll<
                    BattleDebugWindow>();

            foreach (BattleDebugWindow window in windows)
            {
                window.Close();
            }
        }

        private void Update()
        {
            if (EditorApplication.timeSinceStartup <
                nextRepaintTime)
            {
                return;
            }

            nextRepaintTime =
                EditorApplication.timeSinceStartup + 0.1;

            Repaint();
        }

        private void OnGUI()
        {
            scrollPosition =
                EditorGUILayout.BeginScrollView(
                    scrollPosition);

            DrawStatus();

            BattleDebugRuntime runtime =
                BattleDebugRuntime.Instance;

            using (new EditorGUI.DisabledScope(
                       !EditorApplication.isPlaying ||
                       runtime == null ||
                       !runtime.IsReady))
            {
                EditorGUILayout.Space(10f);

                DrawBackpackSection(
                    "Player Backpack",
                    runtime,
                    BattleBackpackOwner.Player);

                EditorGUILayout.Space(12f);

                DrawBackpackSection(
                    "Enemy Backpack",
                    runtime,
                    BattleBackpackOwner.Enemy);

                EditorGUILayout.Space(12f);

                DrawBackpackModifier(runtime);
            }

            EditorGUILayout.EndScrollView();
        }

        private static void DrawBackpackSection(
            string title,
            BattleDebugRuntime runtime,
            BattleBackpackOwner owner)
        {
            EditorGUILayout.LabelField(
                title,
                EditorStyles.boldLabel);

            BattleBackpack backpack =
                runtime != null
                    ? runtime.GetBackpack(owner)
                    : null;
            
            if (backpack == null)
            {
                EditorGUILayout.HelpBox(
                    "背包尚未创建。",
                    MessageType.Warning);

                return;
            }

            if (backpack.ItemCount == 0)
            {
                EditorGUILayout.HelpBox(
                    "背包中没有物品。",
                    MessageType.Info);

                return;
            }

            BattleBackpackItem itemToRemove = null;

            using (new EditorGUILayout.VerticalScope(
                       EditorStyles.helpBox))
            {
                for (int index = 0;
                     index < backpack.ItemCount;
                     index++)
                {
                    BattleBackpackItem item =
                        backpack.GetItem(index);

                    if (item == null)
                    {
                        continue;
                    }

                    if (DrawBackpackItem(item))
                    {
                        itemToRemove = item;
                    }
                }
            }

            if (itemToRemove != null &&
                runtime != null)
            {
                runtime.RemoveBackpackItem(
                    owner,
                    itemToRemove);
            }
        }
        
        private static bool DrawBackpackItem(
            BattleBackpackItem item)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(
                    item.DisplayName,
                    GUILayout.Width(130f));

                Rect progressRect =
                    GUILayoutUtility.GetRect(
                        100f,
                        20f,
                        GUILayout.ExpandWidth(true));

                string progressText =
                    $"{item.RemainingCooldown:0.0} / " +
                    $"{item.CooldownDuration:0.0}s";

                EditorGUI.ProgressBar(
                    progressRect,
                    item.CooldownProgress,
                    progressText);

                return GUILayout.Button(
                    "Delete",
                    GUILayout.Width(65f),
                    GUILayout.Height(20f));
            }
        }
        
        private void DrawBackpackModifier(
            BattleDebugRuntime runtime)
        {
            EditorGUILayout.LabelField(
                "Backpack Modifier",
                EditorStyles.boldLabel);

            if (runtime == null)
            {
                EditorGUILayout.HelpBox(
                    "Battle Runtime尚未连接。",
                    MessageType.Warning);

                return;
            }

            using (new EditorGUILayout.VerticalScope(
                       EditorStyles.helpBox))
            {
                modifierTarget =
                    (BattleBackpackOwner)EditorGUILayout.EnumPopup(
                        "Target Backpack",
                        modifierTarget);

                modifierDefinition =
                    (FighterDefinition)EditorGUILayout.ObjectField(
                        "Fighter",
                        modifierDefinition,
                        typeof(FighterDefinition),
                        false);

                modifierCooldown =
                    EditorGUILayout.FloatField(
                        "Cooldown",
                        modifierCooldown);

                modifierCooldown =
                    Mathf.Max(
                        0.1f,
                        modifierCooldown);

                using (new EditorGUI.DisabledScope(
                           modifierDefinition == null))
                {
                    if (GUILayout.Button(
                            "Add Fighter",
                            GUILayout.Height(32f)))
                    {
                        AddModifierItem(runtime);
                    }
                }

                if (modifierDefinition == null)
                {
                    EditorGUILayout.HelpBox(
                        "请选择要添加的Fighter Definition。",
                        MessageType.Info);
                }
                else
                {
                    EditorGUILayout.LabelField(
                        "Item Name",
                        modifierDefinition.DisplayName);

                    EditorGUILayout.LabelField(
                        "Item Type",
                        "Fighter");

                    EditorGUILayout.LabelField(
                        "Cooldown",
                        $"{modifierCooldown:0.##}s");
                }
            }
        }
        
        private void AddModifierItem(
            BattleDebugRuntime runtime)
        {
            if (runtime == null ||
                modifierDefinition == null)
            {
                return;
            }

            float safeCooldown =
                Mathf.Max(
                    0.1f,
                    modifierCooldown);

            runtime.AddBackpackFighter(
                modifierTarget,
                modifierDefinition,
                safeCooldown);
        }
        
        private static void DrawStatus()
        {
            EditorGUILayout.LabelField(
                "Battle Runtime",
                EditorStyles.boldLabel);

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "进入Play Mode后自动连接SampleScene中的" +
                    "BattleDebugRuntime。",
                    MessageType.Info);

                return;
            }

            BattleDebugRuntime runtime =
                BattleDebugRuntime.Instance;

            if (runtime == null)
            {
                EditorGUILayout.HelpBox(
                    "当前场景中没有BattleDebugRuntime。",
                    MessageType.Warning);

                return;
            }

            if (!runtime.IsReady)
            {
                EditorGUILayout.HelpBox(
                    "Runtime配置不完整，请检查Prefab、" +
                    "Player、Enemy和Curve Line引用。",
                    MessageType.Error);

                return;
            }

            EditorGUILayout.HelpBox(
                "BattleDebugRuntime已连接。",
                MessageType.Info);
        }
        
    }
}