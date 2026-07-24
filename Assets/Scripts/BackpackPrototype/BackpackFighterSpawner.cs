using System;
using System.Collections;
using System.Collections.Generic;
using BackpackHero.Battle;
using UnityEngine;

namespace BackpackPrototype
{
    /// <summary>
    /// 将背包飞机生成请求排队，并根据背包阵营初始化飞机。
    /// </summary>
    [RequireComponent(typeof(FactionMember))]
    [DisallowMultipleComponent]
    public sealed class BackpackFighterSpawner : MonoBehaviour
    {
        private readonly struct SpawnRequest
        {
            public SpawnRequest(
                ItemInstance item,
                float? curveValue)
            {
                Item = item;
                CurveValue = curveValue;
            }

            public ItemInstance Item { get; }
            public float? CurveValue { get; }
        }

        [SerializeField]
        private GameObject fighterPrefab;

        [SerializeField]
        private Transform fighterSpawnPoint;

        [SerializeField]
        private Transform fighterContainer;

        [SerializeField, Min(0.1f)]
        private float spawnInterval = 0.1f;

        [SerializeField, Min(0f)]
        private float spawnOffset = 1f;

        [SerializeField, Min(0f)]
        private float maximumBendDistance = 3f;

        [SerializeField]
        private Color playerColor =
            new(0.45f, 0.85f, 1f, 1f);

        [SerializeField]
        private Color enemyColor =
            new(1f, 0.5f, 0.5f, 1f);

        private readonly Queue<SpawnRequest>
            pendingSpawns = new();

        private BackpackCombatController combatController;
        private FactionMember factionMember;
        private Coroutine spawnCoroutine;

        public int PendingCount => pendingSpawns.Count;
        public float SpawnInterval =>
            Mathf.Max(0.1f, spawnInterval);
        public float LastSpawnTime { get; private set; } =
            float.NegativeInfinity;

        public event Action<GameObject, ItemInstance>
            FighterSpawned;

        private void Awake()
        {
            combatController =
                GetComponent<BackpackCombatController>();
            factionMember = GetComponent<FactionMember>();
            UpdateSpawnPointPosition();
        }

        private void OnEnable()
        {
            BattleFlowController.PhaseChanged +=
                HandlePhaseChanged;
        }

        public void Configure(
            GameObject newFighterPrefab,
            Transform newSpawnPoint = null,
            Transform newContainer = null)
        {
            if (newFighterPrefab != null)
            {
                fighterPrefab = newFighterPrefab;
            }

            fighterSpawnPoint = newSpawnPoint;
            fighterContainer = newContainer;
            UpdateSpawnPointPosition();
        }

        public bool RequestSpawn(
            ItemInstance aircraftItem,
            float? curveValue = null)
        {
            EnsureReferences();

            if (!BattleFlowController.IsCombatPhase ||
                aircraftItem == null ||
                aircraftItem.Data == null ||
                aircraftItem.Data.ItemType != ItemType.Aircraft)
            {
                return false;
            }

            pendingSpawns.Enqueue(
                new SpawnRequest(
                    aircraftItem,
                    curveValue));

            if (spawnCoroutine == null)
            {
                spawnCoroutine =
                    StartCoroutine(ProcessQueue());
            }

            return true;
        }

        public void ClearPendingSpawns()
        {
            pendingSpawns.Clear();

            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }
        }

        private IEnumerator ProcessQueue()
        {
            while (pendingSpawns.Count > 0 &&
                   BattleFlowController.IsCombatPhase)
            {
                SpawnRequest request =
                    pendingSpawns.Dequeue();

                if (IsRequestValid(request))
                {
                    GameObject fighter =
                        SpawnFighter(request);

                    if (fighter != null)
                    {
                        LastSpawnTime = Time.time;
                    }
                }

                yield return new WaitForSeconds(
                    SpawnInterval);
            }

            pendingSpawns.Clear();
            spawnCoroutine = null;
        }

        private bool IsRequestValid(SpawnRequest request)
        {
            EnsureReferences();

            return combatController != null &&
                   combatController.Backpack != null &&
                   combatController.Backpack.Contains(
                       request.Item) &&
                   request.Item.Data != null &&
                   request.Item.Data.ItemType ==
                   ItemType.Aircraft &&
                   BattleFlowController.IsCombatPhase;
        }

        private GameObject SpawnFighter(
            SpawnRequest request)
        {
            FighterDefinition definition =
                request.Item.Data.FighterDefinition;

            if (fighterPrefab == null ||
                definition == null)
            {
                Debug.LogWarning(
                    $"{name} 无法生成飞机：缺少Prefab或飞机配置。",
                    this);
                return null;
            }

            Vector2 direction = GetDefaultDirection();
            Vector3 start = GetSpawnPosition();

            GameObject fighterObject =
                Instantiate(
                    fighterPrefab,
                    start,
                    Quaternion.identity,
                    fighterContainer);

            if (!fighterObject.TryGetComponent(
                    out Fighter2D fighter) ||
                !fighterObject.TryGetComponent(
                    out DirectionalMover2D mover))
            {
                Debug.LogError(
                    "Fighter Prefab缺少Fighter2D或DirectionalMover2D。",
                    fighterObject);
                Destroy(fighterObject);
                return null;
            }

            BattleFaction faction =
                factionMember != null
                    ? factionMember.Faction
                    : BattleFaction.Player;

            fighter.Initialize(
                definition,
                faction,
                faction == BattleFaction.Player
                    ? playerColor
                    : enemyColor);

            fighterObject.name =
                $"{faction} - {definition.DisplayName}";
            mover.Initialize(direction);

            if (fighterObject.TryGetComponent(
                    out FighterFlight2D flight))
            {
                flight.RestartBurst();
            }

            BackpackCombatController enemy =
                FindNearestEnemy();

            if (enemy != null &&
                fighterObject.TryGetComponent(
                    out BattleCurveFollower2D curveFollower))
            {
                curveFollower.BeginCurve(
                    start,
                    enemy.transform.position,
                    request.CurveValue ?? 0f,
                    maximumBendDistance);
            }

            AttachAdjacentEquipmentEffects(
                fighterObject.transform,
                request.Item);

            FighterSpawned?.Invoke(
                fighterObject,
                request.Item);

            return fighterObject;
        }

        private void AttachAdjacentEquipmentEffects(
            Transform fighter,
            ItemInstance aircraftItem)
        {
            HashSet<GameObject> attachedPrefabs =
                new();

            foreach (ItemInstance equipment in
                     combatController.Backpack
                         .GetAdjacentEquipmentItems(
                             aircraftItem))
            {
                GameObject effectPrefab =
                    equipment.Data.EquipmentEffectPrefab;

                if (effectPrefab == null ||
                    !attachedPrefabs.Add(effectPrefab))
                {
                    continue;
                }

                GameObject effect =
                    Instantiate(
                        effectPrefab,
                        fighter,
                        false);
                effect.name =
                    $"{effectPrefab.name} (Equipment)";
            }
        }

        private BackpackCombatController FindNearestEnemy()
        {
            BackpackCombatController nearest = null;
            float nearestDistance =
                float.PositiveInfinity;

            foreach (BackpackCombatController candidate
                     in BackpackCombatController
                         .ActiveControllers)
            {
                if (candidate == null ||
                    candidate == combatController ||
                    candidate.Faction ==
                    combatController.Faction)
                {
                    continue;
                }

                float distance =
                    (candidate.transform.position -
                     transform.position).sqrMagnitude;

                if (distance < nearestDistance)
                {
                    nearest = candidate;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        private Vector2 GetDefaultDirection()
        {
            return factionMember != null &&
                   factionMember.Faction ==
                   BattleFaction.Enemy
                ? Vector2.down
                : Vector2.up;
        }

        private Vector3 GetSpawnPosition()
        {
            return fighterSpawnPoint != null
                ? fighterSpawnPoint.position
                : transform.position +
                  (Vector3)(GetDefaultDirection() *
                            spawnOffset);
        }

        private void UpdateSpawnPointPosition()
        {
            if (fighterSpawnPoint != null &&
                fighterSpawnPoint.parent == transform)
            {
                fighterSpawnPoint.localPosition =
                    GetDefaultDirection() * spawnOffset;
            }
        }

        private void EnsureReferences()
        {
            if (combatController == null)
            {
                combatController =
                    GetComponent<BackpackCombatController>();
            }

            if (factionMember == null)
            {
                factionMember =
                    GetComponent<FactionMember>();
            }
        }

        private void HandlePhaseChanged(BattlePhase phase)
        {
            if (phase == BattlePhase.Preparation)
            {
                ClearPendingSpawns();
            }
        }

        private void OnDisable()
        {
            BattleFlowController.PhaseChanged -=
                HandlePhaseChanged;
            ClearPendingSpawns();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            spawnInterval =
                Mathf.Max(0.1f, spawnInterval);
            spawnOffset = Mathf.Max(0f, spawnOffset);
            maximumBendDistance =
                Mathf.Max(0f, maximumBendDistance);
        }
#endif
    }
}
