#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Outpost404.Environment;
using Outpost404.Survival;
using Outpost404.UI;

namespace Outpost404.EditorTools
{
    public static class Phase7SceneSetup
    {
        [MenuItem("Outpost 404/Setup Full Game Scene (Phase 7 - Day-Night & Environmental Hazards)", false, 10)]
        public static void BuildPhase7Scene()
        {
            // 1. Build base sandbox with 3D models, inventory, crafting & structures
            Phase6SceneSetup.BuildPhase6Scene();

            string scenePath = "Assets/Scenes/SampleScene.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // 2. Locate or create Directional Light
            Light sunLight = null;
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i].type == LightType.Directional)
                {
                    sunLight = lights[i];
                    break;
                }
            }

            if (sunLight == null)
            {
                GameObject sunObj = new GameObject("Directional Light");
                sunLight = sunObj.AddComponent<Light>();
                sunLight.type = LightType.Directional;
            }

            // 3. Day / Night Cycle Manager
            var cycleObj = GameObject.Find("DayNightCycle");
            if (cycleObj == null)
            {
                cycleObj = new GameObject("DayNightCycle");
                Transform env = GameObject.Find("Environment")?.transform;
                if (env != null) cycleObj.transform.SetParent(env);
            }

            var cycleMgr = cycleObj.GetComponent<DayNightCycleManager>();
            if (cycleMgr == null) cycleMgr = cycleObj.AddComponent<DayNightCycleManager>();

            cycleMgr.SunLight = sunLight;
            cycleMgr.DayLengthInSeconds = 120f;
            cycleMgr.EnsureGradientsAndCurves();
            cycleMgr.UpdateLightingAndEnvironment();

            // 4. Update HUD Canvas with Clock, Weather & Thermometer Gauges
            GameObject canvasObj = GameObject.Find("HUD_Canvas");
            if (canvasObj != null)
            {
                var hud = canvasObj.GetComponent<SurvivalHUD>();
                if (hud != null)
                {
                    SetupEnvironmentalHUD(canvasObj, hud);
                }
            }

            // 5. Bake NavMesh to ensure dynamic structures and terrain are fully walkable
            try
            {
                UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
                Debug.Log("<color=green>[Outpost 404]</color> NavMesh baked cleanly for Phase 7 scene!");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Outpost 404] NavMesh bake note: {ex.Message}");
            }

            // Save Scene
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("<color=green>[Outpost 404]</color> Phase 7 Day/Night Cycle & Environmental Hazards integrated successfully!");
        }

        private static void SetupEnvironmentalHUD(GameObject canvasObj, SurvivalHUD hud)
        {
            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // 4a. Clock & Ambient Weather Panel (top-left)
            Transform existingClockPanel = canvasObj.transform.Find("ClockWeatherPanel");
            if (existingClockPanel != null) Object.DestroyImmediate(existingClockPanel.gameObject);

            GameObject clockPanelObj = new GameObject("ClockWeatherPanel");
            clockPanelObj.transform.SetParent(canvasObj.transform, false);
            var cpRt = clockPanelObj.AddComponent<RectTransform>();
            cpRt.anchorMin = new Vector2(0f, 1f);
            cpRt.anchorMax = new Vector2(0f, 1f);
            cpRt.pivot = new Vector2(0f, 1f);
            cpRt.anchoredPosition = new Vector2(30f, -30f);
            cpRt.sizeDelta = new Vector2(260f, 65f);

            var cpBg = clockPanelObj.AddComponent<Image>();
            cpBg.color = new Color(0.08f, 0.10f, 0.14f, 0.88f);

            // Phase indicator dot / icon
            GameObject phaseDotObj = new GameObject("PhaseIcon");
            phaseDotObj.transform.SetParent(clockPanelObj.transform, false);
            var dotRt = phaseDotObj.AddComponent<RectTransform>();
            dotRt.anchorMin = new Vector2(0f, 0.5f);
            dotRt.anchorMax = new Vector2(0f, 0.5f);
            dotRt.pivot = new Vector2(0f, 0.5f);
            dotRt.anchoredPosition = new Vector2(14f, 0f);
            dotRt.sizeDelta = new Vector2(16f, 16f);
            var dotImg = phaseDotObj.AddComponent<Image>();
            dotImg.color = new Color(1.0f, 0.75f, 0.35f);

            // Digital Clock Text
            GameObject clockTextObj = new GameObject("ClockText");
            clockTextObj.transform.SetParent(clockPanelObj.transform, false);
            var ctRt = clockTextObj.AddComponent<RectTransform>();
            ctRt.anchorMin = new Vector2(0f, 0.5f);
            ctRt.anchorMax = new Vector2(1f, 1f);
            ctRt.pivot = new Vector2(0f, 0.5f);
            ctRt.anchoredPosition = new Vector2(40f, 0f);
            ctRt.sizeDelta = new Vector2(-45f, 0f);
            var ct = clockTextObj.AddComponent<Text>();
            ct.font = defaultFont;
            ct.fontSize = 15;
            ct.fontStyle = FontStyle.Bold;
            ct.alignment = TextAnchor.MiddleLeft;
            ct.color = Color.white;
            ct.text = "Day 1 | 06:00 (Dawn)";

            // Ambient Temperature Text
            GameObject ambTextObj = new GameObject("AmbientTempText");
            ambTextObj.transform.SetParent(clockPanelObj.transform, false);
            var ambRt = ambTextObj.AddComponent<RectTransform>();
            ambRt.anchorMin = new Vector2(0f, 0f);
            ambRt.anchorMax = new Vector2(1f, 0.5f);
            ambRt.pivot = new Vector2(0f, 0.5f);
            ambRt.anchoredPosition = new Vector2(40f, 0f);
            ambRt.sizeDelta = new Vector2(-45f, 0f);
            var at = ambTextObj.AddComponent<Text>();
            at.font = defaultFont;
            at.fontSize = 13;
            at.alignment = TextAnchor.MiddleLeft;
            at.color = new Color(0.4f, 0.9f, 1f);
            at.text = "Amb: 8°C";

            // 4b. Thermometer Gauge (added to StatsPanel at bottom-left)
            Transform statsPanel = canvasObj.transform.Find("StatsPanel");
            Slider bodyTempSlider = null;
            Text bodyTempText = null;

            if (statsPanel != null)
            {
                var spRt = statsPanel.GetComponent<RectTransform>();
                if (spRt != null)
                {
                    spRt.sizeDelta = new Vector2(280f, 165f); // expand to fit thermometer
                }

                bodyTempSlider = CreateThermometerBar(statsPanel, "BodyTemperatureBar", new Vector2(0f, 120f), new Color(1.0f, 0.55f, 0.2f), out bodyTempText, defaultFont);
            }

            // 4c. Frost / Hypothermia Warning Banner (top center)
            Transform existingFrost = canvasObj.transform.Find("FrostWarningBanner");
            if (existingFrost != null) Object.DestroyImmediate(existingFrost.gameObject);

            GameObject frostObj = new GameObject("FrostWarningBanner");
            frostObj.transform.SetParent(canvasObj.transform, false);
            var frostRt = frostObj.AddComponent<RectTransform>();
            frostRt.anchorMin = new Vector2(0.5f, 1f);
            frostRt.anchorMax = new Vector2(0.5f, 1f);
            frostRt.pivot = new Vector2(0.5f, 1f);
            frostRt.anchoredPosition = new Vector2(0f, -95f);
            frostRt.sizeDelta = new Vector2(460f, 44f);
            var frostImg = frostObj.AddComponent<Image>();
            frostImg.color = new Color(0.12f, 0.45f, 0.75f, 0.90f);

            GameObject frostTextObj = new GameObject("FrostBannerText");
            frostTextObj.transform.SetParent(frostObj.transform, false);
            var fttRt = frostTextObj.AddComponent<RectTransform>();
            fttRt.anchorMin = Vector2.zero;
            fttRt.anchorMax = Vector2.one;
            fttRt.sizeDelta = Vector2.zero;
            var fttText = frostTextObj.AddComponent<Text>();
            fttText.font = defaultFont;
            fttText.fontSize = 14;
            fttText.fontStyle = FontStyle.Bold;
            fttText.alignment = TextAnchor.MiddleCenter;
            fttText.color = new Color(0.9f, 0.98f, 1.0f);
            fttText.text = "WARNING: HYPOTHERMIA DETECTED - SEEK HEAT BEACON";
            frostObj.SetActive(false);

            hud.ConfigureEnvironmentalUI(ct, dotImg, at, bodyTempText, bodyTempSlider, frostObj);
        }

        private static Slider CreateThermometerBar(Transform parent, string name, Vector2 pos, Color fillColor, out Text textLabel, Font font)
        {
            Transform existing = parent.Find(name);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            GameObject barObj = new GameObject(name);
            barObj.transform.SetParent(parent, false);
            var rt = barObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(250f, 24f);

            var bgImg = barObj.AddComponent<Image>();
            bgImg.color = new Color(0.1f, 0.12f, 0.15f, 0.85f);

            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(barObj.transform, false);
            var fillAreaRt = fillArea.AddComponent<RectTransform>();
            fillAreaRt.anchorMin = Vector2.zero;
            fillAreaRt.anchorMax = Vector2.one;
            fillAreaRt.sizeDelta = Vector2.zero;

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillRt = fill.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.sizeDelta = Vector2.zero;
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = fillColor;

            var slider = barObj.AddComponent<Slider>();
            slider.fillRect = fillRt;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f; // 37°C / normal

            // Numeric Label overlay
            GameObject labelObj = new GameObject("TempLabel");
            labelObj.transform.SetParent(barObj.transform, false);
            var lRt = labelObj.AddComponent<RectTransform>();
            lRt.anchorMin = Vector2.zero;
            lRt.anchorMax = Vector2.one;
            lRt.sizeDelta = Vector2.zero;
            textLabel = labelObj.AddComponent<Text>();
            textLabel.font = font;
            textLabel.fontSize = 12;
            textLabel.fontStyle = FontStyle.Bold;
            textLabel.alignment = TextAnchor.MiddleCenter;
            textLabel.color = Color.white;
            textLabel.text = "Body: 37.0°C";

            return slider;
        }
    }
}
#endif
