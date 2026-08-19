using System;
using System.Collections;
using System.Collections.Generic;
using BackpackHero.Battle;
using BackpackHero.Debugging;
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
        private const float MinimumSpawnInterval = 0.5f;

        private readonly struct SpawnRequest
        {
            public SpawnRequest(
                ItemInstance item,
                ItemInstance triggeringEquipment,
                float? curveValue)
            {
                Item = item;
                TriggeringEquipment = triggeringEquipment;
                CurveValue = curveValue;
            }

            public ItemInstance Item { get; }
            public ItemInstance TriggeringEquipment { get; }
            public float? CurveValue { get; }
        }

        [SerializeField]
        private GameObject fighterPrefab;

        [SerializeField]
        private Transform fighterSpawnPoint;

        [SerializeField]
        private Transform fighterContainer;

        [SerializeField, Min(MinimumSpawnInterval)]
        private float spawnInterval = MinimumSpawnInterval;

        [SerializeField, Min(0f)]
        private float spawnOffset = 1f;

        [SerializeField, Min(0f)]
        private float maximumBendDistance = 3f;

        [Header("Per-Fighter Variation")]
        [Tooltip("每架飞机生成时随机抽取的方向角偏移范围（度）。会旋转该飞机的整条飞行曲线。")]
        [SerializeField]
        private Vector2 spawnDirectionOffsetRange =
            new(-3f, 3f);

        [Tooltip("每架飞机生成时随机抽取的索敌距离偏移范围（世界单位）。")]
        [SerializeField]
        private Vector2 attackRangeOffsetRange =
            new(-0.15f, 0.15f);

        [Tooltip("敌机每次生成时随机选择曲线值的范围。")]
        [SerializeField]
        private Vector2 enemyCurveValueRange =
            new(-1f, 1f);

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
            Mathf.Max(MinimumSpawnInterval, spawnInterval);
        public float LastSpawnTime { get; private set; } =
            float.NegativeInfinity;
        public float CurrentCurveValue { get; private set; }
        public float MaximumBendDistance =>
            maximumBendDistance;
        public Vector2 EnemyCurveValueRange =>
            GetNormalizedEnemyCurveValueRange();
        public Vector2 SpawnDirectionOffsetRange =>
            NormalizeRange(spawnDirectionOffsetRange);
        public Vector2 AttackRangeOffsetRange =>
            NormalizeRange(attackRangeOffsetRange);
        public Transform SpawnPoint => fighterSpawnPoint;
        public Vector3 SpawnPosition => GetSpawnPosition();

        public event Action<GameObject, ItemInstance>
            FighterSpawned;

        private void Awake()
        {
            combatController =
                GetComponent<BackpackCombatController>();
            factionMember = GetComponent<FactionMember>();
            ResolveFighterContainer();
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
            ItemInstance triggeringEquipment = null,
            float? curveValue = null)
        {
            EnsureReferences();

            if (!BattleFlowController.IsCombatPhase ||
                aircraftItem == null ||
                aircraftItem.Data == null ||
                aircraftItem.Data.ItemType != ItemType.Aircraft ||
                (triggeringEquipment != null &&
                 (triggeringEquipment.Data == null ||
                  triggeringEquipment.Data.ItemType !=
                  ItemType.Equipment)))
            {
                return false;
            }

            pendingSpawns.Enqueue(
                new SpawnRequest(
                    aircraftItem,
                    triggeringEquipment,
                    curveValue));

            if (spawnCoroutine == null)
            {
                spawnCoroutine =
                    StartCoroutine(ProcessQueue());
            }

            return true;
        }

        public void SetCurveValue(float value)
        {
            CurrentCurveValue =
                Mathf.Clamp(value, -1f, 1f);
        }

        public void SetMaximumBendDistance(float distance)
        {
            maximumBendDistance =
                Mathf.Max(0f, distance);
        }

        public void SetEnemyCurveValueRange(
            Vector2 range)
        {
            enemyCurveValueRange =
                NormalizeCurveValueRange(range);
        }

        public void SetSpawnDirectionOffsetRange(
            Vector2 range)
        {
            spawnDirectionOffsetRange =
                NormalizeRange(range);
        }

        public void SetAttackRangeOffsetRange(
            Vector2 range)
        {
            attackRangeOffsetRange =
                NormalizeRange(range);
        }

        public float SampleEnemyCurveValue()
        {
            Vector2 range =
                GetNormalizedEnemyCurveValueRange();
            return UnityEngine.Random.Range(
                range.x,
                range.y);
        }

        public bool SetSpawnPointWorldPosition(
            Vector3 worldPosition)
        {
            if (fighterSpawnPoint == null)
            {
                return false;
            }

            fighterSpawnPoint.position = worldPosition;
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
                   (request.TriggeringEquipment == null
                       ? StyleTendencyDebugRuntime
                             .GetCooldownItemType() ==
                         CooldownItemType.Aircraft
                       : StyleTendencyDebugRuntime
                             .GetCooldownItemType() ==
                         CooldownItemType.Equipment) &&
                   (request.TriggeringEquipment == null ||
                    (combatController.Backpack.Contains(
                         request.TriggeringEquipment) &&
                     request.TriggeringEquipment.Data != null &&
                     request.TriggeringEquipment.Data.ItemType ==
                     ItemType.Equipment)) &&
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

            Vector3 start = GetSpawnPosition();
            float directionOffset =
                SampleRange(spawnDirectionOffsetRange);
            float attackRangeOffset =
                SampleRange(attackRangeOffsetRange);
            Vector2 direction =
                RotateDirection(
                    GetDefaultDirection(),
                    directionOffset);

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
            fighter.SetDamageSourceItem(request.Item);

            fighterObject.name =
                $"{faction} - {definition.DisplayName}";
            mover.Initialize(direction);

            if (fighterObject.TryGetComponent(
                    out FighterCombat2D fighterCombat))
            {
                fighterCombat.SetAttackRangeOffset(
                    attackRangeOffset);
                fighterCombat.SetDamageSourceItem(request.Item);
            }

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
                Vector3 curveEnd =
                    enemy.FighterSpawner != null
                        ? enemy.FighterSpawner.SpawnPosition
                        : enemy.transform.position;

                curveFollower.BeginCurve(
                    start,
                    RotateEndAroundStart(
                        start,
                        curveEnd,
                        directionOffset),
                    ResolveCurveValue(
                        request.CurveValue),
                    maximumBendDistance);
            }

            ConfigureAdjacentEquipmentMarkers(
                fighterObject.transform,
                request.Item,
                request.TriggeringEquipment);

            ConfigureAdjacentEquipmentEffects(
                fighterObject.transform,
                request.Item,
                request.TriggeringEquipment);

            FighterSpawned?.Invoke(
                fighterObject,
                request.Item);

            return fighterObject;
        }

        private void ConfigureAdjacentEquipmentMarkers(
            Transform fighter,
            ItemInstance aircraftItem,
            ItemInstance triggeringEquipment)
        {
            if (fighter == null || combatController == null)
            {
                return;
            }

            var colors = new List<Color>();
            var uniqueColors = new HashSet<Color>();

            foreach (ItemInstance equipment in GetEquipmentItems(
                         aircraftItem,
                         triggeringEquipment))
            {
                if (equipment?.Data != null &&
                    uniqueColors.Add(
                        equipment.Data.EquipmentColor))
                {
                    colors.Add(equipment.Data.EquipmentColor);
                }
            }

            AircraftEquipmentMarkerDisplay display =
                fighter.GetComponent<
                    AircraftEquipmentMarkerDisplay>();

            if (display == null)
            {
                display = fighter.gameObject.AddComponent<
                    AircraftEquipmentMarkerDisplay>();
            }

            display.SetColors(colors);
        }

        private void ConfigureAdjacentEquipmentEffects(
            Transform fighter,
            ItemInstance aircraftItem,
            ItemInstance triggeringEquipment)
        {
            if (fighter == null || combatController == null ||
                !fighter.TryGetComponent(
                    out FighterCombat2D fighterCombat))
            {
                return;
            }

            var effects = new List<(EquipmentEffectDefinition Effect, ItemInstance Item)>();

            foreach (ItemInstance equipment in GetEquipmentItems(
                         aircraftItem,
                         triggeringEquipment))
            {
                if (equipment?.Data == null)
                {
                    continue;
                }

                foreach (EquipmentEffectDefinition effect in
                         equipment.Data.EquipmentEffects)
                {
                    if (effect != null)
                    {
                        effects.Add((effect, equipment));
                    }
                }
            }

            fighterCombat.ConfigureEquipmentEffects(
                effects,
                aircraftItem.Data.EquipmentItemModifier);
            if (triggeringEquipment != null)
            {
                fighterCombat.DisableDefaultFireMode();
            }
        }

        private IReadOnlyList<ItemInstance> GetEquipmentItems(
            ItemInstance aircraftItem,
            ItemInstance triggeringEquipment)
        {
            if (triggeringEquipment != null)
            {
                return new[] { triggeringEquipment };
            }

            return combatController.Backpack.GetAdjacentEquipmentItems(
                aircraftItem);
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

        private float ResolveCurveValue(
            float? requestedCurveValue)
        {
            if (requestedCurveValue.HasValue)
            {
                return Mathf.Clamp(
                    requestedCurveValue.Value,
                    -1f,
                    1f);
            }

            return factionMember != null &&
                   factionMember.Faction ==
                   BattleFaction.Enemy
                ? SampleEnemyCurveValue()
                : CurrentCurveValue;
        }

        private Vector2
            GetNormalizedEnemyCurveValueRange()
        {
            return NormalizeCurveValueRange(
                enemyCurveValueRange);
        }

        private static Vector2 NormalizeCurveValueRange(
            Vector2 range)
        {
            float minimum = Mathf.Clamp(
                Mathf.Min(range.x, range.y),
                -1f,
                1f);
            float maximum = Mathf.Clamp(
                Mathf.Max(range.x, range.y),
                -1f,
                1f);
            return new Vector2(minimum, maximum);
        }

        private static Vector2 NormalizeRange(
            Vector2 range)
        {
            return new Vector2(
                Mathf.Min(range.x, range.y),
                Mathf.Max(range.x, range.y));
        }

        private static float SampleRange(Vector2 range)
        {
            Vector2 normalized = NormalizeRange(range);
            return UnityEngine.Random.Range(
                normalized.x,
                normalized.y);
        }

        private static Vector2 RotateDirection(
            Vector2 direction,
            float angleDegrees)
        {
            return Quaternion.Euler(
                0f,
                0f,
                angleDegrees) * direction;
        }

        private static Vector3 RotateEndAroundStart(
            Vector3 start,
            Vector3 end,
            float angleDegrees)
        {
            Vector3 offset = end - start;
            return start +
                   Quaternion.Euler(
                       0f,
                       0f,
                       angleDegrees) * offset;
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

            ResolveFighterContainer();
        }

        private void ResolveFighterContainer()
        {
            if (fighterContainer != null)
            {
                return;
            }

            GameObject container =
                GameObject.Find("Fighters");
            fighterContainer =
                container != null
                    ? container.transform
                    : null;
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
                Mathf.Max(MinimumSpawnInterval, spawnInterval);
            spawnOffset = Mathf.Max(0f, spawnOffset);
            maximumBendDistance =
                Mathf.Max(0f, maximumBendDistance);
            spawnDirectionOffsetRange =
                NormalizeRange(
                    spawnDirectionOffsetRange);
            attackRangeOffsetRange =
                NormalizeRange(
                    attackRangeOffsetRange);
        }
#endif
    }
}
