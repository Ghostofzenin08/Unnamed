#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Outpost404.Player;
using Outpost404.Survival;
using Outpost404.Combat;
using Outpost404.Inventory;
using Outpost404.Crafting;
using Outpost404.Building;
using Outpost404.UI;
using Outpost404.EditorTools;

namespace Outpost404.Tests
{
    public class Phase6SceneVerificationTests
    {
        [Test, Order(1)]
        public void Scene_BuildPhase6Scene_ExecutesCleanlyAndSaves()
        {
            Assert.DoesNotThrow(() =>
            {
                Phase6SceneSetup.BuildPhase6Scene();
            }, "Phase6SceneSetup.BuildPhase6Scene must execute without errors.");
        }

        [Test, Order(2)]
        public void Scene_Player_ConfiguredWith3DAstronautAndCraftingComponents()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            var player = GameObject.FindWithTag("Player");
            Assert.IsNotNull(player, "Player with tag 'Player' must exist in scene.");

            Transform vis = player.transform.Find("Visual_OrbitalAstronaut");
            Assert.IsNotNull(vis, "Player must have 'Visual_OrbitalAstronaut' 3D visual child.");
            Renderer[] renderers = vis.GetComponentsInChildren<Renderer>(true);
            Assert.Greater(renderers.Length, 0, "OrbitalAstronaut visual must contain active Renderers.");

            Assert.IsNotNull(player.GetComponent<PlayerController>(), "Player must have PlayerController.");
            Assert.IsNotNull(player.GetComponent<PlayerStats>(), "Player must have PlayerStats.");
            Assert.IsNotNull(player.GetComponent<PlayerCombat>(), "Player must have PlayerCombat.");
            Assert.IsNotNull(player.GetComponent<PlayerInteraction>(), "Player must have PlayerInteraction.");

            var inv = player.GetComponent<InventorySystem>();
            Assert.IsNotNull(inv, "Player must have InventorySystem.");
            Assert.Greater(inv.SlotCapacity, 0);

            var craftMgr = player.GetComponent<CraftingManager>();
            Assert.IsNotNull(craftMgr, "Player must have CraftingManager.");
            Assert.GreaterOrEqual(craftMgr.RegisteredRecipes.Count, 3, "CraftingManager must have at least 3 survival recipes registered.");

            var placer = player.GetComponent<StructurePlacer>();
            Assert.IsNotNull(placer, "Player must have StructurePlacer.");
            Assert.IsNotNull(placer.ValidHologramMaterial, "StructurePlacer must have valid hologram material assigned.");
            Assert.IsNotNull(placer.BlockedHologramMaterial, "StructurePlacer must have blocked hologram material assigned.");
        }

        [Test, Order(3)]
        public void Scene_Enemies_ConfiguredWithAlienPredatorBeastVisuals()
        {
            var enemies = GameObject.Find("Enemies");
            Assert.IsNotNull(enemies, "Enemies root must exist.");
            Assert.GreaterOrEqual(enemies.transform.childCount, 2, "At least 2 enemy prowlers must exist.");

            for (int i = 0; i < enemies.transform.childCount; i++)
            {
                Transform enemy = enemies.transform.GetChild(i);
                Transform vis = enemy.Find("Visual_AlienPredatorBeast");
                Assert.IsNotNull(vis, $"Enemy {enemy.name} must have 'Visual_AlienPredatorBeast' 3D child.");
                Renderer[] r = vis.GetComponentsInChildren<Renderer>(true);
                Assert.Greater(r.Length, 0, "AlienPredatorBeast visual must contain active Renderers.");
            }
        }

        [Test, Order(4)]
        public void Scene_EnvironmentNodes_ConfiguredWithCustom3DModels()
        {
            var env = GameObject.Find("Environment");
            Assert.IsNotNull(env, "Environment root must exist.");

            var tree = env.transform.Find("Tree_Alien_1");
            Assert.IsNotNull(tree, "Tree_Alien_1 must exist.");
            Transform treeVis = tree.Find("Visual_AlienTimberTree");
            Assert.IsNotNull(treeVis, "Tree must have 'Visual_AlienTimberTree' 3D child.");

            var rock = env.transform.Find("Rock_Pulse_1");
            Assert.IsNotNull(rock, "Rock_Pulse_1 must exist.");
            Transform rockVis = rock.Find("Visual_MineralBoulder");
            Assert.IsNotNull(rockVis, "Rock must have 'Visual_MineralBoulder' 3D child.");
        }

        [Test, Order(5)]
        public void Scene_HUDCanvas_ConfiguredWithCraftingTerminalUI()
        {
            var canvas = GameObject.Find("HUD_Canvas");
            Assert.IsNotNull(canvas, "HUD_Canvas must exist.");

            var craftUI = canvas.GetComponent<CraftingUI>();
            Assert.IsNotNull(craftUI, "HUD_Canvas must contain CraftingUI component.");

            Transform craftPanel = canvas.transform.Find("Crafting_Panel");
            Assert.IsNotNull(craftPanel, "Crafting_Panel window must exist under HUD_Canvas.");

            Transform header = craftPanel.Find("Header");
            Assert.IsNotNull(header, "Crafting_Panel must have Header.");

            Transform container = craftPanel.Find("RecipeContainer");
            Assert.IsNotNull(container, "Crafting_Panel must have RecipeContainer.");
        }
    }
}
#endif
