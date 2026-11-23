using UnityEngine;

namespace Outpost404.Environment
{
    public enum BiomeSector
    {
        SectorAlpha_CrashSite = 0,
        SectorBeta_TimberForest = 1,
        SectorGamma_CrystallineBasin = 2,
        SectorDelta_AnomalyHive = 3
    }

    /// <summary>
    /// Coordinates atmospheric and biome-specific environmental particle emitters across the 4 planetary sectors:
    /// - Sector Beta (Timber Forest): Drifting bioluminescent flora spores (gentle glowing green/teal motes).
    /// - Sector Gamma (Crystalline Basin): Shimmering crystalline energy dust (sparkling cyan motes).
    /// - Sector Delta (Anomaly Hive): Swirling dark violet toxic embers and anomaly vapor.
    /// - Sector Alpha (Crash Site): Smoldering wreckage smoke and ember wisps.
    /// </summary>
    [DisallowMultipleComponent]
    public class BiomeParticleManager : MonoBehaviour
    {
        public static BiomeParticleManager Instance { get; set; }

        [Header("Sector Environmental Emitters")]
        [SerializeField] private ParticleSystem betaFloraParticles;
        [SerializeField] private ParticleSystem gammaCrystalParticles;
        [SerializeField] private ParticleSystem deltaAnomalyParticles;
        [SerializeField] private ParticleSystem alphaWreckageParticles;

        public ParticleSystem BetaFloraParticles => betaFloraParticles;
        public ParticleSystem GammaCrystalParticles => gammaCrystalParticles;
        public ParticleSystem DeltaAnomalyParticles => deltaAnomalyParticles;
        public ParticleSystem AlphaWreckageParticles => alphaWreckageParticles;

        public static Bounds SectorAlphaBounds => new Bounds(new Vector3(-30f, 4f, -30f), new Vector3(60f, 20f, 60f));
        public static Bounds SectorBetaBounds => new Bounds(new Vector3(-30f, 4f, 30f), new Vector3(60f, 20f, 60f));
        public static Bounds SectorGammaBounds => new Bounds(new Vector3(30f, 4f, 30f), new Vector3(60f, 20f, 60f));
        public static Bounds SectorDeltaBounds => new Bounds(new Vector3(30f, 4f, -30f), new Vector3(60f, 20f, 60f));

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            EnsureEmitters();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Resolves which biome sector contains the given world coordinates.
        /// </summary>
        public static BiomeSector GetSectorAt(Vector3 worldPosition)
        {
            if (worldPosition.x < 0f)
            {
                return (worldPosition.z >= 0f) ? BiomeSector.SectorBeta_TimberForest : BiomeSector.SectorAlpha_CrashSite;
            }
            else
            {
                return (worldPosition.z >= 0f) ? BiomeSector.SectorGamma_CrystallineBasin : BiomeSector.SectorDelta_AnomalyHive;
            }
        }

        /// <summary>
        /// Returns the bounding box defining the world space boundary of the specified sector.
        /// </summary>
        public static Bounds GetSectorBounds(BiomeSector sector)
        {
            return sector switch
            {
                BiomeSector.SectorAlpha_CrashSite => SectorAlphaBounds,
                BiomeSector.SectorBeta_TimberForest => SectorBetaBounds,
                BiomeSector.SectorGamma_CrystallineBasin => SectorGammaBounds,
                BiomeSector.SectorDelta_AnomalyHive => SectorDeltaBounds,
                _ => SectorAlphaBounds
            };
        }

        /// <summary>
        /// Returns the ParticleSystem associated with the specified sector.
        /// </summary>
        public ParticleSystem GetEmitter(BiomeSector sector)
        {
            EnsureEmitters();
            return sector switch
            {
                BiomeSector.SectorAlpha_CrashSite => alphaWreckageParticles,
                BiomeSector.SectorBeta_TimberForest => betaFloraParticles,
                BiomeSector.SectorGamma_CrystallineBasin => gammaCrystalParticles,
                BiomeSector.SectorDelta_AnomalyHive => deltaAnomalyParticles,
                _ => null
            };
        }

        /// <summary>
        /// Verifies that all 4 sector particle emitters exist and are procedurally configured.
        /// </summary>
        public void EnsureEmitters()
        {
            Material particleMat = GetDefaultParticleMaterial();

            // 1. Sector Beta: Drifting bioluminescent flora spores
            if (betaFloraParticles == null)
            {
                var existing = transform.Find("Emitter_Beta_BiolumSpores");
                if (existing != null) betaFloraParticles = existing.GetComponent<ParticleSystem>();
            }
            if (betaFloraParticles == null)
            {
                GameObject go = new GameObject("Emitter_Beta_BiolumSpores");
                go.transform.SetParent(transform, false);
                go.transform.position = new Vector3(-30f, 4f, 30f);
                betaFloraParticles = go.AddComponent<ParticleSystem>();
            }
            ConfigureEmitter(
                betaFloraParticles,
                particleMat,
                boxSize: new Vector3(50f, 8f, 50f),
                minColor: new Color(0.20f, 0.95f, 0.65f, 0.85f),
                maxColor: new Color(0.10f, 0.85f, 0.90f, 0.70f),
                minLifetime: 5.0f, maxLifetime: 8.0f,
                minSpeed: 0.3f, maxSpeed: 0.8f,
                minSize: 0.08f, maxSize: 0.18f,
                emissionRate: 30f,
                gravity: -0.02f
            );

            // 2. Sector Gamma: Shimmering crystalline energy dust
            if (gammaCrystalParticles == null)
            {
                var existing = transform.Find("Emitter_Gamma_CrystallineDust");
                if (existing != null) gammaCrystalParticles = existing.GetComponent<ParticleSystem>();
            }
            if (gammaCrystalParticles == null)
            {
                GameObject go = new GameObject("Emitter_Gamma_CrystallineDust");
                go.transform.SetParent(transform, false);
                go.transform.position = new Vector3(30f, 4f, 30f);
                gammaCrystalParticles = go.AddComponent<ParticleSystem>();
            }
            ConfigureEmitter(
                gammaCrystalParticles,
                particleMat,
                boxSize: new Vector3(50f, 8f, 50f),
                minColor: new Color(0.30f, 0.88f, 1.0f, 0.90f),
                maxColor: new Color(0.75f, 0.95f, 1.0f, 0.80f),
                minLifetime: 3.5f, maxLifetime: 6.0f,
                minSpeed: 0.5f, maxSpeed: 1.2f,
                minSize: 0.05f, maxSize: 0.14f,
                emissionRate: 35f,
                gravity: 0.01f
            );

            // 3. Sector Delta: Swirling dark violet toxic embers and anomaly vapor
            if (deltaAnomalyParticles == null)
            {
                var existing = transform.Find("Emitter_Delta_AnomalyEmbers");
                if (existing != null) deltaAnomalyParticles = existing.GetComponent<ParticleSystem>();
            }
            if (deltaAnomalyParticles == null)
            {
                GameObject go = new GameObject("Emitter_Delta_AnomalyEmbers");
                go.transform.SetParent(transform, false);
                go.transform.position = new Vector3(30f, 4f, -30f);
                deltaAnomalyParticles = go.AddComponent<ParticleSystem>();
            }
            ConfigureEmitter(
                deltaAnomalyParticles,
                particleMat,
                boxSize: new Vector3(50f, 8f, 50f),
                minColor: new Color(0.45f, 0.08f, 0.75f, 0.85f),
                maxColor: new Color(0.75f, 0.15f, 0.55f, 0.90f),
                minLifetime: 3.0f, maxLifetime: 5.0f,
                minSpeed: 0.9f, maxSpeed: 2.2f,
                minSize: 0.10f, maxSize: 0.24f,
                emissionRate: 40f,
                gravity: -0.05f
            );

            // 4. Sector Alpha: Smoldering wreckage wisps
            if (alphaWreckageParticles == null)
            {
                var existing = transform.Find("Emitter_Alpha_SmolderingWisps");
                if (existing != null) alphaWreckageParticles = existing.GetComponent<ParticleSystem>();
            }
            if (alphaWreckageParticles == null)
            {
                GameObject go = new GameObject("Emitter_Alpha_SmolderingWisps");
                go.transform.SetParent(transform, false);
                go.transform.position = new Vector3(-30f, 2.5f, -30f);
                alphaWreckageParticles = go.AddComponent<ParticleSystem>();
            }
            ConfigureEmitter(
                alphaWreckageParticles,
                particleMat,
                boxSize: new Vector3(32f, 4f, 32f),
                minColor: new Color(0.35f, 0.32f, 0.30f, 0.50f),
                maxColor: new Color(0.75f, 0.40f, 0.15f, 0.60f),
                minLifetime: 3.0f, maxLifetime: 5.5f,
                minSpeed: 0.4f, maxSpeed: 1.1f,
                minSize: 0.16f, maxSize: 0.38f,
                emissionRate: 20f,
                gravity: -0.04f
            );
        }

        private static void ConfigureEmitter(
            ParticleSystem ps,
            Material mat,
            Vector3 boxSize,
            Color minColor,
            Color maxColor,
            float minLifetime,
            float maxLifetime,
            float minSpeed,
            float maxSpeed,
            float minSize,
            float maxSize,
            float emissionRate,
            float gravity)
        {
            var pRenderer = ps.GetComponent<ParticleSystemRenderer>();
            if (pRenderer != null && mat != null)
            {
                pRenderer.material = mat;
            }

            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(minLifetime, maxLifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
            main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
            main.startColor = new ParticleSystem.MinMaxGradient(minColor, maxColor);
            main.gravityModifier = gravity;
            main.maxParticles = 1000;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = emissionRate;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = boxSize;

            if (!ps.isPlaying && Application.isPlaying)
            {
                ps.Play();
            }
        }

        private static Material defaultMat;
        public static Material GetDefaultParticleMaterial()
        {
            if (defaultMat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") 
                             ?? Shader.Find("Particles/Standard Unlit") 
                             ?? Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    defaultMat = new Material(shader);
                }
            }
            return defaultMat;
        }
    }
}
