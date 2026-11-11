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
* **Storage & Data**: ScriptableObject-driven items (`ItemDataSO`), resource node yields (`ResourceNodeSO`), and enemy balance (`EnemyStatsSO`)
* **Verification Suite**: 41 automated EditMode NUnit tests (Locomotion, Camera, Survival Stats, Inventory, Harvesting, Combat, Enemy FSM) with 100% pass rate in Unity Test Runner

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

---

## 4. Controls Guide

| Action | Key / Input | Gamepad | Description |
| :--- | :--- | :--- | :--- |
| **Move** | `W` `A` `S` `D` / Arrow Keys | `Left Stick` | Dynamic Rigidbody locomotion on world horizontal plane |
| **Sprint** | `Left Shift` / `Right Shift` | `Left Stick Click` / `RT` | Speed boost; consumes stamina and accelerates hunger depletion |
| **Aim / Face** | `Mouse Cursor` | Cursor / Aim | 3D collider raycast with ground-plane fallback and deadzone |
| **Melee Attack** | `Left Mouse Button` | `Button West` / `X` | Melee swing attack; consumes stamina, damages enemies and resource nodes |
| **Interact / Loot** | `E` | `Button South` / `A` | Collect nearby dropped items and interact with objects |
| **Camera Follow**| Automated | Automated | SmoothDamp follow with yaw-aware orbit and pitch framing |

---

## 5. Hierarchy & Component Setup

### Player (`GameObject: Player`)
* `Transform` (Position: 0, 1, -10)
* `CapsuleCollider` (Height: 2, Radius: 0.5)
* `Rigidbody` (Interpolate: On, CollisionDetection: Continuous, FreezeRotation: X & Z)
* `PlayerController`: Locomotion & cursor aiming
* `PlayerStats`: Health (100), Stamina (100), Hunger (100), sprint check
* `PlayerCombat`: Attack Damage (25), Cooldown (0.45s), Stamina Cost (12), Range (1.6m)
* `PlayerInteraction`: Interact Radius (2.5m), Interact Key (E)
* `InventorySystem`: 16 slots with item stacking

### UI HUD (`GameObject: HUD_Canvas`)
* `Canvas` (Screen Space - Overlay)
* `CanvasScaler` (Scale With Screen Size, 1920x1080)
* `GraphicRaycaster`
* `SurvivalHUD`: Subscribes to `PlayerStats` events; drives Health, Stamina, and Hunger sliders

### Harvestable Nodes (`GameObject: Tree_Alien_*` / `Rock_Pulse_*`)
* Visual mesh (Trunk & Canopy / Basalt rock body)
* `Collider` (Capsule / Box)
* `ResourceNode`: Implements `IDamageable`; wobble animation on hit; spawns drops on depletion
* `NavMeshObstacle`: Carving enabled to dynamically cut NavMesh

### Enemy Predator (`GameObject: Enemy_Prowler_*`)
* Visual mesh (Capsule with head crest)
* `Collider` (Capsule)
* `NavMeshAgent`: Speed 2.6 - 4.8, acceleration 16, stopping distance 1.2
* `EnemyController`: Implements `IDamageable`; FSM transitions: Patrol, Chase, Attack, Die

---

## 6. Verification Suite

Run EditMode tests via Unity Test Runner or CLI:
```bash
unity cmd "test run"
```
* **Locomotion & Camera**: 10 tests
* **Survival Stats**: 11 tests
* **Inventory System**: 8 tests
* **Resource Harvesting**: 4 tests
* **Enemy AI & Combat**: 8 tests
* **Total**: 41 passed / 0 failed (100% pass rate)
