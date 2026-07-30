using MoreMountains.Feedbacks;
using UnityEngine;

namespace BackpackHero.Battle
{
    /// <summary>
    /// 为未手动放置飘字对象池的测试场景提供 FEEL 运行时后备。
    /// 正式战斗场景已有场景级 Spawner 时不会重复创建。
    /// </summary>
    public sealed class FloatingTextSpawnerBootstrap : MonoBehaviour
    {
        [SerializeField] private FloatingDamageTextSettings settings;

        private static GameObject runtimeRoot;

        private void Awake()
        {
            if (runtimeRoot != null ||
                FindFirstObjectByType<MMFloatingTextSpawner>() != null ||
                settings == null)
            {
                return;
            }

            runtimeRoot = new GameObject("Runtime Damage Floating Text Spawners");
            CreateSpawner(
                "Player Damage Floating Text Spawner",
                41,
                BattleFaction.Player);
            CreateSpawner(
                "Enemy Damage Floating Text Spawner",
                42,
                BattleFaction.Enemy);
        }

        private void CreateSpawner(
            string spawnerName,
            int channel,
            BattleFaction faction)
        {
            GameObject spawnerObject = new(spawnerName);
            spawnerObject.transform.SetParent(runtimeRoot.transform);
            spawnerObject.SetActive(false);

            MMFloatingTextSpawner spawner =
                spawnerObject.AddComponent<MMFloatingTextSpawner>();

            spawner.Channel = channel;
            settings.ConfigureSpawner(spawner, faction);

            // MMFloatingTextSpawner 在 Awake 创建对象池，因此必须在
            // 完成所有预制体与参数赋值后再激活对象。
            spawnerObject.SetActive(true);
        }
    }
}
