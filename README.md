# Project Genesis: Outpost 404
**A 3D Top-Down Survival Prototype in Unity**

---

## 1. Game Concept & Narrative Architecture

### Storyline: The Crash Landing in an Overgrown Anomaly
You are **Dr. Nolan Cross**, an orbital biosphere engineer whose survey drop-pod sustained an electromagnetic atmospheric shear and crash-landed into **Outpost 404**—a terraforming research sector decommissioned and isolated fourteen years prior.

* **The Environment**: The containment bio-domes have breached. Hyper-oxygenated flora and mutant vegetation have swallowed the Brutalist concrete installations, intertwined with pulsating electromagnetic conduits known as *Pulse Veins*.
* **The Threat**: By daylight, the biome remains relatively calm, permitting resource harvesting and scouting. At twilight, the atmospheric anomaly flares into a high-radiation electromagnetic surge that agitates bioluminescent predators and territorial anomaly beasts.
* **Survival Objective**:
  1. Scavenge wreckage scrap, biomass, and crystalline conductors to maintain vitals (**Health**, **Stamina**, **Caloric Hunger**).
  2. Fortify the pod crash enclave.
  3. Purge anomalous bio-growths and reignite dormant atmospheric cleansing pylons.
  4. Construct a high-yield orbital distress beacon to signal evacuation.

---

## 2. Technical Specifications & Stack

* **Engine**: Unity 6 (6000.0.85f2)
* **Perspective**: High-Angle Top-Down / Bird's-Eye View (56° pitch, yaw orbit support, smooth follow)
* **Movement Paradigm**: Dynamic Rigidbody physics velocity with continuous collision detection, vertical gravity retention, and 3D world + ground-plane cursor targeting
* **Render Pipeline**: Built-In 3D with PBR-compatible stylized prototype materials
* **Input Compatibility**: Dual-support architecture (Unity Input System Keyboard & Gamepad + Legacy Input Manager fallback)
* **Verification Suite**: 10 automated EditMode tests covering locomotion physics, deadzone protection, input clamping, and camera orbital math

---

## 3. Phased Development Roadmap

- [x] **Phase 1: Project Setup & Core Controllers**
  - Project environment initialization & Git tracking
  - Dynamic Rigidbody `PlayerController` (WASD/Gamepad movement, 3D cursor facing rotation, deadzone protection)
  - `TopDownCameraFollow` smooth-damp follow camera with high-angle orientation and yaw-aware orbiting
  - Automated editor hierarchy generator (`Phase1SceneSetup`) with stylized prototype materials
  - 10 comprehensive automated unit tests (`Phase1ControllerTests`)
- [ ] **Phase 2: Asset Generation & Environment**
  - AI mesh/texture generation (Player suit, terrain textures)
  - Prototype level graybox layout & NavMesh baking
- [ ] **Phase 3: Survival Stats & UI**
  - Health, Stamina, Hunger decay & replenishment system
  - Screen-space UI HUD gauges with alert states
- [ ] **Phase 4: Resource Gathering & Inventory**
  - Harvestable nodes (Scrap piles, timber trees, conductive crystals)
  - Raycast/trigger interactions & ScriptableObject inventory
- [ ] **Phase 5: Enemy AI & Combat**
  - NavMeshAgent state machine (Patrol, Detect, Chase, Attack)
  - Player basic defensive attack mechanic & hit feedback

---

## 4. Phase 1 Controls

| Action | Key / Input | Gamepad | Description |
| :--- | :--- | :--- | :--- |
| **Move** | `W` `A` `S` `D` / Arrow Keys | `Left Stick` | Dynamic Rigidbody locomotion on world XZ plane |
| **Sprint** | `Left Shift` / `Right Shift` | `Left Stick Click` / `RT` | 1.45x movement speed boost |
| **Aim / Face** | `Mouse Cursor` | Cursor / Aim | 3D collider raycast with ground-plane fallback and deadzone |
| **Camera** | Automated | Automated | SmoothDamp follow with yaw-aware orbit and pitch framing |
