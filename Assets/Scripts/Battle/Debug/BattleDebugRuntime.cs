using System;
using System.Collections;
using BackpackHero.Input;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BackpackHero.Battle
{
    public sealed class BattleDebugRuntime : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private GameObject fighterPrefab;
        
        [Header("Fighter Definitions")]
        [SerializeField]
        private FighterDefinition playerFighterDefinition;

        [SerializeField]
        private FighterDefinition enemyFighterDefinition;
        
        [Header("Random Spawn Pools")]
        [SerializeField]
        private FighterSpawnPool playerSpawnPool;

        [SerializeField]
        private FighterSpawnPool enemySpawnPool;
        
        public FighterSpawnPool PlayerSpawnPool =>
            playerSpawnPool;

        public FighterSpawnPool EnemySpawnPool =>
            enemySpawnPool;

        [SerializeField]
        private Transform playerSpawnPoint;

        [SerializeField]
        private Transform enemySpawnPoint;

        [SerializeField]
        private CurvedConnectionRenderer curveLine;

        [SerializeField]
        private Transform fighterContainer;

        [Header("Fighter Colors")]
        [SerializeField]
        private Color playerFighterColor =
            new Color(0.45f, 0.85f, 1f, 1f);

        [SerializeField]
        private Color enemyFighterColor =
            new Color(1f, 0.5f, 0.5f, 1f);

        [Header("Enemy Curve")]
        [Tooltip("敌机随机选择曲线值的范围。")]
        [SerializeField]
        private Vector2 enemyCurveValueRange =
            new Vector2(-1f, 1f);

        [Header("Per-Fighter Variation")]
        [Tooltip("每架飞机生成时随机抽取的方向角偏移范围（度）。会旋转该飞机的整条飞行曲线。")]
        [SerializeField]
        private Vector2 spawnDirectionOffsetRange =
            new Vector2(-3f, 3f);

        [Tooltip("每架飞机生成时随机抽取的索敌距离偏移范围（世界单位）。")]
        [SerializeField]
        private Vector2 attackRangeOffsetRange =
            new Vector2(-0.15f, 0.15f);

        [Header("Batch Spawn")]
        [Tooltip("批量生成时，每架飞机之间的间隔。")]
        [SerializeField, Min(0f)]
        private float spawnInterval = 0.15f;
        
        public static BattleDebugRuntime Instance
        {
            get;
            private set;
        }

        public static event Action<BattleDebugRuntime>
            InstanceAvailable;

        public static event Action
            InstanceUnavailable;

        public bool IsReady =>
            fighterPrefab != null &&
            playerSpawnPoint != null &&
            enemySpawnPoint != null &&
            curveLine != null;

        public FighterDefinition PlayerFighterDefinition =>
            playerFighterDefinition;

        public FighterDefinition EnemyFighterDefinition =>
            enemyFighterDefinition;

        public Vector2 SpawnDirectionOffsetRange =>
            NormalizeRange(spawnDirectionOffsetRange);

        public Vector2 AttackRangeOffsetRange =>
            NormalizeRange(attackRangeOffsetRange);
        
        
        public void SetPlayerFighterDefinition(
            FighterDefinition definition)
        {
            if (definition == null)
            {
                Debug.LogWarning(
                    "不能将玩家飞机配置设置为空。",
                    this);

                return;
            }

            playerFighterDefinition = definition;
        }

        public void SetEnemyFighterDefinition(
            FighterDefinition definition)
        {
            if (definition == null)
            {
                Debug.LogWarning(
                    "不能将敌人飞机配置设置为空。",
                    this);

                return;
            }

            enemyFighterDefinition = definition;
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
        
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning(
                    "场景中存在重复的BattleDebugRuntime，" +
                    "新的实例将被禁用。",
                    this);

                enabled = false;
                return;
            }

            Instance = this;
            InstanceAvailable?.Invoke(this);
        }

        private void OnDestroy()
        {
            if (Instance != this)
            {
                return;
            }

            Instance = null;
            InstanceUnavailable?.Invoke();
        }

        public void SpawnPlayerFighters(int count)
        {
            if (playerFighterDefinition == null)
            {
                Debug.LogError(
                    "没有选择玩家Fighter Definition。",
                    this);

                return;
            }
            
            if (!ValidateConfiguration())
            {
                return;
            }

            int safeCount = Mathf.Clamp(count, 1, 100);

            // 按下按钮时截取当前玩家曲线值。
            float capturedCurveValue =
                curveLine.CurrentCurveValue;

            StartCoroutine(
                SpawnPlayerBatch(
                    safeCount,
                    capturedCurveValue));
        }

        public void SpawnRandomPlayerFighters(int count)
        {
            if (!ValidateConfiguration())
            {
                return;
            }

            if (playerSpawnPool == null)
            {
                Debug.LogError(
                    "没有配置玩家Fighter Spawn Pool。",
                    this);

                return;
            }

            int safeCount = Mathf.Clamp(count, 1, 100);

            // 玩家整批飞机截取按下按钮时的当前曲线。
            float capturedCurveValue =
                curveLine.CurrentCurveValue;

            StartCoroutine(
                SpawnRandomPlayerBatch(
                    safeCount,
                    capturedCurveValue));
        }
        
        
        public void SpawnEnemyFighters(int count)
        {
            
            if (enemyFighterDefinition == null)
            {
                Debug.LogError(
                    "没有选择敌人Fighter Definition。",
                    this);

                return;
            }
            
            if (!ValidateConfiguration())
            {
                return;
            }

            int safeCount = Mathf.Clamp(count, 1, 100);

            StartCoroutine(
                SpawnEnemyBatch(safeCount));
        }

        public void SpawnRandomEnemyFighters(int count)
        {
            if (!ValidateConfiguration())
            {
                return;
            }

            if (enemySpawnPool == null)
            {
                Debug.LogError(
                    "没有配置敌人Fighter Spawn Pool。",
                    this);

                return;
            }

            int safeCount = Mathf.Clamp(count, 1, 100);

            StartCoroutine(
                SpawnRandomEnemyBatch(safeCount));
        }
        
        private IEnumerator SpawnRandomPlayerBatch(
            int count,
            float curveValue)
        {
            for (int index = 0; index < count; index++)
            {
                if (playerSpawnPool.TryGetRandom(
                        out FighterDefinition definition))
                {
                    SpawnPlayerFighter(
                        definition,
                        curveValue);
                }

                if (index < count - 1 &&
                    spawnInterval > 0f)
                {
                    yield return new WaitForSeconds(
                        spawnInterval);
                }
            }
        }

        private IEnumerator SpawnRandomEnemyBatch(int count)
        {
            for (int index = 0; index < count; index++)
            {
                if (enemySpawnPool.TryGetRandom(
                        out FighterDefinition definition))
                {
                    SpawnEnemyFighter(definition);
                }

                if (index < count - 1 &&
                    spawnInterval > 0f)
                {
                    yield return new WaitForSeconds(
                        spawnInterval);
                }
            }
        }
        
        
        public GameObject SpawnPlayerFighter(
            float curveValue)
        {
            return SpawnPlayerFighter(
                playerFighterDefinition,
                curveValue);
        }

        public GameObject SpawnPlayerFighter(
            FighterDefinition definition,
            float curveValue)
        {
            if (!ValidateConfiguration())
            {
                return null;
            }

            if (definition == null)
            {
                Debug.LogError(
                    "玩家Fighter Definition为空。",
                    this);

                return null;
            }

            return SpawnFighter(
                definition,
                "Player",
                playerSpawnPoint.position,
                enemySpawnPoint.position,
                Vector2.up,
                curveValue,
                BattleFaction.Player,
                playerFighterColor);
        }
        
        public GameObject SpawnEnemyFighter()
        {
            return SpawnEnemyFighter(
                enemyFighterDefinition);
        }
        
        public GameObject SpawnEnemyFighter(
            FighterDefinition definition)
        {
            if (!ValidateConfiguration())
            {
                return null;
            }

            if (definition == null)
            {
                Debug.LogError(
                    "敌人Fighter Definition为空。",
                    this);

                return null;
            }

            float randomCurveValue = Random.Range(
                enemyCurveValueRange.x,
                enemyCurveValueRange.y);

            return SpawnFighter(
                definition,
                "Enemy",
                enemySpawnPoint.position,
                playerSpawnPoint.position,
                Vector2.down,
                randomCurveValue,
                BattleFaction.Enemy,
                enemyFighterColor);
        }

        private IEnumerator SpawnPlayerBatch(
            int count,
            float curveValue)
        {
            for (int index = 0; index < count; index++)
            {
                SpawnPlayerFighter(curveValue);

                if (index < count - 1 &&
                    spawnInterval > 0f)
                {
                    yield return new WaitForSeconds(
                        spawnInterval);
                }
            }
        }

        private IEnumerator SpawnEnemyBatch(int count)
        {
            for (int index = 0; index < count; index++)
            {
                SpawnEnemyFighter();

                if (index < count - 1 &&
                    spawnInterval > 0f)
                {
                    yield return new WaitForSeconds(
                        spawnInterval);
                }
            }
        }

        private GameObject SpawnFighter(
            FighterDefinition definition,
            string factionName,
            Vector3 start,
            Vector3 end,
            Vector2 defaultDirection,
            float curveValue,
            BattleFaction faction,
            Color factionColor)
        {
            if (definition == null)
            {
                Debug.LogError(
                    $"{factionName}没有配置FighterDefinition。",
                    this);

                return null;
            }

            float directionOffset =
                SampleRange(spawnDirectionOffsetRange);
            float attackRangeOffset =
                SampleRange(attackRangeOffsetRange);
            Vector2 variedDirection =
                RotateDirection(
                    defaultDirection,
                    directionOffset);
            Vector3 variedEnd =
                RotateEndAroundStart(
                    start,
                    end,
                    directionOffset);

            GameObject fighter = Instantiate(
                fighterPrefab,
                start,
                Quaternion.identity,
                fighterContainer);

            if (!fighter.TryGetComponent(
                    out Fighter2D fighterInstance))
            {
                Debug.LogError(
                    "Fighter Prefab缺少Fighter2D。",
                    fighter);

                Destroy(fighter);
                return null;
            }

            if (!fighter.TryGetComponent(
                    out DirectionalMover2D mover))
            {
                Debug.LogError(
                    "Fighter Prefab缺少DirectionalMover2D。",
                    fighter);

                Destroy(fighter);
                return null;
            }

            if (!fighter.TryGetComponent(
                    out BattleCurveFollower2D curveFollower))
            {
                Debug.LogError(
                    "Fighter Prefab缺少BattleCurveFollower2D。",
                    fighter);

                Destroy(fighter);
                return null;
            }

            // 应用Sprite、基础速度、最大生命值和阵营颜色。
            fighterInstance.Initialize(
                definition,
                faction,
                factionColor);

            fighter.name =
                $"{factionName} - {definition.DisplayName}";

            mover.Initialize(variedDirection);

            if (fighter.TryGetComponent(
                    out FighterCombat2D fighterCombat))
            {
                fighterCombat.SetAttackRangeOffset(
                    attackRangeOffset);
            }

            if (fighter.TryGetComponent(
                    out FighterFlight2D flight))
            {
                flight.RestartBurst();
            }

            curveFollower.BeginCurve(
                start,
                variedEnd,
                curveValue,
                curveLine.MaxBendDistance);

            return fighter;
        }

        private bool ValidateConfiguration()
        {
            if (IsReady)
            {
                return true;
            }

            Debug.LogError(
                "BattleDebugRuntime配置不完整。请检查Fighter Prefab、" +
                "Player/Enemy Fighter Definition、Player、Enemy和Curve Line。",
                this);

            return false;
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
            return Random.Range(
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

#if UNITY_EDITOR
        private void OnValidate()
        {
            float minimum = Mathf.Clamp(
                Mathf.Min(
                    enemyCurveValueRange.x,
                    enemyCurveValueRange.y),
                -1f,
                1f);

            float maximum = Mathf.Clamp(
                Mathf.Max(
                    enemyCurveValueRange.x,
                    enemyCurveValueRange.y),
                -1f,
                1f);

            enemyCurveValueRange =
                new Vector2(minimum, maximum);

            spawnInterval =
                Mathf.Max(0f, spawnInterval);

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
