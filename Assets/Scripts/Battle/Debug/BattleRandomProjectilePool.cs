using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BackpackHero.Battle
{
    /// <summary>
    /// 调试场景使用的全局随机子弹池。飞机在每次发射时查询此对象，
    /// 因此切换开关会同时影响已生成和之后生成的飞机。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BattleRandomProjectilePool : MonoBehaviour
    {
        public const string PrefabDirectory =
            "Assets/Prefabs/Battle/Projectiles";

        [SerializeField]
        private bool randomProjectilesEnabled;

        [SerializeField]
        private List<BattleAttack2D> projectilePrefabs =
            new();

        public static BattleRandomProjectilePool Instance
        {
            get;
            private set;
        }

        public static event Action SettingsChanged;

        public bool RandomProjectilesEnabled =>
            randomProjectilesEnabled;

        public int ProjectilePrefabCount
        {
            get
            {
                int count = 0;

                foreach (BattleAttack2D prefab in
                         projectilePrefabs)
                {
                    if (prefab != null)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning(
                    "场景中存在重复的随机子弹池，新的实例将被禁用。",
                    this);
                enabled = false;
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                SettingsChanged?.Invoke();
            }
        }

        public void SetRandomProjectilesEnabled(bool enabled)
        {
            if (randomProjectilesEnabled == enabled)
            {
                return;
            }

            randomProjectilesEnabled = enabled;

            if (enabled && ProjectilePrefabCount == 0)
            {
                Debug.LogWarning(
                    "随机子弹已开启，但随机子弹池为空；" +
                    "飞机将回退使用默认子弹。",
                    this);
            }

            SettingsChanged?.Invoke();
        }

        /// <summary>
        /// 未开启或池为空时返回null，调用方应继续使用默认攻击预制体。
        /// </summary>
        public BattleAttack2D SelectProjectilePrefab()
        {
            if (!randomProjectilesEnabled ||
                ProjectilePrefabCount == 0)
            {
                return null;
            }

            int selectedIndex = UnityEngine.Random.Range(
                0,
                ProjectilePrefabCount);

            foreach (BattleAttack2D prefab in projectilePrefabs)
            {
                if (prefab == null)
                {
                    continue;
                }

                if (selectedIndex == 0)
                {
                    return prefab;
                }

                selectedIndex--;
            }

            return null;
        }

        public void SetProjectilePrefabs(
            IEnumerable<BattleAttack2D> prefabs)
        {
            projectilePrefabs.Clear();

            if (prefabs != null)
            {
                foreach (BattleAttack2D prefab in prefabs)
                {
                    if (prefab != null &&
                        !projectilePrefabs.Contains(prefab))
                    {
                        projectilePrefabs.Add(prefab);
                    }
                }
            }

            SettingsChanged?.Invoke();
        }

#if UNITY_EDITOR
        [ContextMenu("Refresh Projectile Prefabs")]
        public void RefreshProjectilePrefabs()
        {
            string[] guids = AssetDatabase.FindAssets(
                "t:Prefab",
                new[] { PrefabDirectory });

            Array.Sort(guids, StringComparer.Ordinal);

            List<BattleAttack2D> prefabs = new();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(path);
                BattleAttack2D attack =
                    prefab != null
                        ? prefab.GetComponent<BattleAttack2D>()
                        : null;

                if (attack != null)
                {
                    prefabs.Add(attack);
                }
            }

            SetProjectilePrefabs(prefabs);
            EditorUtility.SetDirty(this);
        }

        private void OnValidate()
        {
            RefreshProjectilePrefabs();
        }
#endif
    }
}
