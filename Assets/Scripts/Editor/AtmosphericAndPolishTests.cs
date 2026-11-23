#if UNITY_EDITOR
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Outpost404.Audio;
using Outpost404.Building;
using Outpost404.Environment;
using Outpost404.Player;
using Outpost404.Survival;
using Outpost404.EditorTools;

namespace Outpost404.Tests
{
    [TestFixture]
    public class AtmosphericAndPolishTests
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test, Order(1)]
        public void Scene_BuildAtmosphericAndPolish_ExecutesCleanlyAndSaves()
        {
            Assert.DoesNotThrow(() =>
            {
                AtmosphericAndPolishSetup.BuildAtmosphericFXAndPostProcessing();
            }, "AtmosphericAndPolishSetup.BuildAtmosphericFXAndPostProcessing must execute without exceptions.");
        }

        [Test, Order(2)]
        public void TerrainSplatmap_Sampling_ResolvesAccurateDominantLayersPerSector()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var terrain = Object.FindAnyObjectByType<Terrain>();
            Assert.IsNotNull(terrain, "Terrain must exist in the scene.");
            Assert.IsNotNull(terrain.terrainData, "TerrainData must be valid.");
            Assert.GreaterOrEqual(terrain.terrainData.terrainLayers.Length, 3, "Terrain must have at least 3 TerrainLayers.");

            // Sector Alpha: Crash Site crater center (-30, 0, -30) -> Basalt Soil dominant
            FootstepSurfaceType alphaSurface = TerrainSurfaceAudio.DetectSurface(new Vector3(-30f, 0f, -30f), terrain);
            Assert.AreEqual(FootstepSurfaceType.BasaltSoil, alphaSurface, "Sector Alpha crash center must resolve to BasaltSoil.");

            // Sector Beta: Timber Forest center (-30, 0, 30) -> Bioluminescent Moss dominant
            FootstepSurfaceType betaSurface = TerrainSurfaceAudio.DetectSurface(new Vector3(-30f, 0f, 30f), terrain);
            Assert.AreEqual(FootstepSurfaceType.BiolumMoss, betaSurface, "Sector Beta forest turf must resolve to BiolumMoss.");

            // Sector Gamma: Crystalline Basin center (30, 0, 30) -> Crystalline Rock dominant
            FootstepSurfaceType gammaSurface = TerrainSurfaceAudio.DetectSurface(new Vector3(30f, 0f, 30f), terrain);
            Assert.AreEqual(FootstepSurfaceType.CrystallineRock, gammaSurface, "Sector Gamma crystal basin must resolve to CrystallineRock.");
        }

        [Test, Order(3)]
        public void TerrainSplatmap_Sampling_OffTerrainOrOnStructures_FallsBackToDefault()
        {
            var terrain = Object.FindAnyObjectByType<Terrain>();
            Assert.IsNotNull(terrain, "Terrain must exist in the scene.");

            // 1. Off-terrain position (far outside -60 to 60 bounds)
            Vector3 offTerrainPos = new Vector3(450f, 0f, 450f);
            FootstepSurfaceType offTerrainSurface = TerrainSurfaceAudio.DetectSurface(offTerrainPos, terrain);
            Assert.AreEqual(FootstepSurfaceType.Default, offTerrainSurface, "Position far off-terrain must fall back to Default surface.");

            // 2. Position atop a player-constructed structure
            GameObject testStructure = new GameObject("Test_WoodenFloor");
            testStructure.transform.position = new Vector3(-30f, 2.0f, -30f);
            var boxCol = testStructure.AddComponent<BoxCollider>();
            boxCol.size = new Vector3(4f, 0.4f, 4f);
            testStructure.AddComponent<BuildableStructure>();

            try
            {
                // Position directly on top of the structure
                Vector3 atopStructure = new Vector3(-30f, 2.2f, -30f);
                FootstepSurfaceType structureSurface = TerrainSurfaceAudio.DetectSurface(atopStructure, terrain);
                Assert.AreEqual(FootstepSurfaceType.Default, structureSurface, "Position atop a structure must fall back to Default surface.");
            }
            finally
            {
                Object.DestroyImmediate(testStructure);
            }
        }

        [Test, Order(4)]
        public void FootstepAudio_ProceduralClipsAndSurfacePlayback_ExecutesCleanly()
        {
            var audioObj = new GameObject("Test_AudioManager");
            var audioMgr = audioObj.AddComponent<AudioManager>();
            AudioManager.Instance = audioMgr;

            try
            {
                // Verify distinct procedural clips generate with valid characteristics
                Assert.IsNotNull(audioMgr.FootstepBasaltClip, "Basalt footstep clip must generate cleanly.");
                Assert.AreEqual("Footstep_Basalt_Procedural", audioMgr.FootstepBasaltClip.name);
                Assert.Greater(audioMgr.FootstepBasaltClip.length, 0.04f);

                Assert.IsNotNull(audioMgr.FootstepMossClip, "Moss footstep clip must generate cleanly.");
                Assert.AreEqual("Footstep_Moss_Procedural", audioMgr.FootstepMossClip.name);
                Assert.Greater(audioMgr.FootstepMossClip.length, 0.05f);

                Assert.IsNotNull(audioMgr.FootstepRockClip, "Rock footstep clip must generate cleanly.");
                Assert.AreEqual("Footstep_Rock_Procedural", audioMgr.FootstepRockClip.name);
                Assert.Greater(audioMgr.FootstepRockClip.length, 0.03f);

                Assert.IsNotNull(audioMgr.FootstepClip, "Default footstep clip must generate cleanly.");

                // Verify PlayFootstep execution across all surface types
                Assert.DoesNotThrow(() => audioMgr.PlayFootstep(FootstepSurfaceType.BasaltSoil));
                Assert.AreEqual(FootstepSurfaceType.BasaltSoil, audioMgr.LastPlayedFootstepSurface);

                Assert.DoesNotThrow(() => audioMgr.PlayFootstep(FootstepSurfaceType.BiolumMoss));
                Assert.AreEqual(FootstepSurfaceType.BiolumMoss, audioMgr.LastPlayedFootstepSurface);

                Assert.DoesNotThrow(() => audioMgr.PlayFootstep(FootstepSurfaceType.CrystallineRock));
                Assert.AreEqual(FootstepSurfaceType.CrystallineRock, audioMgr.LastPlayedFootstepSurface);

                Assert.DoesNotThrow(() => audioMgr.PlayFootstep(FootstepSurfaceType.Default));
                Assert.AreEqual(FootstepSurfaceType.Default, audioMgr.LastPlayedFootstepSurface);
            }
            finally
            {
                AudioManager.Instance = null;
                Object.DestroyImmediate(audioObj);
            }
        }

        [Test, Order(5)]
        public void BiomeParticleSystem_AllFourSectorEmittersConfiguredWithAccurateVisuals()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var particleMgr = Object.FindAnyObjectByType<BiomeParticleManager>();
            Assert.IsNotNull(particleMgr, "BiomeParticleManager must exist in the scene.");
            particleMgr.EnsureEmitters();

            // 1. Sector Beta: Drifting bioluminescent flora spores
            var beta = particleMgr.BetaFloraParticles;
            Assert.IsNotNull(beta, "Sector Beta Flora particle emitter must exist.");
            Assert.IsTrue(beta.gameObject.activeInHierarchy, "Sector Beta emitter must be active.");
            var betaMain = beta.main;
            Assert.IsTrue(betaMain.loop, "Beta particles must loop.");
            Assert.GreaterOrEqual(betaMain.startLifetime.constantMax, 4.0f, "Beta spores must have long floating lifetime.");
            Assert.LessOrEqual(betaMain.startSpeed.constantMax, 1.2f, "Beta spores must drift gently with low speed.");

            // 2. Sector Gamma: Shimmering crystalline energy dust
            var gamma = particleMgr.GammaCrystalParticles;
            Assert.IsNotNull(gamma, "Sector Gamma Crystal particle emitter must exist.");
            Assert.IsTrue(gamma.gameObject.activeInHierarchy, "Sector Gamma emitter must be active.");
            var gammaMain = gamma.main;
            Assert.IsTrue(gammaMain.loop, "Gamma particles must loop.");

            // 3. Sector Delta: Swirling dark violet toxic embers and anomaly vapor
            var delta = particleMgr.DeltaAnomalyParticles;
            Assert.IsNotNull(delta, "Sector Delta Anomaly particle emitter must exist.");
            Assert.IsTrue(delta.gameObject.activeInHierarchy, "Sector Delta emitter must be active.");
            var deltaMain = delta.main;
            Assert.IsTrue(deltaMain.loop, "Delta particles must loop.");

            // 4. Sector Alpha: Smoldering wreckage wisps
            var alpha = particleMgr.AlphaWreckageParticles;
            Assert.IsNotNull(alpha, "Sector Alpha Wreckage particle emitter must exist.");
            Assert.IsTrue(alpha.gameObject.activeInHierarchy, "Sector Alpha emitter must be active.");
            var alphaMain = alpha.main;
            Assert.IsTrue(alphaMain.loop, "Alpha particles must loop.");
        }

        [Test, Order(6)]
        public void BiomeParticleSystem_SectorBoundsAndPositionResolution_MatchesPlanetaryLayout()
        {
            // Verify sector bounds centers and extents
            Bounds bAlpha = BiomeParticleManager.GetSectorBounds(BiomeSector.SectorAlpha_CrashSite);
            Assert.AreEqual(-30f, bAlpha.center.x, 1f);
            Assert.AreEqual(-30f, bAlpha.center.z, 1f);

            Bounds bBeta = BiomeParticleManager.GetSectorBounds(BiomeSector.SectorBeta_TimberForest);
            Assert.AreEqual(-30f, bBeta.center.x, 1f);
            Assert.AreEqual(30f, bBeta.center.z, 1f);

            Bounds bGamma = BiomeParticleManager.GetSectorBounds(BiomeSector.SectorGamma_CrystallineBasin);
            Assert.AreEqual(30f, bGamma.center.x, 1f);
            Assert.AreEqual(30f, bGamma.center.z, 1f);

            Bounds bDelta = BiomeParticleManager.GetSectorBounds(BiomeSector.SectorDelta_AnomalyHive);
            Assert.AreEqual(30f, bDelta.center.x, 1f);
            Assert.AreEqual(-30f, bDelta.center.z, 1f);

            // Verify coordinate resolution
            Assert.AreEqual(BiomeSector.SectorAlpha_CrashSite, BiomeParticleManager.GetSectorAt(new Vector3(-25f, 0f, -25f)));
            Assert.AreEqual(BiomeSector.SectorBeta_TimberForest, BiomeParticleManager.GetSectorAt(new Vector3(-25f, 0f, 25f)));
            Assert.AreEqual(BiomeSector.SectorGamma_CrystallineBasin, BiomeParticleManager.GetSectorAt(new Vector3(25f, 0f, 25f)));
            Assert.AreEqual(BiomeSector.SectorDelta_AnomalyHive, BiomeParticleManager.GetSectorAt(new Vector3(25f, 0f, -25f)));
        }

        [Test, Order(7)]
        public void URPPostProcessing_GlobalVolumeAndProfileOverrides_ConfiguredAccurately()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var ppMgr = Object.FindAnyObjectByType<AtmospherePostProcessManager>();
            Assert.IsNotNull(ppMgr, "AtmospherePostProcessManager must exist in the scene.");

            Volume volume = ppMgr.Volume;
            Assert.IsNotNull(volume, "Volume component must exist on the post-process manager.");
            Assert.IsTrue(volume.isGlobal, "Volume must be marked isGlobal = true.");

            VolumeProfile profile = volume.sharedProfile;
            Assert.IsNotNull(profile, "Volume must have a valid sharedProfile assigned.");

            // 1. Tonemapping
            Assert.IsTrue(profile.TryGet(out Tonemapping tonemapping), "Profile must contain Tonemapping override.");
            Assert.IsTrue(tonemapping.active, "Tonemapping must be active.");
            Assert.AreEqual(TonemappingMode.ACES, tonemapping.mode.value, "Tonemapping mode must be ACES.");

            // 2. Bloom
            Assert.IsTrue(profile.TryGet(out Bloom bloom), "Profile must contain Bloom override.");
            Assert.IsTrue(bloom.active, "Bloom must be active.");
            Assert.AreEqual(1.2f, bloom.intensity.value, 0.05f, "Bloom intensity must be ~1.2.");
            Assert.AreEqual(0.9f, bloom.threshold.value, 0.05f, "Bloom threshold must be ~0.9.");

            // 3. Vignette
            Assert.IsTrue(profile.TryGet(out Vignette vignette), "Profile must contain Vignette override.");
            Assert.IsTrue(vignette.active, "Vignette must be active.");
            Assert.AreEqual(0.25f, vignette.intensity.value, 0.05f, "Vignette baseline intensity must be ~0.25.");
            Assert.AreEqual(Color.black, vignette.color.value, "Vignette color must be black.");

            // 4. Color Adjustments
            Assert.IsTrue(profile.TryGet(out ColorAdjustments colorAdj), "Profile must contain ColorAdjustments override.");
            Assert.IsTrue(colorAdj.active, "ColorAdjustments must be active.");
            Assert.AreEqual(10f, colorAdj.contrast.value, 0.5f, "ColorAdjustments contrast must be ~10.");
            Assert.AreEqual(12f, colorAdj.saturation.value, 0.5f, "ColorAdjustments saturation must be ~12.");
        }

        [Test, Order(8)]
        public void URPPostProcessing_VignetteDynamicModulation_DeepensOnNightAndFreezing()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var ppMgr = Object.FindAnyObjectByType<AtmospherePostProcessManager>();
            Assert.IsNotNull(ppMgr, "AtmospherePostProcessManager must exist.");

            ppMgr.ResolveProfileComponents();
            ppMgr.ResolveExternalReferences();

            // Baseline daylight evaluation
            float baseline = ppMgr.EvaluateVignetteTarget(1f);
            Assert.AreEqual(ppMgr.BaselineVignette, baseline, 0.05f, "Standard daylight must yield baseline vignette.");

            // Simulate Night phase deepening
            var cycle = Object.FindAnyObjectByType<DayNightCycleManager>();
            if (cycle != null)
            {
                cycle.SetTime(0.75f); // Midnight
                float nightTarget = ppMgr.EvaluateVignetteTarget(1f);
                Assert.Greater(nightTarget, baseline, "Vignette target must deepen during Night phase.");
                cycle.SetTime(0.25f); // Return to Noon
            }

            // Simulate Critical Freezing deepening
            var player = GameObject.FindWithTag("Player");
            var stats = player != null ? player.GetComponent<PlayerStats>() : null;
            if (stats != null)
            {
                stats.SetCurrentBodyTemperatureDirect(22f); // Below critical freezing threshold (25°C)
                float freezingTarget = ppMgr.EvaluateVignetteTarget(1f);
                Assert.Greater(freezingTarget, baseline, "Vignette target must deepen significantly during Critical Freezing.");
                stats.SetCurrentBodyTemperatureDirect(37f); // Restore normal
            }
        }

        [Test, Order(9)]
        public void BuildAutomation_ConfigurationAndTargetVerification()
        {
            Assert.AreEqual("Builds/Windows/Outpost404.exe", BuildAutomation.BuildExecutablePath);
            Assert.AreEqual("Builds/Windows", BuildAutomation.BuildOutputDirectory);

            string[] scenes = BuildAutomation.GetConfiguredBuildScenes();
            Assert.IsNotNull(scenes, "Build scenes array must not be null.");
            Assert.GreaterOrEqual(scenes.Length, 1, "At least one scene must be configured for building.");
            Assert.Contains("Assets/Scenes/SampleScene.unity", scenes, "SampleScene.unity must be included in the build scenes list.");
        }
    }
}
#endif
