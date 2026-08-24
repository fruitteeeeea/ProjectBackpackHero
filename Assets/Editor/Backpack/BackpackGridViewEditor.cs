using System.Collections.Generic;
using BackpackPrototype;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackHero.EditorTools
{
    [CustomEditor(typeof(BackpackGridView))]
    public sealed class BackpackGridViewEditor : global::UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10f);

            if (GUILayout.Button(
                    "同步格子名称与坐标",
                    GUILayout.Height(32f)))
            {
                SynchronizeGrid();
            }
        }

        private void SynchronizeGrid()
        {
            var gridView = (BackpackGridView)target;
            var gridLayout =
                gridView.GetComponent<GridLayoutGroup>();

            if (!TryValidateGrid(
                    gridView,
                    gridLayout,
                    out var columnCount,
                    out var slots))
            {
                return;
            }

            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("同步背包格子");

            for (var index = 0;
                 index < slots.Count;
                 index++)
            {
                var slot = slots[index];
                var x = index % columnCount;
                var y = index / columnCount;
                var cell = new Vector2Int(x, y);

                Undo.RecordObject(
                    slot.gameObject,
                    "重命名背包格子");

                slot.gameObject.name =
                    $"Slot_{x}_{y}";

                var slotSerializedObject =
                    new SerializedObject(slot);

                slotSerializedObject.Update();

                var cellProperty =
                    slotSerializedObject.FindProperty("cell");

                cellProperty.vector2IntValue = cell;

                slotSerializedObject.ApplyModifiedProperties();

                EditorUtility.SetDirty(slot);
                EditorUtility.SetDirty(slot.gameObject);

                PrefabUtility
                    .RecordPrefabInstancePropertyModifications(
                        slot);

                PrefabUtility
                    .RecordPrefabInstancePropertyModifications(
                        slot.gameObject);
            }

            SynchronizeGridView(
                gridView,
                gridLayout,
                slots);

            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log(
                $"同步完成：{slots.Count} 个格子，" +
                $"{columnCount} 列，" +
                $"{slots.Count / columnCount} 行。",
                gridView);
        }

        private static bool TryValidateGrid(
            BackpackGridView gridView,
            GridLayoutGroup gridLayout,
            out int columnCount,
            out List<BackpackSlotView> slots)
        {
            columnCount = 0;
            slots = new List<BackpackSlotView>();

            if (gridLayout == null)
            {
                Debug.LogError(
                    "GridLayer缺少GridLayoutGroup组件。",
                    gridView);

                return false;
            }

            if (gridLayout.constraint !=
                GridLayoutGroup.Constraint.FixedColumnCount)
            {
                Debug.LogError(
                    "Constraint必须设置为Fixed Column Count。",
                    gridView);

                return false;
            }

            columnCount = gridLayout.constraintCount;

            if (columnCount <= 0)
            {
                Debug.LogError(
                    "列数必须大于0。",
                    gridView);

                return false;
            }

            var childSlots =
                gridView.GetComponentsInChildren<
                    BackpackSlotView>(true);

            foreach (var slot in childSlots)
            {
                if (slot == null)
                {
                    continue;
                }

                slots.Add(slot);
            }

            if (slots.Count == 0)
            {
                Debug.LogWarning(
                    "GridLayer下没有格子。",
                    gridView);

                return false;
            }

            if (slots.Count % columnCount != 0)
            {
                Debug.LogError(
                    $"格子数量 {slots.Count} 不能被列数 " +
                    $"{columnCount} 整除。",
                    gridView);

                return false;
            }

            return true;
        }

        private static void SynchronizeGridView(
            BackpackGridView gridView,
            GridLayoutGroup gridLayout,
            IReadOnlyList<BackpackSlotView> slots)
        {
            Undo.RecordObject(
                gridView,
                "同步BackpackGridView");

            var gridSerializedObject =
                new SerializedObject(gridView);

            gridSerializedObject.Update();

            gridSerializedObject
                .FindProperty("gridRect")
                .objectReferenceValue =
                    gridView.GetComponent<RectTransform>();

            gridSerializedObject
                .FindProperty("cellSize")
                .vector2Value =
                    gridLayout.cellSize;

            gridSerializedObject
                .FindProperty("spacing")
                .vector2Value =
                    gridLayout.spacing;

            var slotsProperty =
                gridSerializedObject.FindProperty("slots");

            slotsProperty.arraySize = slots.Count;

            for (var index = 0;
                 index < slots.Count;
                 index++)
            {
                slotsProperty
                    .GetArrayElementAtIndex(index)
                    .objectReferenceValue =
                        slots[index];
            }

            gridSerializedObject.ApplyModifiedProperties();

            EditorUtility.SetDirty(gridView);

            PrefabUtility
                .RecordPrefabInstancePropertyModifications(
                    gridView);
        }
    }
}
