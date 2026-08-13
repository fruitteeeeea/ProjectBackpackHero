using System;
using System.Collections.Generic;
using UnityEngine;

namespace BackpackHero.Background
{
    /// <summary>
    /// 编辑态负责把星图配置实体化到层级；Play Mode 仅绑定已有对象，不生成任何对象。
    /// </summary>
    [ExecuteAlways]
    [DefaultExecutionOrder(-500)]
    public sealed class ConstellationBackgroundBootstrap : MonoBehaviour
    {
        [SerializeField] private Sprite[] planetSprites;
        [SerializeField] private Material constellationMaterial;
        [Tooltip("只保留 Sprite Alpha、以统一灰色显示的星球材质。")]
        [SerializeField] private Material planetMaterial;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Transform linesRoot;
        [SerializeField] private Transform planetsRoot;
        [SerializeField] private BackgroundPlanet[] planets;
        [SerializeField] private BackgroundTouchInteractor touchInteractor;
        [SerializeField] private float backgroundZ = 2f;

        private static readonly Vector2[] Anchors =
        {
            new(-12f, 7f), new(-6f, 8f), new(1f, 7f), new(8f, 8f), new(13f, 5f),
            new(-11f, 0f), new(-4f, 1f), new(4f, 0f), new(11f, -1f), new(-7f, -7f),
        };

        private static readonly (int Start, int End)[] Links =
        {
            (0, 1), (1, 2), (2, 3), (3, 4), (0, 5), (5, 6), (6, 7), (7, 8), (5, 9), (6, 9),
        };

        public IReadOnlyList<BackgroundPlanet> Planets => planets;
        public BackgroundTouchInteractor TouchInteractor => touchInteractor;
        public static ConstellationBackgroundBootstrap ActiveInstance { get; private set; }
        public static event Action<ConstellationBackgroundBootstrap> InstanceAvailable;
        public static event Action InstanceUnavailable;

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                EnsureEditorHierarchy();
                return;
            }

            ActiveInstance = this;
            BindRuntimeReferences();
            InstanceAvailable?.Invoke(this);
        }

        private void OnDisable()
        {
            if (ActiveInstance != this)
            {
                return;
            }

            ActiveInstance = null;
            InstanceUnavailable?.Invoke();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                EnsureEditorHierarchy();
            }
        }

        private void BindRuntimeReferences()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (touchInteractor == null)
            {
                touchInteractor = GetComponent<BackgroundTouchInteractor>();
            }

            // Scene references are normally serialized by the editor bootstrap.  Recover the
            // two authored containers if a scene was saved before that happened; never create
            // scene hierarchy at runtime.
            linesRoot ??= transform.Find("Lines");
            planetsRoot ??= transform.Find("Planets");

            if (planets == null || planets.Length == 0)
            {
                planets = GetComponentsInChildren<BackgroundPlanet>(true);
            }

            if (planetsRoot == null)
            {
                Debug.LogWarning(
                    "Constellation background has no Planets container; skipping star-map binding.",
                    this);
                return;
            }

            RemoveLegacyNodeDots();
            ApplyPlanetMaterial();

            if (touchInteractor != null)
            {
                touchInteractor.Configure(targetCamera, planets, backgroundZ);
            }
        }

        private void EnsureEditorHierarchy()
        {
            if (planetSprites == null || planetSprites.Length == 0)
            {
                return;
            }

            constellationMaterial ??= Resources.Load<Material>("Background/ConstellationAlphaMask");
            targetCamera ??= Camera.main;
            linesRoot ??= FindOrCreateChild("Lines");
            planetsRoot ??= FindOrCreateChild("Planets");
            touchInteractor ??= GetComponent<BackgroundTouchInteractor>() ??
                gameObject.AddComponent<BackgroundTouchInteractor>();

            RemoveLegacyNodeDots();

            var existingPlanets = planetsRoot.GetComponentsInChildren<BackgroundPlanet>(true);
            if (existingPlanets.Length == 0)
            {
                existingPlanets = CreatePlanets();
            }

            planets = existingPlanets;
            ApplyPlanetMaterial();
            if (linesRoot.childCount == 0 && planets.Length >= Anchors.Length)
            {
                CreateLinks();
            }

            touchInteractor.Configure(targetCamera, planets, backgroundZ);
        }

        private Transform FindOrCreateChild(string childName)
        {
            var child = transform.Find(childName);
            if (child != null)
            {
                return child;
            }

            return new GameObject(childName).transform.Also(parent =>
            {
                parent.SetParent(transform, false);
            });
        }

        private BackgroundPlanet[] CreatePlanets()
        {
            var created = new List<BackgroundPlanet>(Anchors.Length);
            for (var index = 0; index < Anchors.Length; index++)
            {
                var planetObject = new GameObject($"Planet_{index + 1:00}");
                planetObject.transform.SetParent(planetsRoot, false);
                planetObject.transform.position = new Vector3(Anchors[index].x, Anchors[index].y, backgroundZ);
                planetObject.transform.rotation = Quaternion.Euler(0f, 0f, index * 31f);
                planetObject.transform.localScale = Vector3.one * (.9f + (index % 3) * .35f);
                var renderer = planetObject.AddComponent<SpriteRenderer>();
                renderer.sprite = planetSprites[index % planetSprites.Length];
                renderer.sharedMaterial = planetMaterial;
                renderer.color = new Color(.62f, .66f, .72f, .42f);
                renderer.sortingLayerName = "BackgroundPlanets";

                var planet = planetObject.AddComponent<BackgroundPlanet>();
                planet.SetAnchor(planetObject.transform.position, index * 7.31f + .13f);
                created.Add(planet);
            }

            return created.ToArray();
        }

        private void RemoveLegacyNodeDots()
        {
            if (planetsRoot == null)
            {
                return;
            }

            var dots = planetsRoot.GetComponentsInChildren<Transform>(true);
            foreach (var dot in dots)
            {
                if (dot != planetsRoot && dot.name == "NodeDot")
                {
                    if (Application.isPlaying)
                    {
                        Destroy(dot.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(dot.gameObject);
                    }
                }
            }
        }

        private void ApplyPlanetMaterial()
        {
            foreach (var planet in planets)
            {
                if (planet == null)
                {
                    continue;
                }

                var planetRenderer = planet.GetComponent<SpriteRenderer>();
                if (planetRenderer != null)
                {
                    planetRenderer.sharedMaterial = planetMaterial;
                }

                foreach (var dotRenderer in planet.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (dotRenderer != planetRenderer)
                    {
                        dotRenderer.sharedMaterial = planetMaterial;
                    }
                }
            }
        }

        private void CreateLinks()
        {
            foreach (var (start, end) in Links)
            {
                var linkObject = new GameObject($"Link_{start + 1:00}_{end + 1:00}");
                linkObject.transform.SetParent(linesRoot, false);
                linkObject.AddComponent<LineRenderer>();
                linkObject.AddComponent<ConstellationLink>().Configure(
                    planets[start].transform, planets[end].transform,
                    constellationMaterial);
            }
        }
    }

    internal static class TransformUtility
    {
        public static T Also<T>(this T value, Action<T> action)
        {
            action(value);
            return value;
        }
    }
}
