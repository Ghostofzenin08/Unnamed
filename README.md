# Project Genesis: Outpost 404
**A 3D Top-Down Survival Game in Unity 6**

---

## 1. Game Concept & Narrative Architecture

### Storyline: The Crash Landing in an Overgrown Anomaly
You are **Dr. Nolan Cross**, an orbital biosphere engineer whose survey drop-pod sustained an electromagnetic atmospheric shear and crash-landed into **Outpost 404**—a terraforming research sector decommissioned and isolated fourteen years prior.

* **The Environment**: The containment bio-domes have breached. Hyper-oxygenated flora and mutant vegetation have swallowed the Brutalist concrete installations, intertwined with pulsating electromagnetic conduits known as *Pulse Veins*.
* **The Threat**: By daylight, the biome remains relatively calm, permitting resource harvesting and scouting. At twilight, the atmospheric anomaly flares into a high-radiation electromagnetic surge that agitates bioluminescent predators and territorial anomaly beasts (*Anomaly Prowlers*).
* **Survival Objective**:
  1. Scavenge wreckage scrap, biomass, and crystalline conductors to maintain vitals (**Health**, **Stamina**, **Caloric Hunger**).
  2. Fortify the pod crash enclave.
  3. Purge anomalous bio-growths and defend against patrolling predators.
  4. Construct a high-yield orbital distress beacon to signal evacuation.

---

## 2. Technical Specifications & Architecture

* **Engine**: Unity 6 (6000.0.85f2)
* **Perspective**: High-Angle Top-Down / Bird's-Eye View (56° pitch, yaw orbit support, smooth follow)
* **Locomotion**: Dynamic Rigidbody physics velocity with continuous collision detection, vertical gravity retention, and 3D world + ground-plane cursor targeting
* **Navigation**: AI NavMesh navigation geometry with carved dynamic obstacles for harvestable trees and rocks
* **Combat & Damage**: Unified `IDamageable` interface shared across Player vitals, Enemy AI beasts, and Harvestable World Nodes
* **AI Architecture**: NavMeshAgent Finite State Machine (`Patrol` -> `Chase` -> `Attack` -> `Dead`) with line of sight and radius awareness
* **Storage & Data**: ScriptableObject-driven items (`ItemDataSO`), crafting recipes (`RecipeSO`), resource node yields (`ResourceNodeSO`), and enemy balance (`EnemyStatsSO`)
* **Base Building & Holograms**: Real-time holographic ghost placement system with clearance collision detection, rotation snapping, and defensive/environmental structures (`BuildableStructure`)
* **Custom 3D Art Pipeline**: Full 11-asset pipeline (Orbital Astronaut, Predator Beast, Alien Timber Tree, Mineral Boulder, Scrap Hull, Ration Packs, Shrub flora) imported via glTFast
* **Verification Suite**: 140 automated EditMode NUnit tests covering Locomotion, Camera, Survival Stats, Inventory, Harvesting, Combat, Enemy FSM, Crafting & Building, Day/Night & Environment, Audio System & Procedural Waveforms, Screen Shake & Combat Juice, and Scene Wiring with 100% pass rate in Unity Test Runner

---

## 3. Phased Development Roadmap

- [x] **Phase 1: Project Setup & Core Controllers**
  - Dynamic Rigidbody `PlayerController` (WASD/Gamepad movement, 3D cursor facing rotation, deadzone protection)
  - `TopDownCameraFollow` smooth-damp follow camera with high-angle orientation and yaw-aware orbiting
  - Verification suite: Locomotion physics, deadzone protection, camera destination math
- [x] **Phase 2: Asset Generation & Environment**
  - Standardized AI 3D asset generation prompts (`Assets/Documentation/AssetGenerationPrompts.md`)
  - 50m x 50m multi-biome arena: Crash Landing Pad, Overgrown Pulse Biome, Crystalline Mineral Zone
  - Perimeter containment barriers and NavMesh surface baking
- [x] **Phase 3: Survival Stats & UI**
  - `PlayerStats`: Health, Stamina, Hunger with depletion over time, sprint drain, starvation damage, well-fed regeneration
  - Screen-space `SurvivalHUD`: Health, Stamina, Hunger gauge sliders, critical stat color grading, starvation alerts
- [x] **Phase 4: Resource Gathering & Inventory**
  - ScriptableObjects: `ItemDataSO` (Composite Timber, Conductive Ore, Raw Meat, Bio-Rations, Titanium Scrap) and `ResourceNodeSO`
  - In-game harvestable nodes: `ResourceNode` (Alien Trees, Pulse Vein Rocks) with hit wobble feedback and loot drops
  - `LootPickup`: Floating/rotating world drops with auto-pickup triggers
  - `InventorySystem`: Slot-based storage with stack overflow handling and event decoupling
  - `PlayerInteraction`: Proximity looting and interaction handling
- [x] **Phase 5: Enemy AI & Combat**
  - `EnemyController`: NavMeshAgent FSM (`Patrol`, `Chase`, `Attack`, `Dead`) with attack cooldowns and drop loot
  - `PlayerCombat`: Forward melee swing attack with stamina cost, attack cooldown, and `IDamageable` sphere overlap detection
  - Complete integration in `SampleScene.unity` with active player, enemies, nodes, and UI
- [x] **Phase 6: Crafting, Base Building & 3D Asset Integration**
  - `RecipeSO` & `CraftingManager`: Multi-ingredient validation, inventory consumption, and yield generation
  - Core recipes: Reinforced Wood Barricade (durability barrier), Pulse Heat Beacon (AoE healing safe zone), Makeshift Tool Axe (2x harvesting multiplier)
  - `BuildableStructure` & `StructurePlacer`: Real-time holographic ghost placement, ground raycasting, collision clearance check, rotation snapping
  - `CraftingUI`: Screen-space terminal UI window toggled via `Tab` or `C`, live ingredient stock counters, and one-click construct triggers
  - Custom 3D Art Pipeline: Integrated all 11 staged assets into `SampleScene.unity` (Orbital Astronaut, Predator Beast, Timber Tree, Mineral Boulder, Scrap Hull, Bio-Rations, Timber Logs, Conductive Ore, Alien Meat, Organic Shrubs)
- [x] **Phase 7: Day/Night Cycle & Environmental Hazards**
  - `DayNightCycleManager`: Configurable planetary day length (120s), normalized time `[0.0, 1.0)`, 360° celestial sun rotation, diurnal color gradients, and ambient illumination
  - Diurnal Temperature & Hypothermia: Diurnal ambient temperature curves (Warm Day +24°C, Dusk +10°C, Sub-zero Night -18°C), body cooling in freezing air, hypothermia health drain (< 32°C), critical freezing damage (< 25°C), and movement speed penalties
  - Thermal Radiance: `BuildableStructure` Pulse Heat Beacons radiate +35°C thermal warmth in aura radius, rapidly restoring body temperature and halting frost damage
  - Night Anomaly Surge: `EnemyController` nocturnal aggression buff during Night phase (expanded detection 12m -> 20m, +20% chase speed, +25% attack damage), smoothly reverting on Dawn
  - HUD Clock, Weather & Thermometer: Digital military clock ("Day 1 | 14:00"), diurnal phase icon badge, ambient temperature readout, body temperature gauge slider, and active hypothermia warning banner
- [x] **Phase 8: Audio & Combat Juice (Audio System & Game Feel)**
  - `AudioManager`: Master, SFX, and Music channel buses with [0, 1] volume clamping and bus muting toggles
  - Procedural Dynamic Audio Synthesis: Zero-asset dynamic sound generation via `AudioClip.Create` (footsteps, melee whooshes, timber chop thuds, rock crystalline clinks, enemy fleshy hits, pickup chimes, crafting success fanfare, day wind breeze, night anomaly drone, hypothermia teeth chattering)
  - Screen Shake Trauma System: `TriggerShake(intensity, duration)` in `TopDownCameraFollow` with smooth damping decay, triggered on melee swings, target impacts, taking damage, and structure destruction
  - Melee Slash Arc: Visual sweeping arc ribbon effect on attack swings in `PlayerCombat`
  - Procedural Combat Particles: Dynamic particle bursts for wood splinters, rock sparks, and bioluminescent alien blood
  - Hit Feedback: Entity visual recoil displacement and material flash on taking damage
  - Automatic Level Opener & Build Settings: `ProjectSceneOpener` ensuring `SampleScene.unity` auto-loads on startup and is registered in `EditorBuildSettings`

---

## 4. Controls Guide

| Action | Key / Input | Gamepad | Description |
| :--- | :--- | :--- | :--- |
| **Move** | `W` `A` `S` `D` / Arrow Keys | `Left Stick` | Dynamic Rigidbody locomotion on world horizontal plane |
| **Sprint** | `Left Shift` / `Right Shift` | `Left Stick Click` / `RT` | Speed boost; consumes stamina and accelerates hunger depletion |
| **Aim / Face** | `Mouse Cursor` | Cursor / Aim | 3D collider raycast with ground-plane fallback and deadzone |
| **Melee Attack** | `Left Mouse Button` | `Button West` / `X` | Melee swing attack; consumes stamina, damages enemies and resource nodes |
| **Interact / Harvest** | `E` | `Button South` / `A` | Collect nearby dropped items and harvest/interact with resource nodes |
| **Quick Eat / Consume** | `Q` | `D-Pad Up` | Consumes available food/rations from inventory to restore hunger & health |
| **Fabricator / Crafting**| `Tab` / `C` | `Select` / `Touchpad` | Toggles the Field Crafting terminal window |
| **Rotate Hologram** | `R` / `Mouse Wheel` | `Shoulder Buttons` | Rotates the holographic build ghost by 45° |
| **Confirm Placement** | `Left Mouse Button` | `Button South` / `A` | Constructs the active structure if placement area is clear |
| **Cancel Placement** | `Right Mouse Button` / `Esc` | `Button East` / `B` | Cancels holographic placement mode and destroys ghost |
| **Camera Follow**| Automated | Automated | SmoothDamp follow with yaw-aware orbit and pitch framing |

---

## 5. Hierarchy & Component Setup

### Player (`GameObject: Player`)
* `Transform` (Position: 0, 0.05, -10)
* `CapsuleCollider` (Height: 2, Radius: 0.5)
* `Rigidbody` (Interpolate: On, CollisionDetection: Continuous, FreezeRotation: X & Z)
* `Visual_OrbitalAstronaut`: Instantiated 3D astronaut model with suit and helmet
* `PlayerController`: Locomotion & cursor aiming
* `PlayerStats`: Health (100), Stamina (100), Hunger (100), sprint check
* `PlayerCombat`: Attack Damage (25), Cooldown (0.45s), Stamina Cost (12), Range (1.6m), Tool multiplier support
* `PlayerInteraction`: Interact Radius (2.5m), Interact Key (E), Quick Eat Key (Q), Tool multiplier support
* `InventorySystem`: 16 slots with item stacking, food consumption, and equipment support
* `CraftingManager`: Recipe registry, ingredient deduction, output yielding
* `StructurePlacer`: Holographic ghost preview, distance checking, collision clearance, structure instantiation

### UI HUD (`GameObject: HUD_Canvas`)
* `Canvas` (Screen Space - Overlay)
* `CanvasScaler` (Scale With Screen Size, 1920x1080)
* `GraphicRaycaster`
* `SurvivalHUD`: Subscribes to `PlayerStats` events; drives Health, Stamina, Hunger sliders, starvation warning, and death banners
* `CraftingUI`: Drives `Crafting_Panel`, binds recipes to cards, triggers crafting and holographic placement
* `Crafting_Panel`: Header, container, scroll layout

### Harvestable Nodes (`GameObject: Tree_Alien_*` / `Rock_Pulse_*`)
* Visual 3D mesh (`Visual_AlienTimberTree` / `Visual_MineralBoulder`)
* `Collider` (Capsule / Box)
* `ResourceNode`: Implements `IDamageable`; wobble animation on hit; spawns drops on depletion
* `NavMeshObstacle`: Carving enabled to dynamically cut NavMesh

### Enemy Predator (`GameObject: Enemy_Prowler_*`)
* Visual 3D mesh (`Visual_AlienPredatorBeast`)
* `Collider` (Capsule)
* `NavMeshAgent`: Speed 2.6 - 4.8, acceleration 16, stopping distance 1.2
* `EnemyController`: Implements `IDamageable`; FSM transitions: Patrol, Chase, Attack, Die

### Buildable Structures (`Assets/Prefabs/Structures/`)
* `Prefab_Barricade`: `BuildableStructure` (150 Durability), `BoxCollider`, `NavMeshObstacle` (Carving: On)
* `Prefab_HeatBeacon`: `BuildableStructure` (120 Durability), 5.5m Aura Radius, 5 HP/s Regen, Point Light, `NavMeshObstacle`

### Audio & Game Feel (`GameObject: AudioManager`)
* `AudioManager`: Configured with `MusicSource` (looping ambient atmosphere) and `SFXSource` (combat & interaction feedback)
* Audio Channels: Master, SFX, and Music channel volume buses with muting controls
* Procedural waveforms: Synthesized real-time audio clips for footsteps, whooshes, timber chopping, mineral mining, beast hits, chimes, and hypothermia cues

### Opening the Complete Game Level in Unity
If the Unity Editor ever displays a blank new scene named `Untitled`:
1. Double-click **`Assets/Scenes/SampleScene.unity`** inside the **Project** window under `Assets/Scenes/` (or click top menu `Outpost 404 -> Open Game Scene (SampleScene)`).
2. The automated `ProjectSceneOpener` startup script also automatically detects blank `Untitled` scenes on launch and restores `SampleScene.unity` with all Phase 1-8 systems active.

---

## 6. Verification Suite

Run EditMode tests via Unity Test Runner or CLI:
```bash
unity cmd "test run"
```
* **Locomotion & Camera**: 10 tests
* **Survival Stats**: 12 tests
* **Inventory System**: 11 tests
* **Resource Harvesting**: 6 tests
* **Enemy AI & Combat**: 10 tests
* **Crafting & Base Building**: 24 tests
* **Day/Night Cycle & Environmental Hazards**: 23 tests
* **Staged 3D Asset Import Pipeline**: 1 test
* **Audio System & Procedural Waveforms**: 12 tests
* **Screen Shake & Combat Juice**: 9 tests
* **Scene Integration, Opener & Wiring**: 22 tests
* **Total**: 140 passed / 0 failed (100% pass rate)
