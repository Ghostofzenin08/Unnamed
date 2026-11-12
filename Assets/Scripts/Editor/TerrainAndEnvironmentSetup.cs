#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AI;
using Outpost404.Environment;

namespace Outpost404.EditorTools
{
    public static class TerrainAndEnvironmentSetup
    {
        public const string TerrainDataAssetPath = "Assets/TerrainData_Outpost404.asset";
        public const string TexturesDirectory = "Assets/Art/Textures/Terrain";
        public const string LayersDirectory = "Assets/TerrainLayers";

        public const string DiffuseBasaltPath = "Assets/Art/Textures/Terrain/T_BasaltSoil_Diffuse.png";
        public const string NormalBasaltPath = "Assets/Art/Textures/Terrain/T_BasaltSoil_Normal.png";
        public const string DiffuseMossPath = "Assets/Art/Textures/Terrain/T_BiolumMoss_Diffuse.png";
        public const string NormalMossPath = "Assets/Art/Textures/Terrain/T_BiolumMoss_Normal.png";
        public const string DiffuseRockPath = "Assets/Art/Textures/Terrain/T_CrystallineRock_Diffuse.png";
        public const string NormalRockPath = "Assets/Art/Textures/Terrain/T_CrystallineRock_Normal.png";

        public const string LayerBasaltPath = "Assets/TerrainLayers/Layer_BasaltSoil.terrainlayer";
        public const string LayerMossPath = "Assets/TerrainLayers/Layer_BiolumMoss.terrainlayer";
        public const string LayerRockPath = "Assets/TerrainLayers/Layer_CrystallineRock.terrainlayer";

        public const string SkyboxDayPath = "Assets/Fantasy Skybox FREE/Panoramics/FS002/FS002_Day.mat";
        public const string SkyboxSunsetPath = "Assets/Fantasy Skybox FREE/Panoramics/FS002/FS002_Sunset.mat";
        public const string SkyboxNightPath = "Assets/Fantasy Skybox FREE/Panoramics/FS002/FS002_Night.mat";
        public const string SkyboxSunrisePath = "Assets/Fantasy Skybox FREE/Panoramics/FS002/FS002_Sunrise.mat";

        [MenuItem("Outpost 404/Setup Sculpted Native Terrain and Environment", false, 14)]
        public static void BuildSculptedTerrainAndEnvironment()
        {
            string scenePath = "Assets/Scenes/SampleScene.unity";
            var activeScene = EditorSceneManager.GetActiveScene();
            Scene scene;
            if (activeScene.path == scenePath && activeScene.isLoaded)
            {
                scene = activeScene;
            }
            else
            {
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            // If base game objective or entities missing, build Phase 9 foundation first
            if (GameObject.Find("Beacon_SOS_Terminal") == null)
            {
                Phase9SceneSetup.BuildPhase9Scene();
                return;
            }

            // 1. Ensure texture and layer assets exist
            EnsureProceduralTexturesAndLayers();

            // 2. Load or create TerrainData asset
            TerrainData terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainDataAssetPath);
            if (terrainData == null)
            {
                terrainData = new TerrainData();
                AssetDatabase.CreateAsset(terrainData, TerrainDataAssetPath);
            }

            terrainData.heightmapResolution = 129;
            terrainData.size = new Vector3(120f, 18f, 120f);
            terrainData.baseMapResolution = 512;
            terrainData.SetDetailResolution(256, 16);

            // 3. Assign Stylized PBR TerrainLayers
            TerrainLayer layerBasalt = AssetDatabase.LoadAssetAtPath<TerrainLayer>(LayerBasaltPath);
            TerrainLayer layerMoss = AssetDatabase.LoadAssetAtPath<TerrainLayer>(LayerMossPath);
            TerrainLayer layerRock = AssetDatabase.LoadAssetAtPath<TerrainLayer>(LayerRockPath);
            terrainData.terrainLayers = new TerrainLayer[] { layerBasalt, layerMoss, layerRock };

            // 4. Sculpt multi-biome heightmap
            SculptHeightmap(terrainData);

            // 5. Generate procedural splatmaps (alphamaps)
            GenerateSplatmaps(terrainData);

            EditorUtility.SetDirty(terrainData);
            AssetDatabase.SaveAssets();

            // 6. Setup or update Terrain GameObject in scene under Environment root
            GameObject envRoot = GameObject.Find("Environment");
            if (envRoot == null) envRoot = new GameObject("Environment");

            Terrain terrain = envRoot.GetComponentInChildren<Terrain>();
            GameObject terrainObj;
            if (terrain == null)
            {
                terrainObj = Terrain.CreateTerrainGameObject(terrainData);
                terrainObj.name = "Terrain_Outpost404";
                terrainObj.transform.SetParent(envRoot.transform, false);
                terrain = terrainObj.GetComponent<Terrain>();
            }
            else
            {
                terrainObj = terrain.gameObject;
                terrain.terrainData = terrainData;
            }

            terrainObj.transform.position = new Vector3(-60f, 0f, -60f);
            terrainObj.transform.rotation = Quaternion.identity;
            terrainObj.transform.localScale = Vector3.one;

            terrain.drawHeightmap = true;
            terrain.drawTreesAndFoliage = true;
            terrain.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

            var tCol = terrainObj.GetComponent<TerrainCollider>();
            if (tCol == null) tCol = terrainObj.AddComponent<TerrainCollider>();
            tCol.terrainData = terrainData;

            GameObjectUtility.SetStaticEditorFlags(terrainObj, StaticEditorFlags.NavigationStatic | StaticEditorFlags.ContributeGI);

            // 7. Embed old flat ground planes beneath terrain so legacy test references remain valid without visual obstruction
            AdjustLegacyGroundZones(envRoot.transform);

            // 8. Snap all entities (Player, Beacon, Wreckage, Horde Nodes, Trees, Rocks, Pickups, Prowlers) to sculpted ground
            AlignEntitiesToTerrain(terrain);

            // 9. Wire Stylized Skybox Materials to DayNightCycleManager
            ConfigureSkyboxTransitions();

            // 10. Re-bake NavMesh across sculpted slopes
            BakeNavMesh();

            // 11. Mark scene dirty and save
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("<color=green>[Outpost 404]</color> Sculpted Native Unity Terrain, Splatmaps, Skybox, and Entity Ground Alignment successfully completed!");
        }

        public static void EnsureProceduralTexturesAndLayers()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Art/Textures"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Art")) AssetDatabase.CreateFolder("Assets", "Art");
                AssetDatabase.CreateFolder("Assets/Art", "Textures");
            }
            if (!AssetDatabase.IsValidFolder(TexturesDirectory))
            {
                AssetDatabase.CreateFolder("Assets/Art/Textures", "Terrain");
            }
            if (!AssetDatabase.IsValidFolder(LayersDirectory))
            {
                AssetDatabase.CreateFolder("Assets", "TerrainLayers");
            }

            int texSize = 256;
            bool createdAny = false;

            // Layer 1: Basalt Soil / Ash
            if (!File.Exists(DiffuseBasaltPath) || !File.Exists(NormalBasaltPath))
            {
                var diffuse = new Texture2D(texSize, texSize, TextureFormat.RGBA32, true);
                var heightMap = new float[texSize, texSize];
                Color baseCol = new Color(0.18f, 0.17f, 0.19f);
                Color darkCol = new Color(0.11f, 0.10f, 0.12f);
                Color rustCol = new Color(0.25f, 0.20f, 0.17f);

                for (int y = 0; y < texSize; y++)
                {
                    for (int x = 0; x < texSize; x++)
                    {
                        float n1 = Mathf.PerlinNoise(x * 0.08f, y * 0.08f);
                        float n2 = Mathf.PerlinNoise(x * 0.35f + 10f, y * 0.35f + 10f);
                        float h = Mathf.Clamp01(n1 * 0.7f + n2 * 0.3f);
                        heightMap[x, y] = h;

                        Color c = Color.Lerp(darkCol, baseCol, n1);
                        if (n2 > 0.65f) c = Color.Lerp(c, rustCol, (n2 - 0.65f) * 2f);
                        diffuse.SetPixel(x, y, c);
                    }
                }
                diffuse.Apply();
                File.WriteAllBytes(DiffuseBasaltPath, diffuse.EncodeToPNG());

                var normal = GenerateNormalMap(heightMap, texSize, 2.8f);
                File.WriteAllBytes(NormalBasaltPath, normal.EncodeToPNG());
                createdAny = true;
            }

            // Layer 2: Bioluminescent Moss / Turf
            if (!File.Exists(DiffuseMossPath) || !File.Exists(NormalMossPath))
            {
                var diffuse = new Texture2D(texSize, texSize, TextureFormat.RGBA32, true);
                var heightMap = new float[texSize, texSize];
                Color deepMoss = new Color(0.10f, 0.30f, 0.22f);
                Color lushMoss = new Color(0.18f, 0.52f, 0.36f);
                Color biolumGlow = new Color(0.24f, 0.88f, 0.68f);

                for (int y = 0; y < texSize; y++)
                {
                    for (int x = 0; x < texSize; x++)
                    {
                        float n1 = Mathf.PerlinNoise(x * 0.06f + 30f, y * 0.06f + 30f);
                        float n2 = Mathf.PerlinNoise(x * 0.25f + 60f, y * 0.25f + 60f);
                        float h = Mathf.Clamp01(n1 * 0.65f + n2 * 0.35f);
                        heightMap[x, y] = h;

                        Color c = Color.Lerp(deepMoss, lushMoss, n1);
                        if (n2 > 0.68f) c = Color.Lerp(c, biolumGlow, (n2 - 0.68f) * 2.8f);
                        diffuse.SetPixel(x, y, c);
                    }
                }
                diffuse.Apply();
                File.WriteAllBytes(DiffuseMossPath, diffuse.EncodeToPNG());

                var normal = GenerateNormalMap(heightMap, texSize, 2.2f);
                File.WriteAllBytes(NormalMossPath, normal.EncodeToPNG());
                createdAny = true;
            }

            // Layer 3: Crystalline Rock / Dark Ore
            if (!File.Exists(DiffuseRockPath) || !File.Exists(NormalRockPath))
            {
                var diffuse = new Texture2D(texSize, texSize, TextureFormat.RGBA32, true);
                var heightMap = new float[texSize, texSize];
                Color rockDark = new Color(0.13f, 0.12f, 0.18f);
                Color rockMid = new Color(0.26f, 0.24f, 0.34f);
                Color crystalVein = new Color(0.48f, 0.38f, 0.72f);
                Color crystalShine = new Color(0.35f, 0.75f, 0.85f);

                for (int y = 0; y < texSize; y++)
                {
                    for (int x = 0; x < texSize; x++)
                    {
                        float n1 = Mathf.PerlinNoise(x * 0.05f + 80f, y * 0.05f + 80f);
                        float stripe = Mathf.Abs(Mathf.Sin((x * 0.12f + y * 0.16f) * Mathf.PI));
                        float h = Mathf.Clamp01(n1 * 0.5f + (1f - stripe) * 0.5f);
                        heightMap[x, y] = h;

                        Color c = Color.Lerp(rockDark, rockMid, n1);
                        if (stripe < 0.25f) c = Color.Lerp(c, crystalVein, (0.25f - stripe) * 4f);
                        if (stripe < 0.08f) c = Color.Lerp(c, crystalShine, (0.08f - stripe) * 10f);
                        diffuse.SetPixel(x, y, c);
                    }
                }
                diffuse.Apply();
                File.WriteAllBytes(DiffuseRockPath, diffuse.EncodeToPNG());

                var normal = GenerateNormalMap(heightMap, texSize, 3.5f);
                File.WriteAllBytes(NormalRockPath, normal.EncodeToPNG());
                createdAny = true;
            }

            if (createdAny)
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }

            ConfigureNormalMapImporter(NormalBasaltPath);
            ConfigureNormalMapImporter(NormalMossPath);
            ConfigureNormalMapImporter(NormalRockPath);

            // Create or update TerrainLayer assets
            CreateOrUpdateLayer(LayerBasaltPath, DiffuseBasaltPath, NormalBasaltPath, new Vector2(6f, 6f), 0.15f, 0.20f);
            CreateOrUpdateLayer(LayerMossPath, DiffuseMossPath, NormalMossPath, new Vector2(5f, 5f), 0.05f, 0.35f);
            CreateOrUpdateLayer(LayerRockPath, DiffuseRockPath, NormalRockPath, new Vector2(8f, 8f), 0.45f, 0.50f);
        }

        private static void ConfigureNormalMapImporter(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.NormalMap)
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.SaveAndReimport();
            }
        }

        private static Texture2D GenerateNormalMap(float[,] heightMap, int size, float strength)
        {
            var normalTex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int xL = (x - 1 + size) % size;
                    int xR = (x + 1) % size;
                    int yD = (y - 1 + size) % size;
                    int yU = (y + 1) % size;

                    float dX = (heightMap[xR, y] - heightMap[xL, y]) * strength;
                    float dY = (heightMap[x, yU] - heightMap[x, yD]) * strength;

                    Vector3 n = new Vector3(-dX, -dY, 1f).normalized;
                    Color normalColor = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
                    normalTex.SetPixel(x, y, normalColor);
                }
            }
            normalTex.Apply();
            return normalTex;
        }

        private static void CreateOrUpdateLayer(string layerPath, string diffusePath, string normalPath, Vector2 tileSize, float metallic, float smoothness)
        {
            TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);
            if (layer == null)
            {
                layer = new TerrainLayer();
                AssetDatabase.CreateAsset(layer, layerPath);
            }

            layer.diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(diffusePath);
            layer.normalMapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            layer.tileSize = tileSize;
            layer.metallic = metallic;
            layer.smoothness = smoothness;
            EditorUtility.SetDirty(layer);
        }

        public static float CalculateSculptedHeight(float worldX, float worldZ)
        {
            // 1. Base ground datum: 1.8m
            float baseHeight = 1.8f;

            // 2. Sector Alpha: Crash Site Enclave (-60 to 0 in X, -60 to 0 in Z)
            // Impact crater bowl centered around pod wreckage & beacon (-30, -30)
            float distToCrash = Vector2.Distance(new Vector2(worldX, worldZ), new Vector2(-30f, -30f));
            float alphaElevation = 0f;
            if (distToCrash < 22f)
            {
                if (distToCrash < 10f)
                {
                    float bowlT = distToCrash / 10f;
                    alphaElevation = -1.25f * (1f - bowlT * bowlT); // Sunken crater bowl (-1.25m at center)
                }
                else
                {
                    float rimT = (distToCrash - 10f) / 10f; // 0 to 1
                    alphaElevation = Mathf.Sin(rimT * Mathf.PI) * 1.35f; // Raised ejecta rim (+1.35m)
                }
            }

            // 3. Sector Beta: Deep Timber Forest (-60 to 0 in X, 0 to 60 in Z)
            // Rolling organic hills and gentle winding pathways
            float betaHills = Mathf.Sin(worldX * 0.08f) * Mathf.Cos(worldZ * 0.08f) * 1.5f 
                            + (Mathf.PerlinNoise(worldX * 0.05f + 15f, worldZ * 0.05f + 15f) - 0.5f) * 2.8f;

            // 4. Sector Gamma: Crystalline Basin (0 to 60 in X, 0 to 60 in Z)
            // Stepped basalt terraces, rocky plateaus, and crevices
            float gammaRaw = (worldX + worldZ) * 0.06f + Mathf.PerlinNoise(worldX * 0.08f + 50f, worldZ * 0.08f + 50f) * 1.2f;
            float steppedTerrace = Mathf.Floor(gammaRaw * 3f) * 0.9f;
            float crevicePattern = Mathf.Abs(Mathf.Sin(worldX * 0.22f - worldZ * 0.16f));
            if (crevicePattern < 0.15f)
            {
                steppedTerrace -= 1.2f * (1f - crevicePattern / 0.15f); // Sunken crevice fissure
            }
            float gammaTerraces = steppedTerrace;

            // 5. Sector Delta: Anomaly Hive (0 to 60 in X, -60 to 0 in Z)
            // Jagged mounds and sunken corrupted ravines
            float deltaMounds = Mathf.Abs(Mathf.Sin(worldX * 0.15f) * Mathf.Cos(worldZ * 0.15f)) * 2.6f;
            float ravinePattern = Mathf.Abs(Mathf.PerlinNoise(worldX * 0.10f + 100f, worldZ * 0.10f + 100f) * 2f - 1f);
            if (ravinePattern < 0.22f)
            {
                deltaMounds -= 1.8f * (1f - ravinePattern / 0.22f); // Sunken corrupted ravine
            }
            float deltaHive = deltaMounds;

            // Soft multi-biome sector blending
            float blendRange = 8f;
            float wx = Mathf.Clamp01((worldX + blendRange) / (blendRange * 2f));
            float wz = Mathf.Clamp01((worldZ + blendRange) / (blendRange * 2f));
            wx = wx * wx * (3f - 2f * wx);
            wz = wz * wz * (3f - 2f * wz);

            float wAlpha = (1f - wx) * (1f - wz);
            float wBeta  = (1f - wx) * wz;
            float wGamma = wx * wz;
            float wDelta = wx * (1f - wz);

            float interiorElevation = baseHeight 
                                    + (wAlpha * alphaElevation) 
                                    + (wBeta * betaHills) 
                                    + (wGamma * gammaTerraces) 
                                    + (wDelta * deltaHive);

            // 6. Perimeter Mountain Ring: High surrounding natural ridge cliffs (rising to 12m - 15m)
            float distToEdge = Mathf.Min(60f - Mathf.Abs(worldX), 60f - Mathf.Abs(worldZ));
            float mountainCliffs = 0f;
            if (distToEdge < 14f)
            {
                float edgeFactor = 1f - Mathf.Clamp01(distToEdge / 14f);
                mountainCliffs = (edgeFactor * edgeFactor) * 12.5f;
                mountainCliffs += edgeFactor * (Mathf.Sin(worldX * 0.35f) * Mathf.Cos(worldZ * 0.35f) * 1.8f 
                                              + (Mathf.PerlinNoise(worldX * 0.12f + 200f, worldZ * 0.12f + 200f) - 0.5f) * 2.4f);
            }

            float finalHeight = interiorElevation + mountainCliffs;
            return Mathf.Clamp(finalHeight, 0.4f, 17.5f);
        }

        public static void SculptHeightmap(TerrainData terrainData)
        {
            int res = terrainData.heightmapResolution; // 129
            float terrainHeightScale = terrainData.size.y; // 18f
            float[,] heights = new float[res, res];

            for (int iz = 0; iz < res; iz++)
            {
                float worldZ = -60f + (iz / (float)(res - 1)) * 120f;
                for (int ix = 0; ix < res; ix++)
                {
                    float worldX = -60f + (ix / (float)(res - 1)) * 120f;
                    float hMeters = CalculateSculptedHeight(worldX, worldZ);
                    heights[iz, ix] = Mathf.Clamp01(hMeters / terrainHeightScale);
                }
            }

            terrainData.SetHeights(0, 0, heights);
        }

        public static void GenerateSplatmaps(TerrainData terrainData)
        {
            int alphaRes = 256;
            terrainData.alphamapResolution = alphaRes;
            float[,,] splatmaps = new float[alphaRes, alphaRes, 3];

            for (int iz = 0; iz < alphaRes; iz++)
            {
                float normZ = iz / (float)(alphaRes - 1);
                float worldZ = -60f + normZ * 120f;

                for (int ix = 0; ix < alphaRes; ix++)
                {
                    float normX = ix / (float)(alphaRes - 1);
                    float worldX = -60f + normX * 120f;

                    float slope = terrainData.GetSteepness(normX, normZ);
                    float heightMeters = terrainData.GetHeight(Mathf.RoundToInt(normX * (terrainData.heightmapResolution - 1)), Mathf.RoundToInt(normZ * (terrainData.heightmapResolution - 1)));

                    // Biome sector weights
                    float blendRange = 8f;
                    float wx = Mathf.Clamp01((worldX + blendRange) / (blendRange * 2f));
                    float wz = Mathf.Clamp01((worldZ + blendRange) / (blendRange * 2f));
                    wx = wx * wx * (3f - 2f * wx);
                    wz = wz * wz * (3f - 2f * wz);

                    float wAlpha = (1f - wx) * (1f - wz);
                    float wBeta  = (1f - wx) * wz;
                    float wGamma = wx * wz;
                    float wDelta = wx * (1f - wz);

                    // Steep slopes and high mountain cliffs favor crystalline rock
                    float rockWeight = Mathf.Clamp01((slope - 20f) / 12f) * 1.6f + Mathf.Clamp01((heightMeters - 7.0f) / 4.0f) * 1.5f;
                    rockWeight += (wGamma * 0.85f) + (wDelta * 0.65f);

                    // Forest turf in Sector Beta favors bioluminescent moss
                    float mossWeight = wBeta * 1.8f * (1f - Mathf.Clamp01((slope - 15f) / 10f));
                    mossWeight += (1f - Mathf.Clamp01(slope / 15f)) * 0.12f;

                    // Sector Alpha crash site and base ground favor dark basalt ash
                    float distToCrash = Vector2.Distance(new Vector2(worldX, worldZ), new Vector2(-30f, -30f));
                    float basaltWeight = (wAlpha * 2.2f) + (wDelta * 0.8f) + (wGamma * 0.6f) + 0.35f;
                    if (distToCrash < 16f)
                    {
                        basaltWeight += 1.8f * (1f - distToCrash / 16f);
                    }

                    float total = basaltWeight + mossWeight + rockWeight + 0.0001f;
                    splatmaps[iz, ix, 0] = basaltWeight / total; // Basalt
                    splatmaps[iz, ix, 1] = mossWeight / total;   // Moss
                    splatmaps[iz, ix, 2] = rockWeight / total;   // Crystalline Rock
                }
            }

            terrainData.SetAlphamaps(0, 0, splatmaps);
        }

        private static void AdjustLegacyGroundZones(Transform envRoot)
        {
            // Lower legacy flat ground cubes below terrain and disable their renderers so they remain in scene hierarchy for compatibility tests
            string[] legacyGroundNames = new string[]
            {
                "Ground_SectorAlpha_CrashZone",
                "Ground_SectorBeta_Forest",
                "Ground_SectorGamma_CrystallineBasin",
                "Ground_SectorDelta_AnomalyHive"
            };

            for (int i = 0; i < legacyGroundNames.Length; i++)
            {
                Transform ground = envRoot.Find(legacyGroundNames[i]);
                if (ground != null)
                {
                    ground.position = new Vector3(ground.position.x, -2.5f, ground.position.z);
                    var rend = ground.GetComponent<MeshRenderer>();
                    if (rend != null) rend.enabled = false;
                    var col = ground.GetComponent<BoxCollider>();
                    if (col != null) col.isTrigger = true;
                }
            }

            // Adjust perimeter walls height to seamlessly integrate with perimeter mountain cliffs
            string[] wallNames = new string[] { "Wall_North", "Wall_South", "Wall_East", "Wall_West" };
            for (int i = 0; i < wallNames.Length; i++)
            {
                Transform wall = envRoot.Find(wallNames[i]);
                if (wall != null)
                {
                    wall.localScale = new Vector3(wall.localScale.x, 18.0f, wall.localScale.z);
                    wall.position = new Vector3(wall.position.x, 9.0f, wall.position.z);
                }
            }
        }

        public static void AlignEntitiesToTerrain(Terrain terrain)
        {
            float TerrainY(Vector3 pos) => terrain.SampleHeight(pos) + terrain.transform.position.y;

            // 1. Player
            GameObject player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
            if (player != null)
            {
                Vector3 pos = player.transform.position;
                player.transform.position = new Vector3(pos.x, TerrainY(pos), pos.z);
            }

            // 2. SOS Beacon Terminal
            GameObject beacon = GameObject.Find("Beacon_SOS_Terminal");
            if (beacon != null)
            {
                Vector3 pos = beacon.transform.position;
                beacon.transform.position = new Vector3(pos.x, TerrainY(pos), pos.z);
            }

            // 3. Crashed Pod Wreckage
            GameObject wreckage = GameObject.Find("Pod_Wreckage_Crashed");
            if (wreckage != null)
            {
                Vector3 pos = wreckage.transform.position;
                wreckage.transform.position = new Vector3(pos.x, TerrainY(pos) + 0.4f, pos.z);
            }

            // 4. Horde Spawn Points
            GameObject hordeRoot = GameObject.Find("HordeSpawnPoints");
            if (hordeRoot != null)
            {
                foreach (Transform sp in hordeRoot.transform)
                {
                    Vector3 pos = sp.position;
                    sp.position = new Vector3(pos.x, TerrainY(pos) + 0.1f, pos.z);
                }
            }

            // 5. Trees, Shrubs, Rocks, Pickups
            GameObject env = GameObject.Find("Environment");
            if (env != null)
            {
                foreach (Transform child in env.transform)
                {
                    if (child.name.StartsWith("Tree_Alien_"))
                    {
                        Vector3 pos = child.position;
                        child.position = new Vector3(pos.x, TerrainY(pos), pos.z);
                    }
                    else if (child.name.StartsWith("Shrub_Alien_"))
                    {
                        Vector3 pos = child.position;
                        child.position = new Vector3(pos.x, TerrainY(pos), pos.z);
                    }
                    else if (child.name.StartsWith("Rock_Pulse_"))
                    {
                        Vector3 pos = child.position;
                        child.position = new Vector3(pos.x, TerrainY(pos), pos.z);
                    }
                    else if (child.name.StartsWith("Pickup_"))
                    {
                        Vector3 pos = child.position;
                        child.position = new Vector3(pos.x, TerrainY(pos) + 0.25f, pos.z);
                    }
                }
            }

            // 6. Enemies
            GameObject enemiesRoot = GameObject.Find("Enemies");
            if (enemiesRoot != null)
            {
                foreach (Transform enemy in enemiesRoot.transform)
                {
                    Vector3 pos = enemy.position;
                    enemy.position = new Vector3(pos.x, TerrainY(pos) + 0.05f, pos.z);
                }
            }
        }

        public static void ConfigureSkyboxTransitions()
        {
            var dayNightManager = UnityEngine.Object.FindFirstObjectByType<DayNightCycleManager>();
            if (dayNightManager == null)
            {
                var cycleObj = GameObject.Find("DayNightCycle");
                if (cycleObj == null) cycleObj = new GameObject("DayNightCycle");
                dayNightManager = cycleObj.GetComponent<DayNightCycleManager>();
                if (dayNightManager == null) dayNightManager = cycleObj.AddComponent<DayNightCycleManager>();
            }

            dayNightManager.EnsureGradientsAndCurves();

            Material daySky = AssetDatabase.LoadAssetAtPath<Material>(SkyboxDayPath);
            Material sunsetSky = AssetDatabase.LoadAssetAtPath<Material>(SkyboxSunsetPath);
            Material nightSky = AssetDatabase.LoadAssetAtPath<Material>(SkyboxNightPath);
            Material sunriseSky = AssetDatabase.LoadAssetAtPath<Material>(SkyboxSunrisePath);

            dayNightManager.DaySkyboxMaterial = daySky;
            dayNightManager.DuskSkyboxMaterial = sunsetSky;
            dayNightManager.NightSkyboxMaterial = nightSky;
            dayNightManager.DawnSkyboxMaterial = sunriseSky;

            dayNightManager.UpdateLightingAndEnvironment();
            EditorUtility.SetDirty(dayNightManager);
        }

        private static void BakeNavMesh()
        {
            try
            {
                UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
                Debug.Log("<color=green>[Outpost 404]</color> Terrain NavMesh bake completed successfully!");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Outpost 404] NavMesh bake warning: {ex.Message}");
            }
        }
    }
}
#endif
