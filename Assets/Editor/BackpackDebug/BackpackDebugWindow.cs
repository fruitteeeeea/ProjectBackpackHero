using System.Collections.Generic;
using System.Text;
using BackpackPrototype;
using UnityEditor;
using UnityEngine;

namespace BackpackPrototypeEditor
{
    public sealed class BackpackDebugWindow : EditorWindow
    {
        private const double RepaintInterval = 0.1;

        private double nextRepaintTime;
        private Vector2 scrollPosition;
        
        [MenuItem("Tools/Backpack/Backpack Debug")]
        public static void ShowFromMenu()
        {
            OpenWindow();
        }

        internal static BackpackDebugWindow OpenWindow()
        {
            BackpackDebugWindow window =
                GetWindow<BackpackDebugWindow>();

            window.titleContent =
                new GUIContent("Backpack Debug");

            window.minSize =
                new Vector2(420f, 520f);

            window.Show();
            return window;
        }

        internal static void CloseAllWindows()
        {
            BackpackDebugWindow[] windows =
                Resources.FindObjectsOfTypeAll<
                    BackpackDebugWindow>();

            foreach (BackpackDebugWindow window
                     in windows)
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
                EditorApplication.timeSinceStartup +
                RepaintInterval;

            Repaint();
        }

        private void OnGUI()
        {
            scrollPosition =
                EditorGUILayout.BeginScrollView(
                    scrollPosition);

            DrawStatus();

            BackpackDebugRuntime runtime =
                BackpackDebugRuntime.Instance;

            using (new EditorGUI.DisabledScope(
                       !EditorApplication.isPlaying ||
                       runtime == null ||
                       !runtime.IsReady))
            {
                EditorGUILayout.Space(10f);
                DrawActions(runtime);

                EditorGUILayout.Space(12f);
                DrawDisplayedItem(runtime);

                EditorGUILayout.Space(12f);
                DrawBackpackItems(runtime);
            }

            EditorGUILayout.EndScrollView();
        }

        private static void DrawStatus()
        {
            EditorGUILayout.LabelField(
                "Backpack Runtime",
                EditorStyles.boldLabel);

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "请进入Play Mode并打开背包调试场景。",
                    MessageType.Info);

                return;
            }

            BackpackDebugRuntime runtime =
                BackpackDebugRuntime.Instance;

            if (runtime == null)
            {
                EditorGUILayout.HelpBox(
                    "没有找到BackpackDebugRuntime。",
                    MessageType.Warning);

                return;
            }

            if (!runtime.IsReady)
            {
                EditorGUILayout.HelpBox(
                    "BackpackDebugRuntime配置不完整，" +
                    "请检查Console和Inspector引用。",
                    MessageType.Error);

                return;
            }

            EditorGUILayout.HelpBox(
                "Backpack Runtime已连接。",
                MessageType.None);
        }

        private void DrawActions(
            BackpackDebugRuntime runtime)
        {
            EditorGUILayout.LabelField(
                "Actions",
                EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(
                       EditorStyles.helpBox))
            {
                if (GUILayout.Button(
                        "恢复默认",
                        GUILayout.Height(32f)))
                {
                    runtime.RestoreDefaultBackpack();
                }

                if (GUILayout.Button(
                        "刷新商店物品",
                        GUILayout.Height(32f)))
                {
                    runtime.RefreshShop();
                }
                
                if (GUILayout.Button(
                        "背包物品进入冷却",
                        GUILayout.Height(32f)))
                {
                    runtime.EnterAllBackpackItemsCooldown();
                }
            }
        }
        

        private static void DrawBackpackItems(
            BackpackDebugRuntime runtime)
        {
            EditorGUILayout.LabelField(
                "Current Backpack Items",
                EditorStyles.boldLabel);

            IReadOnlyList<ItemInstance> items =
                runtime != null
                    ? runtime.GetBackpackItems()
                    : null;

            using (new EditorGUILayout.VerticalScope(
                       EditorStyles.helpBox))
            {
                if (items == null ||
                    items.Count == 0)
                {
                    EditorGUILayout.HelpBox(
                        "背包中没有物品。",
                        MessageType.Info);

                    return;
                }

                for (int index = 0;
                     index < items.Count;
                     index++)
                {
                    ItemInstance item = items[index];

                    if (item == null ||
                        item.Data == null)
                    {
                        continue;
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(
                            (index + 1).ToString(),
                            GUILayout.Width(28f));

                        EditorGUILayout.LabelField(
                            item.Data.ItemName);
                    }
                }
            }
        }

        private static string BuildShapeText(
            IReadOnlyList<Vector2Int> offsets)
        {
            if (offsets == null ||
                offsets.Count == 0)
            {
                return "-";
            }

            StringBuilder builder =
                new StringBuilder();

            for (int index = 0;
                 index < offsets.Count;
                 index++)
            {
                if (index > 0)
                {
                    builder.Append(" ");
                }

                Vector2Int offset =
                    offsets[index];

                builder.Append(
                    $"({offset.x},{offset.y})");
            }

            return builder.ToString();
        }
        
        private static void DrawDisplayedItem(
            BackpackDebugRuntime runtime)
        {
            EditorGUILayout.LabelField(
                "Current Item",
                EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(
                       EditorStyles.helpBox))
            {
                ItemView selectedItem =
                    runtime != null
                        ? runtime.SelectedItem
                        : null;

                if (selectedItem == null ||
                    selectedItem.Instance == null ||
                    selectedItem.Instance.Data == null)
                {
                    EditorGUILayout.HelpBox(
                        "点击商店或背包中的物品后，" +
                        "这里会自动显示物品信息。",
                        MessageType.Info);

                    return;
                }

                EditorGUILayout.LabelField(
                    "Name",
                    selectedItem.DisplayName);

                EditorGUILayout.LabelField(
                    "Shape",
                    BuildShapeText(
                        selectedItem.Instance.Data.ShapeOffsets));

                EditorGUILayout.LabelField(
                    "Location",
                    selectedItem.LocationName);

                string anchorText =
                    selectedItem.TryGetBackpackAnchor(
                        out Vector2Int anchorCell)
                        ? $"({anchorCell.x}, {anchorCell.y})"
                        : "-";

                EditorGUILayout.LabelField(
                    "Anchor",
                    anchorText);
            }
        }
    }
}