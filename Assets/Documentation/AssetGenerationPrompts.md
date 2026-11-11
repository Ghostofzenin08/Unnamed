# 3D Asset Generation Protocol & Prompt Library
**Project: Unnamed (Outpost 404 - 3D Top-Down Survival)**

This document defines the asset generation pipeline and standardized prompts for generating 3D models and textures using Unity's AI 3D Object Generator, Unity MCP, or generative 3D toolchains.

All prompts adhere strictly to the engineering specification:
`"Generate a [Object], optimized for Unity 3D. It should be low-poly, PBR compatible, with a [stylized/blocky/pixel] aesthetic suitable for a top-down camera angle."`

---

## 1. Player Character Assets

### Dr. Nolan Cross (Orbital Biosphere Engineer)
* **Prompt**:
  ```text
  Generate a Hazmat Orbital Engineer with survival backpack and tinted helmet visor, optimized for Unity 3D. It should be low-poly, PBR compatible, with a stylized aesthetic suitable for a top-down camera angle.
  ```
* **Art Direction**: High-contrast safety-orange hazard suit, cyan reflective visor emissive strip, modular oxygen/survival backpack. Silhouette clearly readable from a 56° top-down camera pitch.
* **Target Polygon Budget**: 1,800 – 3,200 triangles.
* **Texture Maps**: Albedo/BaseColor (512x512 or 1024x1024), Metallic-Smoothness mask, Normal map, Emission map for visor.

---

## 2. Enemy & Creature Assets

### Bioluminescent Anomaly Prowler (Territorial Quadruped)
* **Prompt**:
  ```text
  Generate an alien predator quadruped beast with bioluminescent pulse veins and spiked carapace, optimized for Unity 3D. It should be low-poly, PBR compatible, with a stylized aesthetic suitable for a top-down camera angle.
  ```
* **Art Direction**: Aggressive low-slung stalking stance, chitinous obsidian plates, pulsating neon purple/cyan vein accents, distinct head crest for direction clarity in top-down view.
* **Target Polygon Budget**: 1,500 – 2,800 triangles.
* **Texture Maps**: Albedo, Normal, Metallic-Smoothness, Pulsing Emission mask.

---

## 3. Harvestable Environment Nodes

### Alien Timber Canopy Tree
* **Prompt**:
  ```text
  Generate a clustered sci-fi alien timber tree with thick twisted trunk and broad flat foliage canopy, optimized for Unity 3D. It should be low-poly, PBR compatible, with a stylized aesthetic suitable for a top-down camera angle.
  ```
* **Art Direction**: Broad horizontal umbrella crown for readability from bird's-eye view; sturdy trunk; warm amber/teal foliage facets.
* **Target Polygon Budget**: 600 – 1,200 triangles.

### Pulse Vein Crystalline Rock
* **Prompt**:
  ```text
  Generate an angular mineral boulder embedded with glowing crystalline energy veins, optimized for Unity 3D. It should be low-poly, PBR compatible, with a stylized aesthetic suitable for a top-down camera angle.
  ```
* **Art Direction**: Sharp chiseled geometric facets, dark basalt stone with embedded emissive cyan crystalline struts.
* **Target Polygon Budget**: 400 – 800 triangles.

### Biomass Anomaly Bush / Flora
* **Prompt**:
  ```text
  Generate an alien organic shrub with bulbous bioluminescent spore pods, optimized for Unity 3D. It should be low-poly, PBR compatible, with a stylized aesthetic suitable for a top-down camera angle.
  ```
* **Art Direction**: Clustered organic globes, low-profile ground spread, toxic lime-green glow.
* **Target Polygon Budget**: 300 – 600 triangles.

---

## 4. World Loot & Dropped Items

### Composite Timber Log
* **Prompt**:
  ```text
  Generate a bundle of harvested alien timber logs tied with synthetic strap, optimized for Unity 3D. It should be low-poly, PBR compatible, with a stylized aesthetic suitable for a top-down camera angle.
  ```

### Conductive Ore Chunk
* **Prompt**:
  ```text
  Generate an angular chunk of crystalline conductive ore, optimized for Unity 3D. It should be low-poly, PBR compatible, with a stylized aesthetic suitable for a top-down camera angle.
  ```

### Organic Raw Meat Slab
* **Prompt**:
  ```text
  Generate a slab of alien anomaly beast meat with bioluminescent streaks, optimized for Unity 3D. It should be low-poly, PBR compatible, with a stylized aesthetic suitable for a top-down camera angle.
  ```

### Scavenged Titanium Scrap
* **Prompt**:
  ```text
  Generate a pile of salvaged metallic hull plating and twisted mechanical gears, optimized for Unity 3D. It should be low-poly, PBR compatible, with a stylized aesthetic suitable for a top-down camera angle.
  ```

### Emergency Bio-Ration Pack
* **Prompt**:
  ```text
  Generate a sealed vacuum-packed orbital emergency ration container with survival insignia, optimized for Unity 3D. It should be low-poly, PBR compatible, with a stylized aesthetic suitable for a top-down camera angle.
  ```

---

## 5. Environment & Level Graybox Specifications

* **World Boundary Walls**: 3.5m height modular perimeter barriers preventing falloff.
* **Crash Landing Zone**: Debris-strewn metallic/concrete pad with smoking drop-pod pod chassis.
* **Overgrowth Zone**: Heavy organic coverage, harvestable trees and spore clusters.
* **Mineral Zone**: Dark volcanic rock plate with concentrated crystalline veins.
* **NavMesh Compatibility**: All ground terrain marked Static; obstacles flagged with NavMeshObstacle or baked into navigation geometry with 45° max slope and 0.5m agent radius.
