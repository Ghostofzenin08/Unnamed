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
* **Perspective**: High-Angle Top-Down / Bird's-Eye View (56° pitch, smooth follow)
* **Movement Paradigm**: Rigidbody-driven physics locomotion (WASD) with continuous collision detection and plane-based mouse cursor targeting
* **Render Pipeline**: Built-In 3D with PBR-compatible stylized shaders
* **Input Compatibility**: Dual-support architecture (compatible with both the Unity Input System Package and Legacy Input Manager)

---

## 3. Phased Development Roadmap

- [x] **Phase 1: Project Setup & Core Controllers**
  - Project environment initialization & Git tracking
  - Rigidbody-based `PlayerController` (WASD movement, cursor facing rotation)
  - `TopDownCameraFollow` smooth-damp follow camera with high-angle orientation
  - Automated editor hierarchy generator (`Phase1SceneSetup`)
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

| Action | Key / Input | Description |
| :--- | :--- | :--- |
| **Move** | `W` `A` `S` `D` / Arrow Keys | Physics-based locomotion on world XZ plane |
| **Sprint** | `Left Shift` / `Right Shift` | 1.45x movement speed boost |
| **Aim / Face** | `Mouse Cursor` | Raycasts onto player ground plane for 360° facing |
| **Camera** | Automated | SmoothDamp follow with fixed isometric angle |
